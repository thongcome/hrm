using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Hr.EmployeeImport;

// Loads the company's own dropdown values (banks, positions, current org tree) and hands
// them to EmployeeImportTemplateBuilder, so the file a customer downloads already lists
// exactly the codes the importer will accept for that company.
public class EmployeeImportTemplateService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<byte[]> BuildAsync(string companyCode, CancellationToken ct = default)
    {
        var lookups = await LoadLookupsAsync(companyCode, ct);
        return EmployeeImportTemplateBuilder.Build(lookups);
    }

    public async Task<EmployeeImportLookups> LoadLookupsAsync(string companyCode, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var company = await context.com_companies.AsNoTracking()
            .Where(c => c.code == companyCode)
            .Select(c => new { c.id, c.name })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"ไม่พบบริษัท {companyCode}");

        var banks = await context.Com_Banks.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Code)
            .Select(b => new ImportLookupItem(b.Code, b.Name ?? ""))
            .ToListAsync(ct);

        var today = DateTime.Today;
        var positions = await context.pos_positions.AsNoTracking()
            .Where(p => p.expiredate == null || p.expiredate >= today)
            .OrderBy(p => p.pos_code)
            .Select(p => new ImportLookupItem(p.pos_code, p.name ?? ""))
            .ToListAsync(ct);

        // Only real departments of this company: active, coded, not the company node itself.
        var orgs = await context.com_organizations.AsNoTracking()
            .Where(o => o.companyid == company.id && o.isActive && o.code != null && !o.isCompany)
            .Select(o => new { o.id, o.code, o.name, o.parent_code, o.parentID, o.approver_empid, o.orgcodefull })
            .ToListAsync(ct);
        var codeById = orgs.ToDictionary(o => o.id, o => o.code!);
        var codes = orgs.Select(o => o.code!).ToHashSet();

        // A parent outside this list (the company node) means "directly under the company" = blank.
        var orgRows = orgs
            .OrderBy(o => o.orgcodefull ?? o.code)
            .Select(o =>
            {
                var parent = o.parentID is long pid && codeById.TryGetValue(pid, out var pc) ? pc : o.parent_code;
                return new ImportOrgRow(o.code!, o.name ?? o.code!,
                    parent is not null && codes.Contains(parent) ? parent : null, o.approver_empid);
            })
            .ToList();

        return new EmployeeImportLookups(companyCode, company.name ?? companyCode, banks, positions, orgRows);
    }
}
