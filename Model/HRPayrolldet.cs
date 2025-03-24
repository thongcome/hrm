using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

 [Table("HRPAYROLLDET")]
public partial class Hrpayrolldet
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    [Column("ID")]
    public long id { get; set; }

    [Column("companyid")]
    [StringLength(6)]
    
    public string companyid { get; set; } = null!;

    
    [Column("PAYROLLSLIP_NO")]
    [StringLength(12)]
    
    public string PayrollslipNo { get; set; } = null!;

  
    [Column("SEQ_NO")]
    public Int32 SeqNo { get; set; }


    [Column("EMP_NO")]
    [StringLength(6)]
    
    public string? EmpNo { get; set; }


    [Column("PAYROLL_PERIOD")]
    [StringLength(8)]
    
    public string? PayrollPeriod { get; set; }

    [Column("SALITEM_CODE")]
    [StringLength(3)]
    
    public string? SalitemCode { get; set; }

    [Column("DESCRIPTION")]
    [StringLength(200)]
    
    public string? Description { get; set; }

    [Column("ITEM_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? ItemAmt { get; set; }
}
