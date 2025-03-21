using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Model
{
    public class SubTask
    {
        // Foreign key to TodoTask
        [Required]
        public int TaskId { get; set; }  // This is the property you are missing
        [ForeignKey("TaskId")]  // ForeignKey should match the property in the TodoTask class
        [InverseProperty("SubTasks")]  // This references the navigation property in TodoTask
        public virtual  TodoTask TodoTask { get; set; } = new TodoTask();

        [Key]
        public int SubId { get; set; }

        [Required]
        [StringLength(250)]
        [Unicode(true)]
        public string Name { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

       
        
        public int? Order { get; set; }

         
        [StringLength(250)]
        [Unicode(true)]
        public string? Description { get; set; } = string.Empty;

        
        [StringLength(50)]
        [Unicode(true)]
        public string? LessonMode { get; set; } = string.Empty;

        
        [StringLength(500)]
        [Unicode(true)]
        public string? Lesson1 { get; set; } = string.Empty;

        
        [StringLength(500)]
        [Unicode(true)]
        public string? Lesson2 { get; set; } = string.Empty;

        
        [StringLength(500)]
        [Unicode(true)]
        public string? ThankFully { get; set; } = string.Empty;


        [Unicode(true)]
        public string? StatusCode { get; set; } = string.Empty;

    }
}
