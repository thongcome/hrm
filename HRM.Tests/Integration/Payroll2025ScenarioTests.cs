using System.Globalization;
using System.Text;
using HRM.Models;
using HRM.Services.Leave;
using HRM.Services.Pay;
using HRM.Services.Pay.Calculators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace HRM.Tests.Integration;

// ทดสอบเงินเดือนทั้งปี ม.ค.–ธ.ค. 2568 (CEO, 12 ก.ย. 2569: "test payroll เดือน มค ถึง ธค 68 ทดสอบให้ครบ")
//
// สร้างบริษัททดสอบแยกต่างหาก (PTEST = จ่ายเดือนละงวด, PTEST2 = จ่ายเดือนละ 2 งวด) ในฐานข้อมูล dev
// ล้างของเก่าทุกครั้งก่อนเริ่ม แล้วเดินรอบเงินเดือนจริงผ่าน service ตัวเดียวกับหน้าจอ:
// สร้างรอบ → คำนวณ → ส่งตรวจ → อนุมัติ (คนละคน) → บันทึกบัญชี → ไฟล์ธนาคาร → ไฟล์ GL → สลิป → จ่ายแล้ว
// ครบ 12 เดือน + รอบโบนัส + กลับรายการ/ปรับปรุง แล้วตรวจกับตัวเลขที่คำนวณอิสระ (oracle) ในไฟล์นี้
//
// ผลทุกข้อถูกเก็บเป็น "รายการตรวจ" แล้วเขียนรายงาน markdown ไว้ที่ payroll-2025-report.md ข้าง dll
// (หรือโฟลเดอร์ใน HRM_TEST_REPORT_DIR) — เทสตกเมื่อมีข้อใดข้อหนึ่งไม่ผ่าน แต่รายงานยังออกครบทุกข้อ
public class Payroll2025ScenarioTests(ITestOutputHelper output)
{
    private const string Co = "PTEST";      // จ่ายเดือนละงวด
    private const string Co2 = "PTEST2";    // จ่ายเดือนละ 2 งวด
    private const long Calc = 13;           // ผู้คำนวณ/ผู้ส่งตรวจ (admin)
    private const long Approver = 7034;     // ผู้อนุมัติ (advadmin) — ต้องคนละคนกับผู้คำนวณ
    private const int Year = 2025;
    private const decimal SsoRate = 5m, SsoCap = 15000m;

    private sealed record Check(string Area, string Name, bool Passed, string Detail);
    private readonly List<Check> _checks = new();
    private readonly List<string> _notes = new();
    private readonly HashSet<DateOnly> _holidays = new();

    private void Pass(string area, string name, string detail = "") => _checks.Add(new Check(area, name, true, detail));
    private void Fail(string area, string name, string detail) => _checks.Add(new Check(area, name, false, detail));
    private void Expect(string area, string name, bool ok, string detail) => _checks.Add(new Check(area, name, ok, detail));
    private void Near(string area, string name, decimal actual, decimal expected, decimal tol, string extra = "")
        => Expect(area, name, Math.Abs(actual - expected) <= tol, $"ได้ {actual:N2} คาดหวัง {expected:N2}{(extra.Length > 0 ? " — " + extra : "")}");
    private void Note(string s) => _notes.Add(s);

