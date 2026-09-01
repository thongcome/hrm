namespace HRM.Services.Org;

using HRM.Models;
using Microsoft.EntityFrameworkCore;

// CEO rule (1 ก.ย. 2569): the system runs with exactly ONE active company
// at a time ("บริษัท active ได้ 1") — org-chart pages always show the
// active company's tree (com_organization.comp_code == com_company.code).
// Multi-company later = configure comp_code per node; nothing else changes.
// CompanyAdmin enforces the single-active invariant on save; OrderBy(id)
// is only a deterministic tiebreak if legacy data ever violates it.
public static class ActiveCompanyHelper
{
    public static Task<com_company?> GetActiveAsync(HRMContext context) =>
        context.com_companies
            .Where(c => c.isActive == true)
            .OrderBy(c => c.id)
            .FirstOrDefaultAsync();
}
