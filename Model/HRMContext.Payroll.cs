using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Payroll-module additions kept out of the scaffolded HRMContext.cs.
public partial class HRMContext
{
    // Phase B: per-run employee holds (skipped by the calculation engine).
    public virtual DbSet<Pay_PayrollRunHold> Pay_PayrollRunHolds { get; set; }

    // Called from OnModelCreatingPartial (HRMContext.Security.cs).
    private static void ConfigurePayrollModel(ModelBuilder modelBuilder)
    {
        // #2 Immutable posted runs: these tables carry AFTER UPDATE/DELETE triggers
        // (trg_*_Immutable, migration PayrollPhaseABAndPayElementCatalog). EF Core's
        // SQL Server provider uses an OUTPUT clause for SaveChanges, which SQL
        // Server rejects on tables with triggers ("cannot have any enabled triggers
        // if the statement contains an OUTPUT clause without INTO") — declaring the
        // trigger makes EF fall back to the trigger-compatible save strategy.
        modelBuilder.Entity<Pay_PayrollEmployee>().ToTable(tb => tb.HasTrigger("trg_PayrollEmployee_Immutable"));
        modelBuilder.Entity<Pay_PayrollLineItem>().ToTable(tb => tb.HasTrigger("trg_PayrollLineItem_Immutable"));
    }
}
