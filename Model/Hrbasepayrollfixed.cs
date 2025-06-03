using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace HRM.Models
{
 
     [Table("HRBASEPAYROLLFIXED")]
    public partial class Hrbasepayrollfixed
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
        [Column("ID")]
        public long id { get; set; }

        [Column("companyid")]
        [StringLength(50)]
        public string companyid { get; set; } = null!;

        [Column("EMP_NO")]
        [StringLength(50)]
        
        public string EmpNo { get; set; } = null!;

        [Column("SEQ_NO")]
        [Precision(3)]
        public Int32 SeqNo { get; set; }

        [Column("SALITEM_CODE")]
        [StringLength(3)]
        
        public string? SalitemCode { get; set; }

        [Column("ITEM_AMT", TypeName = "decimal(15,2)")]
        public decimal? ItemAmt { get; set; }
    }

}
