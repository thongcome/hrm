using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LeaderDevelop.Model
{
    public class TaskMaster
    {
        // Parameterless constructor for EF Core and other uses
        //public TaskMaster()
        //{
        //    // Initialize properties with default values if necessary
        //    Name = string.Empty;
        //    StatusCode = string.Empty;
        //    Description = string.Empty;
        //    Remark = string.Empty;
        //    Color = string.Empty;
        //}

        //// Constructor to initialize required properties
        //public TaskMaster(string name, bool isActive, string statusCode, string description, string remark, string color, int level, int defaultDay)
        //{
        //    Name = name;
        //    IsActive = isActive;
        //    StatusCode = statusCode;
        //    Description = description;
        //    Remark = remark;
        //    Color = color;
        //    Level = level;
        //    DefaultDay = defaultDay;
        //}

        [Key]
        public int Id { get; set; }

        [StringLength(250)]
        [Unicode(true)]
        [Required]
        public string Name { get; set; } = string.Empty; // Initialized with default value in constructor

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public string StatusCode { get; set; } = string.Empty; // Initialized with default value in constructor

        [StringLength(250)]
        [Unicode(true)]
        [Required]
        public string Description { get; set; } = string.Empty; // Initialized with default value in constructor

        [StringLength(250)]
        [Unicode(true)]
        [Required]
        public string Remark { get; set; } = string.Empty;// Initialized with default value in constructor

        [StringLength(250)]
        [Unicode(true)]
        [Required]
        public string Color { get; set; } = string.Empty; // Initialized with default value in constructor

        [Required]
        public int Level { get; set; }

        [Required]
        public int DefaultDay { get; set; }
    }
}
