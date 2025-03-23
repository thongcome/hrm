using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HRM.Models
{


    [Table("HRPayrollPayByRequest")]
    public partial class Hrbasepayrollrequest
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
        [Column("ID")]
        public long id { get; set; }

        [Key]
        [Column("CompCode")]
        [StringLength(50)]
        [Unicode(false)]
        public string CompCode { get; set; } = null!;


        [Key]
        [Column("EMP_NO")]
        [StringLength(50)]
        [Unicode(false)]
        public string EmpNo { get; set; } = null!;


        [Column("SEQ_NO")]
        [Precision(3)]
        public Int32 SeqNo { get; set; }


        [Column("SALITEM_CODE")]
        [StringLength(3)]
        public string? SalitemCode { get; set; }



        [Column("ITEM_AMT", TypeName = "NUMBER(9,2)")]
        public decimal? ItemAmt { get; set; }

        [Column("Reason_Pay")]
        [StringLength(250)]
        public string? ReasonPay { get; set; }

        [Column("Period_Attend")]
        [StringLength(8)]
        public string? PeriodAttend { get; set; }


        public DateTime? ApproveDate { get; set; }

        [Column("Approve_by")]
        [StringLength(250)]
        public string? ApproveBy { get; set; }

        [Column("OrgCode")]
        [StringLength(250)]
        public string? OrgCode { get; set; }

        [Column("Acc_Code")]
        [StringLength(250)]
        public string? AccCode { get; set; }

        [Column("is_Tax_Cal")]
        public bool? IsTaxCal { get; set; }

        [Column("IsApproved")]
        public bool? IsApproved { get; set; }

        public int? SignFlag { get; set; }

        public String? description { get; set; }

        [Column("isFix")]
        public bool? IsFix { get; set; }

        [Column("approve_date", TypeName = "DATE")]
        public DateTime? ApprovedDate { get; set; }

        [Column("REMARK")]
        [StringLength(150)]
        [Unicode(false)]
        public string? REMARK { get; set; }

        [Column("MODIFIED_DATE", TypeName = "DATE")]
        public DateTime? MODIFIED_DATE { get; set; }

        [Column("MODIFIED_BY")]
        [StringLength(50)]
        [Unicode(false)]
        public string? MODIFIED_BY { get; set; }

    }

}


