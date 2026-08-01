using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("info_message")]
public class info_message
{
    [Key]
    public long Id { get; set; }

    [StringLength(200)]
    public string? subject { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    public bool? isactive { get; set; }

    [StringLength(1000)]
    public string? message { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
