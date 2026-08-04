namespace HRM.Models;

public enum PerfPeriodType
{
    Annual = 1,     // ประจำปี
    SemiAnnual = 2, // ครึ่งปี
    Probation = 3,  // ทดลองงาน
    Custom = 4,     // กำหนดเอง
}

public enum PerfTargetScope
{
    Organization = 1, // ทั้งหน่วยงาน (รวมหน่วยงานย่อย)
    Position = 2,      // ตามชื่อตำแหน่ง (Pos_ExecType)
    Employee = 3,      // รายบุคคล
}

public enum PerfInstanceStatus
{
    Draft = 1,          // เพิ่งสร้าง ยังไม่มีใครให้คะแนน
    InProgress = 2,      // มีคนให้คะแนนแล้วบางส่วน
    PendingApproval = 3, // ให้คะแนนครบ คำนวณผลรวมแล้ว รออนุมัติ
    Approved = 4,
    Rejected = 5,
    Cancelled = 6,
}

// สอดคล้องกับ EvalDirection ของระบบเดิม — self, สายบังคับบัญชาขึ้น 3 ชั้น,
// สายลูกน้องลง 3 ชั้น, peer (ระดับเดียวกัน)
public enum PerfRaterDirection
{
    Self = 0,
    Superior1 = 1,
    Superior2 = 2,
    Superior3 = 3,
    Subordinate1 = 11,
    Subordinate2 = 12,
    Subordinate3 = 13,
    Peer = 20,
}

public enum PerfRaterStatus
{
    Pending = 1,
    Submitted = 2,
    // ไม่มีคนอยู่ในตำแหน่งนั้นจริง (เช่น ไม่มีหัวหน้าขั้นที่ 3 เพราะสายสั้นกว่านั้น
    // หรือ com_organization.approver_empid ยังไม่ได้ตั้งค่า) — ข้ามไปเฉยๆ ไม่ error
    Skipped = 3,
}

// จาก EvalSumEdit.jsp เดิม: "พนักงานผู้รับการประเมิน มีจริยธรรม/ไม่มีจริยธรรม/มีจริยธรรมแต่ต้องปรับปรุง"
public enum PerfEthicsRating
{
    Ethical = 1,           // มีจริยธรรม
    NeedsImprovement = 2,  // มีจริยธรรม แต่ต้องปรับปรุง
    Unethical = 3,          // ไม่มีจริยธรรม
}

public enum PerfGoalOwnerType
{
    Company = 1,
    Organization = 2,
    Employee = 3,
}

public enum PerfGoalStatus
{
    NotStarted = 1,
    OnTrack = 2,
    AtRisk = 3,
    Completed = 4,
}
