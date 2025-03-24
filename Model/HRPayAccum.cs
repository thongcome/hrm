using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HRM.Models
{

    [Table("HRPAYACCUM")]
    public class HRPayAccum
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
        [Column("ID")]
        public long id { get; set; }


        [Column("companyid")]
        [StringLength(6)]
        public string companyid { get; set; } = null!;

        [Column("YEAR")]
        [StringLength(4)]
        public string? YEAR { get; set; }

        [Column("EMP_NO")]
        [StringLength(6)]
        public string? EmpNo { get; set; }

        [Column("SALARYBASE", TypeName = "NUMBER(15,2)")]
        public decimal? Salary { get; set; }


        [Column("OTHERINCOME", TypeName = "NUMBER(15,2)")]
        public decimal? OtherIncome { get; set; }

        [Column("INCOMEPREDICTAMT", TypeName = "NUMBER(15,2)")]
        public decimal? IncomePredictAmt { get; set; }

        [Column("INCOMEYEARAMT", TypeName = "NUMBER(15,2)")]
        public decimal? IncomeYearAmt { get; set; }

        [Column("TAXYEARAMT", TypeName = "NUMBER(15,2)")]  // equal TaxYearAmt when start working
        public decimal? TaxYearAmt { get; set; }

        [Column("INCOMEFORWARDAMT", TypeName = "NUMBER(15,2)")] // last workplace 
        public decimal? IncomeForwardAmt { get; set; }




        [Column("STARTMONTH", TypeName = "NUMBER(2)")]
        public decimal? StartMonth { get; set; }

        [Column("ENDMONTH", TypeName = "NUMBER(2)")]
        public decimal? EndMonth { get; set; }

        [Column("WORKMONTH", TypeName = "NUMBER(5,2)")] // last workplace 
        public decimal? WORKMONTH { get; set; }


        [Column("LASTMONTHCAL", TypeName = "NUMBER(2)")]
        public decimal LastMonthCal { get; set; }


        [Column("LASTCALPERIODCODE")]
        [StringLength(50)]
        public string? LastPeriodCode { get; set; }

        [Column("MODBY")]
        [StringLength(250)]

        public string? MODIFIED_BY { get; set; }


        [Column("MODDATE", TypeName = "DATE")]
        public DateTime? MODIFIED_DATE { get; set; }

    }

}
