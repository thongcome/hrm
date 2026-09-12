using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Advance.Payroll.Domain;

 [Table("HRUCFSECURITY")]
public partial class Hrucfsecurity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    [Column("ID")]
    public long id { get; set; }

    
    [Column("companyid")]
    [StringLength(6)]
    
    public string companyid { get; set; } = null!;

    
    [Column("SECURITY_CODE")]
    [StringLength(2)]
    
    public string SecurityCode { get; set; } = null!;

    [Column("SECURITY_DESC")]
    [StringLength(50)]
    
    public string? SecurityDesc { get; set; }

    [Column("PERCEN_SECURITY", TypeName = "decimal(3,2)")]
    public decimal? PercenSecurity { get; set; }

    [Column("SECURITY_MONEY", TypeName = "decimal(10,2)")]
    public decimal? SecurityMoney { get; set; }

    [Column("OVER_SECURITY_MONEY", TypeName = "decimal(10,2)")]
    public decimal? OverSecurityMoney { get; set; }

    [Column("PERCENMG_SECURITY", TypeName = "decimal(3,2)")]
    public decimal? PercenmgSecurity { get; set; }

    // อัตราประกันสังคมส่วนนายจ้าง (%) — NULL = ใช้อัตราเดียวกับลูกจ้าง (audit M10, 11 ก.ย. 2569)
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? EmployerPercenSecurity { get; set; }


    // add new
    //[Column("AreaCode")]
    //[StringLength(50)]
    // public string? AreaCode { get; set; }


    //[Column("isFix", TypeName = "decimal(1)")]
    //public bool isFix { get; set; } = false;

    //[Column("FixAmount", TypeName = "decimal(15,2)")]
    //public decimal? FixAmount { get; set; } = 0.00m;
}
