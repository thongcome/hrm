using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Model
{
    public class WOL
    {
        [Key]
        public int id { get; set; }

        [StringLength(250)]
        [Unicode(true)]
        [Required]
        [Display(Name = ("ชื่อหมวด/Mode"))]
        public string Name { get; set; }=string.Empty;

        [StringLength(500)]
        [Unicode(true)]
        [Display(Name = ("คำอธิบาย/Description"))]
        public string? description { get; set; }

        [StringLength(50)]
        [Unicode(true)]
        [Required]
        [Display(Name = ("ชื่อรหัส/Code Mode"))]
        public string code { get; set; } = string.Empty;

        [Display(Name = ("Theme"))]
        public string? theme { get; set; }

      
 

    }
}
