// ---------------------------------------------------------------------------
// ScMenuNavCatalog — data catalog of the hardcoded drawer navigation.
//
// Source: Components/Layout/MainLayout.razor (<MudNavMenu> block), extracted
// 2026-08-31 for the sc_menu access-menu build-out. Names resolved from
// wwwroot/Resources/th.json and en.json; literal Thai strings in the layout
// are kept verbatim (NameEn = same string when no translation key exists).
// To regenerate: re-run this extraction against MainLayout.razor.
//
// Notes:
// - Group gating is not modeled on NavCatalogGroup (record shape is fixed);
//   the guard that wraps each group is noted in a comment next to it.
// - The drawer has NO nested MudNavGroups today — every group is a direct
//   child of the MudNavMenu, so no flattening was required.
// - The two MudText overline section headers ("ModuleHubCategoryHrm" /
//   "ModuleHubCategoryHrd") are visual dividers, not nav items — omitted.
// ---------------------------------------------------------------------------

namespace HRM.Services.Security;

public record NavCatalogEntry(
    string? GroupCode,   // null for top-level links; else the synthetic group code this link belongs to
    string? Code,        // gate menucode (innermost Has()/HasAny(); first code for HasAny); null = renders for every logged-in user
    string Url,          // Href verbatim
    string NameTh,
    string NameEn,
    string Icon,         // e.g. "Icons.Material.Filled.EventBusy" (stored as plain string)
    int Order);          // running order within its group (10, 20, 30...) matching on-screen order

public record NavCatalogGroup(
    string GroupCode,    // synthetic, stable: "GRP_" + short latin slug
    string NameTh,
    string NameEn,
    string Icon,
    int Order);          // order of the group in the drawer (10, 20, ...)

public static class ScMenuNavCatalog
{
    public static readonly IReadOnlyList<NavCatalogGroup> Groups = new List<NavCatalogGroup>
    {
        // guard: _hasEssAccess (menu claim "ESS_ACCESS")
        new("GRP_ESS", "พื้นที่พนักงาน (ESS)", "Employee Self-Service (ESS)", "Icons.Material.Filled.Person", 10),
        // guard: Has("REC_ADMIN")
        new("GRP_RECRUIT", "สรรหาบุคลากร (Recruitment)", "Recruitment", "Icons.Material.Filled.PersonSearch", 20),
        // guard: Has("PAY_ADMIN")
        new("GRP_EMPLOYEE", "ระบบบริหารการพนักงาน (Employee)", "Employee Management", "Icons.Material.Filled.Person", 30),
        // guard: HasAny("ORG_ADMIN","SYS_ADMIN","POS_ADMIN","PAY_ADMIN"); literal title in layout (no Translate key)
        new("GRP_ORG_STRUCT", "โครงสร้างองค์กร (Organization Structure)", "โครงสร้างองค์กร (Organization Structure)", "Icons.Material.Filled.AccountBalance", 40),
        // guard: HasAny("ORG_ADMIN","POS_ADMIN")
        new("GRP_ORG_CHART", "ผังองค์กร (Organization)", "Organization Chart", "Icons.Material.Filled.AccountTree", 50),
        // guard: Has("POS_ADMIN"); literal title in layout (no Translate key)
        new("GRP_POSITION", "ตำแหน่งและอัตรากำลัง", "ตำแหน่งและอัตรากำลัง", "Icons.Material.Filled.Badge", 60),
        // guard: Has("ATT_ADMIN")
        new("GRP_ATTENDANCE", "เวลาเข้างาน (Attendance)", "Attendance", "Icons.Material.Filled.AccessTime", 70),
        // no guard on the group itself (individual links gated inside)
        new("GRP_PAYROLL", "เงินเดือน (Payroll)", "Payroll", "Icons.Material.Filled.Payments", 80),
        // guard: Has("CT_CONTRACT_ADMIN")
        new("GRP_CONTRACTS", "สัญญา (Contracts)", "Contracts", "Icons.Material.Filled.Assignment", 90),
        // guard: HasAny("HR_ANNOUNCE_ACCESS","HR_ANNOUNCE_ADMIN")
        new("GRP_ANNOUNCE", "ประกาศ/ข่าวสาร (Announcements)", "Announcements", "Icons.Material.Filled.Campaign", 100),
        // guard: HasAny("HR_DISCIPLINE_ADMIN","HR_GRIEVANCE_ADMIN","HR_REWARD_ADMIN")
        new("GRP_EMP_RELATIONS", "ความสัมพันธ์พนักงาน (Employee Relations)", "Employee Relations", "Icons.Material.Filled.Balance", 110),
        // guard: Has("EXP_ADMIN")
        new("GRP_EXPENSE", "เบิกค่าใช้จ่าย (Expense Claims - Admin)", "Expense Claims (Admin)", "Icons.Material.Filled.ReceiptLong", 120),
        // guard: Has("JOBCOMP_ADMIN")
        new("GRP_JOBCOMP", "สายอาชีพ/สมรรถนะ (Job & Competency)", "Job & Competency", "Icons.Material.Filled.EmojiObjects", 130),
        // guard: HasAny("PERF_ADMIN","PERF_ACCESS")
        new("GRP_PERF", "ประเมินผลปฏิบัติงาน (Performance/KPI)", "Performance / KPI", "Icons.Material.Filled.Assessment", 140),
        // guard: Has("OKR_ADMIN")
        new("GRP_OKR", "OKR เป้าหมายเชิงกลยุทธ์ (v2)", "OKR Strategic Goals (v2)", "Icons.Material.Filled.TrackChanges", 150),
        // guard: HasAny("TALENT_ADMIN","TALENT_ACCESS")
        new("GRP_TALENT", "Talent Management (9-Box)", "Talent Management (9-Box)", "Icons.Material.Filled.GridView", 160),
        // guard: HasAny("IDP_ACCESS","IDP_ADMIN")
        new("GRP_IDP", "แผนพัฒนารายบุคคล (IDP)", "Individual Development Plan (IDP)", "Icons.Material.Filled.TrendingUp", 170),
        // guard: HasAny("CAREER_ACCESS","CAREER_ADMIN")
        new("GRP_CAREER", "Career Management", "Career Management", "Icons.Material.Filled.TrendingUp", 180),
        // guard: Has("SUCC_ADMIN")
        new("GRP_SUCCESSION", "Succession Planning", "Succession Planning", "Icons.Material.Filled.AccountTree", 190),
        // guard: Has("LMS_ADMIN")
        new("GRP_LMS", "ฝึกอบรม (Training / LMS)", "Training (LMS)", "Icons.Material.Filled.School", 200),
        // guard: HasAny("KM_ACCESS","KM_ADMIN")
        new("GRP_KM", "จัดการความรู้ (Knowledge Management)", "Knowledge Management", "Icons.Material.Filled.MenuBook", 210),
        // guard: Has("ENG_ADMIN")
        new("GRP_ENGAGEMENT", "Employee Engagement", "Employee Engagement", "Icons.Material.Filled.Favorite", 220),
        // guard: Has("ORGDEV_ADMIN")
        new("GRP_ORGDEV", "Organization Development", "Organization Development", "Icons.Material.Filled.Business", 230),
        // no guard on the group itself (individual links gated inside)
        new("GRP_WF_ENGINE", "Workflow (Engine)", "Workflow (Engine)", "Icons.Material.Filled.Rule", 240),
        // guard: HasAny("WF_EMPLOYEE_ADMIN","WF_ORG_TYPE_ADMIN")
        new("GRP_WF_SCAFFOLD", "Workflow (ตัวอย่าง CRUD Scaffold)", "Workflow (CRUD Scaffold Example)", "Icons.Material.Filled.AccountTree", 250),
        // guard: Has("HR_ANALYTICS")
        new("GRP_HR_ANALYTICS", "HR Analytics", "HR Analytics", "Icons.Material.Filled.QueryStats", 260),
        // guard: Has("SYS_ADMIN")
        new("GRP_SYS_ADMIN", "จัดการระบบ (เมนู/สิทธิ์)", "System Administration (Menus/Permissions)", "Icons.Material.Filled.AdminPanelSettings", 270),
        // no guard on the group itself (unauthorized-by-design / dead legacy routes)
        new("GRP_LEGACY", "ระบบ / อื่นๆ (เก่า)", "System / Other (Legacy)", "Icons.Material.Filled.Settings", 280),
    };

