
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Model;

[Table("generalinfo")]
[Index("Firstname", Name = "firstname")]
[Index("Personid", Name = "personid")]
[Index("Personid", Name = "primary key", IsUnique = true)]
public partial class Generalinfo
{


    [Key]
    [Column("id")]

    
    public long id { get; set; }

    [Column("personid")]
    [StringLength(15)]
 
    public string EmployeeID { get; set; } = null!;

    [Column("titleid")]
    public int? Titleid { get; set; }

    [Column("picname")]
    [StringLength(100)]

    public string? Picname { get; set; } 

    [Required]
    [Column("firstname")]
    [StringLength(250)]
 
    public string Firstname { get; set; } = null!;

    [Column("midname")]
    [StringLength(100)]
   
    public string? Midname { get; set; }

    [Required]
    [Column("lastname")]
    [StringLength(100)]
    
    public string Lastname { get; set; } = null!;

    [Column("nickname")]
    [StringLength(20)]
   
    public string? Nickname { get; set; }

    [Required]
    [Column("cardid")]
    [StringLength(20)]
    
    public string Cardid { get; set; } = null!;

    [Required]
    [Column("cardtype")]
    [StringLength(15)]
    
    public string? Cardtype { get; set; }

    [Column("cardstartdate", TypeName = "datetime")]
    public DateTime? Cardstartdate { get; set; }

    [Column("cardexpiredate", TypeName = "datetime")]
    public DateTime? Cardexpiredate { get; set; }

    [Required]
    [Column("cardissueby")]
    [StringLength(50)]
    
    public string? Cardissueby { get; set; }

    [Required]
    [Column("provincecareissue")]
    [StringLength(25)]
    
    public string? Provincecareissue { get; set; }

    [Required]
    [Column("sexid")]
    [StringLength(15)]
    
    public string? Sexid { get; set; }

    [Column("birthdate", TypeName = "datetime")]
    public DateTime? Birthdate { get; set; }

    [Column("religion")]
    [StringLength(50)]
    
    public string? Religion { get; set; }

    [Column("race")]
    [StringLength(3)]
    
    public string? Race { get; set; }

    [Required]
    [Column("nation")]
    [StringLength(3)]
    
    public string? Nation { get; set; }

    [Column("bloodgrp")]
    [StringLength(10)]
    
    public string? Bloodgrp { get; set; }

    [Column("bloodrh")]
    [StringLength(10)]
    
    public string? Bloodrh { get; set; }

    [Column("motorlicenseno")]
    [StringLength(15)]
    
    public string? Motorlicenseno { get; set; }

    [Column("carlicenseno")]
    [StringLength(15)]
    
    public string? Carlicenseno { get; set; }

    [Column("militarystatus")]
    [StringLength(1)]
    
    public string? Militarystatus { get; set; }

    [Column("militarydetail")]
    
    public string? Militarydetail { get; set; }

    [Column("ordainstatus")]
    [StringLength(1)]
    
    public string? Ordainstatus { get; set; }

    [Column("ordaindetail")]
    
    public string? Ordaindetail { get; set; }

    [Column("gbcstatus")]
    [StringLength(1)]
    
    public string? Gbcstatus { get; set; }

    [Column("gbcdetail", TypeName = "datetime")]
    public DateTime? Gbcdetail { get; set; }

    [Column("coopstatus")]
    [StringLength(1)]
    
    public string? Coopstatus { get; set; }

    [Column("coopdetail")]
    
    public string? Coopdetail { get; set; }

    [Column("hobby")]
    
    public string? Hobby { get; set; }

    [Column("specialcapa")]
    
    public string? Specialcapa { get; set; }

    [Column("remark")]
    
    public string? Remark { get; set; }

    [Column("createdate", TypeName = "datetime")]
    public DateTime? Createdate { get; set; }

    [Column("createby")]
    [StringLength(250)]
    
