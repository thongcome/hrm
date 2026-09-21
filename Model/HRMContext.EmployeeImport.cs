using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HRMContext
{
    public virtual DbSet<Hr_EmployeeImportBatch> Hr_EmployeeImportBatches { get; set; }
}
