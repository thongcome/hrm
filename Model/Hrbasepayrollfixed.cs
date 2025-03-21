using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace hrm.Models
{
  
 

     [Table("HRBASEPAYROLLFIXED")]
    public partial class Hrbasepayrollfixed
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
        [Column("ID")]
        public long id { get; set; }

        [Column("compcode")]
        [StringLength(50)]
      
        public string CoopId { get; set; } = null!;

        [Key]
        [Column("EMP_NO")]
        [StringLength(50)]
        [Unicode(false)]
        public string EmpNo { get; set; } = null!;

        [Key]
        [Column("SEQ_NO")]
        [Precision(3)]
        public Int32 SeqNo { get; set; }

        [Column("SALITEM_CODE")]
        [StringLength(3)]
        [Unicode(false)]
        public string? SalitemCode { get; set; }

        [Column("ITEM_AMT", TypeName = "NUMBER(9,2)")]
        public decimal? ItemAmt { get; set; }
    }

}
