using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Keyless]
[Table("HRUCFSALARYITEM")]
[Index("SalitemCode", Name = "HRUCFSALARYITEM_X", IsUnique = true)]
public partial class Hrucfsalaryitem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long ID { get; set; }

    [Column("companyid")]
    [StringLength(6)]
    public string companyid { get; set; } = null!;

    [Column("SALITEM_CODE")]
    [StringLength(3)]
    
    public string? SalitemCode { get; set; }

    [Column("SALITEM_DESC")]
    [StringLength(100)]
    
    public string? SalitemDesc { get; set; }

    [Column("SIGN_FLAG")]
    [Precision(2)]
    public Int32? SignFlag { get; set; }

    [Column("MANUAL_FLAG")]
    public Int32? ManualFlag { get; set; }
}
