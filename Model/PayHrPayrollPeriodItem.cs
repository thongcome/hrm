using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HRM.Models
{
    public class PayHrPayrollPeriodItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
        [Column("ID")]
        public long id { get; set; }


        [Column("PAYROLLSLIP_NO")]
        [StringLength(12)]

        public string PayrollslipNo { get; set; } = null!;


        [Column("EMP_NO")]
        [StringLength(50)]

        public string? EmpNo { get; set; }


        [Column("PAYROLL_PERIOD")]
        [StringLength(15)]

        public string? PayrollPeriod { get; set; }

        [Column("SALITEM_CODE")]
        [StringLength(50)]
        public string? SalitemCode { get; set; }

        [Column("DESCRIPTION")]
        [StringLength(250)]

        public string? Description { get; set; }

        [Column("ITEM_AMT", TypeName = "decimal(15,2)")]
        public decimal? ItemAmt { get; set; }

        [Column("code")]
        [StringLength(250)]
        public string? code { get; set; }

        [Column("isIncludeTax")]
        public bool isIncludeTax { get; set; } = true;

    }
}
