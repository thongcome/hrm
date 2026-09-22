namespace HRM.Services.Hr.EmployeeImport;

// The single definition of the customer-facing employee import file (CEO, 18 ก.ย. 2569:
// "กำหนด format ให้ลูกค้า"). The template builder writes exactly these sheets/columns and
// the importer reads them back by header text, so a column is added or renamed here once.
// Required columns end with "*" in the header; that is also what the importer checks.
public enum ImportColumnKind { Text, Code, Date, Money, Percent, WholeNumber, List }

public sealed record ImportColumn(
    string Key,
    string Header,
    bool Required,
    ImportColumnKind Kind,
    double Width,
    string Note,
    string Example,
    string? ListName = null,
    int? MaxLength = null,
    int? ExactLength = null);

public sealed record ImportSheet(string Name, string Purpose, IReadOnlyList<ImportColumn> Columns, int MaxRows);

public static class EmployeeImportSchema
{
    public const int TemplateVersion = 1;
    public const string VersionCellNote = "EmployeeImport v1";

    // Dropdown lists held on the hidden "_lists" sheet.
    public const string ListPrename = "Prename";
    public const string ListSex = "Sex";
    public const string ListEmpType = "EmpType";
    public const string ListPayType = "PayType";
    public const string ListYesNo = "YesNo";
    public const string ListBank = "Bank";
    public const string ListPosition = "Position";
    // These two point at other data sheets instead of _lists.
    public const string ListOrgSheet = "@Org";
    public const string ListEmployeeSheet = "@Employee";

    public const string PayMonthly = "รายเดือน";
    public const string PayDaily = "รายวัน";
    public const string Yes = "ใช่";
    public const string No = "ไม่";

    // Legacy HREMPLOYEE.PRENAME_CODE values seen in live data: 1 = นาย (all SEX M), 2/3 = female
    // titles. The importer stores the code and derives SEX when the sex column is blank.
    public static readonly IReadOnlyList<(string Text, string Code, string Sex)> Prenames =
    [
        ("นาย", "1", "M"),
        ("นาง", "2", "F"),
        ("นางสาว", "3", "F"),
    ];

    public static readonly IReadOnlyList<(string Text, string Code)> Sexes = [("ชาย", "M"), ("หญิง", "F")];

    // HREMPLOYEE.EMPTYPE_CODE — the same two values the employee form offers.
    public static readonly IReadOnlyList<(string Text, string Code)> EmpTypes = [("ประจำ", "01"), ("ชั่วคราว", "02")];

    public static readonly ImportSheet Org = new("หน่วยงาน",
        "ผังองค์กร — แถวที่มีอยู่ในระบบแล้วเติมมาให้ แก้ชื่อหรือเพิ่มหน่วยงานใหม่ต่อท้ายได้",
        [
            new("OrgCode", "รหัสหน่วยงาน*", true, ImportColumnKind.Code, 16, "ไม่ซ้ำกันในบริษัท", "HR-PAY", MaxLength: 50),
            new("OrgName", "ชื่อหน่วยงาน*", true, ImportColumnKind.Text, 36, "", "แผนกเงินเดือน", MaxLength: 250),
            new("ParentCode", "รหัสหน่วยงานแม่", false, ImportColumnKind.List, 18, "ว่าง = อยู่ใต้บริษัทโดยตรง", "HR", ListOrgSheet),
            new("ApproverEmpNo", "รหัสพนักงานผู้อนุมัติ", false, ImportColumnKind.List, 20,
                "หัวหน้าที่อนุมัติใบลา/OT ของหน่วยงานนี้", "E0001", ListEmployeeSheet),
        ], MaxRows: 1000);

