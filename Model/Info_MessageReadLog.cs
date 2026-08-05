using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Append-only event log — never overwritten/deduped — so "has this employee
// read it yet" is a cheap .Any() query while still keeping full history
// (view count, last-viewed date) for free. Mirrors the audit-log style used
// throughout this system (HRMContext.Audit.cs).
[Table("Info_MessageReadLog")]
[Index(nameof(InfoMessageId), nameof(HremployeeId))]
public class Info_MessageReadLog
{
    public long Id { get; set; }

    public long InfoMessageId { get; set; }

    // Soft-link -> Hremployee.id
    public long HremployeeId { get; set; }

    public InfoMessageReadAction Action { get; set; }

    // Soft-link -> doc_center.Id. Null when Action=Viewed (the announcement
    // body itself); set when Action=Downloaded to record which attachment.
    public long? DocCenterId { get; set; }

    public DateTime EventDate { get; set; } = DateTime.Now;
}
