using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("emp_overtime_request")]
public partial class emp_overtime_request
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string empid { get; set; } = null!;

    [StringLength(250)]
    
    public string name { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime requestDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime starttime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime endtime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? workhour { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? workminute { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ot_rate { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(50)]
    
    public string? approveby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approvedate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? real_starttime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? real_endtime { get; set; }

    [StringLength(1000)]
    public string? objective { get; set; }

    [StringLength(1000)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    [StringLength(500)]
    public string? orgname { get; set; }

    [StringLength(500)]
    public string? com_code_all { get; set; }
}
