using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models
{

    [Table("HREmpTaxRateDet")]
    public class HREmpTaxRateDet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
        [Column("ID")]
        public long id { get; set; }

        [Column("COOP_ID")]
        [StringLength(6)]
        public string CoopId { get; set; } = null!;

        [Column("EMP_NO")]
        [StringLength(6)]
        public string EmpNo { get; set; } = null!;

        [Column("Peroid")]
        [StringLength(6)]
        public string Peroid { get; set; } = null!;

        [Column("CODE")]
        [StringLength(50)]
        public string code { get; set; } = null!;


        [Column("NAME")]
        [StringLength(250)]
        public string? Name { get; set; }

        [Column("NAMEEN")]
        [StringLength(250)]
        public string? NameEn { get; set; }

        [Column("MINRATE")]
        public decimal minRate { get; set; }

        [Column("MAXRATE")]
        public decimal maxRate { get; set; }


        [Column("PERCENTRATE")]
        public decimal PercentRate { get; set; }

        [Column("TAXLEVELRATE")]
        public Int32 taxLevelRate { get; set; }

        [Column("TAXACCUMLEVELRATE")]
        public decimal taxAccumLevelRate { get; set; }

        [Column("BASELEVELTAX")]
        public decimal BaseLevelTax { get; set; }

        [Column("BASELEVELINCOME")]
        public decimal BaseLevelIncome { get; set; }

        [Column("ISACTIVE")]
        public Int32 isActive { get; set; } = 1; // 1 = true, 0 = false


        //[Column("ISACTIVE", TypeName = "decimal(1)")]
        //public Int32 isActive { get; set; } = 1;

        //[Column("ISACTIVE")]
        //public bool? isActive { get; set; } = true;

        [Column("YEAR")]
        [StringLength(4)]
        public String? year { get; set; }

        [Column("STEP")]
        public int step { get; set; }

        [Column("MODIFIED_DATE", TypeName = "DATE")]
        public DateTime? MODIFIED_DATE { get; set; }

        [Column("MODIFIED_BY")]
        [StringLength(250)]
        public string? MODIFIED_BY { get; set; }


    }

}
