namespace HRM.Services.Hr;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Runtime seeder for the employee-document type master (CEO order 2026-08-31:
// personnel-profile attachments get a document type picked from a dropdown).
// Reuses the legacy mas_doc_type table (verified: real IDENTITY id, has
// code/name/name_en/isActive/orderTh — shape fits, so no new table and no EF
// migration). Idempotent add-missing-by-code: an existing row with the same
// code is NEVER touched (name edits, deactivation, reordering done by admins
// all survive restarts). group_ref = "EMPLOYEE_PROFILE_DOC" ties these rows
// to the doc_center.doctypecode family PersonnelProfileView writes, and is
// what the upload dropdown filters on.
//
// Wire-up (one line in Program.cs, next to the other startup seeders):
//   await HRM.Services.Hr.EmployeeDocTypeSeeder.SeedAsync(app.Services);
public static class EmployeeDocTypeSeeder
{
    public const string GroupRef = "EMPLOYEE_PROFILE_DOC";

    // (code, thai name, english name, sort order)
    private static readonly (string Code, string NameTh, string NameEn, int Order)[] StandardTypes =
    {
        ("EMP_CONTRACT",       "สัญญาจ้าง",             "Employment contract",                 1),
        ("EMP_CONTRACT_AMEND", "สัญญาแก้ไขเพิ่มเติม",     "Contract amendment",                  2),
        ("EMP_ID_CARD",        "บัตรประชาชน",           "National ID card",                    3),
        ("EMP_HOUSE_REG",      "ทะเบียนบ้าน",            "House registration",                  4),
        ("EMP_EDU_CERT",       "วุฒิการศึกษา",           "Education certificate",               5),
        ("EMP_MED_CERT",       "ใบรับรองแพทย์เข้างาน",    "Pre-employment medical certificate",  6),
        ("EMP_OTHER",          "อื่นๆ",                  "Other",                               99),
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HRMContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        // Existence check by code only (case-insensitive), ignoring isActive
        // and group_ref on purpose — a row an admin renamed, regrouped, or
        // deactivated is a decision, not a gap to refill.
        var existingCodes = (await context.mas_doc_types
                .Where(t => t.code != null)
                .Select(t => t.code!)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var (code, nameTh, nameEn, order) in StandardTypes)
        {
            if (existingCodes.Contains(code))
                continue;

            // mas_doc_type.id is a real IDENTITY column (verified via
            // sys.columns.is_identity = 1), so a plain Add is safe here — no
            // EntitySearchHelper.NextIdAsync needed for this table.
            context.mas_doc_types.Add(new mas_doc_type
            {
                code = code,
                name = nameTh,
                name_en = nameEn,
                group_ref = GroupRef,
                orderTh = order,
                isActive = true,
                moddate = DateTime.Now,
                modby = "startup-seed",
            });
            added = true;
        }

        if (added)
            await context.SaveChangesAsync();
    }
}