    // ─────────────────────────────────────────────────────────────────────────────
    [DevDbFact]
    public async Task Payroll_year_2025_end_to_end()
    {
        var root = Path.Combine(Path.GetTempPath(), "hrm-payroll-2025-test");
        Directory.CreateDirectory(root);
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // TIS-620 สำหรับไฟล์ สปส.1-10

        await using var sp = DevDatabase.BuildPayrollServices(root);
        var factory = sp.GetRequiredService<IDbContextFactory<HRMContext>>();
        try
        {
            await ResetAsync(factory);
            await SeedGlobalTaxConfigAsync(factory);
            var emps = await SeedMonthlyCompanyAsync(factory);
            await RunMonthlyCompanyYearAsync(sp, factory, emps);
            await YearEndChecksAsync(factory, emps);

            var emps2 = await SeedSemiMonthlyCompanyAsync(factory);
            await RunSemiMonthlyCompanyYearAsync(sp, factory, emps2);
        }
        catch (Exception ex)
        {
            Fail("FLOW", "สถานการณ์หยุดกลางคัน", ex.ToString());
        }
        finally
        {
            WriteReport();
        }

        var failures = _checks.Where(c => !c.Passed).ToList();
        Assert.True(failures.Count == 0,
            $"{failures.Count} ข้อไม่ผ่าน:\n" + string.Join("\n", failures.Select(f => $"- [{f.Area}] {f.Name}: {f.Detail}")));
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // ล้างข้อมูลบริษัททดสอบ (รอบที่ post แล้วมี trigger กันลบ — ต้องเปลี่ยนสถานะเป็นยกเลิกก่อน)
    private static async Task ResetAsync(IDbContextFactory<HRMContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        var cos = new[] { Co, Co2 };

        var empIds = await ctx.Hremployee.Where(e => cos.Contains(e.companyid)).Select(e => e.id).ToListAsync();
        if (empIds.Count > 0)
        {
            await ctx.Pay_AdhocPayItems.Where(a => empIds.Contains(a.HremployeeId)).ExecuteDeleteAsync();
            await ctx.Pay_ProvidentFundElections.Where(a => empIds.Contains(a.HremployeeId)).ExecuteDeleteAsync();
            await ctx.Pay_EmployeeTaxDeductionElections.Where(a => empIds.Contains(a.HremployeeId)).ExecuteDeleteAsync();
            await ctx.Pay_EmployeePriorEmployerIncomes.Where(a => empIds.Contains(a.HremployeeId)).ExecuteDeleteAsync();
            await ctx.Pay_EmployeePayScheduleOverrides.Where(a => empIds.Contains(a.HremployeeId)).ExecuteDeleteAsync();
            await ctx.Att_DailyAttendances.Where(a => cos.Contains(a.CompanyId)).ExecuteDeleteAsync();
            await ctx.HrwOts.Where(a => cos.Contains(a.companyid)).ExecuteDeleteAsync();
        }

        var runIds = await ctx.Pay_PayrollRuns.Where(r => cos.Contains(r.CompanyId)).Select(r => r.Id).ToListAsync();
        if (runIds.Count > 0)
        {
            await ctx.Pay_PayrollRuns.Where(r => runIds.Contains(r.Id)).ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, PayrollRunStatus.Cancelled));
            await ctx.Pay_Payslips.Where(p => runIds.Contains(p.Pay_PayrollEmployee.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_BankFileExportLines.Where(l => runIds.Contains(l.Pay_BankFileExportBatch.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_BankFileExportBatches.Where(b => runIds.Contains(b.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_GLExportEntries.Where(e => runIds.Contains(e.Pay_GLExportBatch.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_GLExportBatches.Where(b => runIds.Contains(b.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_PayrollAnomalies.Where(a => runIds.Contains(a.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_PayrollAuditLogs.Where(a => runIds.Contains(a.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_PayrollLineItems.Where(l => runIds.Contains(l.Pay_PayrollEmployee.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_PayrollEmployees.Where(e => runIds.Contains(e.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_PayrollRunHolds.Where(h => runIds.Contains(h.PayrollRunId)).ExecuteDeleteAsync();
            await ctx.Pay_PayrollRuns.Where(r => runIds.Contains(r.Id)).ExecuteDeleteAsync();
        }

        // พนักงานลบหลังรอบ (Pay_PayrollEmployee/holds อ้างถึง) — รายการที่อ้างรอบ (adhoc/เงินกู้/เบิกล่วงหน้า) ลบก่อนรอบ
        if (empIds.Count > 0)
            await ctx.Hremployee.Where(e => empIds.Contains(e.id)).ExecuteDeleteAsync();

        await ctx.Hrucfsecuritys.Where(x => cos.Contains(x.companyid)).ExecuteDeleteAsync();
        await ctx.Pay_PaySchedules.Where(x => cos.Contains(x.CompanyId)).ExecuteDeleteAsync();
        await ctx.Pay_AttendanceDeductionPolicies.Where(x => cos.Contains(x.CompanyId)).ExecuteDeleteAsync();
        await ctx.Pay_GLAccountMappings.Where(x => cos.Contains(x.CompanyId)).ExecuteDeleteAsync();
        await ctx.Pay_BankFileFormats.Where(x => cos.Contains(x.CompanyId)).ExecuteDeleteAsync();
        await ctx.Pay_PayslipSettings.Where(x => cos.Contains(x.CompanyId)).ExecuteDeleteAsync();
        await ctx.Lve_CompanySettings.Where(x => cos.Contains(x.CompanyId)).ExecuteDeleteAsync();
        await ctx.Lve_CompanyHolidays.Where(x => cos.Contains(x.CompanyId)).ExecuteDeleteAsync();
        await ctx.Pay_ProvidentFundPolicies.Where(x => cos.Contains(x.CompanyId)).ExecuteDeleteAsync();
        await ctx.com_companies.Where(x => cos.Contains(x.code)).ExecuteDeleteAsync();
    }

    // ตารางภาษี/ค่าลดหย่อนของปี 2568 (ตารางกลาง ไม่ผูกบริษัท) — เพิ่มเฉพาะที่ยังไม่มี ไม่ลบทิ้ง
    private async Task SeedGlobalTaxConfigAsync(IDbContextFactory<HRMContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        if (!await ctx.Pay_TaxBrackets.AnyAsync(b => b.EffectiveYear == Year))
        {
            var src = await ctx.Pay_TaxBrackets.Where(b => b.EffectiveYear == Year + 1 && b.IsActive).OrderBy(b => b.Step).ToListAsync();
            Expect("SEED", $"มีตารางภาษีปี {Year + 1} ให้คัดลอกเป็นปี {Year}", src.Count > 0, $"{src.Count} ขั้น");
            foreach (var b in src)
                ctx.Pay_TaxBrackets.Add(new Pay_TaxBracket { Code = $"TAX-{Year}-{b.Step:00}", EffectiveYear = Year, Step = b.Step, MinIncome = b.MinIncome, MaxIncome = b.MaxIncome, RatePercent = b.RatePercent, IsActive = true });
            Note($"เพิ่มตารางอัตราภาษีปี {Year} ({src.Count} ขั้น คัดลอกจากปี {Year + 1}) — ก่อนหน้านี้ระบบมีเฉพาะปี {Year + 1}");
        }
        if (!await ctx.Pay_TaxDeductionSettings.AnyAsync(s => s.EffectiveYear == Year))
        {
            ctx.Pay_TaxDeductionSettings.Add(new Pay_TaxDeductionSetting { Code = $"TAXDED-{Year}", EffectiveYear = Year, PersonalAllowancePerYear = 60000m, ExpenseDeductionRate = 0.5m, ExpenseDeductionCap = 100000m, ProvidentFundDeductionCapPerYear = 500000m, IsActive = true });
            Note($"เพิ่มค่าลดหย่อนมาตรฐานปี {Year} (ส่วนตัว 60,000 / ค่าใช้จ่าย 50% ไม่เกิน 100,000 / กองทุนไม่เกิน 500,000)");
        }
        if (!await ctx.Pay_TaxDeductionTypes.AnyAsync(t => t.EffectiveYear == Year && t.Code == "LIFE_INSURANCE"))
        {
            ctx.Pay_TaxDeductionTypes.Add(new Pay_TaxDeductionType { Code = "LIFE_INSURANCE", EffectiveYear = Year, NameTh = "เบี้ยประกันชีวิต", NameEn = "Life insurance premium", MaxAmountPerYear = 100000m, IsActive = true, SortOrder = 1 });
            Note($"เพิ่มประเภทค่าลดหย่อน LIFE_INSURANCE ปี {Year}");
        }
        await ctx.SaveChangesAsync();
    }

    private static readonly (int m, int d, string name)[] ThaiHolidays2025 =
    {
        (1, 1, "วันขึ้นปีใหม่"), (2, 12, "วันมาฆบูชา"), (4, 7, "ชดเชยวันจักรี"), (4, 14, "วันสงกรานต์"), (4, 15, "วันสงกรานต์"),
        (5, 1, "วันแรงงาน"), (5, 5, "ชดเชยวันฉัตรมงคล"), (5, 12, "ชดเชยวันวิสาขบูชา"), (6, 3, "วันเฉลิมพระชนมพรรษาพระราชินี"),
        (7, 10, "วันอาสาฬหบูชา"), (7, 28, "วันเฉลิมพระชนมพรรษา ร.10"), (8, 12, "วันแม่แห่งชาติ"), (10, 13, "วันนวมินทรมหาราช"),
        (10, 23, "วันปิยมหาราช"), (12, 5, "วันพ่อแห่งชาติ"), (12, 10, "วันรัฐธรรมนูญ"), (12, 31, "วันสิ้นปี"),
    };

    private async Task SeedCompanyConfigAsync(HRMContext ctx, string co, string name, int periodsPerMonth)
    {
        ctx.com_companies.Add(new com_company { code = co, name = name, abbr = co, isActive = true, tax_id = "0105561000001" });
        ctx.Hrucfsecuritys.Add(new Hrucfsecurity { companyid = co, SecurityCode = HrucfsecurityRateProvider.CurrentEmployeeSecurityCode, SecurityDesc = "ประกันสังคม (พนักงาน)", PercenSecurity = SsoRate, SecurityMoney = SsoCap, OverSecurityMoney = 0m, PercenmgSecurity = SsoRate, EmployerPercenSecurity = SsoRate });
        ctx.Pay_PayslipSettings.Add(new Pay_PayslipSettings { CompanyId = co, PasswordTemplate = "{IdCardLast4}", CompanyName = name, CompanyTaxId = "0105561000001", CompanyAddress = "1 ถนนทดสอบ กรุงเทพฯ 10110", PayDayOfMonth = 25, ModifiedDate = DateTime.Now });
        ctx.Lve_CompanySettings.Add(new Lve_CompanySetting { CompanyId = co, CountryCode = "TH", WorkDaysMask = null });
        foreach (var (m, d, hn) in ThaiHolidays2025)
        {
            var date = new DateOnly(Year, m, d);
            _holidays.Add(date);
            ctx.Lve_CompanyHolidays.Add(new Lve_CompanyHoliday { CompanyId = co, HolidayDate = date, Name = hn, IsActive = true });
        }
        ctx.Pay_ProvidentFundPolicies.Add(new Pay_ProvidentFundPolicy { CompanyId = co, PolicyCode = $"PF-{co}", EffectiveFrom = new DateOnly(2020, 1, 1), MinEmployeeRate = 2, MaxEmployeeRate = 15, MinCompanyRate = 2, MaxCompanyRate = 15, IsEnabled = true, UseFundMembershipYearsForVesting = false });
        ctx.Pay_AttendanceDeductionPolicies.Add(new Pay_AttendanceDeductionPolicy { CompanyId = co, LateMode = PayLateDeductionMode.None, AbsentMode = PayAbsentDeductionMode.DailyRate, DaysPerMonthDivisor = 30, HoursPerDay = 8, DailyWageMode = PayDailyWageDaysMode.WorkingDays, ProrationMode = PayProrationMode.ActualDaysInPeriod, IsActive = true, ModifiedDate = DateTime.Now });
        ctx.Pay_GLAccountMappings.AddRange(
            new Pay_GLAccountMapping { CompanyId = co, MappingKey = GLMappingKeys.NetPayable, DisplayName = "เงินเดือนค้างจ่าย", CreditAccountCode = "2110", IsActive = true, ModifiedDate = DateTime.Now },
            new Pay_GLAccountMapping { CompanyId = co, MappingKey = GLMappingKeys.EmployerSso, DisplayName = "ประกันสังคมนายจ้าง", DebitAccountCode = "5210", CreditAccountCode = "2120", IsActive = true, ModifiedDate = DateTime.Now },
            new Pay_GLAccountMapping { CompanyId = co, MappingKey = GLMappingKeys.EmployerProvidentFund, DisplayName = "สมทบกองทุน", DebitAccountCode = "5220", CreditAccountCode = "2130", IsActive = true, ModifiedDate = DateTime.Now });
        ctx.Pay_PaySchedules.Add(new Pay_PaySchedule { CompanyId = co, Code = periodsPerMonth == 1 ? "MONTHLY" : "SEMI", Name = periodsPerMonth == 1 ? "เดือนละงวด" : "เดือนละ 2 งวด", PeriodsPerMonth = periodsPerMonth, SecondTermStartDay = 16, AppliesTo = PayScheduleGroup.All, EffectiveFrom = new DateOnly(Year, 1, 1), IsActive = true });
    }

    private static Hremployee NewEmp(string co, string no, string name, string surname, decimal? salary, decimal? daily, DateOnly work, DateOnly? resign = null, string sex = "M")
        => new()
        {
            companyid = co, EmpNo = no, EmpName = name, EmpSurname = surname, Sex = sex,
            IdCard = ("110" + new string(no.Where(char.IsDigit).ToArray()).PadLeft(4, '0') + "000000").PadRight(13, '0')[..13], BirthDate = new DateTime(1990, 5, 15),
            WorkDate = work.ToDateTime(TimeOnly.MinValue), ResignDate = resign?.ToDateTime(TimeOnly.MinValue),
            SalaryAmt = salary, DailyWage = daily, SalexpBank = "014", SalexpBranch = "0001", SalexpAccid = $"0140{no.Replace("E", "").Replace("S", "").PadLeft(6, '0')}",
            OverleaveFlag = 0,
        };

    // ═════════════════════════════════════════════════════════════════════════════
    // บริษัท PTEST — จ่ายเดือนละงวด 9 คน ครอบคลุมทุกกรณี
    private async Task<Dictionary<string, long>> SeedMonthlyCompanyAsync(IDbContextFactory<HRMContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        await SeedCompanyConfigAsync(ctx, Co, "บริษัท ทดสอบเงินเดือน 2568 จำกัด", 1);

        var e = new Dictionary<string, Hremployee>
        {
            ["E001"] = NewEmp(Co, "E001", "สมชาย", "ตั้งใจ", 30000m, null, new(2022, 1, 1)),                       // เงินเดือนปกติ + PF 5/5 + ขาดงาน 1 วัน มิ.ย.
            ["E002"] = NewEmp(Co, "E002", "สมหญิง", "ขยัน", 80000m, null, new(2020, 6, 1), sex: "F"),              // OT + ค่าคอมฯ มิ.ย. + ลดหย่อนประกันชีวิต + PF 3/3 + โบนัส ธ.ค. + ปรับปรุง ต.ค.
            ["E003"] = NewEmp(Co, "E003", "วิชัย", "รายวัน", null, 400m, new(2024, 6, 1)),                          // รายวัน 400 บาท นับวันทำงาน
            ["E004"] = NewEmp(Co, "E004", "อรุณ", "ย้ายมา", 50000m, null, new(Year, 7, 1)),                        // เข้ากลางปี 1 ก.ค. มีเงินได้จากนายจ้างเดิม
            ["E005"] = NewEmp(Co, "E005", "มานะ", "ลาออก", 35000m, null, new(2023, 1, 1), new DateOnly(Year, 9, 15)), // ลาออก 15 ก.ย.
            ["E006"] = NewEmp(Co, "E006", "ปิติ", "เข้าใหม่", 40000m, null, new(Year, 3, 10)),                     // เข้ากลางเดือน 10 มี.ค.
            ["E007"] = NewEmp(Co, "E007", "ชูใจ", "รอข้อมูล", 25000m, null, new(2021, 1, 1), sex: "F"),           // พัก (hold) ก.พ. + กันออก (exclude) เม.ย.
            ["E008"] = NewEmp(Co, "E008", "กานดา", "ผู้บริหาร", 120000m, null, new(2019, 1, 1), sex: "F"),        // เพดานประกันสังคม + ขั้นภาษีสูง + PF 10/10 + โบนัส ธ.ค.
            ["E009"] = NewEmp(Co, "E009", "น้อย", "ไม่ถึงเกณฑ์", 12000m, null, new(2024, 1, 1)),                   // ต่ำกว่าเกณฑ์ภาษี — ไม่มีบรรทัดภาษี ไม่ขึ้น ภ.ง.ด.1
        };
        ctx.Hremployee.AddRange(e.Values);
        await ctx.SaveChangesAsync();
        var ids = e.ToDictionary(k => k.Key, k => k.Value.id);

        var jan1 = new DateOnly(Year, 1, 1);
        ctx.Pay_ProvidentFundElections.AddRange(
            new Pay_ProvidentFundElection { HremployeeId = ids["E001"], EmployeeContributionRate = 5, CompanyContributionRate = 5, EffectiveFrom = jan1, IsActive = true, ElectedByUserId = Calc, ElectedDate = DateTime.Now },
            new Pay_ProvidentFundElection { HremployeeId = ids["E002"], EmployeeContributionRate = 3, CompanyContributionRate = 3, EffectiveFrom = jan1, IsActive = true, ElectedByUserId = Calc, ElectedDate = DateTime.Now },
            new Pay_ProvidentFundElection { HremployeeId = ids["E008"], EmployeeContributionRate = 10, CompanyContributionRate = 10, EffectiveFrom = jan1, IsActive = true, ElectedByUserId = Calc, ElectedDate = DateTime.Now });

        var lifeIns = await ctx.Pay_TaxDeductionTypes.FirstAsync(t => t.EffectiveYear == Year && t.Code == "LIFE_INSURANCE");
        ctx.Pay_EmployeeTaxDeductionElections.Add(new Pay_EmployeeTaxDeductionElection { HremployeeId = ids["E002"], DeductionTypeId = lifeIns.Id, AnnualAmount = 50000m, ApplyMonthly = true, IsActive = true, ElectedByUserId = Calc, ElectedDate = DateTime.Now });

        ctx.Pay_EmployeePriorEmployerIncomes.Add(new Pay_EmployeePriorEmployerIncome { HremployeeId = ids["E004"], TaxYear = Year, PriorEmployerName = "บริษัท นายจ้างเดิม จำกัด", IncomeAmount = 300000m, DeductionAmount = 0m, TaxWithheldAmount = 5000m, IsActive = true, EnteredByUserId = Calc, EnteredDate = DateTime.Now });

        // OT ของ E002: มี.ค. 3,000 / ส.ค. 4,500 / ต.ค. 2,000 (ต.ค. จะเพิ่มอีก 5,000 หลังจ่ายแล้ว → กลับรายการ+ปรับปรุง)
        ctx.HrwOts.AddRange(
            NewOt(Co, "E002", "OT2503", new DateTime(Year, 3, 12), 3000m),
            NewOt(Co, "E002", "OT2508", new DateTime(Year, 8, 20), 4500m),
            NewOt(Co, "E002", "OT2510", new DateTime(Year, 10, 8), 2000m));

        // ขาดงาน E001 วันที่ 16 มิ.ย. (นโยบายหักตามค่าจ้างรายวัน เงินเดือน ÷ 30)
        ctx.Att_DailyAttendances.Add(new Att_DailyAttendance { HremployeeId = ids["E001"], CompanyId = Co, WorkDate = new DateOnly(Year, 6, 16), IsAbsent = true, WorkLocation = AttWorkLocation.Office });

        // ค่าคอมมิชชัน E002 งวด มิ.ย. 20,000 (รายการเฉพาะกิจในรอบปกติ — จ่ายครั้งเดียว)
        var bonusType = await ctx.Pay_PayItemTypes.FirstAsync(t => t.Code == "BONUS");
        ctx.Pay_AdhocPayItems.Add(new Pay_AdhocPayItem { HremployeeId = ids["E002"], PayItemTypeId = bonusType.Id, TargetPeriod = $"{Year}06", Amount = 20000m, IsTaxable = true, Reason = "ค่าคอมมิชชันไตรมาส 2", Status = PayAdhocItemStatus.Approved, RequestedByUserId = Calc, RequestedDate = DateTime.Now, ApprovedByUserId = Approver, ApprovedDate = DateTime.Now });

        await ctx.SaveChangesAsync();
        return ids;
    }

    private static HrwOt NewOt(string co, string empNo, string docNo, DateTime date, decimal amount)
        => new() { companyid = co, EmpNo = empNo, OtDocno = docNo, SeqNo = 1, DateWork = date, OtAmt = amount, ApvOtStatus = "A", PostStatus = 1 };

    // ═════════════════════════════════════════════════════════════════════════════
    private sealed class RunLog
    {
        public long Id; public string Period = ""; public PayrollRunType Type; public int Month; public string Label = "";
    }
    private readonly List<RunLog> _runs = new();
    private decimal _e002MayTax;
    private decimal _e001Recovery;

    private async Task RunMonthlyCompanyYearAsync(ServiceProvider sp, IDbContextFactory<HRMContext> factory, Dictionary<string, long> ids)
    {
        var wf = sp.GetRequiredService<PayrollWorkflowService>();
        var bank = sp.GetRequiredService<BankFileExportService>();
        var gl = sp.GetRequiredService<GLExportService>();
        var slips = sp.GetRequiredService<PayslipGenerationService>();

        for (var m = 1; m <= 12; m++)
        {
            var period = $"{Year}{m:00}";
            var runId = await CreateRunAsync(factory, Co, m, 1, 1, PayrollRunType.Regular);
            _runs.Add(new RunLog { Id = runId, Period = period, Type = PayrollRunType.Regular, Month = m, Label = $"รอบปกติ {period}" });

            if (m == 1)
                await NegativeChecksBeforeJanuaryApprovedAsync(factory, wf, runId);

            if (m == 2)
            {
                await using var ctx = await factory.CreateDbContextAsync();
                ctx.Pay_PayrollRunHolds.Add(new Pay_PayrollRunHold { PayrollRunId = runId, HremployeeId = ids["E007"], EmpNo = "E007", Reason = "เอกสารไม่ครบ รอตรวจ", HeldByUserId = Calc, HeldDate = DateTime.Now, IsActive = true });
                await ctx.SaveChangesAsync();
            }

            if (m == 12)
                await SeedDecemberBonusItemsAsync(factory, ids);   // ตั้งไว้ก่อนรอบปกติ — รอบปกติต้องไม่หยิบไป

            var summary = await wf.CalculateAsync(runId, Calc);
            var expectedCount = ExpectedHeadcount(m) - (m == 2 ? 1 : 0);
            Expect("RUN", $"{period} จำนวนพนักงานในรอบ", summary.EmployeeCount == expectedCount, $"ได้ {summary.EmployeeCount} คาดหวัง {expectedCount}");
            Expect("RUN", $"{period} ไม่มียอดสุทธิติดลบ", summary.NegativeNetPayCount == 0, $"{summary.NegativeNetPayCount} คน");

            if (m == 4)
            {
                await using var ctx = await factory.CreateDbContextAsync();
                var row = await ctx.Pay_PayrollEmployees.FirstAsync(r => r.PayrollRunId == runId && r.HremployeeId == ids["E007"]);
                await wf.SetEmployeeExclusionAsync(runId, row.Id, true, "ยอดโต้แย้ง รอเคลียร์", Calc);
                await wf.CalculateAsync(runId, Calc);   // คำนวณใหม่ — ธงกันออกต้องอยู่
                var again = await ctx.Pay_PayrollEmployees.AsNoTracking().FirstAsync(r => r.PayrollRunId == runId && r.HremployeeId == ids["E007"]);
                Expect("HOLD", "ธง 'กันออก' รอดการคำนวณใหม่", again.IsExcluded && again.ExcludeReason == "ยอดโต้แย้ง รอเคลียร์", $"IsExcluded={again.IsExcluded} reason={again.ExcludeReason}");
            }

            await wf.SubmitForReviewAsync(runId, Calc);
            if (m == 1)
            {
                try { await wf.ApproveAsync(runId, Calc, null); Fail("STATE", "ผู้คำนวณอนุมัติเองต้องถูกปฏิเสธ", "อนุมัติผ่าน"); }
                catch (InvalidOperationException ex) { Pass("STATE", "ผู้คำนวณอนุมัติเองถูกปฏิเสธ (แยกหน้าที่)", ex.Message); }
            }
            await wf.ApproveAsync(runId, Approver, "ตรวจแล้ว");
            if (m == 1)
            {
                try { await wf.CalculateAsync(runId, Calc); Fail("STATE", "คำนวณรอบที่อนุมัติแล้วต้องถูกปฏิเสธ", "คำนวณผ่าน"); }
                catch (Exception ex) { Pass("STATE", "คำนวณรอบที่อนุมัติแล้วถูกปฏิเสธ", ex.GetType().Name); }
            }
            await wf.PostAsync(runId, Approver);
            await ExportAndCheckAsync(factory, bank, gl, slips, runId, period);
            await wf.MarkPaidAsync(runId, Approver);

            await CheckRunRowsAsync(factory, runId, period, m, ids);

            if (m == 1)
                await NegativeChecksAfterPaidAsync(factory, wf, runId);

            if (m == 10)
                await ReverseAndAdjustOctoberAsync(factory, wf, bank, runId, ids);

            if (m == 12)
                await BonusRunDecemberAsync(factory, wf, bank, gl, ids);

            await CheckPor1MonthlyAsync(factory, period);
        }
    }

    // ใครอยู่ในรอบเดือนไหน (ไม่นับ hold)
    private static int ExpectedHeadcount(int m)
    {
        var n = 9;
        if (m < 3) n--;      // E006 เข้า 10 มี.ค.
        if (m < 7) n--;      // E004 เข้า 1 ก.ค.
        if (m > 9) n--;      // E005 ลาออก 15 ก.ย.
        return n;
    }

    private static async Task<long> CreateRunAsync(IDbContextFactory<HRMContext> factory, string co, int month, int term, int periodsPerMonth, PayrollRunType type)
    {
        var start = new DateOnly(Year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        if (periodsPerMonth == 2)
        {
            if (term == 1) end = new DateOnly(Year, month, 15);
            else start = new DateOnly(Year, month, 16);
        }
        await using var ctx = await factory.CreateDbContextAsync();
        var run = new Pay_PayrollRun
        {
            CompanyId = co, PayrollPeriod = start.ToString("yyyyMM", CultureInfo.InvariantCulture),
            TermNo = periodsPerMonth == 2 ? term : 1,
            PeriodStart = start, PeriodEnd = end, PayDate = end.AddDays(-3),
            RunType = type, Status = PayrollRunStatus.Draft, CreatedByUserId = Calc,
        };
        ctx.Pay_PayrollRuns.Add(run);
        await ctx.SaveChangesAsync();
        return run.Id;
    }

    private async Task NegativeChecksBeforeJanuaryApprovedAsync(IDbContextFactory<HRMContext> factory, PayrollWorkflowService wf, long janRunId)
    {
        // งวด ก.พ. คำนวณไม่ได้ขณะ ม.ค. ยังไม่อนุมัติ (ลำดับงวด)
        var feb = await CreateRunAsync(factory, Co, 2, 1, 1, PayrollRunType.Regular);
        await wf.CalculateAsync(janRunId, Calc);   // ม.ค. = Calculated แต่ยังไม่อนุมัติ
        try { await wf.CalculateAsync(feb, Calc); Fail("STATE", "คำนวณ ก.พ. ก่อน ม.ค. อนุมัติต้องถูกปฏิเสธ", "คำนวณผ่าน"); }
        catch (InvalidOperationException ex) { Pass("STATE", "คำนวณ ก.พ. ก่อน ม.ค. อนุมัติถูกปฏิเสธ", ex.Message); }
        await wf.CancelAsync(feb, Calc, "รอบทดสอบลำดับงวด");
    }

    private async Task NegativeChecksAfterPaidAsync(IDbContextFactory<HRMContext> factory, PayrollWorkflowService wf, long janRunId)
    {
        // รอบปรับปรุงคำนวณไม่ได้ถ้ายังไม่กลับรายการรอบต้นทาง
        var adj = await wf.CreateAdjustmentRunAsync(janRunId, Calc);
        try { await wf.CalculateAsync(adj, Calc); Fail("STATE", "รอบปรับปรุงก่อนกลับรายการต้องถูกปฏิเสธ", "คำนวณผ่าน"); }
        catch (InvalidOperationException ex) { Pass("STATE", "รอบปรับปรุงก่อนกลับรายการถูกปฏิเสธ", ex.Message); }
        await wf.CancelAsync(adj, Calc, "รอบทดสอบ");

        // แถวของรอบที่จ่ายแล้วแก้ไม่ได้ (trigger ในฐานข้อมูล)
        await using var ctx = await factory.CreateDbContextAsync();
        try
        {
            await ctx.Pay_PayrollEmployees.Where(e => e.PayrollRunId == janRunId).ExecuteUpdateAsync(s => s.SetProperty(e => e.Remark, "แก้หลังจ่าย"));
            Fail("STATE", "แก้แถวรอบที่จ่ายแล้วต้องถูกปฏิเสธ", "แก้ผ่าน");
        }
        catch (Exception ex) { Pass("STATE", "แก้แถวรอบที่จ่ายแล้วถูกฐานข้อมูลปฏิเสธ", ex.GetBaseException().Message.Split('\n')[0]); }
    }

    private async Task ExportAndCheckAsync(IDbContextFactory<HRMContext> factory, BankFileExportService bank, GLExportService gl, PayslipGenerationService slips, long runId, string label)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        var rows = await ctx.Pay_PayrollEmployees.AsNoTracking().Where(e => e.PayrollRunId == runId).ToListAsync();
        var included = rows.Where(r => !r.IsExcluded).ToList();

        var bankId = await bank.ExportAsync(runId, Approver);
        var b = await ctx.Pay_BankFileExportBatches.AsNoTracking().FirstAsync(x => x.Id == bankId);
        Near("BANK", $"{label} ยอดรวมไฟล์ธนาคาร = Σ สุทธิ (ไม่รวมคนที่กันออก)", b.TotalAmount, included.Sum(r => r.NetPay), 0.005m);
        Expect("BANK", $"{label} จำนวนแถวไฟล์ธนาคาร", b.TotalRecordCount == included.Count, $"ได้ {b.TotalRecordCount} คาดหวัง {included.Count}");

        var glId = await gl.ExportAsync(runId, Approver);
        var g = await ctx.Pay_GLExportBatches.AsNoTracking().FirstAsync(x => x.Id == glId);
        Near("GL", $"{label} เดบิต = เครดิต", g.TotalDebit, g.TotalCredit, 0.005m);
        var entries = await ctx.Pay_GLExportEntries.AsNoTracking().Where(x => x.GLExportBatchId == glId).ToListAsync();
        Near("GL", $"{label} รายการ GL รวมเดบิต = เครดิต", entries.Sum(x => x.DebitAmount), entries.Sum(x => x.CreditAmount), 0.005m);
        var employerSso = included.Sum(r => r.SocialSecurityCompanyAmount);
        Near("GL", $"{label} ประกันสังคมนายจ้างลง GL", entries.Where(x => x.GLAccountCode == "5210").Sum(x => x.DebitAmount), employerSso, 0.005m);
        Expect("GL", $"{label} ไม่มีบัญชี UNMAPPED ของฝั่งนายจ้าง", entries.All(x => !x.GLAccountCode.StartsWith("UNMAPPED-EMPLOYER")), string.Join(",", entries.Where(x => x.GLAccountCode.StartsWith("UNMAPPED")).Select(x => x.GLAccountCode).Distinct()));

        try
        {
            var s = await slips.GenerateAsync(runId);
            Expect("SLIP", $"{label} สลิปครบทุกคน", s.Generated == included.Count && s.SkippedReasons.Count == 0, $"สร้าง {s.Generated}/{included.Count} ข้าม: {string.Join("; ", s.SkippedReasons)}");
        }
        catch (Exception ex) { Fail("SLIP", $"{label} สร้างสลิป", ex.GetBaseException().Message); }
    }

    // ตรวจตัวเลขรายคนของรอบปกติเทียบสูตรอิสระ
    private async Task CheckRunRowsAsync(IDbContextFactory<HRMContext> factory, long runId, string period, int m, Dictionary<string, long> ids)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        var run = await ctx.Pay_PayrollRuns.AsNoTracking().FirstAsync(r => r.Id == runId);
        var rows = await ctx.Pay_PayrollEmployees.AsNoTracking().Include(e => e.Pay_PayrollLineItems).ThenInclude(l => l.Pay_PayItemType)
            .Where(e => e.PayrollRunId == runId).ToListAsync();
        var byNo = rows.ToDictionary(r => r.EmpNo!);
        decimal Line(Pay_PayrollEmployee r, string code) => r.Pay_PayrollLineItems.Where(l => l.Pay_PayItemType.Code == code).Sum(l => l.Amount);

        foreach (var r in rows)
        {
            var wageBase = Line(r, "BASE") - Line(r, "ABSENT") - Line(r, "LATE");
            var sso = Math.Round(Math.Min(wageBase, SsoCap) * SsoRate / 100m, 2);
            Near("SSO", $"{period} {r.EmpNo} ประกันสังคมลูกจ้าง 5% เพดาน 15,000", r.SocialSecurityAmount, sso, 0.01m);
            Near("SSO", $"{period} {r.EmpNo} ประกันสังคมนายจ้างเท่าลูกจ้าง", r.SocialSecurityCompanyAmount, sso, 0.01m);
            Near("NET", $"{period} {r.EmpNo} สุทธิ = รับ − หัก", r.NetPay, r.GrossEarnings - r.TotalDeductions, 0.01m);
            Near("NET", $"{period} {r.EmpNo} รับรวม = Σ บรรทัดรับ", r.GrossEarnings, r.Pay_PayrollLineItems.Where(l => l.SignFlag > 0).Sum(l => l.Amount), 0.01m);
            Near("NET", $"{period} {r.EmpNo} หักรวม = Σ บรรทัดหัก", r.TotalDeductions, r.Pay_PayrollLineItems.Where(l => l.SignFlag < 0).Sum(l => l.Amount), 0.01m);
            if (r.TaxAmount == 0) Expect("TAX", $"{period} {r.EmpNo} ภาษี 0 → ไม่มีบรรทัดภาษี", Line(r, "TAX") == 0, "");
        }

        // PF
        void Pf(string no, decimal rate)
        {
            var r = byNo[no];
            var pfBase = Line(r, "BASE") - Line(r, "ABSENT");
            Near("PF", $"{period} {no} สะสมพนักงาน {rate}% ของค่าจ้างฐาน", r.ProvidentFundEmployeeAmount, Math.Round(pfBase * rate / 100m, 2), 0.01m);
            Near("PF", $"{period} {no} สมทบบริษัท {rate}%", r.ProvidentFundCompanyAmount, Math.Round(pfBase * rate / 100m, 2), 0.01m);
        }
        Pf("E001", 5); Pf("E002", 3); Pf("E008", 10);

        // E001 ขาดงาน มิ.ย. 1 วัน = 30,000/30
        if (m == 6) Near("ATT", $"{period} E001 หักขาดงาน 1 วัน (30,000 ÷ 30)", Line(byNo["E001"], "ABSENT"), 1000m, 0.01m);
        else Near("ATT", $"{period} E001 ไม่มีรายการหักขาดงาน", Line(byNo["E001"], "ABSENT"), 0m, 0.001m);

        // รายวัน E003 = 400 × วันทำงาน จ.–ศ. ไม่รวมวันหยุดบริษัท
        var workDays = LeaveDayCalculator.CalculateWorkingDays(run.PeriodStart, run.PeriodEnd, _holidays, null);
        Near("DAILY", $"{period} E003 ค่าจ้างรายวัน 400 × {workDays} วันทำงาน", Line(byNo["E003"], "BASE"), 400m * workDays, 0.01m);
        Near("SSO", $"{period} E003 รายวันก็เข้าประกันสังคม", byNo["E003"].SocialSecurityAmount, Math.Round(Math.Min(400m * workDays, SsoCap) * 0.05m, 2), 0.01m);

        // สัดส่วนคนเข้า/ออกกลางเดือน
        if (m == 3) Near("PRORATE", $"{period} E006 เข้า 10 มี.ค. = 40,000 × 22/31", Line(byNo["E006"], "BASE"), Math.Round(40000m * Math.Round(22m / 31m, 4), 2), 0.01m);
        if (m == 9) Near("PRORATE", $"{period} E005 ลาออก 15 ก.ย. = 35,000 × 15/30", Line(byNo["E005"], "BASE"), Math.Round(35000m * 0.5m, 2), 0.01m);
        if (m == 7) Near("PRORATE", $"{period} E004 เข้า 1 ก.ค. ได้เต็มเดือน", Line(byNo["E004"], "BASE"), 50000m, 0.01m);
        Expect("ELIG", $"{period} E004 อยู่ในรอบเฉพาะตั้งแต่ ก.ค.", byNo.ContainsKey("E004") == (m >= 7), "");
        Expect("ELIG", $"{period} E005 อยู่ในรอบถึง ก.ย.", byNo.ContainsKey("E005") == (m <= 9), "");
        Expect("ELIG", $"{period} E006 อยู่ในรอบตั้งแต่ มี.ค.", byNo.ContainsKey("E006") == (m >= 3), "");
        Expect("HOLD", $"{period} E007 {(m == 2 ? "ถูกพักไว้ ไม่อยู่ในรอบ" : "อยู่ในรอบ")}", byNo.ContainsKey("E007") == (m != 2), "");

        // OT E002
        var otExpected = m switch { 3 => 3000m, 8 => 4500m, 10 => 2000m, _ => 0m };
        Near("OT", $"{period} E002 ค่าล่วงเวลา", Line(byNo["E002"], "OT"), otExpected, 0.01m);
        // ค่าคอมฯ มิ.ย.
        if (m == 6) Near("ADHOC", $"{period} E002 ค่าคอมมิชชัน 20,000 เข้ารอบปกติ", Line(byNo["E002"], "BONUS"), 20000m, 0.01m);
        // ธ.ค.: โบนัสที่ตั้ง "รอบโบนัส" ไว้ก่อนคำนวณ ต้องไม่ถูกรอบปกติหยิบไป
        if (m == 12)
        {
            Near("ADHOC", $"{period} E002 รอบปกติไม่มีโบนัส (ตั้งเป็นรอบโบนัสไว้)", Line(byNo["E002"], "BONUS"), 0m, 0.001m);
            Near("ADHOC", $"{period} E008 รอบปกติไม่มีโบนัส (ตั้งเป็นรอบโบนัสไว้)", Line(byNo["E008"], "BONUS"), 0m, 0.001m);
        }
        // พ.ย.: รายการหักคืนเงินที่โอนเกิน (จากรอบปรับปรุง ต.ค. ที่ E001 ยอดลด) ถูกหักในรอบปกติถัดไป
        if (m == 11) Near("BANK", $"{period} E001 หักคืนเงินที่โอนเกินจากรอบปรับปรุง ต.ค.", Line(byNo["E001"], "ADHOC_DEDUCT"), _e001Recovery, 0.01m);

        // ภาษี ม.ค. ของคนเงินเดือนคงที่ = ภาษีทั้งปี ÷ 12 (ตรวจสูตรประมาณการ)
        if (m == 1)
        {
            var brackets = await ctx.Pay_TaxBrackets.AsNoTracking().Where(b => b.EffectiveYear == Year && b.IsActive).ToListAsync();
            decimal AnnualTax(decimal monthly, decimal pfRate, decimal elected = 0m)
            {
                var income = monthly * 12;
                var flat = (Math.Min(monthly, SsoCap) * 0.05m + Math.Round(monthly * pfRate / 100m, 2)) * 12;
                var taxable = income - Math.Min(income * 0.5m, 100000m) - 60000m - elected - flat;
                return TaxBracketCalculator.CalculateProgressiveTax(taxable, brackets).TotalAnnualTax;
            }
            Near("TAX", "202501 E001 ภาษีเดือนแรก = ภาษีทั้งปี ÷ 12", byNo["E001"].TaxAmount, Math.Round(AnnualTax(30000m, 5) / 12m, 2), 0.01m);
            Near("TAX", "202501 E008 ภาษีเดือนแรก = ภาษีทั้งปี ÷ 12", byNo["E008"].TaxAmount, Math.Round(AnnualTax(120000m, 10) / 12m, 2), 0.01m);
            Near("TAX", "202501 E002 ภาษีเดือนแรก (หักลดหย่อนประกันชีวิต 50,000 เต็มปี)", byNo["E002"].TaxAmount, Math.Round(AnnualTax(80000m, 3, 50000m) / 12m, 2), 0.01m);
            Near("TAX", "202501 E009 เงินได้ต่ำกว่าเกณฑ์ ภาษี 0", byNo["E009"].TaxAmount, 0m, 0.001m);
        }
        // ค่าคอมฯ มิ.ย. 20,000 = เงินได้ครั้งเดียว: ภาษีเดือน มิ.ย. ต้อง = ส่วนประจำ + ส่วนต่างของก้อน (ไม่ใช่ประมาณการ 20,000 × 7 เดือน)
        // และเดือนถัดไปต้องกลับมาใกล้เดิม (ไม่กระโดดขึ้นแล้วลด)
        if (m == 5) _e002MayTax = byNo["E002"].TaxAmount;
        if (m == 6)
        {
            var brackets = await ctx.Pay_TaxBrackets.AsNoTracking().Where(b => b.EffectiveYear == Year && b.IsActive).ToListAsync();
            var prior = await ctx.Pay_PayrollEmployees.AsNoTracking()
                .Where(e => e.CompanyId == Co && e.EmpNo == "E002" && e.Pay_PayrollRun.PeriodStart < run.PeriodStart && e.Pay_PayrollRun.Status >= PayrollRunStatus.Approved)
                .Select(e => new { e.TaxableIncome, e.SocialSecurityAmount, e.ProvidentFundEmployeeAmount, e.TaxAmount }).ToListAsync();
            var ytdIncome = prior.Sum(x => x.TaxableIncome);
            var ytdDed = prior.Sum(x => x.SocialSecurityAmount + x.ProvidentFundEmployeeAmount);
            var ytdTax = prior.Sum(x => x.TaxAmount);
            var e2 = byNo["E002"];
            var flat = e2.SocialSecurityAmount + e2.ProvidentFundEmployeeAmount;
            var recurringAnnual = ytdIncome + 80000m * 7;
            var ded = ytdDed + flat * 7 + 60000m + 50000m;
            decimal TaxOn(decimal inc) => TaxBracketCalculator.CalculateProgressiveTax(inc - ded - Math.Min(inc * 0.5m, 100000m), brackets).TotalAnnualTax;
            var expected = Math.Round((TaxOn(recurringAnnual) - ytdTax) / 7m, 2) + (TaxOn(recurringAnnual + 20000m) - TaxOn(recurringAnnual));
            Near("ONEOFF", "202506 E002 ภาษี = ส่วนประจำ + ส่วนต่างของค่าคอมฯ 20,000 (หักทั้งก้อนเดือนนี้)", e2.TaxAmount, expected, 0.05m, $"ส่วนต่างของก้อน {TaxOn(recurringAnnual + 20000m) - TaxOn(recurringAnnual):N2}");
        }
        if (m == 7)
            Near("ONEOFF", "202507 E002 ภาษีเดือนถัดไปกลับมาเท่าก่อนมีค่าคอมฯ (ไม่กระโดด)", byNo["E002"].TaxAmount, _e002MayTax, 2m);
        if (m == 7)
            Note($"E004 ภาษี ก.ค. (เดือนแรก มีเงินได้เดิม 300,000 หักไว้ 5,000) = {byNo["E004"].TaxAmount:N2}");
    }

    // ต.ค.: จ่ายแล้วพบ OT ตกหล่น 5,000 → กลับรายการ → รอบปรับปรุง
    private async Task ReverseAndAdjustOctoberAsync(IDbContextFactory<HRMContext> factory, PayrollWorkflowService wf, BankFileExportService bank, long octRunId, Dictionary<string, long> ids)
    {
        await using (var ctx = await factory.CreateDbContextAsync())
        {
            ctx.HrwOts.Add(NewOt(Co, "E002", "OT2510B", new DateTime(Year, 10, 20), 5000m));
            ctx.Att_DailyAttendances.Add(new Att_DailyAttendance { HremployeeId = ids["E001"], CompanyId = Co, WorkDate = new DateOnly(Year, 10, 20), IsAbsent = true, WorkLocation = AttWorkLocation.Office });
            await ctx.SaveChangesAsync();
        }
        var rev = await wf.CreateReversalRunAsync(octRunId, Calc, "OT ของ E002 ตกหล่น 5,000 และ E001 ขาดงาน 20 ต.ค. ไม่ได้หัก");
        _runs.Add(new RunLog { Id = rev, Period = $"{Year}10", Type = PayrollRunType.Reversal, Month = 10, Label = "รอบกลับรายการ ต.ค." });
        try { await wf.CreateReversalRunAsync(octRunId, Calc, "ซ้ำ"); Fail("STATE", "กลับรายการซ้ำต้องถูกปฏิเสธ", "ผ่าน"); }
        catch (InvalidOperationException ex) { Pass("STATE", "กลับรายการซ้ำถูกปฏิเสธ", ex.Message); }
        try { await wf.CreateReversalRunAsync(rev, Calc, "ซ้อน"); Fail("STATE", "กลับรายการของรอบกลับรายการต้องถูกปฏิเสธ", "ผ่าน"); }
        catch (Exception ex) { Pass("STATE", "กลับรายการซ้อนถูกปฏิเสธ", ex.GetType().Name); }
        try { await wf.CalculateAsync(rev, Calc); Fail("STATE", "คำนวณรอบกลับรายการต้องถูกปฏิเสธ", "ผ่าน"); }
        catch (Exception ex) { Pass("STATE", "คำนวณรอบกลับรายการถูกปฏิเสธ", ex.GetType().Name); }
        try { await bank.ExportAsync(rev, Approver); Fail("STATE", "ไฟล์ธนาคารของรอบกลับรายการต้องถูกปฏิเสธ", "ผ่าน"); }
        catch (InvalidOperationException ex) { Pass("STATE", "ไฟล์ธนาคารของรอบกลับรายการถูกปฏิเสธ", ex.Message); }

        await wf.SubmitForReviewAsync(rev, Calc);
        await wf.ApproveAsync(rev, Approver, "กลับรายการ");
        await wf.PostAsync(rev, Approver);

        await using (var ctx = await factory.CreateDbContextAsync())
        {
            var orig = await ctx.Pay_PayrollEmployees.AsNoTracking().Where(e => e.PayrollRunId == octRunId).ToListAsync();
            var neg = await ctx.Pay_PayrollEmployees.AsNoTracking().Where(e => e.PayrollRunId == rev).ToListAsync();
            Expect("REV", "รอบกลับรายการมีแถวเท่ารอบต้นทาง", neg.Count == orig.Count, $"{neg.Count}/{orig.Count}");
            Near("REV", "Σ สุทธิ ต้นทาง + กลับรายการ = 0", orig.Sum(e => e.NetPay) + neg.Sum(e => e.NetPay), 0m, 0.001m);
            Near("REV", "Σ ภาษี ต้นทาง + กลับรายการ = 0", orig.Sum(e => e.TaxAmount) + neg.Sum(e => e.TaxAmount), 0m, 0.001m);
            Near("REV", "Σ ประกันสังคม ต้นทาง + กลับรายการ = 0", orig.Sum(e => e.SocialSecurityAmount) + neg.Sum(e => e.SocialSecurityAmount), 0m, 0.001m);
        }

        var adj = await wf.CreateAdjustmentRunAsync(octRunId, Calc);
        _runs.Add(new RunLog { Id = adj, Period = $"{Year}10", Type = PayrollRunType.Adjustment, Month = 10, Label = "รอบปรับปรุง ต.ค." });
        var s = await wf.CalculateAsync(adj, Calc);
        Expect("ADJ", "รอบปรับปรุงคำนวณทั้งงวด (คนเท่ารอบต้นทาง)", s.EmployeeCount == ExpectedHeadcount(10), $"{s.EmployeeCount}");
        await wf.SubmitForReviewAsync(adj, Calc);
        await wf.ApproveAsync(adj, Approver, "ปรับปรุง");
        await wf.PostAsync(adj, Approver);
        var bankId = await bank.ExportAsync(adj, Approver);
        await wf.MarkPaidAsync(adj, Approver);

        await using (var ctx = await factory.CreateDbContextAsync())
        {
            var adjRows = await ctx.Pay_PayrollEmployees.AsNoTracking().Include(e => e.Pay_PayrollLineItems).ThenInclude(l => l.Pay_PayItemType).Where(e => e.PayrollRunId == adj).ToListAsync();
            var e002 = adjRows.First(e => e.EmpNo == "E002");
            Near("ADJ", "รอบปรับปรุง E002 OT = 2,000 + 5,000", e002.Pay_PayrollLineItems.Where(l => l.Pay_PayItemType.Code == "OT").Sum(l => l.Amount), 7000m, 0.01m);
            var origRows = await ctx.Pay_PayrollEmployees.AsNoTracking().Where(e => e.PayrollRunId == octRunId).ToListAsync();
            foreach (var o in origRows.Where(o => o.EmpNo != "E002" && o.EmpNo != "E001"))
                Near("ADJ", $"รอบปรับปรุง {o.EmpNo} สุทธิเท่ารอบเดิม (ไม่มีอะไรเปลี่ยน)", adjRows.First(a => a.EmpNo == o.EmpNo).NetPay, o.NetPay, 0.01m);
            var adjE001 = adjRows.First(a => a.EmpNo == "E001");
            var origE001 = origRows.First(o => o.EmpNo == "E001");
            Near("ADJ", "รอบปรับปรุง E001 หักขาดงาน 20 ต.ค. 1 วัน", adjE001.Pay_PayrollLineItems.Where(l => l.Pay_PayItemType.Code == "ABSENT").Sum(l => l.Amount), 1000m, 0.01m);
            Expect("ADJ", "รอบปรับปรุง E001 สุทธิต่ำกว่าที่โอนไปแล้ว", adjE001.NetPay < origE001.NetPay, $"{adjE001.NetPay:N2} < {origE001.NetPay:N2}");
            _e001Recovery = origE001.NetPay - adjE001.NetPay;
            var recovery = await ctx.Pay_AdhocPayItems.FirstOrDefaultAsync(a => a.HremployeeId == ids["E001"] && a.Remark != null && a.Remark.StartsWith("BANKDELTA:"));
            Expect("BANK", "สร้างรายการหักคืนให้ E001 ในงวดถัดไป (รอ HR อนุมัติ)", recovery is not null && recovery.TargetPeriod == $"{Year}11" && recovery.Status == PayAdhocItemStatus.Pending, recovery is null ? "ไม่พบ" : $"{recovery.TargetPeriod} {recovery.Amount:N2} {recovery.Status}");
            if (recovery is not null)
            {
                Near("BANK", "ยอดหักคืน E001 = ที่โอนแล้ว − ยอดใหม่", recovery.Amount, _e001Recovery, 0.005m);
                recovery.Status = PayAdhocItemStatus.Approved; recovery.ApprovedByUserId = Approver; recovery.ApprovedDate = DateTime.Now;   // HR อนุมัติ → หักใน พ.ย.
                await ctx.SaveChangesAsync();
            }
            var b = await ctx.Pay_BankFileExportBatches.AsNoTracking().FirstAsync(x => x.Id == bankId);
            var origE002 = origRows.First(o => o.EmpNo == "E002");
            Expect("BANK", "รอบปรับปรุง ต.ค. ไฟล์ธนาคารเป็น 'ส่วนต่าง' จากรอบที่จ่ายแล้ว", b.DeltaOfPayrollRunId == octRunId, $"DeltaOfPayrollRunId={b.DeltaOfPayrollRunId}");
            Near("BANK", "รอบปรับปรุง ต.ค. ยอดโอน = ส่วนต่างสุทธิของ E002 เท่านั้น (คนอื่นไม่เปลี่ยน ไม่โอนซ้ำ)", b.TotalAmount, e002.NetPay - origE002.NetPay, 0.005m);
            Expect("BANK", "รอบปรับปรุง ต.ค. ไฟล์มี 1 แถว", b.TotalRecordCount == 1, $"{b.TotalRecordCount} แถว — {b.Remark}");
            Expect("BANK", "รอบปรับปรุง ต.ค. รายการเรียกคืนมีเฉพาะ E001", await ctx.Pay_AdhocPayItems.CountAsync(a => a.Remark != null && a.Remark.StartsWith("BANKDELTA:")) == 1, "");
        }
    }

    // ธ.ค.: รอบโบนัส (รายการเฉพาะกิจที่ HR อนุมัติหลังรอบปกติ) — ภาษีแบบส่วนต่าง
    private async Task SeedDecemberBonusItemsAsync(IDbContextFactory<HRMContext> factory, Dictionary<string, long> ids)
    {
        await using (var ctx = await factory.CreateDbContextAsync())
        {
            var bonusType = await ctx.Pay_PayItemTypes.FirstAsync(t => t.Code == "BONUS");
            ctx.Pay_AdhocPayItems.AddRange(
                new Pay_AdhocPayItem { HremployeeId = ids["E002"], PayItemTypeId = bonusType.Id, TargetPeriod = $"{Year}12", TargetRunType = PayrollRunType.Bonus, Amount = 100000m, IsTaxable = true, Reason = "โบนัสประจำปี", Status = PayAdhocItemStatus.Approved, RequestedByUserId = Calc, RequestedDate = DateTime.Now, ApprovedByUserId = Approver, ApprovedDate = DateTime.Now },
                new Pay_AdhocPayItem { HremployeeId = ids["E008"], PayItemTypeId = bonusType.Id, TargetPeriod = $"{Year}12", TargetRunType = PayrollRunType.Bonus, Amount = 50000m, IsTaxable = true, Reason = "โบนัสประจำปี", Status = PayAdhocItemStatus.Approved, RequestedByUserId = Calc, RequestedDate = DateTime.Now, ApprovedByUserId = Approver, ApprovedDate = DateTime.Now });
            await ctx.SaveChangesAsync();
        }
    }

    private async Task BonusRunDecemberAsync(IDbContextFactory<HRMContext> factory, PayrollWorkflowService wf, BankFileExportService bank, GLExportService gl, Dictionary<string, long> ids)
    {
        var bonus = await CreateRunAsync(factory, Co, 12, 1, 1, PayrollRunType.Bonus);
        _runs.Add(new RunLog { Id = bonus, Period = $"{Year}12", Type = PayrollRunType.Bonus, Month = 12, Label = "รอบโบนัส ธ.ค." });
        var s = await wf.CalculateAsync(bonus, Calc);
        Expect("BONUS", "รอบโบนัสมีเฉพาะคนที่มีรายการ (2 คน)", s.EmployeeCount == 2, $"{s.EmployeeCount}");
        await wf.SubmitForReviewAsync(bonus, Calc);
        await wf.ApproveAsync(bonus, Approver, "โบนัส");
        await wf.PostAsync(bonus, Approver);
        var bankId = await bank.ExportAsync(bonus, Approver);
        var glId = await gl.ExportAsync(bonus, Approver);
        await wf.MarkPaidAsync(bonus, Approver);

        await using (var ctx = await factory.CreateDbContextAsync())
        {
            var rows = await ctx.Pay_PayrollEmployees.AsNoTracking().Include(e => e.Pay_PayrollLineItems).ThenInclude(l => l.Pay_PayItemType).Where(e => e.PayrollRunId == bonus).ToListAsync();
            foreach (var r in rows)
            {
                Near("BONUS", $"{r.EmpNo} รอบโบนัสไม่มีเงินเดือน/ประกันสังคม/กองทุน", r.Pay_PayrollLineItems.Where(l => l.Pay_PayItemType.Code is "BASE" or "SSO" or "PF").Sum(l => l.Amount), 0m, 0.001m);
                Expect("BONUS", $"{r.EmpNo} ภาษีโบนัส > 0", r.TaxAmount > 0, $"{r.TaxAmount:N2}");
            }
            var g = await ctx.Pay_GLExportBatches.AsNoTracking().FirstAsync(x => x.Id == glId);
            Near("GL", "รอบโบนัส เดบิต = เครดิต", g.TotalDebit, g.TotalCredit, 0.005m);
            var b = await ctx.Pay_BankFileExportBatches.AsNoTracking().FirstAsync(x => x.Id == bankId);
            Near("BANK", "รอบโบนัส ยอดโอน = Σ สุทธิ", b.TotalAmount, rows.Sum(r => r.NetPay), 0.005m);
        }
    }

    // ภ.ง.ด.1 รายเดือน = Σ ภาษีของทุกรอบที่อนุมัติในงวดนั้น (ปกติ + โบนัส + กลับรายการ + ปรับปรุง)
    private async Task CheckPor1MonthlyAsync(IDbContextFactory<HRMContext> factory, string period)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        var rows = await ctx.Pay_PayrollEmployees.AsNoTracking()
            .Where(e => e.CompanyId == Co && e.Pay_PayrollRun.PayrollPeriod == period && e.Pay_PayrollRun.Status >= PayrollRunStatus.Approved && e.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled)
            .Select(e => new { e.HremployeeId, e.TaxAmount, e.TaxableIncome }).ToListAsync();
        var perEmp = rows.GroupBy(r => r.HremployeeId).Select(g => new { Tax = g.Sum(x => x.TaxAmount), Taxable = g.Sum(x => x.TaxableIncome) }).Where(x => x.Tax > 0).ToList();
        var por1 = await Por1DataService.BuildMonthlyAsync(ctx, Co, period);
        Expect("POR1", $"{period} ภ.ง.ด.1 สร้างได้", por1 is not null, "");
        if (por1 is null) return;
        Near("POR1", $"{period} ภ.ง.ด.1 ภาษีรวม = Σ ภาษีสุทธิของงวด (รวมกลับรายการ)", por1.TotalTaxWithheld, perEmp.Sum(x => x.Tax), 0.005m);
        Near("POR1", $"{period} ภ.ง.ด.1 เงินได้รวม = Σ เงินได้พึงประเมินสุทธิของงวด", por1.TotalTaxableIncome, perEmp.Sum(x => x.Taxable), 0.005m);
        Expect("POR1", $"{period} ภ.ง.ด.1 จำนวนคน = คนที่มีภาษี", por1.Lines.Count == perEmp.Count, $"ได้ {por1.Lines.Count} คาดหวัง {perEmp.Count}");
    }

    // ═════════════════════════════════════════════════════════════════════════════
    private async Task YearEndChecksAsync(IDbContextFactory<HRMContext> factory, Dictionary<string, long> ids)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        var runs = await ctx.Pay_PayrollRuns.AsNoTracking().Where(r => r.CompanyId == Co && r.Status != PayrollRunStatus.Cancelled).ToListAsync();
        Expect("RUNS", "ทั้งปีมี 12 รอบปกติ + กลับรายการ + ปรับปรุง + โบนัส = 15 รอบ", runs.Count == 15, $"{runs.Count} รอบ: " + string.Join(", ", runs.GroupBy(r => r.RunType).Select(g => $"{g.Key}={g.Count()}")));
        Expect("RUNS", "รอบปกติทุกรอบสถานะจ่ายแล้ว", runs.Where(r => r.RunType == PayrollRunType.Regular).All(r => r.Status == PayrollRunStatus.Paid), string.Join(",", runs.Where(r => r.RunType == PayrollRunType.Regular && r.Status != PayrollRunStatus.Paid).Select(r => r.PayrollPeriod)));

        var brackets = await ctx.Pay_TaxBrackets.AsNoTracking().Where(b => b.EffectiveYear == Year && b.IsActive).ToListAsync();
        var rows = await ctx.Pay_PayrollEmployees.AsNoTracking().Include(e => e.Pay_PayrollRun)
            .Where(e => e.CompanyId == Co && e.Pay_PayrollRun.Status >= PayrollRunStatus.Approved && e.Pay_PayrollRun.Status != PayrollRunStatus.Cancelled && e.Pay_PayrollRun.PeriodStart.Year == Year)
            .ToListAsync();
        var elected = new Dictionary<string, decimal> { ["E002"] = 50000m };
        var priorIncome = new Dictionary<string, (decimal Income, decimal Tax)> { ["E004"] = (300000m, 5000m) };
        var leftBeforeYearEnd = new HashSet<string> { "E005" };

        foreach (var g in rows.GroupBy(r => r.EmpNo!).OrderBy(g => g.Key))
        {
            var no = g.Key;
            var taxable = g.Sum(r => r.TaxableIncome);
            var sso = g.Sum(r => r.SocialSecurityAmount);
            var pf = g.Sum(r => r.ProvidentFundEmployeeAmount);
            var withheld = g.Sum(r => r.TaxAmount);
            var (pIncome, pTax) = priorIncome.GetValueOrDefault(no);
            var annualIncome = taxable + pIncome;
            var expense = Math.Min(annualIncome * 0.5m, 100000m);
            var net = annualIncome - expense - 60000m - elected.GetValueOrDefault(no) - sso - Math.Min(pf, 500000m);
            var annualTax = TaxBracketCalculator.CalculateProgressiveTax(net, brackets).TotalAnnualTax;
            var expectedWithheld = Math.Max(0m, annualTax - pTax);
            var detail = $"เงินได้ทั้งปี {annualIncome:N2} (เรา {taxable:N2}{(pIncome > 0 ? $" + นายจ้างเดิม {pIncome:N2}" : "")}) หักค่าใช้จ่าย {expense:N2} ลดหย่อน 60,000{(elected.ContainsKey(no) ? $"+{elected[no]:N0}" : "")} SSO {sso:N2} PF {pf:N2} → เงินได้สุทธิ {net:N2} ภาษีทั้งปี {annualTax:N2}{(pTax > 0 ? $" หักที่เดิมแล้ว {pTax:N2}" : "")} → ควรหักที่เรา {expectedWithheld:N2} หักจริง {withheld:N2}";
            if (leftBeforeYearEnd.Contains(no))
            {
                Expect("YEAR", $"{no} ลาออกก่อนสิ้นปี: หักไว้ ≥ ภาษีจริง (ส่วนเกินขอคืนตอนยื่น ภ.ง.ด.91)", withheld >= expectedWithheld - 0.05m, detail);
                Note($"{no} หักไว้เกิน {withheld - expectedWithheld:N2} บาท เพราะประมาณการทั้งปีตอนยังทำงานอยู่ — พนักงานขอคืนได้ตอนยื่นแบบ (พฤติกรรมมาตรฐาน ไม่ใช่บั๊ก)");
            }
            else
                Near("YEAR", $"{no} Σ ภาษีหัก ณ ที่จ่ายทั้งปี = ภาษีทั้งปีตามขั้นบันได", withheld, expectedWithheld, 0.05m, detail);

            Expect("SSO", $"{no} ประกันสังคมทั้งปี ≤ 9,000", sso <= 9000m + 0.005m, $"{sso:N2}");

            // 50 ทวิ
            var cert = await WithholdingCertificateDataService.BuildAsync(ctx, ids[no], Year);
            Expect("CERT", $"{no} 50 ทวิ สร้างได้", cert is not null, "");
            if (cert is not null)
            {
                Near("CERT", $"{no} 50 ทวิ เงินได้ = Σ เงินได้พึงประเมิน (ไม่รวมนายจ้างเดิม)", cert.TotalTaxableIncome, taxable, 0.005m);
                Near("CERT", $"{no} 50 ทวิ ภาษี = Σ ภาษีหัก", cert.TotalTaxWithheld, withheld, 0.005m);
                Near("CERT", $"{no} 50 ทวิ ประกันสังคม", cert.TotalSocialSecurity, sso, 0.005m);
                Near("CERT", $"{no} 50 ทวิ กองทุน", cert.TotalProvidentFund, pf, 0.005m);
            }
        }

        // ภ.ง.ด.1ก ทั้งปี
        var annual = await Por1DataService.BuildAnnualAsync(ctx, Co, Year);
        Expect("POR1K", "ภ.ง.ด.1ก สร้างได้", annual is not null, "");
        if (annual is not null)
        {
            var perEmp = rows.GroupBy(r => r.HremployeeId).Select(g => new { Tax = g.Sum(r => r.TaxAmount), Taxable = g.Sum(r => r.TaxableIncome) }).ToList();
            Near("POR1K", "ภ.ง.ด.1ก ภาษีรวมทั้งปี = Σ ภาษีทุกคน", annual.TotalTaxWithheld, perEmp.Sum(x => x.Tax), 0.005m);
            Near("POR1K", "ภ.ง.ด.1ก เงินได้รวมทั้งปี", annual.TotalTaxableIncome, perEmp.Sum(x => x.Taxable), 0.005m);
            var por1Monthly = 0m;
            for (var m = 1; m <= 12; m++)
                por1Monthly += (await Por1DataService.BuildMonthlyAsync(ctx, Co, $"{Year}{m:00}"))?.TotalTaxWithheld ?? 0m;
            Near("POR1K", "Σ ภ.ง.ด.1 รายเดือน 12 เดือน = ภ.ง.ด.1ก", por1Monthly, annual.TotalTaxWithheld, 0.005m);
        }

        // ไฟล์ e-Filing: ภ.ง.ด.1 ต.ค. (เดือนที่มีกลับรายการ+ปรับปรุง) ต้องตรงกับ ภ.ง.ด.1 PDF และ สปส.1-10 ต้องยาว 135 ทุกบรรทัด
        {
            var oct = $"{Year}10";
            var por1Oct = await Por1DataService.BuildMonthlyAsync(ctx, Co, oct);
            var pnd1 = await EFilingExportService.BuildPnd1Async(ctx, Co, oct);
            Expect("EFILE", "ภ.ง.ด.1 ต.ค. สร้างไฟล์ได้", pnd1 is not null, "");
            if (pnd1 is not null && por1Oct is not null)
            {
                var text = Encoding.UTF8.GetString(pnd1.Content);
                var fileLines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                Expect("EFILE", "ภ.ง.ด.1 ต.ค. จำนวนบรรทัด = จำนวนคนที่มีภาษี", fileLines.Length == por1Oct.Lines.Count && pnd1.RowCount == por1Oct.Lines.Count, $"{fileLines.Length}/{por1Oct.Lines.Count}");
                Expect("EFILE", "ภ.ง.ด.1 ต.ค. ทุกบรรทัดมี 10 ช่อง คั่นด้วย |", fileLines.All(l => l.Split('|').Length == 10), "");
                Near("EFILE", "ภ.ง.ด.1 ต.ค. Σ ภาษีในไฟล์ = ภ.ง.ด.1", fileLines.Sum(l => decimal.Parse(l.Split('|')[8], CultureInfo.InvariantCulture)), por1Oct.TotalTaxWithheld, 0.005m);
                Near("EFILE", "ภ.ง.ด.1 ต.ค. Σ เงินได้ในไฟล์ = ภ.ง.ด.1", fileLines.Sum(l => decimal.Parse(l.Split('|')[7], CultureInfo.InvariantCulture)), por1Oct.TotalTaxableIncome, 0.005m);
                Expect("EFILE", "ภ.ง.ด.1 ต.ค. เลขบัตรครบ 13 หลักทุกคน (ไม่มีคำเตือน)", pnd1.Warnings.Count == 0, string.Join("; ", pnd1.Warnings));
                Expect("EFILE", "ภ.ง.ด.1 ต.ค. วันที่จ่ายเป็น พ.ศ. 2568", fileLines.All(l => l.Split('|')[6].EndsWith("2568")), fileLines.FirstOrDefault()?.Split('|')[6] ?? "");
            }
            var sso = await EFilingExportService.BuildSso110Async(ctx, Co, oct);
            Expect("EFILE", "สปส.1-10 ต.ค. สร้างไฟล์ได้", sso is not null, "");
            if (sso is not null)
            {
                var text = Encoding.GetEncoding(874).GetString(sso.Content);
                var fileLines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                Expect("EFILE", "สปส.1-10 ต.ค. ทุกบรรทัดยาว 135 ตัวอักษร", fileLines.All(l => l.Length == 135), string.Join(",", fileLines.Select(l => l.Length).Distinct()));
                Expect("EFILE", "สปส.1-10 ต.ค. บรรทัดแรกเป็นหัว (1) ที่เหลือเป็นรายคน (2)", fileLines[0].StartsWith("1") && fileLines.Skip(1).All(l => l.StartsWith("2")), "");
                var ssoRows = rows.Where(r => r.Pay_PayrollRun.PayrollPeriod == oct).GroupBy(r => r.HremployeeId).Select(g => new { Emp = g.Sum(r => r.SocialSecurityAmount), Er = g.Sum(r => r.SocialSecurityCompanyAmount) }).Where(x => x.Emp > 0).ToList();
                Expect("EFILE", "สปส.1-10 ต.ค. จำนวนผู้ประกันตน = คนที่มีเงินสมทบสุทธิ > 0", sso.RowCount == ssoRows.Count && fileLines.Length == ssoRows.Count + 1, $"{sso.RowCount}/{ssoRows.Count}");
                Near("EFILE", "สปส.1-10 ต.ค. เงินสมทบรวม (ลูกจ้าง+นายจ้าง) = Σ จากรอบที่อนุมัติ", sso.Total2, ssoRows.Sum(x => x.Emp + x.Er), 0.005m);
                Expect("EFILE", "สปส.1-10 ต.ค. ค่าจ้างต่อคนไม่เกินเพดาน 15,000", fileLines.Skip(1).All(l => decimal.Parse(l.Substring(82, 14), CultureInfo.InvariantCulture) <= 15000m), "");
                Note("สปส.1-10: ไฟล์ทดสอบเตือน '" + string.Join("; ", sso.Warnings) + "' — บริษัททดสอบไม่ได้ตั้งเลขที่บัญชีนายจ้าง (ตั้งได้ที่หน้าตั้งค่าสลิป/บริษัท)");
            }
            var pnd1k = await EFilingExportService.BuildPnd1KorAsync(ctx, Co, Year);
            Expect("EFILE", "ภ.ง.ด.1ก สร้างไฟล์ได้", pnd1k is not null, "");
            if (pnd1k is not null && annual is not null)
            {
                var fileLines = Encoding.UTF8.GetString(pnd1k.Content).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                Expect("EFILE", "ภ.ง.ด.1ก ทุกบรรทัดมี 9 ช่อง", fileLines.All(l => l.Split('|').Length == 9), "");
                Near("EFILE", "ภ.ง.ด.1ก Σ ภาษีในไฟล์ = ภ.ง.ด.1ก", fileLines.Sum(l => decimal.Parse(l.Split('|')[7], CultureInfo.InvariantCulture)), annual.TotalTaxWithheld, 0.005m);
            }
        }

        // GL ทุก batch สมดุล
        var gls = await ctx.Pay_GLExportBatches.AsNoTracking().Where(b => b.Pay_PayrollRun.CompanyId == Co).ToListAsync();
        Expect("GL", "ไฟล์ GL ทุกรอบเดบิต = เครดิต", gls.All(b => Math.Abs(b.TotalDebit - b.TotalCredit) < 0.005m), $"{gls.Count} ไฟล์");
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // บริษัท PTEST2 — จ่ายเดือนละ 2 งวด (1–15, 16–สิ้นเดือน) 2 คน
    private async Task<Dictionary<string, long>> SeedSemiMonthlyCompanyAsync(IDbContextFactory<HRMContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        await SeedCompanyConfigAsync(ctx, Co2, "บริษัท ทดสอบจ่ายครึ่งเดือน จำกัด", 2);
        var e = new Dictionary<string, Hremployee>
        {
            ["S001"] = NewEmp(Co2, "S001", "สมศักดิ์", "ครึ่งเดือน", 30000m, null, new(2022, 1, 1)),
            ["S002"] = NewEmp(Co2, "S002", "สมปอง", "รายวัน", null, 500m, new(2022, 1, 1)),
        };
        ctx.Hremployee.AddRange(e.Values);
        ctx.Pay_BankFileFormats.Add(new Pay_BankFileFormat
        {
            CompanyId = Co2, Code = "FIXED", Name = "ตัวอย่างความยาวคงที่", Encoding = "TIS620", LineEnding = "CRLF", IsDefault = true, IsActive = true,
            CompanyBankCode = "014", CompanyBranchCode = "0001", CompanyAccountNo = "1234567890",
            HeaderTemplate = "H{CompanyBankCode:,3,0}{CompanyAccountNo:,-15}{PayDateBE:ddMMyyyy}{RecordCount:,6,0}{TotalAmountCents:,15,0}",
            LineTemplate = "D{Seq:,6,0}{BankCode:,3,0}{BranchCode:,4,0}{AccountNo:,-15}{Name:,-50}{AmountCents:,13,0}{EmpNo:,-10}",
            TrailerTemplate = "T{RecordCount:,6,0}{TotalAmountCents:,15,0}",
            FileNamePattern = "PAYROLL_{Period}_{PayDate:yyyyMMdd}.txt",
        });
        await ctx.SaveChangesAsync();
        return e.ToDictionary(k => k.Key, k => k.Value.id);
    }

    private async Task RunSemiMonthlyCompanyYearAsync(ServiceProvider sp, IDbContextFactory<HRMContext> factory, Dictionary<string, long> ids)
    {
        var wf = sp.GetRequiredService<PayrollWorkflowService>();
        var brackets = await (await factory.CreateDbContextAsync()).Pay_TaxBrackets.AsNoTracking().Where(b => b.EffectiveYear == Year && b.IsActive).ToListAsync();
        var runIds = new List<long>();
        for (var m = 1; m <= 12; m++)
        {
            for (var term = 1; term <= 2; term++)
            {
                var runId = await CreateRunAsync(factory, Co2, m, term, 2, PayrollRunType.Regular);
                runIds.Add(runId);
                var label = $"{Year}{m:00} งวด {term}";
                try
                {
                    var s = await wf.CalculateAsync(runId, Calc);
                    Expect("SEMI", $"{label} มี 2 คน", s.EmployeeCount == 2, $"{s.EmployeeCount}");
                    await wf.SubmitForReviewAsync(runId, Calc);
                    await wf.ApproveAsync(runId, Approver, null);
                    await wf.PostAsync(runId, Approver);
                    if (m == 1 && term == 1)
                        await CheckTemplatedBankFileAsync(sp, factory, runId, label);
                    await wf.MarkPaidAsync(runId, Approver);
                }
                catch (Exception ex)
                {
                    Fail("SEMI", $"{label} เดินรอบไม่ผ่าน", ex.GetBaseException().Message);
                    return;
                }

                await using var ctx = await factory.CreateDbContextAsync();
                var run = await ctx.Pay_PayrollRuns.AsNoTracking().FirstAsync(r => r.Id == runId);
                var rows = await ctx.Pay_PayrollEmployees.AsNoTracking().Include(e => e.Pay_PayrollLineItems).ThenInclude(l => l.Pay_PayItemType).Where(e => e.PayrollRunId == runId).ToListAsync();
                var s001 = rows.First(r => r.EmpNo == "S001");
                var s002 = rows.First(r => r.EmpNo == "S002");
                var baseS001 = s001.Pay_PayrollLineItems.Where(l => l.Pay_PayItemType.Code == "BASE").Sum(l => l.Amount);
                Near("SEMI", $"{label} S001 เงินเดือนครึ่งงวด = 30,000 × ½", baseS001, 15000m, 0.01m);
                Near("SEMI", $"{label} S001 ประกันสังคม (เพดานรายเดือน: งวด 1 = 750, งวด 2 = 0)", s001.SocialSecurityAmount, term == 1 ? 750m : 0m, 0.01m);
                var days = LeaveDayCalculator.CalculateWorkingDays(run.PeriodStart, run.PeriodEnd, _holidays, null);
                Near("SEMI", $"{label} S002 รายวัน 500 × {days} วันทำงานในงวด", s002.Pay_PayrollLineItems.Where(l => l.Pay_PayItemType.Code == "BASE").Sum(l => l.Amount), 500m * days, 0.01m);
                if (m == 1 && term == 1)
                {
                    var flat = (750m) * 12;   // SSO 750/เดือน ไม่มี PF
                    var taxable = 360000m - 100000m - 60000m - flat;
                    var annualTax = TaxBracketCalculator.CalculateProgressiveTax(taxable, brackets).TotalAnnualTax;
                    Near("SEMI", "202501 งวด 1 S001 ภาษีต่องวด = ภาษีทั้งปี ÷ 24", s001.TaxAmount, Math.Round(annualTax / 24m, 2), 0.02m);
                }
            }
        }

        await using (var ctx = await factory.CreateDbContextAsync())
        {
            var rows = await ctx.Pay_PayrollEmployees.AsNoTracking().Where(e => e.CompanyId == Co2 && e.Pay_PayrollRun.Status >= PayrollRunStatus.Approved).ToListAsync();
            foreach (var g in rows.GroupBy(r => r.EmpNo!))
            {
                var taxable = g.Sum(r => r.TaxableIncome);
                var sso = g.Sum(r => r.SocialSecurityAmount);
                var net = taxable - Math.Min(taxable * 0.5m, 100000m) - 60000m - sso;
                var annualTax = TaxBracketCalculator.CalculateProgressiveTax(net, brackets).TotalAnnualTax;
                Near("SEMI", $"{g.Key} Σ ภาษี 24 งวด = ภาษีทั้งปี", g.Sum(r => r.TaxAmount), annualTax, 0.05m, $"เงินได้ {taxable:N2} SSO {sso:N2}");
                Expect("SEMI", $"{g.Key} ประกันสังคมทั้งปี ≤ 9,000 แม้จ่าย 24 งวด", sso <= 9000.005m, $"{sso:N2}");
                Expect("SEMI", $"{g.Key} มี 24 แถว", g.Count() == 24, $"{g.Count()}");
            }
            var por1 = await Por1DataService.BuildMonthlyAsync(ctx, Co2, $"{Year}03");
            Expect("SEMI", "ภ.ง.ด.1 มี.ค. รวม 2 งวดเป็นบรรทัดเดียวต่อคน", por1 is not null && por1.Lines.Count == por1.Lines.Select(l => l.EmpNo).Distinct().Count(), $"{por1?.Lines.Count}");
        }
        Note("PTEST2: รอบครึ่งเดือนใช้ PayrollPeriod เดือนเดียวกันทั้งสองงวด แยกด้วย TermNo (หน้าสร้างรอบมีตัวเลือกงวดเมื่อบริษัทตั้งปฏิทิน 2 งวด/เดือน)");
    }

    // ไฟล์ธนาคารตามแม่แบบ config (ความยาวคงที่ TIS-620): H = 48 ตัว, D = 102 ตัว, T = 22 ตัว
    private async Task CheckTemplatedBankFileAsync(ServiceProvider sp, IDbContextFactory<HRMContext> factory, long runId, string label)
    {
        var bank = sp.GetRequiredService<BankFileExportService>();
        var storage = sp.GetRequiredService<PrivateFileStorage>();
        var batchId = await bank.ExportAsync(runId, Approver);
        await using var ctx = await factory.CreateDbContextAsync();
        var b = await ctx.Pay_BankFileExportBatches.AsNoTracking().FirstAsync(x => x.Id == batchId);
        Expect("BANKFMT", $"{label} ไฟล์ใช้แม่แบบ FIXED ของบริษัท", b.BankFormatCode == "FIXED", b.BankFormatCode);
        Expect("BANKFMT", $"{label} ชื่อไฟล์ตาม pattern", b.FilePath.Contains("PAYROLL_202501_"), b.FilePath);
        var bytes = await storage.ReadAsync(b.FilePath);
        var text = Encoding.GetEncoding(874).GetString(bytes);
        var fileLines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Expect("BANKFMT", $"{label} หัว/รายคน/ท้าย = 1 + {b.TotalRecordCount} + 1 บรรทัด", fileLines.Length == b.TotalRecordCount + 2, $"{fileLines.Length}");
        Expect("BANKFMT", $"{label} บรรทัดหัวยาว 48", fileLines[0].StartsWith("H014") && fileLines[0].Length == 48, $"{fileLines[0].Length}: {fileLines[0]}");
        Expect("BANKFMT", $"{label} บรรทัดรายคนยาว 102 ทุกบรรทัด", fileLines.Skip(1).Take(b.TotalRecordCount).All(l => l.StartsWith("D") && l.Length == 102), string.Join(",", fileLines.Skip(1).Take(b.TotalRecordCount).Select(l => l.Length)));
        Expect("BANKFMT", $"{label} บรรทัดท้ายยาว 22 และยอดรวมเป็นสตางค์", fileLines[^1].Length == 22 && fileLines[^1] == "T" + b.TotalRecordCount.ToString("000000") + ((long)Math.Round(b.TotalAmount * 100)).ToString("000000000000000"), fileLines[^1]);
        Expect("BANKFMT", $"{label} วันที่จ่ายในหัวเป็น พ.ศ.", fileLines[0].Substring(19, 8).EndsWith("2568"), fileLines[0].Substring(19, 8));
    }

    // ═════════════════════════════════════════════════════════════════════════════
    private void WriteReport()
    {
        var sb = new StringBuilder();
        var passed = _checks.Count(c => c.Passed);
        sb.AppendLine($"# ผลทดสอบเงินเดือนทั้งปี พ.ศ. {Year + 543} (ค.ศ. {Year})");
        sb.AppendLine();
        sb.AppendLine($"รัน {DateTime.Now:dd/MM/yyyy HH:mm} — ผ่าน {passed}/{_checks.Count} ข้อ" + (passed == _checks.Count ? " ✅" : $" — ไม่ผ่าน {_checks.Count - passed} ข้อ ❌"));
        sb.AppendLine();
        var failures = _checks.Where(c => !c.Passed).ToList();
        if (failures.Count > 0)
        {
            sb.AppendLine("## ข้อที่ไม่ผ่าน");
            sb.AppendLine();
            foreach (var f in failures) sb.AppendLine($"- **[{f.Area}] {f.Name}** — {f.Detail}");
            sb.AppendLine();
        }
        if (_notes.Count > 0)
        {
            sb.AppendLine("## ข้อสังเกต");
            sb.AppendLine();
            foreach (var n in _notes) sb.AppendLine($"- {n}");
            sb.AppendLine();
        }
        sb.AppendLine("## รอบที่เดิน");
        sb.AppendLine();
        foreach (var r in _runs) sb.AppendLine($"- #{r.Id} {r.Label}");
        sb.AppendLine();
        sb.AppendLine("## รายการตรวจทั้งหมด");
        sb.AppendLine();
        foreach (var g in _checks.GroupBy(c => c.Area))
        {
            sb.AppendLine($"### {g.Key} — {g.Count(c => c.Passed)}/{g.Count()}");
            sb.AppendLine();
            foreach (var c in g) sb.AppendLine($"- {(c.Passed ? "✅" : "❌")} {c.Name}{(c.Detail.Length > 0 ? " — " + c.Detail : "")}");
            sb.AppendLine();
        }
        var text = sb.ToString();
        var dirs = new List<string> { AppContext.BaseDirectory };
        var extra = Environment.GetEnvironmentVariable("HRM_TEST_REPORT_DIR");
        if (!string.IsNullOrWhiteSpace(extra)) dirs.Add(extra);
        foreach (var d in dirs)
        {
            try { Directory.CreateDirectory(d); File.WriteAllText(Path.Combine(d, "payroll-2025-report.md"), text, Encoding.UTF8); }
            catch { /* รายงานเป็นของแถม ไม่ทำให้เทสล้ม */ }
        }
        output.WriteLine(text);
    }
}
