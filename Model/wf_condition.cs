using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_condition")]
public partial class wf_condition
{
    public long wfcondiionid { get; set; }

    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long subworkflowid { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(50)]
    
    public string approveLevel { get; set; } = null!;

    [StringLength(250)]
    
    public string? con_type { get; set; }

    [StringLength(50)]
    
    public string? wlevel { get; set; }

    [StringLength(50)]
    
    public string? conditionLower { get; set; }

    [Column(TypeName = "decimal(20, 4)")]
    public decimal? con_value_lower { get; set; }

    [Column(TypeName = "decimal(20, 4)")]
    public decimal? con_value_upper { get; set; }

    [StringLength(50)]
    
    public string? conditionUpper { get; set; }

    [StringLength(2)]
    
    public string? isforcecheck { get; set; }

    public bool? isactive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(50)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? reftable { get; set; }

    public long? refid { get; set; }

    [StringLength(50)]
    
    public string? value_type { get; set; }

    public long? eff_subworkflowid { get; set; }

    [StringLength(50)]
    
    public string? eff_wlevel { get; set; }

    public long? workflowid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    [StringLength(250)]
    
    public string? types { get; set; }
}