    public static readonly IReadOnlyList<NavCatalogEntry> Links = new List<NavCatalogEntry>
    {
        // ---- Top-level links (before any group) ----
        new(null, null, "/modules", "ภาพรวมระบบตามโมดูล", "Module Overview", "Icons.Material.Filled.Apps", 10), // TODO ungated today
        new(null, null, "/dev/pages", "รวมลิงก์ทั้งหมด (ชั่วคราว)", "All Links (temp)", "Icons.Material.Filled.ListAlt", 20), // TODO ungated today

        // ---- GRP_ESS (group guarded by _hasEssAccess -> "ESS_ACCESS") ----
        new("GRP_ESS", "ESS_ACCESS", "/ess", "หน้าแรก", "Home", "Icons.Material.Filled.Home", 10),
        new("GRP_ESS", "ESS_ACCESS", "/ess/profile", "ข้อมูลส่วนตัว", "My Profile", "Icons.Material.Filled.Badge", 20),
        new("GRP_ESS", "ESS_ACCESS", "/ess/my-profile", "ข้อมูลบุคลากรของฉัน", "My Personnel Profile", "Icons.Material.Filled.PermContactCalendar", 30),
        new("GRP_ESS", "ESS_ACCESS", "/ess/payslips", "สลิปเงินเดือน", "Payslips", "Icons.Material.Filled.Receipt", 40),
        new("GRP_ESS", "ESS_ACCESS", "/ess/my-recurring-items", "เงินได้เงินหักประจำของฉัน", "My Recurring Pay Items", "Icons.Material.Filled.Repeat", 50),
        new("GRP_ESS", "ESS_ACCESS", "/ess/attendance-checkin", "เช็คอิน/เช็คเอาท์ (GPS)", "Check-in/Check-out (GPS)", "Icons.Material.Filled.MyLocation", 60),
        new("GRP_ESS", "ESS_ACCESS", "/att/my-timesheet", "Timesheet ของฉัน", "My Timesheet", "Icons.Material.Filled.Schedule", 70),
        new("GRP_ESS", "LEAVE_ACCESS", "/leave-requests", "คำขอลางาน", "Leave Requests", "Icons.Material.Filled.EventBusy", 80),
        new("GRP_ESS", "LEAVE_ACCESS", "/leave-requests/team-calendar", "ปฏิทินทีม", "ปฏิทินทีม", "Icons.Material.Filled.CalendarViewMonth", 90), // literal Thai label, no Translate key
        new("GRP_ESS", "OKR_ACCESS", "/ess/my-okr", "OKR ของฉัน", "My OKR", "Icons.Material.Filled.TrackChanges", 100),
        new("GRP_ESS", "LMS_ACCESS", "/ess/lms/catalog", "หลักสูตรอบรม", "Training Courses", "Icons.Material.Filled.MenuBook", 110),
        new("GRP_ESS", "LMS_ACCESS", "/ess/lms/my-training", "ประวัติการอบรมของฉัน", "My Training History", "Icons.Material.Filled.School", 120),
        new("GRP_ESS", "HR_ANNOUNCE_ACCESS", "/hr/announcements", "ประกาศ", "Announcements", "Icons.Material.Filled.Campaign", 130), // same Url also listed under GRP_ANNOUNCE
        new("GRP_ESS", "ESS_ACCESS", "/ess/surveys", "แบบสำรวจของฉัน", "My Surveys", "Icons.Material.Filled.Poll", 140),
        new("GRP_ESS", "ESS_ACCESS", "/ess/recognition", "Kudos / คำชื่นชม", "Kudos / Recognition", "Icons.Material.Filled.EmojiEvents", 150),
        new("GRP_ESS", "EXP_ACCESS", "/exp/my-claims", "เบิกค่าใช้จ่าย", "Expense Claims", "Icons.Material.Filled.ReceiptLong", 160),
        new("GRP_ESS", "ESS_ACCESS", "/ess/grievance/new", "แจ้งเรื่องร้องเรียน", "Submit a Grievance", "Icons.Material.Filled.ReportProblem", 170),
        new("GRP_ESS", "ESS_ACCESS", "/ess/grievance/my", "เรื่องร้องเรียนของฉัน", "My Grievances", "Icons.Material.Filled.ListAlt", 180),

        // ---- GRP_RECRUIT (group guarded by Has("REC_ADMIN")) ----
        new("GRP_RECRUIT", "REC_ADMIN", "/rec/requisitions", "คำขออัตรากำลัง", "Headcount Requisitions", "Icons.Material.Filled.RequestPage", 10),
        new("GRP_RECRUIT", "REC_ADMIN", "/rec/postings", "ประกาศรับสมัคร", "Job Postings", "Icons.Material.Filled.Campaign", 20),
        new("GRP_RECRUIT", "REC_ADMIN", "/rec/offers", "ข้อเสนอจ้างงาน", "Job Offers", "Icons.Material.Filled.Mail", 30),
        new("GRP_RECRUIT", "REC_ADMIN", "/rec/dashboard", "ภาพรวมการสรรหา", "Recruitment Overview", "Icons.Material.Filled.Dashboard", 40),

        // ---- GRP_EMPLOYEE (group guarded by Has("PAY_ADMIN")) ----
        new("GRP_EMPLOYEE", "PAY_ADMIN", "/employee", "ค้นหาข้อมูลพนักงาน", "Employee Search", "Icons.Material.Filled.Search", 10),
        new("GRP_EMPLOYEE", "PAY_ADMIN", "/employee/personnel-profile", "ข้อมูลบุคลากร (AI Search)", "Personnel Profile (AI Search)", "Icons.Material.Filled.Badge", 20),

        // ---- GRP_ORG_STRUCT (group guarded by HasAny("ORG_ADMIN","SYS_ADMIN","POS_ADMIN","PAY_ADMIN")) ----
        new("GRP_ORG_STRUCT", "SYS_ADMIN", "/admin/system/companies", "ข้อมูลบริษัท", "Company Information", "Icons.Material.Filled.Apartment", 10),
        new("GRP_ORG_STRUCT", "ORG_ADMIN", "/org/organizations", "ข้อมูลสังกัด", "Organization Units", "Icons.Material.Filled.Business", 20),
        new("GRP_ORG_STRUCT", "ORG_ADMIN", "/org/section-types", "ข้อมูลประเภทสังกัด", "Organization Unit Types", "Icons.Material.Filled.Layers", 30),
        new("GRP_ORG_STRUCT", "ORG_ADMIN", "/org/subsection-types", "ลักษณะของหน่วยงาน", "Unit Characteristics", "Icons.Material.Filled.Category", 40),
        new("GRP_ORG_STRUCT", "ORG_ADMIN", "/org/layers", "ระดับชั้นหน่วยงาน (Org Layer)", "ระดับชั้นหน่วยงาน (Org Layer)", "Icons.Material.Filled.Layers", 50), // literal Thai label
        new("GRP_ORG_STRUCT", "ORG_ADMIN", "/org/layer-groups", "กลุ่มระดับชั้นหน่วยงาน", "กลุ่มระดับชั้นหน่วยงาน", "Icons.Material.Filled.Domain", 60), // literal Thai label
        new("GRP_ORG_STRUCT", "POS_ADMIN", "/pos/position-master", "ทำเนียบตำแหน่ง (Legacy)", "ทำเนียบตำแหน่ง (Legacy)", "Icons.Material.Filled.Badge", 70), // literal Thai label
        new("GRP_ORG_STRUCT", "PAY_ADMIN", "/pay/admin/chart-of-accounts", "ผังบัญชี", "Chart of Accounts", "Icons.Material.Filled.AccountBalance", 80),

        // ---- GRP_ORG_CHART (group guarded by HasAny("ORG_ADMIN","POS_ADMIN")) ----
        new("GRP_ORG_CHART", "ORG_ADMIN", "/org/set-boss", "แสดงกำหนดหัวหน้า", "Assign Supervisors", "Icons.Material.Filled.SupervisorAccount", 10),
        new("GRP_ORG_CHART", "ORG_ADMIN", "/org/change-history", "ประวัติการปรับผังองค์กร", "Org Chart Change History", "Icons.Material.Filled.History", 20),
        new("GRP_ORG_CHART", "POS_ADMIN", "/org/headcount", "อัตรากำลังตามผังองค์กร", "Headcount by Org Chart", "Icons.Material.Filled.Groups", 30),
        new("GRP_ORG_CHART", "ORG_ADMIN", "/org/chart", "ผังองค์กรแบบภาพ (Org Chart)", "Org Chart (Visual)", "Icons.Material.Filled.AccountTree", 40),
        new("GRP_ORG_CHART", "ORG_ADMIN", "/org/chart-draggable", "ผังองค์กร (แบบลากอิสระ)", "Org Chart (Free Drag)", "Icons.Material.Filled.PanTool", 50),

        // ---- GRP_POSITION (group guarded by Has("POS_ADMIN")) ----
        new("GRP_POSITION", "POS_ADMIN", "/pos/employee-types", "ประเภทพนักงาน", "Employee Types", "Icons.Material.Filled.Groups", 10),
        new("GRP_POSITION", "POS_ADMIN", "/pos/exec-types", "ชื่อตำแหน่ง", "Position Titles", "Icons.Material.Filled.WorkOutline", 20),
        new("GRP_POSITION", "POS_ADMIN", "/pos/positions", "อัตราตำแหน่ง (Legacy)", "อัตราตำแหน่ง (Legacy)", "Icons.Material.Filled.AssignmentInd", 30), // literal Thai label
        new("GRP_POSITION", "POS_ADMIN", "/pos/position-levels", "ระดับตำแหน่ง (Legacy)", "ระดับตำแหน่ง (Legacy)", "Icons.Material.Filled.Stairs", 40), // literal Thai label
        new("GRP_POSITION", "POS_ADMIN", "/pos/position-slots", "เลขที่อัตรา", "Position Slots", "Icons.Material.Filled.EventSeat", 50),
        new("GRP_POSITION", "POS_ADMIN", "/pos/headcount-budget", "งบประมาณอัตรากำลัง", "Headcount Budget", "Icons.Material.Filled.PieChart", 60),

        // ---- GRP_ATTENDANCE (group guarded by Has("ATT_ADMIN")) ----
        new("GRP_ATTENDANCE", "ATT_ADMIN", "/att/settings", "ตั้งค่าเวลาเข้างาน", "Attendance Settings", "Icons.Material.Filled.Tune", 10),
        new("GRP_ATTENDANCE", "ATT_ADMIN", "/att/shifts", "จัดการกะ", "Shift Management", "Icons.Material.Filled.Schedule", 20),
        new("GRP_ATTENDANCE", "ATT_ADMIN", "/att/import", "นำเข้า Log เครื่องสแกน", "Import Scanner Log", "Icons.Material.Filled.UploadFile", 30),
        new("GRP_ATTENDANCE", "ATT_ADMIN", "/att/report", "รายงานเวลาเข้างาน", "Attendance Report", "Icons.Material.Filled.Assessment", 40),
        new("GRP_ATTENDANCE", "ATT_ADMIN", "/att/geofence-locations", "พื้นที่อนุญาตเช็คอิน (GPS)", "Allowed Check-in Areas (GPS)", "Icons.Material.Filled.MyLocation", 50),
        new("GRP_ATTENDANCE", "ATT_ADMIN", "/att/projects", "โครงการ (Timesheet)", "Projects (Timesheet)", "Icons.Material.Filled.Work", 60),
        new("GRP_ATTENDANCE", "ATT_ADMIN", "/att/ot-rules", "อัตราคูณค่าล่วงเวลา (OT)", "Overtime Multiplier Rules", "Icons.Material.Filled.MoneyOff", 70),

        // ---- GRP_PAYROLL (group itself ungated; per-link gates inside) ----
        new("GRP_PAYROLL", null, "/hrpayrolldasboard", "แดชบอร์ด (เดิม)", "Dashboard (Legacy)", "Icons.Material.Filled.Dashboard", 10), // TODO ungated today (legacy link, no Menu: policy)
        new("GRP_PAYROLL", null, "/hremployees", "พนักงาน (payrolls)", "Employees (legacy)", "Icons.Material.Filled.People", 20), // TODO ungated today (legacy link, no Menu: policy)
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/employees", "ข้อมูลพนักงาน / จัดการพนักงาน (Payroll)", "Employee Data / Management (Payroll)", "Icons.Material.Filled.Badge", 30),
        new("GRP_PAYROLL", "PAY_RUNS", "/payrollprocess", "ประมวลผลเงินเดือน", "Payroll Processing", "Icons.Material.Filled.PlaylistPlay", 40),
        new("GRP_PAYROLL", null, "/AIpayrollprocess", "AI ประมวลผลเงินเดือน", "AI Payroll Processing", "Icons.Material.Filled.AutoAwesome", 50), // TODO ungated today (legacy link, no Menu: policy)
        new("GRP_PAYROLL", "PAY_RUNS", "/pay/runs", "เงินเดือน (ใหม่)", "Payroll (New)", "Icons.Material.Filled.RequestQuote", 60),
        new("GRP_PAYROLL", "PAY_ADHOC", "/pay/adhoc", "รายการเฉพาะกิจ", "Ad-hoc Pay Items", "Icons.Material.Filled.PostAdd", 70),
        new("GRP_PAYROLL", "PAY_ADHOC", "/pay/recurring-items", "เงินได้เงินหักประจำ", "Recurring Pay Items", "Icons.Material.Filled.Repeat", 80),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/payslip-settings", "ตั้งค่าสลิป", "Payslip Settings", "Icons.Material.Filled.Receipt", 90),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/periods", "จัดการงวดเงินเดือน", "Payroll Periods", "Icons.Material.Filled.CalendarMonth", 100),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/salary-grades", "โครงสร้างเงินเดือน (Pay Grade)", "Salary Structure (Pay Grade)", "Icons.Material.Filled.BarChart", 110),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/lifecycle-task-templates", "Checklist การเริ่มงาน/ลาออก", "Onboarding/Offboarding Checklist", "Icons.Material.Filled.ChecklistRtl", 120),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/insurance-plans", "แผนประกันกลุ่ม", "Group Insurance Plans", "Icons.Material.Filled.HealthAndSafety", 130),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/welfare-fund-policy", "กองทุนสงเคราะห์ลูกจ้าง", "Employee Welfare Fund", "Icons.Material.Filled.Savings", 140),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/provident-fund-policy", "กองทุนสำรองเลี้ยงชีพ", "Provident Fund", "Icons.Material.Filled.Savings", 150),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/tax-deduction-config", "ตั้งค่าค่าลดหย่อนภาษี", "ตั้งค่าค่าลดหย่อนภาษี", "Icons.Material.Filled.RequestQuote", 160), // literal Thai label
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/employee-tax-deductions", "ค่าลดหย่อนภาษีพนักงาน", "ค่าลดหย่อนภาษีพนักงาน", "Icons.Material.Filled.PersonSearch", 170), // literal Thai label
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/prior-employer-income", "เงินได้จากนายจ้างเดิม", "เงินได้จากนายจ้างเดิม", "Icons.Material.Filled.MoveToInbox", 180), // literal Thai label
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/provident-fund-rate-requests", "คำขอเปลี่ยนอัตรากองทุนสำรอง", "คำขอเปลี่ยนอัตรากองทุนสำรอง", "Icons.Material.Filled.SwapHoriz", 190), // literal Thai label
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/provident-fund-exit-cases", "ปิดสมาชิกภาพกองทุนสำรอง", "ปิดสมาชิกภาพกองทุนสำรอง", "Icons.Material.Filled.ExitToApp", 200), // literal Thai label
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/admin/document-expiry", "เอกสารใกล้หมดอายุ", "Expiring Documents", "Icons.Material.Filled.EventBusy", 210),
        new("GRP_PAYROLL", "PAY_REPORTS", "/pay/reports/labor-cost-trend", "แนวโน้มต้นทุนแรงงาน", "Labor Cost Trend", "Icons.Material.Filled.TrendingUp", 220),
        new("GRP_PAYROLL", "PAY_REPORTS", "/pay/reports/pay-item-breakdown", "สัดส่วนรายรับ-รายหัก", "Earnings-Deductions Breakdown", "Icons.Material.Filled.PieChart", 230),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/withholding-cert", "หนังสือรับรองหักภาษี 50 ทวิ", "Withholding Tax Certificate (50 Tawi)", "Icons.Material.Filled.Receipt", 240),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/por1", "ภ.ง.ด.1 / ภ.ง.ด.1ก", "PND.1 / PND.1A", "Icons.Material.Filled.RequestPage", 250),
        new("GRP_PAYROLL", "PAY_ADMIN", "/pay/salary-cert", "หนังสือรับรองเงินเดือน", "Salary Certificate", "Icons.Material.Filled.Description", 260),
        new("GRP_PAYROLL", "PAY_REPORTS", "/pay/dashboard", "แดชบอร์ดเงินเดือน", "Payroll Dashboard", "Icons.Material.Filled.Dashboard", 270),

        // ---- GRP_CONTRACTS (group guarded by Has("CT_CONTRACT_ADMIN")) ----
        new("GRP_CONTRACTS", "CT_CONTRACT_ADMIN", "/contracts", "รายการสัญญา", "Contract List", "Icons.Material.Filled.Description", 10),
        new("GRP_CONTRACTS", "CT_CONTRACT_ADMIN", "/contracts/expiring", "สัญญาใกล้หมดอายุ", "Expiring Contracts", "Icons.Material.Filled.EventBusy", 20),
        new("GRP_CONTRACTS", "CT_CONTRACT_ADMIN", "/contracts/currencies", "สกุลเงิน", "Currencies", "Icons.Material.Filled.AttachMoney", 30),
        new("GRP_CONTRACTS", "CT_CONTRACT_ADMIN", "/contracts/warranty-types", "ประเภทการรับประกัน", "Warranty Types", "Icons.Material.Filled.VerifiedUser", 40),

        // ---- GRP_ANNOUNCE (group guarded by HasAny("HR_ANNOUNCE_ACCESS","HR_ANNOUNCE_ADMIN")) ----
        // Duplicate Url: /hr/announcements also appears under GRP_ESS above — kept both per extraction rules; seeder decides.
        new("GRP_ANNOUNCE", "HR_ANNOUNCE_ACCESS", "/hr/announcements", "หน้าประชาสัมพันธ์", "Announcements Board", "Icons.Material.Filled.Article", 10),
        new("GRP_ANNOUNCE", "HR_ANNOUNCE_ADMIN", "/hr/announcements/admin", "จัดการประกาศ", "Manage Announcements", "Icons.Material.Filled.EditNote", 20),

        // ---- GRP_EMP_RELATIONS (group guarded by HasAny("HR_DISCIPLINE_ADMIN","HR_GRIEVANCE_ADMIN","HR_REWARD_ADMIN")) ----
        new("GRP_EMP_RELATIONS", "HR_DISCIPLINE_ADMIN", "/hr/disciplinary", "วินัยพนักงาน", "Employee Discipline", "Icons.Material.Filled.Gavel", 10),
        new("GRP_EMP_RELATIONS", "HR_GRIEVANCE_ADMIN", "/hr/grievances", "เรื่องร้องเรียน (HR)", "Grievances (HR)", "Icons.Material.Filled.ReportProblem", 20),
        new("GRP_EMP_RELATIONS", "HR_REWARD_ADMIN", "/hr/reward", "รางวัลพนักงาน", "Employee Rewards", "Icons.Material.Filled.EmojiEvents", 30),

        // ---- GRP_EXPENSE (group guarded by Has("EXP_ADMIN")) ----
        new("GRP_EXPENSE", "EXP_ADMIN", "/exp/admin/categories", "ประเภทค่าใช้จ่าย", "Expense Categories", "Icons.Material.Filled.Category", 10),
        new("GRP_EXPENSE", "EXP_ADMIN", "/exp/admin/claims", "ใบเบิกทั้งหมด", "All Claims", "Icons.Material.Filled.FactCheck", 20),

        // ---- GRP_JOBCOMP (group guarded by Has("JOBCOMP_ADMIN")) ----
        new("GRP_JOBCOMP", "JOBCOMP_ADMIN", "/job/families", "สายอาชีพ (Job Family)", "Job Families", "Icons.Material.Filled.AccountTree", 10),
        new("GRP_JOBCOMP", "JOBCOMP_ADMIN", "/job/levels", "ระดับสายอาชีพ (Job Level)", "Job Levels", "Icons.Material.Filled.Stairs", 20),
        new("GRP_JOBCOMP", "JOBCOMP_ADMIN", "/competency/categories", "หมวดสมรรถนะ", "Competency Categories", "Icons.Material.Filled.Category", 30),
        new("GRP_JOBCOMP", "JOBCOMP_ADMIN", "/competency/library", "คลังสมรรถนะ", "Competency Library", "Icons.Material.Filled.MenuBook", 40),

        // ---- GRP_PERF (group guarded by HasAny("PERF_ADMIN","PERF_ACCESS")) ----
        new("GRP_PERF", "PERF_ADMIN", "/perf/periods", "รอบการประเมิน", "Evaluation Periods", "Icons.Material.Filled.DateRange", 10),
        new("GRP_PERF", "PERF_ADMIN", "/perf/evaluation-types", "ประเภทแบบประเมิน", "Evaluation Types", "Icons.Material.Filled.Description", 20),
        new("GRP_PERF", "PERF_ADMIN", "/perf/rater-directions", "ทิศทางการประเมิน", "Rater Directions", "Icons.Material.Filled.CompareArrows", 30),
        new("GRP_PERF", "PERF_ADMIN", "/perf/assignments", "มอบหมายการประเมิน", "Evaluation Assignments", "Icons.Material.Filled.AssignmentInd", 40),
        new("GRP_PERF", "PERF_ADMIN", "/perf/goals", "เป้าหมาย OKR", "OKR Goals", "Icons.Material.Filled.Flag", 50),
        new("GRP_PERF", "PERF_ADMIN", "/perf/calibration", "Calibration การกลั่นกรองคะแนน", "Score Calibration", "Icons.Material.Filled.Tune", 60),
        new("GRP_PERF", "PERF_ADMIN", "/perf/pip", "Performance Improvement Plan (PIP)", "Performance Improvement Plan (PIP)", "Icons.Material.Filled.TrendingDown", 70),
        new("GRP_PERF", "PERF_ADMIN", "/perf/hr-dashboard", "ภาพรวม (HR Dashboard)", "Overview (HR Dashboard)", "Icons.Material.Filled.Dashboard", 80),
        new("GRP_PERF", "PERF_ACCESS", "/perf/my-evaluations", "งานประเมินของฉัน", "My Evaluations", "Icons.Material.Filled.RateReview", 90),

        // ---- GRP_OKR (group guarded by Has("OKR_ADMIN")) ----
        new("GRP_OKR", "OKR_ADMIN", "/okr/cycles", "วงจร OKR", "OKR Cycles", "Icons.Material.Filled.DateRange", 10),
        new("GRP_OKR", "OKR_ADMIN", "/okr/categories", "หมวดกลยุทธ์", "Strategic Categories", "Icons.Material.Filled.Category", 20),
        new("GRP_OKR", "OKR_ADMIN", "/okr/tree", "ต้นไม้ OKR ทั้งบริษัท", "Company-wide OKR Tree", "Icons.Material.Filled.AccountTree", 30),
        new("GRP_OKR", "OKR_ADMIN", "/okr/dashboard", "แดชบอร์ด OKR", "OKR Dashboard", "Icons.Material.Filled.Dashboard", 40),

        // ---- GRP_TALENT (group guarded by HasAny("TALENT_ADMIN","TALENT_ACCESS")) ----
        new("GRP_TALENT", "TALENT_ACCESS", "/talent/team-rating", "ให้คะแนนศักยภาพทีม", "Rate Team Potential", "Icons.Material.Filled.Star", 10),
        new("GRP_TALENT", "TALENT_ADMIN", "/talent/nine-box", "9-Box Grid ทั้งบริษัท", "Company-wide 9-Box Grid", "Icons.Material.Filled.GridOn", 20),
        new("GRP_TALENT", "TALENT_ADMIN", "/talent/pool", "Talent Pool", "Talent Pool", "Icons.Material.Filled.EmojiEvents", 30),

        // ---- GRP_IDP (group guarded by HasAny("IDP_ACCESS","IDP_ADMIN")) ----
        new("GRP_IDP", "IDP_ACCESS", "/idp/my-plans", "สมรรถนะ+แผนของฉัน", "My Competencies & Plan", "Icons.Material.Filled.Person", 10),
        new("GRP_IDP", "IDP_ACCESS", "/idp/team", "ทีมของฉัน", "My Team", "Icons.Material.Filled.Group", 20),
        new("GRP_IDP", "IDP_ADMIN", "/idp/hr-overview", "ภาพรวม HR", "HR Overview", "Icons.Material.Filled.Dashboard", 30),

        // ---- GRP_CAREER (group guarded by HasAny("CAREER_ACCESS","CAREER_ADMIN")) ----
        new("GRP_CAREER", "CAREER_ACCESS", "/career/my-path", "เส้นทางความก้าวหน้าของฉัน", "My Career Path", "Icons.Material.Filled.Timeline", 10),
        new("GRP_CAREER", "CAREER_ACCESS", "/career/explorer", "สำรวจเส้นทางความก้าวหน้า", "Career Path Explorer", "Icons.Material.Filled.AccountTree", 20),
        new("GRP_CAREER", "CAREER_ACCESS", "/career/internal-jobs", "โอกาสเติบโตภายใน", "Internal Growth Opportunities", "Icons.Material.Filled.SwapHoriz", 30),
        new("GRP_CAREER", "CAREER_ADMIN", "/career/paths", "จัดการ Career Path", "Manage Career Paths", "Icons.Material.Filled.EditRoad", 40),

        // ---- GRP_SUCCESSION (group guarded by Has("SUCC_ADMIN")) ----
        new("GRP_SUCCESSION", "SUCC_ADMIN", "/succession/key-positions", "ตำแหน่งสำคัญ", "Key Positions", "Icons.Material.Filled.Shield", 10),
        new("GRP_SUCCESSION", "SUCC_ADMIN", "/succession/bench-strength", "Bench Strength", "Bench Strength", "Icons.Material.Filled.Insights", 20),

        // ---- GRP_LMS (group guarded by Has("LMS_ADMIN")) ----
        new("GRP_LMS", "LMS_ADMIN", "/lms/dashboard", "ภาพรวม", "Overview", "Icons.Material.Filled.Dashboard", 10),
        new("GRP_LMS", "LMS_ADMIN", "/lms/categories", "หมวดหมู่หลักสูตร", "Course Categories", "Icons.Material.Filled.Category", 20),
        new("GRP_LMS", "LMS_ADMIN", "/lms/courses", "คลังหลักสูตร", "Course Catalog", "Icons.Material.Filled.MenuBook", 30),
        new("GRP_LMS", "LMS_ADMIN", "/lms/enrollments", "ผู้ลงทะเบียนอบรม", "Enrollments", "Icons.Material.Filled.HowToReg", 40),
        new("GRP_LMS", "LMS_ADMIN", "/lms/mandatory-compliance", "ติดตามคอร์สบังคับ", "Mandatory Training", "Icons.Material.Filled.FactCheck", 50),
        new("GRP_LMS", "LMS_ADMIN", "/lms/training-needs", "ความต้องการฝึกอบรม", "Training Needs", "Icons.Material.Filled.Assignment", 60),
        new("GRP_LMS", "LMS_ADMIN", "/lms/training-budget", "งบประมาณฝึกอบรม", "Training Budget", "Icons.Material.Filled.AccountBalanceWallet", 70),

        // ---- GRP_KM (group guarded by HasAny("KM_ACCESS","KM_ADMIN")) ----
        new("GRP_KM", "KM_ACCESS", "/km/articles-list", "คลังความรู้", "Knowledge Base", "Icons.Material.Filled.LibraryBooks", 10),
        new("GRP_KM", "KM_ACCESS", "/km/experts", "ผู้เชี่ยวชาญภายในองค์กร", "Internal Experts", "Icons.Material.Filled.Person", 20),
        new("GRP_KM", "KM_ADMIN", "/km/categories", "หมวดหมู่ความรู้", "Knowledge Categories", "Icons.Material.Filled.Category", 30),
        new("GRP_KM", "KM_ADMIN", "/km/articles", "จัดการบทความ", "Manage Articles", "Icons.Material.Filled.Edit", 40),

        // ---- GRP_ENGAGEMENT (group guarded by Has("ENG_ADMIN")) ----
        new("GRP_ENGAGEMENT", "ENG_ADMIN", "/eng/dashboard", "ภาพรวม", "Overview", "Icons.Material.Filled.Dashboard", 10),
        new("GRP_ENGAGEMENT", "ENG_ADMIN", "/eng/campaigns", "แคมเปญ Survey/Pulse/eNPS", "Survey/Pulse/eNPS Campaigns", "Icons.Material.Filled.Poll", 20),
        new("GRP_ENGAGEMENT", "ENG_ADMIN", "/eng/question-bank", "คลังคำถาม", "Question Bank", "Icons.Material.Filled.QuestionAnswer", 30),
        new("GRP_ENGAGEMENT", "ENG_ADMIN", "/eng/action-plans", "แผนปฏิบัติการ", "Action Plans", "Icons.Material.Filled.Checklist", 40),

        // ---- GRP_ORGDEV (group guarded by Has("ORGDEV_ADMIN")) ----
        new("GRP_ORGDEV", "ORGDEV_ADMIN", "/orgdev/dashboard", "ภาพรวมสุขภาพองค์กร", "Org Health Overview", "Icons.Material.Filled.Dashboard", 10),
        new("GRP_ORGDEV", "ORGDEV_ADMIN", "/orgdev/workforce-plan", "แผนอัตรากำลัง", "Workforce Plan", "Icons.Material.Filled.People", 20),
        new("GRP_ORGDEV", "ORGDEV_ADMIN", "/orgdev/leadership-development", "พัฒนาผู้นำ", "Leadership Development", "Icons.Material.Filled.School", 30),
        new("GRP_ORGDEV", "ORGDEV_ADMIN", "/orgdev/change-initiatives", "โครงการเปลี่ยนแปลง", "Change Initiatives", "Icons.Material.Filled.Autorenew", 40),
        new("GRP_ORGDEV", "ORGDEV_ADMIN", "/orgdev/culture-assessment", "ประเมินวัฒนธรรมองค์กร", "Culture Assessment", "Icons.Material.Filled.Favorite", 50),

        // ---- GRP_WF_ENGINE (group itself ungated; per-link gates inside) ----
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/canvas", "ภาพรวม Workflow (Canvas)", "Workflow Overview (Canvas)", "Icons.Material.Filled.ViewKanban", 10),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/workflows", "จัดการ Workflow", "Manage Workflows", "Icons.Material.Filled.Route", 20),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wfworkflows", "Workflow (CRUD)", "Workflow (CRUD)", "Icons.Material.Filled.Route", 30), // legacy scaffold CRUD, superseded by /wf/workflows
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/sub-workflow-levels", "จัดการระดับการอนุมัติ", "Approval Levels", "Icons.Material.Filled.Layers", 40),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/custom-users", "ผู้อนุมัติเจาะจง (Custom User)", "Specific Approvers (Custom User)", "Icons.Material.Filled.Person", 50),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/custom-roles", "ผู้อนุมัติตาม Role", "Approvers by Role", "Icons.Material.Filled.Badge", 60),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/loa", "ช่วงวงเงินอนุมัติ (LOA)", "Approval Limit Ranges (LOA)", "Icons.Material.Filled.Payments", 70),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/loa-users", "ผู้อนุมัติตามวงเงิน", "Approvers by Limit", "Icons.Material.Filled.PersonPin", 80),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/adhoc-users", "ผู้อนุมัติเฉพาะกิจ", "Ad-hoc Approvers", "Icons.Material.Filled.PersonAdd", 90),
        new("GRP_WF_ENGINE", null, "/wf/my-inbox", "งานรออนุมัติของฉัน", "My Pending Approvals", "Icons.Material.Filled.Inbox", 100), // TODO ungated today (ESS-personalized approval inbox, self-scoped by userid)
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/wf/vacant-approvals", "ตำแหน่งผู้อนุมัติว่าง", "Vacant Approver Positions", "Icons.Material.Filled.PersonSearch", 110),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/ot-requests", "คำขอทำงานล่วงเวลา (OT)", "Overtime Requests", "Icons.Material.Filled.MoreTime", 120),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/leave-requests/on-behalf", "ลา (แทนพนักงาน)", "Leave (On Behalf)", "Icons.Material.Filled.PersonAddAlt", 130),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/leave-requests/policy", "สิทธิการลา", "Leave Policy", "Icons.Material.Filled.Rule", 140),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/leave-requests/leave-types", "ตั้งค่าประเภทการลา", "Leave Type Settings", "Icons.Material.Filled.Category", 150),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/leave-requests/holidays", "ปฏิทินวันหยุดบริษัท", "Company Holiday Calendar", "Icons.Material.Filled.CalendarMonth", 160),
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/leave-requests/block-leave-policy", "นโยบาย Block Leave", "นโยบาย Block Leave", "Icons.Material.Filled.Block", 170), // literal Thai label
        new("GRP_WF_ENGINE", "WF_WORKFLOW_ADMIN", "/leave-requests/block-leave-compliance", "รายงาน Block Leave", "รายงาน Block Leave", "Icons.Material.Filled.FactCheck", 180), // literal Thai label

        // ---- GRP_WF_SCAFFOLD (group guarded by HasAny("WF_EMPLOYEE_ADMIN","WF_ORG_TYPE_ADMIN")) ----
        new("GRP_WF_SCAFFOLD", "WF_EMPLOYEE_ADMIN", "/wf/employees", "จัดการพนักงาน (wf_employee)", "Manage Employees (wf_employee)", "Icons.Material.Filled.Badge", 10),
        new("GRP_WF_SCAFFOLD", "WF_ORG_TYPE_ADMIN", "/wf/org-types", "จัดการประเภทหน่วยงาน (wf_org_type)", "Manage Org Types (wf_org_type)", "Icons.Material.Filled.Business", 20),

        // ---- GRP_HR_ANALYTICS (group guarded by Has("HR_ANALYTICS")) ----
        new("GRP_HR_ANALYTICS", "HR_ANALYTICS", "/hr/analytics", "ภาพรวม Analytics", "Analytics Overview", "Icons.Material.Filled.Dashboard", 10),
        new("GRP_HR_ANALYTICS", "HR_ANALYTICS", "/hr/reports/headcount", "อัตรากำลัง (Headcount)", "Headcount & Workforce", "Icons.Material.Filled.Groups", 20),
        new("GRP_HR_ANALYTICS", "HR_ANALYTICS", "/hr/reports/turnover", "อัตราการลาออก (Turnover)", "Turnover Rate", "Icons.Material.Filled.TrendingDown", 30),
        new("GRP_HR_ANALYTICS", "HR_ANALYTICS", "/hr/reports/absenteeism", "รายงานการขาดงาน (Absenteeism)", "Absenteeism Report", "Icons.Material.Filled.EventBusy", 40),

        // ---- GRP_SYS_ADMIN (group guarded by Has("SYS_ADMIN")) ----
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/menus", "จัดการเมนู", "Menu Management", "Icons.Material.Filled.AccountTree", 10),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/roles", "จัดการกลุ่มผู้ใช้", "User Group Management", "Icons.Material.Filled.Groups", 20),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/users", "จัดการผู้ใช้", "User Management", "Icons.Material.Filled.ManageAccounts", 30),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/permissions", "จัดการสิทธิ์การใช้ระบบ", "Permission Management", "Icons.Material.Filled.Security", 40),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/role-scopes", "ขอบเขตข้อมูล (Data Scope)", "Data Scope", "Icons.Material.Filled.Domain", 50),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/user-sessions", "เซสชันการเข้าใช้งาน", "User Sessions", "Icons.Material.Filled.DevicesOther", 60),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/document-types", "ประเภทเอกสาร (Document Types)", "Document Types", "Icons.Material.Filled.Description", 70),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/languages", "ตั้งค่าภาษา (Language Settings)", "Language Settings", "Icons.Material.Filled.Language", 80),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/system/sso-roles", "สิทธิ์ผู้ใช้สำหรับระบบภายนอก (SSO)", "External App Roles (SSO)", "Icons.Material.Filled.Key", 90),
        new("GRP_SYS_ADMIN", "SYS_ADMIN", "/admin/workflow-design", "WorkflowDesign (ออกแบบหน้าจอ)", "WorkflowDesign (Screen Designer)", "Icons.Material.Filled.DesignServices", 100),

        // ---- GRP_LEGACY (group itself ungated; targets are unauthorized-by-design or dead routes) ----
        new("GRP_LEGACY", null, "/income", "Income Mng.", "Income Mng.", "Icons.Material.Filled.AttachMoney", 10), // TODO ungated today
        new("GRP_LEGACY", null, "/masterdata", "Master Data", "Master Data", "Icons.Material.Filled.Storage", 20), // TODO ungated today
        new("GRP_LEGACY", null, "/ot", "OT", "OT", "Icons.Material.Filled.MoreTime", 30), // TODO ungated today
        new("GRP_LEGACY", null, "/sc_users", "User Mng", "User Mng", "Icons.Material.Filled.ManageAccounts", 40), // TODO ungated today
        new("GRP_LEGACY", null, "/onlinereport", "report", "report", "Icons.Material.Filled.Description", 50), // TODO ungated today
        new("GRP_LEGACY", null, "/hrpayrolls", "hrpayrolls", "hrpayrolls", "Icons.Material.Filled.Folder", 60), // TODO ungated today
        new("GRP_LEGACY", "SYS_AUDIT_LOG", "/admin/audit-log", "Audit Log ระบบ", "System Audit Log", "Icons.Material.Filled.History", 70),
    };
}
