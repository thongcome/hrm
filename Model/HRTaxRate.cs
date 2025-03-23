using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HRM.Models
{
    [Table("HRTaxRate")]
    public class HRTaxRate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
        [Column("ID")]
        public long id { get; set; }

        [Column("COOP_ID")]
        [StringLength(6)]
        public string CoopId { get; set; } = null!;

        [Column("CODE")]
        [StringLength(50)]
        public string code { get; set; } = null!;


        [Column("name")]
        [StringLength(250)]
        public required string Name { get; set; }

        [Column("nameEn")]
        [StringLength(250)]
        public required string NameEn { get; set; }

        [Column("minRate")]
        public decimal minRate { get; set; }

        [Column("maxRate")]
        public decimal maxRate { get; set; }


        [Column("PercentRate")]
        public decimal PercentRate { get; set; }

        [Column("taxLevelRate")]
        public decimal taxLevelRate { get; set; }

        [Column("taxAccumLevelRate")]
        public decimal taxAccumLevelRate { get; set; }

        [Column("isActive")]
        public bool isActive { get; set; } = true;

        [Column("year")]
        [StringLength(4)]
        public required String year { get; set; }

        [Column("step")]
        public required int step { get; set; }

        [Column("MODIFIED_DATE", TypeName = "DATE")]
        public DateTime? MODIFIED_DATE { get; set; }

        [Column("MODIFIED_BY")]
        [StringLength(250)]
        public string? MODIFIED_BY { get; set; }


    }

}

