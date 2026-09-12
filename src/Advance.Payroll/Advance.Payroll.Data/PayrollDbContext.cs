using Advance.Payroll.Domain;
using Microsoft.EntityFrameworkCore;

namespace Advance.Payroll.Data;

// Ported from HRM Model/HRMContext.cs (DbSet declarations, lines 354-436/548-550) and
// Model/HRMContext.Payroll.cs (Pay_PayrollRunHold/Pay_AttendanceDeductionPolicy/
// Pay_SalaryAdvance/Pay_BankFileFormat DbSets + the two trigger declarations), combined
// into one context because Advance.Payroll owns its own database now instead of sharing
// HRMContext's single DbContext with 24 other modules.
//
// Every `.HasOne(d => d.Hremployee)` FK config from the HRM original is DELIBERATELY
// OMITTED here (see TODO(seam) comments) — Advance.Payroll.Domain's entities kept their
// scalar HremployeeId FK column but dropped the Hremployee navigation property (that
// entity isn't copied into this solution; see EXTRACTION-PLAN.md's pay_employee design).
// EF will map HremployeeId as a plain long column with no FK constraint, which is
// correct: pay_employee is a separate sync target IEmployeeSource populates, not
// something PayrollDbContext can declare a real FK against.
public class PayrollDbContext : DbContext
{
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options) : base(options)
    {
    }

    // ----- Pay_* module -----
    public virtual DbSet<Pay_PayrollRun> Pay_PayrollRuns { get; set; } = null!;
    public virtual DbSet<Pay_PayrollEmployee> Pay_PayrollEmployees { get; set; } = null!;
    public virtual DbSet<Pay_PayrollLineItem> Pay_PayrollLineItems { get; set; } = null!;
    public virtual DbSet<Pay_PayItemType> Pay_PayItemTypes { get; set; } = null!;
    public virtual DbSet<Pay_TaxBracket> Pay_TaxBrackets { get; set; } = null!;
    public virtual DbSet<Pay_TaxDeductionSetting> Pay_TaxDeductionSettings { get; set; } = null!;
    public virtual DbSet<Pay_TaxDeductionType> Pay_TaxDeductionTypes { get; set; } = null!;
    public virtual DbSet<Pay_EmployeeTaxDeductionElection> Pay_EmployeeTaxDeductionElections { get; set; } = null!;
    public virtual DbSet<Pay_EmployeePriorEmployerIncome> Pay_EmployeePriorEmployerIncomes { get; set; } = null!;
    public virtual DbSet<Pay_PaySchedule> Pay_PaySchedules { get; set; } = null!;
    public virtual DbSet<Pay_EmployeePayScheduleOverride> Pay_EmployeePayScheduleOverrides { get; set; } = null!;
    public virtual DbSet<Pay_PayScheduleChangeLog> Pay_PayScheduleChangeLogs { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundElection> Pay_ProvidentFundElections { get; set; } = null!;
    public virtual DbSet<Pay_PayrollAuditLog> Pay_PayrollAuditLogs { get; set; } = null!;
    public virtual DbSet<Pay_PayrollAnomaly> Pay_PayrollAnomalies { get; set; } = null!;
    public virtual DbSet<Pay_Payslip> Pay_Payslips { get; set; } = null!;
    public virtual DbSet<Pay_BankFileExportBatch> Pay_BankFileExportBatches { get; set; } = null!;
    public virtual DbSet<Pay_BankFileExportLine> Pay_BankFileExportLines { get; set; } = null!;
    public virtual DbSet<Pay_BankFileFormat> Pay_BankFileFormats { get; set; } = null!;
    public virtual DbSet<Pay_GLExportBatch> Pay_GLExportBatches { get; set; } = null!;
    public virtual DbSet<Pay_GLExportEntry> Pay_GLExportEntries { get; set; } = null!;
    public virtual DbSet<Pay_GLAccountMapping> Pay_GLAccountMappings { get; set; } = null!;
    public virtual DbSet<Pay_AdhocPayItem> Pay_AdhocPayItems { get; set; } = null!;
    public virtual DbSet<Pay_PositionSalaryHistory> Pay_PositionSalaryHistories { get; set; } = null!;
    public virtual DbSet<Pay_SalaryGrade> Pay_SalaryGrades { get; set; } = null!;
    public virtual DbSet<Pay_EmployeeLoan> Pay_EmployeeLoans { get; set; } = null!;
    public virtual DbSet<Pay_EmployeeLoanInstallment> Pay_EmployeeLoanInstallments { get; set; } = null!;
    public virtual DbSet<Pay_InsurancePlan> Pay_InsurancePlans { get; set; } = null!;
    public virtual DbSet<Pay_EmployeeInsuranceEnrollment> Pay_EmployeeInsuranceEnrollments { get; set; } = null!;
    public virtual DbSet<Pay_WelfareFundPolicy> Pay_WelfareFundPolicies { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundPolicy> Pay_ProvidentFundPolicies { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundVestingTier> Pay_ProvidentFundVestingTiers { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundInvestmentPolicy> Pay_ProvidentFundInvestmentPolicies { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundRateChangeWindow> Pay_ProvidentFundRateChangeWindows { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundRateMatrixRule> Pay_ProvidentFundRateMatrixRules { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundExitReasonRule> Pay_ProvidentFundExitReasonRules { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundMembershipPeriod> Pay_ProvidentFundMembershipPeriods { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundRateChangeRequest> Pay_ProvidentFundRateChangeRequests { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundExitCase> Pay_ProvidentFundExitCases { get; set; } = null!;
    public virtual DbSet<Pay_ProvidentFundCalculationDetail> Pay_ProvidentFundCalculationDetails { get; set; } = null!;
    public virtual DbSet<Pay_PayslipSettings> Pay_PayslipSettings { get; set; } = null!;
    public virtual DbSet<Pay_PayrollPeriod> Pay_PayrollPeriods { get; set; } = null!;
    public virtual DbSet<Pay_EmployeeDocument> Pay_EmployeeDocuments { get; set; } = null!;

    // Wave 1 HR gaps (2026-09-07 in HRM) — attendance-based deductions + salary advances.
    public virtual DbSet<Pay_AttendanceDeductionPolicy> Pay_AttendanceDeductionPolicies { get; set; } = null!;
    public virtual DbSet<Pay_SalaryAdvance> Pay_SalaryAdvances { get; set; } = null!;

    // Phase B: per-run employee holds (skipped by the calculation engine).
    public virtual DbSet<Pay_PayrollRunHold> Pay_PayrollRunHolds { get; set; } = null!;

    // Social-security rate table — HRUCFSECURITY, same physical table HRM reads.
    public virtual DbSet<Hrucfsecurity> Hrucfsecuritys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── #2 Immutable posted runs (HRMContext.Payroll.cs:27-28) ──────────────────
        // trg_PayrollEmployee_Immutable / trg_PayrollLineItem_Immutable are SQL Server
        // AFTER UPDATE/DELETE triggers (see Migrations/Manual/PayrollPhaseABAndPayElementCatalog)
        // that block edits to a posted run's calculated rows. Declaring them here makes
        // EF's SQL Server provider fall back to the trigger-compatible save strategy
        // instead of an OUTPUT clause SQL Server rejects on triggered tables.
        // SQL-SERVER-SPECIFIC — see EXTRACTION-PLAN.md "DB triggers" section for the
        // Postgres-portability flag on this.
        modelBuilder.Entity<Pay_PayrollEmployee>().ToTable(tb => tb.HasTrigger("trg_PayrollEmployee_Immutable"));
        modelBuilder.Entity<Pay_PayrollLineItem>().ToTable(tb => tb.HasTrigger("trg_PayrollLineItem_Immutable"));

        // ── Pay_PayrollRun (HRMContext.cs:1507-1520) ─────────────────────────────────
        modelBuilder.Entity<Pay_PayrollRun>(entity =>
        {
            entity.HasOne(d => d.AdjustmentOfRun)
                .WithMany()
                .HasForeignKey(d => d.AdjustmentOfRunId)
                .OnDelete(DeleteBehavior.Restrict);
            // หนึ่งรอบต่อ (บริษัท, งวด, งวดย่อย, ชนิด) เฉพาะรอบที่ยังไม่ยกเลิก
            entity.HasIndex(r => new { r.CompanyId, r.PayrollPeriod, r.TermNo, r.RunType })
                .IsUnique()
                .HasDatabaseName("IX_Pay_PayrollRun_CompanyId_PayrollPeriod_RunType")
                .HasFilter("[Status] <> 9");
        });

        // ── Pay_PayrollEmployee (HRMContext.cs:1522-1533) ────────────────────────────
        modelBuilder.Entity<Pay_PayrollEmployee>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany(p => p.Pay_PayrollEmployees)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict —
            // no FK here; HremployeeId now only resolves through IEmployeeSource/pay_employee.
        });

        // ── Pay_PayrollLineItem (HRMContext.cs:1535-1546) ────────────────────────────
        modelBuilder.Entity<Pay_PayrollLineItem>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithMany(p => p.Pay_PayrollLineItems)
                .HasForeignKey(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Pay_PayItemType)
                .WithMany(p => p.Pay_PayrollLineItems)
                .HasForeignKey(d => d.PayItemTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Pay_ProvidentFundElection (HRMContext.cs:1560-1571) ──────────────────────
        modelBuilder.Entity<Pay_ProvidentFundElection>(entity =>
        {
            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict.
            entity.HasOne(d => d.InvestmentPolicy)
                .WithMany()
                .HasForeignKey(d => d.InvestmentPolicyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_ProvidentFundVestingTier>(entity =>
        {
            entity.HasOne(d => d.Policy)
                .WithMany(p => p.VestingTiers)
                .HasForeignKey(d => d.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Explicit FK — DeductionTypeId doesn't match EF's naming convention for the
        // Pay_TaxDeductionType navigation (HRMContext.cs:1587-1598).
        modelBuilder.Entity<Pay_EmployeeTaxDeductionElection>(entity =>
        {
            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict.
            entity.HasOne(d => d.Pay_TaxDeductionType)
                .WithMany(p => p.Elections)
                .HasForeignKey(d => d.DeductionTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Pay_EmployeePriorEmployerIncome (HRMContext.cs:1600-1606):
        // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict — no FK config needed here now.

        modelBuilder.Entity<Pay_PaySchedule>(entity =>
        {
            entity.HasIndex(e => new { e.CompanyId, e.EffectiveFrom });
        });

        modelBuilder.Entity<Pay_EmployeePayScheduleOverride>(entity =>
        {
            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict.
            entity.HasOne(d => d.PaySchedule).WithMany().HasForeignKey(d => d.PayScheduleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.HremployeeId);
        });

        modelBuilder.Entity<Pay_ProvidentFundRateChangeWindow>(entity =>
        {
            entity.HasOne(d => d.Policy).WithMany().HasForeignKey(d => d.PolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_ProvidentFundRateMatrixRule>(entity =>
        {
            entity.HasOne(d => d.Policy).WithMany().HasForeignKey(d => d.PolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_ProvidentFundExitReasonRule>(entity =>
        {
            entity.HasOne(d => d.Policy).WithMany().HasForeignKey(d => d.PolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Pay_ProvidentFundMembershipPeriod (HRMContext.cs:1640-1643):
        // TODO(seam): original configured HasOne(d => d.Hremployee) with Restrict — no FK config needed here now.

        modelBuilder.Entity<Pay_ProvidentFundRateChangeRequest>(entity =>
        {
            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict.
            entity.HasOne(d => d.Policy).WithMany().HasForeignKey(d => d.PolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_ProvidentFundExitCase>(entity =>
        {
            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict.
            entity.HasOne(d => d.Policy).WithMany().HasForeignKey(d => d.PolicyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.ExitReasonRule).WithMany().HasForeignKey(d => d.ExitReasonRuleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_ProvidentFundCalculationDetail>(entity =>
        {
            entity.HasOne(d => d.RateChangeRequest).WithMany().HasForeignKey(d => d.RateChangeRequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.ExitCase).WithMany().HasForeignKey(d => d.ExitCaseId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Pay_PayrollAuditLog / Pay_PayrollAnomaly: secondary Restrict path to avoid
        // SQL Server's "multiple cascade paths" (Run already cascades to both tables
        // directly) — HRMContext.cs:1664-1692 ────────────────────────────────────────
        modelBuilder.Entity<Pay_PayrollAuditLog>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany(p => p.Pay_PayrollAuditLogs)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithMany(p => p.Pay_PayrollAuditLogs)
                .HasForeignKey(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_PayrollAnomaly>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany()
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithMany()
                .HasForeignKey(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_Payslip>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithOne(p => p.Pay_Payslip)
                .HasForeignKey<Pay_Payslip>(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_BankFileExportBatch>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany(p => p.Pay_BankFileExportBatches)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_BankFileExportLine>(entity =>
        {
            entity.HasOne(d => d.Pay_BankFileExportBatch)
                .WithMany(p => p.Pay_BankFileExportLines)
                .HasForeignKey(d => d.BankFileExportBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Pay_PayrollEmployee)
                .WithMany(p => p.Pay_BankFileExportLines)
                .HasForeignKey(d => d.PayrollEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_GLExportBatch>(entity =>
        {
            entity.HasOne(d => d.Pay_PayrollRun)
                .WithMany(p => p.Pay_GLExportBatches)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_GLExportEntry>(entity =>
        {
            entity.HasOne(d => d.Pay_GLExportBatch)
                .WithMany(p => p.Pay_GLExportEntries)
                .HasForeignKey(d => d.GLExportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pay_AdhocPayItem>(entity =>
        {
            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict.
            entity.HasOne(d => d.Pay_PayItemType)
                .WithMany()
                .HasForeignKey(d => d.PayItemTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ConsumedByPayrollRun)
                .WithMany()
                .HasForeignKey(d => d.ConsumedByPayrollRunId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Pay_PositionSalaryHistory (HRMContext.cs:1793-1799):
        // TODO(seam): original configured HasOne(d => d.Hremployee) with Restrict — no FK config needed here now.

        modelBuilder.Entity<Pay_EmployeeLoan>(entity =>
        {
            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict.
        });

        modelBuilder.Entity<Pay_EmployeeLoanInstallment>(entity =>
        {
            entity.HasOne(d => d.Pay_EmployeeLoan)
                .WithMany(p => p.Pay_EmployeeLoanInstallments)
                .HasForeignKey(d => d.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ConsumedByPayrollRun)
                .WithMany()
                .HasForeignKey(d => d.ConsumedByPayrollRunId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_EmployeeInsuranceEnrollment>(entity =>
        {
            // TODO(seam): original also configured HasOne(d => d.Hremployee) with Restrict.
            entity.HasOne(d => d.Pay_InsurancePlan)
                .WithMany(p => p.Pay_EmployeeInsuranceEnrollments)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pay_PayslipSettings>().HasData(
            new Pay_PayslipSettings { Id = 1, CompanyId = "001", PasswordTemplate = "{BirthDateDDMMYYYY}", ModifiedDate = new DateTime(2026, 7, 29) }
        );

        // Pay_EmployeeDocument (HRMContext.cs:1904-1910):
        // TODO(seam): original configured HasOne(d => d.Hremployee) with Restrict — no FK config needed here now.

        // Built-in pay item types — verbatim from HRMContext.cs:1743-1755 (same Ids, so a
        // company migrating from HRM to standalone Advance.Payroll keeps the same catalog).
        modelBuilder.Entity<Pay_PayItemType>().HasData(
            new Pay_PayItemType { Id = 1, Code = "BASE", NameTh = "เงินเดือนฐาน", NameEn = "Base Salary", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = true, IsActive = true, SortOrder = 1, GLAccountCode = "5000-SALARY", IsTaxable = true, IsSsoWageBase = true, IsProrated = true },
            new Pay_PayItemType { Id = 2, Code = "OT", NameTh = "ค่าล่วงเวลา", NameEn = "Overtime", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = true, IsActive = true, SortOrder = 2, GLAccountCode = "5010-OT", IsTaxable = true, IsSsoWageBase = false, IsProrated = false },
            new Pay_PayItemType { Id = 3, Code = "ALLOWANCE", NameTh = "เบี้ยเลี้ยง/เงินเพิ่มประจำ", NameEn = "Allowance", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = false, IsActive = true, SortOrder = 3, GLAccountCode = "5020-ALLOWANCE", IsTaxable = true, IsSsoWageBase = true, IsProrated = false },
            new Pay_PayItemType { Id = 4, Code = "SSO", NameTh = "ประกันสังคม", NameEn = "Social Security", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = true, IsActive = true, SortOrder = 4, GLAccountCode = "2200-SSO-PAYABLE", IsTaxable = false, IsSsoWageBase = false, IsProrated = false },
            new Pay_PayItemType { Id = 5, Code = "PF", NameTh = "กองทุนสำรองเลี้ยงชีพ (พนักงาน)", NameEn = "Provident Fund (Employee)", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = true, IsActive = true, SortOrder = 5, GLAccountCode = "2210-PF-PAYABLE", IsTaxable = false, IsSsoWageBase = false, IsProrated = false },
            new Pay_PayItemType { Id = 6, Code = "TAX", NameTh = "ภาษีหัก ณ ที่จ่าย", NameEn = "Withholding Tax", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = true, IsActive = true, SortOrder = 6, GLAccountCode = "2220-WHT-PAYABLE", IsTaxable = false, IsSsoWageBase = false, IsProrated = false },
            new Pay_PayItemType { Id = 7, Code = "LOAN", NameTh = "หักเงินกู้", NameEn = "Loan Deduction", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = true, IsActive = true, SortOrder = 7, GLAccountCode = "1300-LOAN-RECEIVABLE", IsTaxable = false, IsSsoWageBase = false, IsProrated = false },
            new Pay_PayItemType { Id = 8, Code = "ADJUST", NameTh = "ปรับปรุงพิเศษ", NameEn = "Special Adjustment", Category = PayItemCategory.Informational, DefaultSignFlag = 1, IsSystemReserved = false, IsActive = true, SortOrder = 8, GLAccountCode = "5090-ADJUSTMENT", IsTaxable = false, IsSsoWageBase = false, IsProrated = false },
            new Pay_PayItemType { Id = 9, Code = "BONUS", NameTh = "โบนัส/ค่าคอมมิชชั่นเฉพาะกิจ", NameEn = "Bonus / Commission (ad-hoc)", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = false, IsActive = true, SortOrder = 9, GLAccountCode = "5030-BONUS", IsTaxable = true, IsSsoWageBase = false, IsProrated = false },
            new Pay_PayItemType { Id = 10, Code = "ADHOC_DEDUCT", NameTh = "หักเฉพาะกิจ (เช่น ค่าเสียหาย/ชุดยูนิฟอร์ม)", NameEn = "Ad-hoc Deduction", Category = PayItemCategory.Deduction, DefaultSignFlag = -1, IsSystemReserved = false, IsActive = true, SortOrder = 10, GLAccountCode = "2230-ADHOC-PAYABLE", IsTaxable = false, IsSsoWageBase = false, IsProrated = false },
            new Pay_PayItemType { Id = 11, Code = "SEVERANCE", NameTh = "ค่าชดเชยตามกฎหมาย", NameEn = "Statutory Severance Pay", Category = PayItemCategory.Earning, DefaultSignFlag = 1, IsSystemReserved = true, IsActive = true, SortOrder = 11, GLAccountCode = "5040-SEVERANCE", IsTaxable = true, IsSsoWageBase = false, IsProrated = false }
        );

        // Standard Thai personal-income-tax brackets, effective year 2026 (ค.ศ.) — verbatim
        // from HRMContext.cs:1761-1770. Confirm against the current Revenue Department
        // schedule before relying on this for a real payroll run.
        modelBuilder.Entity<Pay_TaxBracket>().HasData(
            new Pay_TaxBracket { Id = 1, EffectiveYear = 2026, Step = 1, MinIncome = 0m, MaxIncome = 150000m, RatePercent = 0m, IsActive = true },
            new Pay_TaxBracket { Id = 2, EffectiveYear = 2026, Step = 2, MinIncome = 150000m, MaxIncome = 300000m, RatePercent = 5m, IsActive = true },
            new Pay_TaxBracket { Id = 3, EffectiveYear = 2026, Step = 3, MinIncome = 300000m, MaxIncome = 500000m, RatePercent = 10m, IsActive = true },
            new Pay_TaxBracket { Id = 4, EffectiveYear = 2026, Step = 4, MinIncome = 500000m, MaxIncome = 750000m, RatePercent = 15m, IsActive = true },
            new Pay_TaxBracket { Id = 5, EffectiveYear = 2026, Step = 5, MinIncome = 750000m, MaxIncome = 1000000m, RatePercent = 20m, IsActive = true },
            new Pay_TaxBracket { Id = 6, EffectiveYear = 2026, Step = 6, MinIncome = 1000000m, MaxIncome = 2000000m, RatePercent = 25m, IsActive = true },
            new Pay_TaxBracket { Id = 7, EffectiveYear = 2026, Step = 7, MinIncome = 2000000m, MaxIncome = 5000000m, RatePercent = 30m, IsActive = true },
            new Pay_TaxBracket { Id = 8, EffectiveYear = 2026, Step = 8, MinIncome = 5000000m, MaxIncome = null, RatePercent = 35m, IsActive = true }
        );

        modelBuilder.Entity<Pay_PayrollPeriod>().HasData(
            new Pay_PayrollPeriod { Id = 1, CompanyId = "001", Year = 2026, Month = 7, TermNo = 1, Label = "ก.ค. 2569 งวดที่ 1", PeriodStart = new DateOnly(2026, 7, 1), PeriodEnd = new DateOnly(2026, 7, 31), IsActive = true },
            new Pay_PayrollPeriod { Id = 2, CompanyId = "001", Year = 2026, Month = 8, TermNo = 1, Label = "ส.ค. 2569 งวดที่ 1", PeriodStart = new DateOnly(2026, 8, 1), PeriodEnd = new DateOnly(2026, 8, 31), IsActive = true }
        );
    }
}
