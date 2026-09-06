using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Attendance-module additions kept out of the scaffolded HRMContext.cs.
public partial class HRMContext
{
    // HR gap wave 3: attendance correction requests (approved via workflow).
    public virtual DbSet<Att_CorrectionRequest> Att_CorrectionRequests { get; set; }
}