    public static readonly ImportSheet Employee = new("พนักงาน",
        "หนึ่งแถวต่อพนักงานหนึ่งคน — นำเข้าซ้ำได้ รหัสพนักงานเดิมจะถูกอัปเดต ไม่สร้างซ้ำ",
        [
            new("EmpNo", "รหัสพนักงาน*", true, ImportColumnKind.Code, 14, "ไม่ซ้ำกัน สูงสุด 50 ตัวอักษร", "E0001", MaxLength: 50),
            new("Prename", "คำนำหน้า*", true, ImportColumnKind.List, 10, "", "นางสาว", ListPrename),
            new("FirstName", "ชื่อ*", true, ImportColumnKind.Text, 16, "", "สมใจ", MaxLength: 250),
            new("LastName", "นามสกุล*", true, ImportColumnKind.Text, 18, "", "ใจดี", MaxLength: 250),
            new("FirstNameEn", "ชื่อ (อังกฤษ)", false, ImportColumnKind.Text, 14, "", "Somjai", MaxLength: 50),
            new("LastNameEn", "นามสกุล (อังกฤษ)", false, ImportColumnKind.Text, 16, "", "Jaidee", MaxLength: 50),
            new("IdCard", "เลขบัตรประชาชน*", true, ImportColumnKind.Code, 17,
                "13 หลัก ไม่ต้องมีขีด — ระบบตรวจเลขตรวจสอบ (หลักสุดท้าย) ให้", "1101700203450", ExactLength: 13),
            new("BirthDate", "วันเกิด", false, ImportColumnKind.Date, 12, "วัน/เดือน/ปี ค.ศ. หรือ พ.ศ. ก็ได้", "15/03/1990"),
            new("Sex", "เพศ", false, ImportColumnKind.List, 8, "ว่าง = ดูจากคำนำหน้า", "หญิง", ListSex),
            new("HireDate", "วันเริ่มงาน*", true, ImportColumnKind.Date, 12,
                "ไม่มีวันเริ่มงาน = ระบบจะไม่จ่ายเงินเดือนให้คนนี้", "01/06/2024"),
            new("OrgCode", "รหัสหน่วยงาน*", true, ImportColumnKind.List, 16, "เลือกจากชีต \"หน่วยงาน\"", "HR-PAY", ListOrgSheet),
            new("Position", "ตำแหน่ง", false, ImportColumnKind.List, 24, "", "A01 - พนักงาน", ListPosition),
            new("EmpType", "ประเภทพนักงาน", false, ImportColumnKind.List, 13, "ว่าง = ประจำ", "ประจำ", ListEmpType),
            new("PayType", "ประเภทการจ่าย*", true, ImportColumnKind.List, 13, "", PayMonthly, ListPayType),
            new("Pay", "เงินเดือน / ค่าจ้างต่อวัน*", true, ImportColumnKind.Money, 16,
                "รายเดือน = เงินเดือนต่อเดือน, รายวัน = ค่าจ้างต่อวัน", "25,000.00"),
            new("Bank", "ธนาคาร*", true, ImportColumnKind.List, 30, "", "004 - ธนาคารกสิกรไทย", ListBank),
            new("BankAccount", "เลขที่บัญชี*", true, ImportColumnKind.Code, 16, "ตัวเลขล้วน ไม่ต้องมีขีด", "0123456789", MaxLength: 30),
            new("BankBranch", "รหัสสาขา", false, ImportColumnKind.Code, 10, "", "0001", MaxLength: 8),
            new("PvdEmployeeRate", "กองทุนสำรองฯ % ลูกจ้าง", false, ImportColumnKind.Percent, 12,
                "ว่าง = ไม่เป็นสมาชิก (2–15)", "5"),
            new("PvdEmployerRate", "กองทุนสำรองฯ % นายจ้าง", false, ImportColumnKind.Percent, 12, "", "5"),
            new("Email", "อีเมล", false, ImportColumnKind.Text, 26, "ใช้ส่งสลิปและรีเซ็ตรหัสผ่าน", "somjai@company.co.th", MaxLength: 100),
            new("Phone", "โทรศัพท์", false, ImportColumnKind.Code, 14, "", "0812345678", MaxLength: 50),
            // ที่อยู่ตามทะเบียนบ้าน (21 ก.ย. 2569 — ใช้พิมพ์ 50 ทวิ / ภ.ง.ด.1ก) ลงตาราง address เดิมของระบบ JSP ทั้งแถว REG และ CUR
            // (CUR เฉพาะเมื่อยังไม่มีที่อยู่ปัจจุบัน — พนักงานแก้ที่อยู่ปัจจุบันเองได้ใน ESS ไม่ทับของเขา) · ไม่บังคับทุกช่อง
            new("AddrNo", "ที่อยู่: เลขที่", false, ImportColumnKind.Text, 12, "ที่อยู่ตามทะเบียนบ้าน ใช้พิมพ์ 50 ทวิ", "99/1", MaxLength: 100),
            new("AddrMoo", "หมู่", false, ImportColumnKind.Text, 8, "", "4", MaxLength: 50),
            new("AddrVillage", "หมู่บ้าน / อาคาร", false, ImportColumnKind.Text, 22, "", "หมู่บ้านสุขใจ", MaxLength: 100),
            new("AddrSoi", "ซอย", false, ImportColumnKind.Text, 16, "", "ลาดพร้าว 1", MaxLength: 250),
            new("AddrRoad", "ถนน", false, ImportColumnKind.Text, 16, "", "ลาดพร้าว", MaxLength: 250),
            new("AddrSubdistrict", "ตำบล / แขวง", false, ImportColumnKind.Text, 16, "", "จอมพล", MaxLength: 100),
            new("AddrDistrict", "อำเภอ / เขต", false, ImportColumnKind.Text, 16, "", "จตุจักร", MaxLength: 100),
            new("AddrProvince", "จังหวัด", false, ImportColumnKind.Text, 16, "", "กรุงเทพมหานคร", MaxLength: 100),
            new("AddrPostcode", "รหัสไปรษณีย์", false, ImportColumnKind.Code, 12, "ตัวเลข 5 หลัก", "10900", MaxLength: 5),
            new("CreateLogin", "สร้าง user ESS", false, ImportColumnKind.List, 12,
                "ว่าง = ใช่ (ชื่อผู้ใช้ = รหัสพนักงาน ต้องเปลี่ยนรหัสผ่านครั้งแรก)", Yes, ListYesNo),
        ], MaxRows: 2000);

