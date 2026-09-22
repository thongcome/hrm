using HRM.Tests.Integration;
using Microsoft.Data.SqlClient;
using Xunit;

namespace HRM.Tests.Schema;

// กติกา id / code ที่เครื่องตรวจเอง (skill advance-data-discipline Part A0, CEO 21 ก.ย. 2569):
//   1. คอลัมน์จำนวนเต็มชื่อ …Id ในตารางของผลิตภัณฑ์ = การอ้างอิงข้ามตาราง → ต้องมี FK จริง
//   2. code ของตาราง master ที่ใช้จับคู่ตอน import/ย้ายข้อมูล → ต้องมี unique index ในขอบเขตของมัน (HRM หลายบริษัท: ต่อบริษัท)
//
// เป็น "ratchet": หนี้เดิมถูกจดไว้ใน fk-baseline.txt / code-index-baseline.txt (มีอยู่จริง ณ วันที่เริ่มกติกา)
//   · คอลัมน์ใหม่ที่ผิดกติกาและไม่อยู่ใน baseline → เทสแดง (แก้: ใส่ FK/unique index ใน migration เดียวกับที่เพิ่มคอลัมน์
//     หรือถ้าตั้งใจไม่ผูกจริง ๆ — เช่น snapshot/log ที่ต้องอยู่รอดแม้แม่ถูกลบ — เพิ่มลง baseline พร้อมเหตุผลหลัง #)
//   · แก้หนี้เดิมแล้ว → เทสแดงเช่นกัน บอกให้ลบบรรทัดนั้นออกจาก baseline — รายการจึงหดได้อย่างเดียว ไม่งอกเงียบ ๆ
// ต้องมีฐานข้อมูล: อ่าน sys.* อย่างเดียว ไม่เขียนอะไร จึงใช้ฐาน dev เดียวกับ [DevDbFact] (HRM_TEST_CONNECTION หรือ user secret) ไม่มี = ข้าม
// ต้นแบบ: Advance.Payroll/Payroll.Tests/Schema/SchemaConventionTests.cs
public class SchemaConventionTests
{
    // ตารางของ HRM เอง = ตารางยุคใหม่ที่ขึ้นต้นด้วย prefix โมดูลแบบ PascalCase (Pay_, Att_, Lve_, Perf_, Rec_, Lms_, Hrd_, Pos_, Wel_, …)
    // + ตารางแกนของระบบเดิมที่ทุกโมดูลอ้างถึง · ตาราง legacy ตัวพิมพ์เล็ก (wf_*, job_*, pc_*, vd_*, mas_*, …) ส่วนใหญ่เป็น scaffold จาก JSP/epms
    // ที่ไม่ได้ใช้ — ไม่อยู่ในขอบเขต (COLLATE BIN: [A-Z] ใน collation ปกติรวมตัวพิมพ์เล็กด้วย)
    private const string ProductTables = @"
        (t.name COLLATE Latin1_General_BIN LIKE '[A-Z][a-z]%[_]%'
         OR t.name IN ('address', 'com_company', 'com_organization', 'HREMPLOYEE', 'sc_user_role', 'sc_role_menu', 'sc_program_role'))";

    // (ตาราง, คอลัมน์ code) ที่ import / ย้ายข้อมูลใช้จับคู่ — ต้องมี unique index ที่มีคอลัมน์นี้อยู่ในคีย์
    // เพิ่ม master ใหม่ = เพิ่มบรรทัดที่นี่ + unique index (ต่อบริษัท) ใน migration เดียวกับที่สร้างตาราง — ดู 20260921160000_UniqueMasterCodes
    private static readonly (string Table, string CodeColumn)[] MasterCodes =
    {
        ("com_company", "code"), ("com_organization", "code"), ("HREMPLOYEE", "EMP_NO"), ("sc_role", "rolecode"), ("sc_user", "loginname"),
        ("sc_menu", "menucode"), ("wf_workflow", "workflowcode"), ("pos_position", "code"),
        ("Com_Bank", "Code"), ("Com_ChartOfAccount", "Code"), ("Com_SectionType", "Code"), ("Com_SubSectionType", "Code"),
        ("Pos_PositionSlot", "PosCode"), ("Pos_ExecType", "Code"), ("Pos_EmployeeType", "Code"), ("Pos_HeadcountBudget", "BudgetCode"),
        ("Job_Family", "Code"), ("Job_Level", "Code"),
        ("Att_ShiftDefinition", "ShiftCode"), ("Att_OtRule", "Code"), ("Att_Project", "Code"), ("Att_Device", "DeviceCode"), ("Att_GeofenceLocation", "Code"),
        ("Lve_LeaveType", "Code"), ("Lve_LeavePolicy", "Code"), ("Lve_CompanyHoliday", "Code"),
        ("Pay_PayItemType", "Code"), ("Pay_PaySchedule", "Code"), ("Pay_SalaryGrade", "GradeCode"), ("Pay_InsurancePlan", "PlanCode"),
        ("Pay_ProvidentFundPolicy", "PolicyCode"), ("Pay_ProvidentFundInvestmentPolicy", "Code"), ("Pay_WelfareFundPolicy", "Code"),
        ("Pay_TaxDeductionType", "Code"), ("Pay_BankFileFormat", "Code"), ("Pay_CommissionPlan", "Code"),
        ("Exp_ExpenseCategory", "Code"), ("Wel_BenefitTypes", "Code"), ("Hrd_LifecycleTaskTemplate", "Code"),
        ("Comp_Category", "Code"), ("Comp_Competency", "Code"), ("Skill", "Code"), ("Skill_Category", "Code"),
        ("Perf_EvaluationType", "Code"), ("Perf_EvaluationPeriod", "Code"), ("Perf_RaterDirectionConfig", "Code"),
        ("Perf_Topic", "Code"), ("Perf_Indicator", "Code"), ("Perf_SubTopic", "Code"),
        ("Okr_Cycle", "Code"), ("Okr_GoalCategory", "Code"),
        ("Lms_Course", "Code"), ("Lms_CourseCategory", "Code"), ("Km_ArticleCategory", "Code"),
        ("Eng_QuestionTemplate", "Code"), ("Eng_RedeemItem", "Code"), ("Eng_SurveyCampaign", "Code"),
        ("Succ_KeyPosition", "Code"), ("Rec_Requisition", "RequisitionCode"),
    };

