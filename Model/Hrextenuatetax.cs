using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("HREXTENUATETAX")]
public partial class Hrextenuatetax
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    [Column("ID")]
    public long id { get; set; }

    [Column("companyid")]
    [StringLength(6)]
    
    public string companyid { get; set; } = null!;

   
    [Column("EMP_NO")]
    [StringLength(6)]
    
    public string EmpNo { get; set; } = null!;

    [Column("PAY_TRAVEL", TypeName = "decimal(10,2)")]
    public decimal? PayTravel { get; set; }

    [Column("PAY_CHILD", TypeName = "decimal(10,2)")]
    public decimal? PayChild { get; set; }

    [Column("PAY_PARENT", TypeName = "decimal(10,2)")]
    public decimal? PayParent { get; set; }

    [Column("PAY_INTADDR", TypeName = "decimal(10,2)")]
    public decimal? PayIntaddr { get; set; }

    [Column("PAY_INSOTH", TypeName = "decimal(10,2)")]
    public decimal? PayInsoth { get; set; }

    [Column("PAY_INSRETRY", TypeName = "decimal(10,2)")]
    public decimal? PayInsretry { get; set; }

    [Column("PAY_DISABLED", TypeName = "decimal(10,2)")]
    public decimal? PayDisabled { get; set; }

    [Column("PAY_INSPARENT", TypeName = "decimal(10,2)")]
    public decimal? PayInsparent { get; set; }

    [Column("PAY_MATE", TypeName = "decimal(10,2)")]
    public decimal? PayMate { get; set; }

    [Column("PAY_LTF", TypeName = "decimal(10,2)")]
    public decimal? PayLtf { get; set; }

    [Column("PAY_RMF", TypeName = "decimal(10,2)")]
    public decimal? PayRmf { get; set; }

    [Column("PAY_DONATEOTH", TypeName = "decimal(10,2)")]
    public decimal? PayDonateoth { get; set; }

    [Column("PAY_DONATEEDU", TypeName = "decimal(10,2)")]
    public decimal? PayDonateedu { get; set; }
}
