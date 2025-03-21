using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace hrm.Models
{

    [Table("PayHrPayroll")]
    public partial class PayHrPayroll
    {
      

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
        [Column("ID")]
        public long id { get; set; }


        [Key]
        [Column("PAYROLLSLIP_NO")]
        [StringLength(12)]
        
        public string PayrollslipNo { get; set; } = null!;

        [Column("EMP_NO")]
        [StringLength(50)]
        
        public string? EmpNo { get; set; }

        [Column("PAYROLL_DATE", TypeName = "datetime")] // ✅ Oracle DATE → SQL Server datetime
        public DateTime? PayrollDate { get; set; }

        [Column("PAYROLL_PERIOD")]
        [StringLength(15)]
        
        public string? PayrollPeriod { get; set; }

        [Column("SALARYBASE_AMT", TypeName = "decimal(9,2)")] // ✅ Oracle NUMBER(9,2) → SQL Server decimal(9,2)
        public decimal? SalarybaseAmt { get; set; }

        [Column("SALARYOTH_AMT", TypeName = "decimal(9,2)")]
        public decimal? SalaryothAmt { get; set; }

        [Column("SALARYSUBT_AMT", TypeName = "decimal(9,2)")]
        public decimal? SalarysubtAmt { get; set; }

        [Column("SALARYNET_AMT", TypeName = "decimal(9,2)")]
        public decimal? SalarynetAmt { get; set; }

        [Column("PAYROLL_STATUS", TypeName = "decimal(2,0)")]
        public decimal? PayrollStatus { get; set; }

        [Column("EXPENSE_CODE")]
        [StringLength(50)]
        
        public string? ExpenseCode { get; set; }

        [Column("EXPENSE_BANK")]
        [StringLength(50)]
        
        public string? ExpenseBank { get; set; }

        [Column("EXPENSE_BRANCH")]
        [StringLength(50)]
        
        public string? ExpenseBranch { get; set; }

        [Column("EXPENSE_ACCID")]
        [StringLength(15)]
        
        public string? ExpenseAccid { get; set; }

        [Column("POST_STATUS")]
        public int? PostStatus { get; set; }  // ✅ Oracle Int32 → SQL Server int

        [Column("MEMBER_NO")]
        [StringLength(8)]
        
        public string? MemberNo { get; set; }

        [Column("PERCEN_SECURITY", TypeName = "decimal(3,2)")]
        public decimal? PercenSecurity { get; set; }

        [Column("PERCENMG_SECURITY", TypeName = "decimal(3,2)")]
        public decimal? PercenmgSecurity { get; set; }

        [Column("SOFMG_SECURITY", TypeName = "decimal(10,2)")]
        public decimal? SofmgSecurity { get; set; }

        [Column("POST_SOF_STATUS")]
        public int PostSofStatus { get; set; }

        [Column("POST_STA_STATUS")]
        public int PostStaStatus { get; set; }


        [Column("daycal", TypeName = "decimal(10,2)")]
        public decimal? DayCal { get; set; }

        [Column("daycalTotal", TypeName = "decimal(10,2)")]
        public decimal? DayCalTotal { get; set; }
    }

  
}