    [DevDbFact]
    public void Every_id_reference_has_a_foreign_key_unless_it_is_recorded_debt()
    {
        var actual = Query($@"
            SELECT t.name + '.' + c.name
            FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE ty.name IN ('bigint', 'int') AND c.is_identity = 0 AND {ProductTables}
              AND (c.name LIKE '%Id' COLLATE Latin1_General_CS_AS OR c.name LIKE '%ID' COLLATE Latin1_General_CS_AS
                   OR c.name LIKE '%[_]id' OR (c.name LIKE '%id' COLLATE Latin1_General_CS_AS AND LEN(c.name) > 2))
              AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                              WHERE i.is_primary_key = 1 AND ic.object_id = c.object_id AND ic.column_id = c.column_id)
              AND NOT EXISTS (SELECT 1 FROM sys.foreign_key_columns f WHERE f.parent_object_id = c.object_id AND f.parent_column_id = c.column_id)");
        AssertMatchesBaseline(actual, "fk-baseline.txt",
            newDebt: "คอลัมน์ id ที่ไม่มี FK (เพิ่ม FK + index ใน migration เดียวกัน — ดู 20260921150000_MissingForeignKeys เป็นแบบ)",
            paidDebt: "มี FK แล้ว — ลบออกจาก HRM.Tests/Schema/fk-baseline.txt");
    }

    [DevDbFact]
    public void Every_master_code_used_for_matching_has_a_unique_index()
    {
        var actual = new List<string>();
        foreach (var (table, column) in MasterCodes)
        {
            var hasUnique = Query($@"
                SELECT TOP 1 i.name FROM sys.indexes i
                JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
                WHERE i.object_id = OBJECT_ID('{table}') AND i.is_unique = 1 AND COL_NAME(ic.object_id, ic.column_id) = '{column}'").Count > 0;
            var exists = Query($"SELECT 1 WHERE COL_LENGTH('{table}', '{column}') IS NOT NULL").Count > 0;
            if (exists && !hasUnique) actual.Add($"{table}.{column}");
        }
        AssertMatchesBaseline(actual, "code-index-baseline.txt",
            newDebt: "code ของ master ที่ไม่มี unique index (import จะจับคู่กำกวม)",
            paidDebt: "มี unique index แล้ว — ลบออกจาก HRM.Tests/Schema/code-index-baseline.txt");
    }

    // ผังองค์กรต่อกันด้วย parentID — parent_code เป็นสำเนาที่ HRMContext.OrgParent.cs ทำให้ตรงเสมอ (ห้ามสองคอลัมน์เล่าคนละเรื่องอีก)
    [DevDbFact]
    public void Org_tree_parent_code_is_a_faithful_copy_of_parentID()
    {
        var drift = Query(@"
            SELECT CONCAT(c.id, ' ', c.code, ': parent_code=', ISNULL(c.parent_code, '(null)'), ' parentID->', ISNULL(p.code, '(null)'))
            FROM com_organization c LEFT JOIN com_organization p ON p.id = c.parentID
            WHERE ISNULL(c.parent_code, '') <> ISNULL(p.code, '')");
        Assert.True(drift.Count == 0, "com_organization ที่ parent_code ไม่ตรงกับรหัสของ parentID (เขียนผ่าน SQL ตรง ๆ ข้าม hook?):\n  " + string.Join("\n  ", drift));
    }

    private static void AssertMatchesBaseline(IEnumerable<string> actual, string baselineFile, string newDebt, string paidDebt)
    {
        var baseline = File.ReadAllLines(FindBaseline(baselineFile))
            .Select(l => l.Split('#')[0].Trim()).Where(l => l.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = actual.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = now.Except(baseline).OrderBy(x => x).ToList();
        var paid = baseline.Except(now).OrderBy(x => x).ToList();
        Assert.True(added.Count == 0, $"{newDebt}:\n  " + string.Join("\n  ", added));
        Assert.True(paid.Count == 0, $"{paidDebt}:\n  " + string.Join("\n  ", paid));
    }

    // อ่านจากโฟลเดอร์ซอร์ส (ไล่ขึ้นจาก bin) — baseline เป็นไฟล์ที่คนแก้และ commit ไม่ใช่ผลลัพธ์ของ build
    private static string FindBaseline(string file)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Schema", file);
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException($"ไม่พบ HRM.Tests/Schema/{file}");
    }

    private static List<string> Query(string sql)
    {
        using var conn = new SqlConnection(DevDatabase.ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read()) rows.Add(reader.GetValue(0)?.ToString() ?? "");
        return rows;
    }
}
