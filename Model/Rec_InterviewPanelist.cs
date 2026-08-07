using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Rec_InterviewPanelist")]
public class Rec_InterviewPanelist
{
    [Key]
    public long Id { get; set; }

    public long InterviewId { get; set; }
    public long HremployeeId { get; set; }

    public bool IsLead { get; set; }
}
