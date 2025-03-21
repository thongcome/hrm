using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Model
{
    public class Activity
    {
        public Activity()
        {
            //StartDate = DateTime.UtcNow; // Default StartDate to now
            //EndDate = DateTime.UtcNow;   // Default EndDate to now
        }
        public Activity(int? maxOrder)
        {
            this.Orders = (maxOrder ?? 0) + 1;
        }
        //// Foreign key to TodoTask
        //[Required]
        // public int TaskId { get; set; }  // This is the property you are missing
        //[ForeignKey("TaskId")]  // ForeignKey should match the property in the TodoTask class
        //[InverseProperty("Activity")]  // This references the navigation property in TodoTask
        //public  GoalTask? GoalTask { get; set; }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Auto-generate Id
        public int Id { get; set; }


        [Required]
        [StringLength(250)]
        [Unicode(true)]
        [Display(Name = ("ชื่อกิจกรรม/Activity"))]
        public string Name { get; set; } = string.Empty;

        [Display(Name = ("วันที่เริ่ม/Start Date"))]
        //[FutureDate(ErrorMessage = "วันที่เริ่มต้องเป็นปัจจุบันหรือในอนาคตเท่านั้น / Start date must be now or in the future.")]
        //[Required(ErrorMessage = "Start Date is required.")]
         public DateTime? StartDate { get; set; }

        [Display(Name = ("วันที่สิ้นสุด/End Date"))]
        //[Required(ErrorMessage = "End Date is required.")]

        //[FutureDate(ErrorMessage = "วันที่สิ้นสุดต้องเป็นปัจจุบันหรือในอนาคตเท่านั้น / End date must be now or in the future.")]
        public DateTime? EndDate { get; set; }


        [Display(Name = ("ลำดับที่/Task No."))]
        public int? Orders { get; set; } = null;


        [StringLength(250)]
        [Unicode(true)]
        [Display(Name = ("หมายเหตุ/Note"))]
        public string? Description { get; set; } = string.Empty;


        [StringLength(500)]
        [Unicode(true)]
        [Display(Name = ("ประเด็นที่ได้เรียนรู้/Lesson Lenarned"))]
        public string? Lesson1 { get; set; } = string.Empty;

        [StringLength(500)]
        [Unicode(true)]
        [Display(Name = ("สิ่งที่ได้เรียนรู้/Learning"))]
        public string? Lesson2 { get; set; } = string.Empty;


        [Display(Name = "ความก้าวหน้า/Progress")]
        [Range(0, 100, ErrorMessage = "ใส่ค่า 0-100 / Progress must be between 0 and 100.")]
        public decimal? progress { get; set; }


        //[Display(Name = ("ความก้าวหน้า/Progress"))]
        //public decimal? progress { get; set; }

        [StringLength(1000)]
        [Unicode(true)]
        [Display(Name = ("ประเด็นที่ได้เรียนรู้/Lesson Learned"))]
        public string? ThankFully { get; set; } = string.Empty;

        //[Display(Name = ("ยกเลิก/Is Active"))]
        //[Required]
        //public bool IsActive { get; set; } = true;

        [Unicode(true)]
        [Display(Name = ("สถานะ/Status"))]
        public ActivityStatus? StatusCode { get; set; } = ActivityStatus.Active;


        [StringLength(250)]
        [Unicode(true)]
        [Display(Name = ("ผู้แก้ไข/Modified By"))]
        public string? Modby { get; set; } = string.Empty;

        [StringLength(250)]
        [Unicode(true)]
        [Display(Name = ("ผู้สร้าง/Create By"))]
        public string? CreateBy { get; set; } = string.Empty;

        [Display(Name = ("วันที่สร้าง/Create Date"))]
        public DateTime? CreateDate { get; set; } = DateTime.UtcNow;

        [Display(Name = ("วันที่แก้/Modify Date"))]
        public DateTime? ModDate { get; set; } = DateTime.UtcNow;



        // Foreign key to GoalTask
        public int TaskId { get; set; }

        // Navigation property back to GoalTask
        [ForeignKey("TaskId")]
        public GoalTask GoalTask { get; set; }

        public enum ActivityStatus
        {
            Active,
            Cancel,
            Success
        }


    }

    //public class FutureDateAttribute : ValidationAttribute
    //{
    //    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    //    {
    //        if (value is DateTime date)
    //        {
    //            if (date < DateTime.UtcNow.Date)
    //            {
    //                return new ValidationResult(ErrorMessage);
    //            }
    //        }
    //        return ValidationResult.Success;
    //    }

    //}
}
