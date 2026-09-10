using HRM.Models;
using HRM.Services.Workflow;
using Xunit;

namespace HRM.Tests.Workflow;

// "กรองแค่ jobstatus = PENDING ยังไม่ถูก ถ้ามีผู้อนุมัติหลายคน คุณจะอ่านผิดตลอด
//  แม้ว่า workflow เลื่อนไปแล้ว" — CEO, 10 ก.ย. 2569
//
// เมื่อขั้นหนึ่งมีผู้อนุมัติหลายคนแล้วขั้นนั้นครบเงื่อนไขจนงานเดินต่อ ใบของคนที่
// เหลือถูกปลด isLast=false แต่ jobstatus ยังค้าง PENDING ตลอดไป (ทั้งสอง engine
// ปลดแค่ isLast) ใครอ่านด้วย PENDING อย่างเดียวจึงเห็นงานที่เดินผ่านไปแล้วเป็น
// "งานค้าง" ตลอด — กล่องงาน, pool inbox, กระดิ่งแจ้งเตือน และคำตอบว่า "ตอนนี้ใคร
// ค้างอนุมัติ" ล้วนอ่านผิดพร้อมกันหมด
//
// IsLiveApprovalRow คือกฎกลางที่ทุกจุดใช้ร่วมกัน เทสนี้ปักหมุดพฤติกรรมของมัน
// โดยเฉพาะสองเคสที่ตรงข้ามกันและพลาดกันบ่อย:
//   - ขั้นที่ยังเปิดอยู่และมีหลายคน  -> คนที่เหลือต้อง "ยังเห็น" (ห้ามหาย)
//   - ขั้นที่งานเดินผ่านไปแล้ว        -> คนที่เหลือต้อง "หายไป"
public class WorkflowLiveApprovalRowTests
{
    private static readonly Func<job_user_list, bool> IsLive =
        WorkflowEngineService.IsLiveApprovalRow.Compile();

    private static job_user_list Row(job_master job, int wlevel, string status, bool isLast, int? jobseq = null)
        => new()
        {
            jobmasterid = job.jobmasterid,
            jobmaster = job,
            wlevel = wlevel,
            jobstatus = status,
            isLast = isLast,
            jobseq = jobseq,
        };

    private static job_master Job(int lastLevel, bool closed = false, int? jobseq = null)
        => new() { jobmasterid = 1, lastLevel = lastLevel, isJobClosed = closed, jobseq = jobseq };

    [Fact]
    public void Pending_row_at_the_level_the_job_is_on_is_live()
    {
        var job = Job(lastLevel: 2);
        Assert.True(IsLive(Row(job, 2, WorkflowEngineService.StatusPending, isLast: true)));
    }

    // ขั้นเดียวมีผู้อนุมัติสามคน คนแรกกดไปแล้ว งานยังไม่ครบเงื่อนไขจึงยังอยู่ขั้นเดิม
    // อีกสองคนต้องยังเห็นงานอยู่ ไม่งั้นงานค้างโดยไม่มีใครรู้
    [Fact]
    public void Remaining_approvers_on_an_open_multi_approver_level_stay_visible()
    {
        var job = Job(lastLevel: 1);
        var acted = Row(job, 1, WorkflowEngineService.StatusApproved, isLast: false);
        var stillWaitingA = Row(job, 1, WorkflowEngineService.StatusPending, isLast: true);
        var stillWaitingB = Row(job, 1, WorkflowEngineService.StatusPending, isLast: true);

        Assert.False(IsLive(acted));
        Assert.True(IsLive(stillWaitingA));
        Assert.True(IsLive(stillWaitingB));
    }

    // เคสหลักที่ CEO ชี้: ขั้น 1 มีสามคน พอครบเงื่อนไขงานเดินไปขั้น 2 ใบของอีกสองคน
    // ถูกปลด isLast แต่ jobstatus ยังเป็น PENDING — ต้องไม่ถูกนับเป็นงานค้างอีก
    [Fact]
    public void Leftover_pending_rows_from_a_level_the_job_has_left_are_not_live()
    {
        var job = Job(lastLevel: 2);
        var leftBehindA = Row(job, 1, WorkflowEngineService.StatusPending, isLast: false);
        var leftBehindB = Row(job, 1, WorkflowEngineService.StatusPending, isLast: false);

        Assert.False(IsLive(leftBehindA));
        Assert.False(IsLive(leftBehindB));
    }

    // ป้องกันการถอยหลัง: ถ้ามีคนเผลอเอา jobseq กลับเข้ามาเป็นเงื่อนไข ขั้นที่มีหลายคน
    // จะพังทันทีใน engine ใหม่ เพราะ jobseq ของ job เดินทุก action ("stamp ทุกครั้ง
    // ที่มี workflow action") ใบของคนที่เหลือออกตอน jobseq เก่าจึงไม่มีวันตรงกัน
    [Fact]
    public void Row_issued_before_the_job_seq_advanced_is_still_live()
    {
        var job = Job(lastLevel: 1, jobseq: 5);   // คนแรกกดอนุมัติไปแล้ว jobseq เดินไป 5
        var issuedEarlier = Row(job, 1, WorkflowEngineService.StatusPending, isLast: true, jobseq: 3);

        Assert.True(IsLive(issuedEarlier));
    }

    [Fact]
    public void Rows_of_a_closed_job_are_never_live()
    {
        var job = Job(lastLevel: 1, closed: true);
        Assert.False(IsLive(Row(job, 1, WorkflowEngineService.StatusPending, isLast: true)));
    }

    [Theory]
    [InlineData("APPROVED")]
    [InlineData("REJECTED")]
    [InlineData("RETURNED")]
    [InlineData("CANCELLED")]
    public void Non_pending_rows_are_never_live(string status)
    {
        var job = Job(lastLevel: 1);
        Assert.False(IsLive(Row(job, 1, status, isLast: true)));
    }
}