    public string? Createby { get; set; }

    [Column("moddate", TypeName = "datetime")]
    public DateTime? Moddate { get; set; }

    [Column("modby")]
    [StringLength(250)]
    
    public string? Modby { get; set; }

     
    [Column("entrance")]
    [StringLength(3)]
    
    public string? Entrance { get; set; }

 
    [Column("married")]
    [StringLength(3)]
    
    public string? Married { get; set; }

    [Column("e_firstname")]
    [StringLength(250)]
    
    public string? EFirstname { get; set; }

    [Column("e_midname")]
    [StringLength(100)]
    
    public string? EMidname { get; set; }

    [Column("e_lastname")]
    [StringLength(250)]
    
    public string? ELastname { get; set; }

    [Column("fathername")]
    [StringLength(250)]
    
    public string? Fathername { get; set; }

    [Column("mothername")]
    [StringLength(250)]
    
    public string? Mothername { get; set; }

    [Column("engagename")]
    [StringLength(250)]
    
    public string? Engagename { get; set; }

    [Column("entrance_date", TypeName = "datetime")]
    public DateTime? EntranceDate { get; set; }

    [Column("employeetype")]
    public int? Employeetype { get; set; }

    [Column("bornplace")]
    public int? Bornplace { get; set; }

    [Column("personstatusid")]
    public int? Personstatusid { get; set; }

    [Column("subpersonstatusid")]
    public int? Subpersonstatusid { get; set; }

    [Column("pstatusorderdate", TypeName = "datetime")]
    public DateTime? Pstatusorderdate { get; set; }

    [Column("pstatusorderno")]
    [StringLength(250)]
    
    public string? Pstatusorderno { get; set; }

    [Column("pstatuseffectivedate", TypeName = "datetime")]
    public DateTime? Pstatuseffectivedate { get; set; }

    [Column("pstatusfinishdate", TypeName = "datetime")]
    public DateTime? Pstatusfinishdate { get; set; }

    [Column("pstatusreason")]
    
    public string? Pstatusreason { get; set; }

    [Column("pstatusyear")]
    [StringLength(4)]
    
    public string? Pstatusyear { get; set; }

    [Column("pstatusdocrefer")]
    [StringLength(250)]
    
    public string? Pstatusdocrefer { get; set; }

    [Column("pstatusdocreferno")]
    [StringLength(250)]
    
    public string? Pstatusdocreferno { get; set; }

    [Column("pstatussecrefer")]
    [StringLength(250)]
    
    public string? Pstatussecrefer { get; set; }

    [Column("pstatusrefer")]
    
    public string? Pstatusrefer { get; set; }

    [Column("pstatusremark")]
    
    public string? Pstatusremark { get; set; }

    [Column("engtitle")]
    public int? Engtitle { get; set; }

    [Column("Employee_date", TypeName = "datetime")]
    public DateTime? EmployeeDate { get; set; }

    [Column("start_date", TypeName = "datetime")]
    public DateTime? StartDate { get; set; }

    [Column("probation_day", TypeName = "datetime")]
    public int? ProbationDay { get; set; }

    [Column("probation_date", TypeName = "datetime")]
    public DateTime? ProbationDate { get; set; }

    [Column("employeesubtype")]
    public int? Employeesubtype { get; set; }

    [Column("levelupdate", TypeName = "datetime")]
    public DateTime? Levelupdate { get; set; }

    [Column("lastestpos_codedate", TypeName = "datetime")]
    public DateTime? LastestposCodedate { get; set; }

    [Column("firstentranceplace")]
    [StringLength(250)]
    
    public string? Firstentranceplace { get; set; }

    [Column("bookmark")]
    [StringLength(1)]
    
    public string?   Bookmark { get; set; }

    [Column("code")]
    public int? Code { get; set; }

    [Column("accumulate")]
    public int? Accumulate { get; set; }

    [Column("email")]
    [StringLength(100)]

