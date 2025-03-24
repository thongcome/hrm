using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

 [Table("HRPAYROLL")]
public partial class Hrpayroll
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

    [Column("EMP_NO")]
    [StringLength(6)]
    
    public string? EmpNo { get; set; }

    [Column("PAYROLL_DATE", TypeName = "DATE")]
    public DateTime? PayrollDate { get; set; }

    [Column("PAYROLL_PERIOD")]
    [StringLength(8)]
    
    public string? PayrollPeriod { get; set; }

    [Column("SALARYBASE_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? SalarybaseAmt { get; set; }

    [Column("SALARYOTH_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? SalaryothAmt { get; set; }

    [Column("SALARYSUBT_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? SalarysubtAmt { get; set; }

    [Column("SALARYNET_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? SalarynetAmt { get; set; }

    //[Column("PAYROLL_STATUS")]
    //[Precision(2)]
    //public decimal? PayrollStatus { get; set; }

    [Column("PAYROLL_STATUS", TypeName = "NUMBER(2,0)")]
    public int? PayrollStatus { get; set; }


    [Column("EXPENSE_CODE")]
    [StringLength(3)]
    
    public string? ExpenseCode { get; set; }

    [Column("EXPENSE_BANK")]
    [StringLength(3)]
    

    public string? ExpenseBank { get; set; }

    [Column("EXPENSE_BRANCH")]
    [StringLength(4)]
    
    public string? ExpenseBranch { get; set; }

    [Column("EXPENSE_ACCID")]
    [StringLength(15)]
    
    public string? ExpenseAccid { get; set; }

    [Column("POST_STATUS")]
    public Int32? PostStatus { get; set; }

    [Column("MEMBER_NO")]
    [StringLength(8)]
    
    public string? MemberNo { get; set; }

    [Column("PERCEN_SECURITY", TypeName = "NUMBER(3,2)")]
    public decimal? PercenSecurity { get; set; }

    [Column("PERCENMG_SECURITY", TypeName = "NUMBER(3,2)")]
    public decimal? PercenmgSecurity { get; set; }

    [Column("SOFMG_SECURITY", TypeName = "NUMBER(10,2)")]
    public decimal? SofmgSecurity { get; set; }

    [Column("POST_SOF_STATUS", TypeName = "NUMBER(1)")]


    public bool PostSofStatus { get; set; } = false;

    [Column("POST_STA_STATUS", TypeName = "NUMBER(1)")]
    public bool PostStaStatus { get; set; } = false;
}
