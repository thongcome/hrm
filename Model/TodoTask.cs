using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Permissions;
using Microsoft.EntityFrameworkCore;

namespace LeaderDevelop.Model
{
    public class TodoTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        [Unicode(true)]
        [Display(Name =("เป้าหมาย/Goal"))]
        public string Name { get; set; } = string.Empty;

        public DateTime? Startdate { get; set; }
        public DateTime? EndDate { get; set; }

        [Required]
        public string StatusCode { get; set; } = string.Empty;

        [StringLength(250)]
        [Unicode(true)]
        [Required]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        [Unicode(true)]
        [Required]
        public string LessonMode { get; set; } = string.Empty;

        [StringLength(500)]
        [Unicode(true)]
        [Required]
        public string Lesson1 { get; set; } = string.Empty;

        [StringLength(500)]
        [Unicode(true)]
        [Required]
        public string Lesson2 { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

      
        public int? coachId { get; set; }

     
        public int? wolId { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; }

        [StringLength(500)]
        [Unicode(true)]
        [Required]
        public string ThankFully { get; set; } = string.Empty;

        [StringLength(250)]
        [Unicode(true)]
        public string Modby{ get; set; } = string.Empty;

        [StringLength(250)]
        [Unicode(true)]
        public string CreateBy { get; set; } = string.Empty;

        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [InverseProperty("TodoTask")]
        public virtual ICollection<SubTask>? SubTasks { get; set; }
    }
}