    // One row per employee per month already paid by the customer's previous system this tax
    // year, so withholding, 50 ทวิ and ภ.ง.ด.1ก see the whole year, not only months paid here.
    // Ported from Advance.Payroll (CEO order, 22 ก.ย. 2569: mirror the payroll domain).
    public static readonly ImportSheet OpeningBalance = new("ยอดยกมา",
        "ใช้เมื่อเริ่มใช้ระบบกลางปี — ยอดของแต่ละเดือนที่จ่ายจากระบบเดิมแล้ว หนึ่งแถวต่อพนักงานต่อเดือน",
        [
            // free text, not a dropdown of the "พนักงาน" sheet: opening balances may be imported later for employees already in the system
            new("EmpNo", "รหัสพนักงาน*", true, ImportColumnKind.Code, 14, "รหัสในชีต \"พนักงาน\" หรือที่มีในระบบแล้ว", "E0001"),
            new("TaxYear", "ปีภาษี (พ.ศ.)*", true, ImportColumnKind.WholeNumber, 12, "", "2569"),
            new("Month", "เดือน*", true, ImportColumnKind.WholeNumber, 8, "1–12", "1"),
            new("GrossIncome", "เงินได้ทั้งหมด*", true, ImportColumnKind.Money, 15, "รวมทุกประเภทก่อนหัก", "27,000.00"),
            new("TaxableIncome", "เงินได้ที่ต้องเสียภาษี*", true, ImportColumnKind.Money, 16, "ยอดที่ขึ้นใน 50 ทวิ", "27,000.00"),
            new("TaxWithheld", "ภาษีหัก ณ ที่จ่าย*", true, ImportColumnKind.Money, 15, "", "450.00"),
            new("SsoEmployee", "ประกันสังคม (ลูกจ้าง)", false, ImportColumnKind.Money, 15, "", "750.00"),
            new("SsoEmployer", "ประกันสังคม (นายจ้าง)", false, ImportColumnKind.Money, 15, "", "750.00"),
            new("PvdEmployee", "กองทุนสำรองฯ (ลูกจ้าง)", false, ImportColumnKind.Money, 15, "", "1,250.00"),
            new("PvdEmployer", "กองทุนสำรองฯ (นายจ้าง)", false, ImportColumnKind.Money, 15, "", "1,250.00"),
            new("NetPay", "เงินได้สุทธิที่จ่าย", false, ImportColumnKind.Money, 15, "", "24,550.00"),
        ], MaxRows: 24000);

    public static readonly IReadOnlyList<ImportSheet> DataSheets = [Org, Employee, OpeningBalance];
}
