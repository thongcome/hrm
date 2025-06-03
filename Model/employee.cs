using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("employee")]
[Index("Companycode", Name = "companycode")]
[Index("id", Name = "employeeID")]
[Index("EmployeeID", "Companycode", Name = "employeeID_CompanyCode")]
public partial class Employee
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public int id { get; set; }

    public long? UserID { get; set; }

    [StringLength(250)]
    
    public string? UserName { get; set; }

    [StringLength(250)]
    
    public string? TitleTh { get; set; }

    [StringLength(250)]
    
    public string? TitleEn { get; set; }

    [StringLength(250)]

    [Required(ErrorMessage = "กรุณาระบุชื่อพนักงาน")]

    public string NameTh { get; set; } = null!;

    [StringLength(250)]
    [Required(ErrorMessage = "Please specific English Name")]
    public string? NameEn { get; set; }

    [StringLength(50)]
    
    public string? CardID { get; set; }

    [StringLength(250)]
    
    public string? CardType { get; set; }

    [StringLength(50)]
    [Required(ErrorMessage = "Please specific Employe ID")]

    public string EmployeeID { get; set; } = null!;

    [StringLength(250)]
    
    public string? Email { get; set; }

    [StringLength(50)]
    
    public string? Companycode { get; set; }

    [StringLength(100)]
    
    public string? Company { get; set; }

    [StringLength(50)]
    
    public string? CostCenter { get; set; }

    [StringLength(250)]
    
    public string? Location { get; set; }

    [StringLength(250)]
    
    public string? MFName { get; set; }

    [StringLength(50)]
    
    public string? SuperviserID { get; set; }

    [StringLength(250)]
    
    public string? SuperViserName { get; set; }

    [StringLength(250)]
    
    public string? MEMail { get; set; }

    [StringLength(15)]
    
    public string? empLevel { get; set; }

    [StringLength(250)]
    
    public string? extensionNo { get; set; }

    [StringLength(1000)]
    public string? OrgNameEn { get; set; }

    [StringLength(1000)]
    public string? OrgNameTh { get; set; }

    [StringLength(250)]
    
    public string? PosCode { get; set; }

    [StringLength(1000)]
    public string? CostCenterName { get; set; }

    [StringLength(50)]
    
    public string? sex { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? hiringDate { get; set; }

    [StringLength(250)]
    
    public string? positionEN { get; set; }

    [StringLength(250)]
    
    public string? positionTH { get; set; }

    [StringLength(250)]
    
    public string? employeegroup { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? enddate { get; set; }

    public bool isActive { get; set; } = true;

    [StringLength(50)]
    
    public string? OrgCode { get; set; }

    [StringLength(1000)]
    public string? departmentName { get; set; }

    [StringLength(50)]
    
    public string? departmentCode { get; set; }

    [StringLength(50)]
    
    public string? comp_code_all { get; set; }

    [StringLength(500)]
    public string? image { get; set; }

    [StringLength(500)]
    public string? image_path { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    public bool isApprove { get; set; } = false;

    [StringLength(250)]
    
    public string? ApproveBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApproveDate { get; set; }

    [StringLength(250)]
    
    public string? last_name { get; set; }

    [StringLength(50)]
    
    public string? mobile { get; set; }

    [StringLength(250)]
    
    public string? marital { get; set; }

    [StringLength(250)]
    
    public string? lastNameEn { get; set; }

    public bool isRegister { get; set; } = false;

    [Column(TypeName = "datetime")]
    public DateTime? registerDate { get; set; }

    public DateOnly? asOfDate { get; set; }

    [StringLength(50)]
    
    public string? job_grade { get; set; }

    [StringLength(500)]
    public string? head_code { get; set; }

    [StringLength(250)]
    
    public string? resign_by { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? resign_date { get; set; }
}
