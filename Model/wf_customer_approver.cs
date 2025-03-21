using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("wf_customer_approver")]
public partial class wf_customer_approver
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long subworkflowid { get; set; }

    public long id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? modate { get; set; }

    public long? modby { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    public bool? isactive { get; set; }

    public long? workflowid { get; set; }

    [StringLength(50)]
    
    public string? wlevel { get; set; }

    public int? emplevel { get; set; }

    [StringLength(50)]
    
    public string? poscode { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }
}
