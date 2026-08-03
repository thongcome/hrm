using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Shared;

// Single source of truth for "what is this employee's email" — extracted from
// PayrollEmployeeAdmin.razor's private ResolveEmployeeEmailAsync so every page
// that needs to email an employee (payslips, withholding cert, salary cert,
// POR.1, credential emails) resolves/writes the same CUR address row instead
// of re-deriving the lookup per page.
public static class EmployeeEmailResolver
{
    private const long CurrentAddressTypeId = 2; // mas_address_type code "CUR"

    // address (type CUR) is the source of truth since the address-table
    // migration; Hremployee.AdnEmail is the pre-migration flat field, kept
    // as a fallback only for records that predate that migration.
    public static async Task<string?> ResolveAsync(HRMContext context, long hremployeeId, CancellationToken ct = default)
    {
        var addr = await context.addresses
            .Where(a => a.hremployeeid == hremployeeId && a.address_type_id == CurrentAddressTypeId && a.isactive && a.email != null)
            .OrderByDescending(a => a.moddate ?? a.createdate)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(addr?.email)) return addr!.email;

        var emp = await context.Hremployee.FirstOrDefaultAsync(e => e.id == hremployeeId, ct);
        return emp?.AdnEmail;
    }

    // Find-or-create the active CUR address row and set its email. Calls
    // SaveChangesAsync itself — a standalone helper for callers that don't
    // already manage the CUR address row themselves.
    public static async Task SetAsync(HRMContext context, long hremployeeId, string email, CancellationToken ct = default)
    {
        var addr = await context.addresses
            .Where(a => a.hremployeeid == hremployeeId && a.address_type_id == CurrentAddressTypeId && a.isactive)
            .OrderByDescending(a => a.moddate ?? a.createdate)
            .FirstOrDefaultAsync(ct);

        if (addr is null)
        {
            addr = new address
            {
                hremployeeid = hremployeeId,
                address_type_id = CurrentAddressTypeId,
                createdate = DateTime.Now,
                isactive = true,
            };
            context.addresses.Add(addr);
        }
        else
        {
            addr.moddate = DateTime.Now;
        }

        addr.email = email;
        await context.SaveChangesAsync(ct);
    }

    // Shared by PayrollEmployeeAdmin.SaveEmployeeAsync and EssProfile.SaveAsync
    // for the "email is mandatory" format check — .NET's own MailAddress parser
    // is a pragmatic RFC-ish check; there's no existing email validator
    // elsewhere in this codebase to match against.
    public static bool IsValidFormat(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { _ = new System.Net.Mail.MailAddress(email); return true; }
        catch (FormatException) { return false; }
    }
}