    public string Email { get; set; } = null!;

    [Column("cardrut")]
    [StringLength(20)]
    
    public string? Cardrut { get; set; }

    [Column("cardissuebyrut")]
    [StringLength(50)]
    
    public string? Cardissuebyrut { get; set; }

    [Column("cardstartdaterut", TypeName = "datetime")]
    public DateTime? Cardstartdaterut { get; set; }

    [Column("cardexpiredaterut", TypeName = "datetime")]
    public DateTime? Cardexpiredaterut { get; set; }


    [StringLength(50)] 
    public string? Companycode { get; set; } // from Employee
    [StringLength(100)]
    public string? Company { get; set; } // from Employee
    [StringLength(50)]
    public string? CostCenter { get; set; } // from Employee
    [StringLength(250)]
    public string? Location { get; set; } // from Employee
    [StringLength(250)]
    public string? MFName { get; set; } // from Employee
    [StringLength(50)]
    public string? SuperviserID { get; set; } // from Employee
    [StringLength(250)]
    public string? SuperViserName { get; set; } // from Employee
    [StringLength(250)]
    public string? MEMail { get; set; } // from Employee
    [StringLength(15)]
    public string? empLevel { get; set; } // from Employee
    [StringLength(250)]
    public string? extensionNo { get; set; } // from Employee
    [StringLength(1000)]
    public string? OrgNameEn { get; set; } // from Employee
    [StringLength(1000)]
    public string? OrgNameTh { get; set; } // from Employee
    [StringLength(250)]
    public string? PosCode { get; set; } // from Employee
    [StringLength(1000)]
    public string? CostCenterName { get; set; } // from Employee
    [StringLength(50)]
    public string? sex { get; set; } // from Employee
    public DateTime? hiringDate { get; set; } // from Employee
    [StringLength(250)] 
    string? positionEN { get; set; } // from Employee
    [StringLength(250)] 
    public string? positionTH { get; set; } // from Employee
    [StringLength(250)]
    public string? employeegroup { get; set; } // from Employee
    public DateTime? moddate { get; set; } // from Employee
    [StringLength(250)]
    public string? modby { get; set; } // from Employee
    public DateTime? startdate { get; set; } // from Employee
    public DateTime? enddate { get; set; } // from Employee
    public bool isActive { get; set; } = true; // from Employee
    [StringLength(50)] 
    public string? OrgCode { get; set; } // from Employee
    [StringLength(1000)]
    public string? departmentName { get; set; } // from Employee
    [StringLength(50)] 
    public string? departmentCode { get; set; } // from Employee
    [StringLength(50)]
    public string? comp_code_all { get; set; } // from Employee
    [StringLength(500)]
    public string? image { get; set; } // from Employee
    [StringLength(500)] 
    public string? image_path { get; set; } // from Employee
    [StringLength(50)]
    public string? status { get; set; } // from Employee
    public bool isApprove { get; set; } = false; // from Employee
    [StringLength(250)] 
    public string? ApproveBy { get; set; } // from Employee
    public DateTime? ApproveDate { get; set; } // from Employee
    [StringLength(250)]
    public string? last_name { get; set; } // from Employee
    [StringLength(50)]
    public string? mobile { get; set; } // from Employee
    [StringLength(250)] 
    public string? marital { get; set; } // from Employee
    [StringLength(250)]
    public string? lastNameEn { get; set; } // from Employee
    public bool isRegister { get; set; } = false; // from Employee
    public DateTime? registerDate { get; set; } // from Employee
    public DateOnly? asOfDate { get; set; } // from Employee
    [StringLength(50)] 
    public string? job_grade { get; set; } // from Employee
    [StringLength(500)]
    public string? head_code { get; set; } // from Employee
    [StringLength(250)] 
    public string? resign_by { get; set; } // from Employee
    public DateTime? resign_date { get; set; } // from Employee
}