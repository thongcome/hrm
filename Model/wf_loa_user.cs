using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Keyless]
[Table("wf_loa_user")]
public partial class wf_loa_user
{
    public long id { get; set; }

    public long? loaid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    public long? modby { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    public bool isactive { get; set; }

    public long workflowid { get; set; }

    public int wlevel { get; set; }

    public long userid { get; set; }

    [StringLength(50)]
    
    public string? empid { get; set; }

    [StringLength(250)]
    
    public string? poscode { get; set; }

    [StringLength(250)]
    
    public string? orgcode { get; set; }

    [StringLength(250)]
    
    public string? costcenter { get; set; }

    [StringLength(50)]
    
    public string? emplevel { get; set; }

    [StringLength(50)]
    
    public string? orgrole { get; set; }
}
