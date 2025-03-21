using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Keyless]
[Table("HeadCode_V1")]
public partial class HeadCode_V1
{
    [StringLength(50)]
    
    public string? PRS_DEPT { get; set; }

    [StringLength(250)]
    
    public string? DPG_THAIDESC { get; set; }

    [StringLength(250)]
    
    public string? DEPT_THAIDESC { get; set; }
}
