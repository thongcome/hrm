using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LeaderDevelop.Model;

[Table("sc_user")]
public partial class ScUser
{
    [Key]
    [Column("userid")]
    public long Userid { get; set; }

  
    [Column("id")]
    [StringLength(450)]
    public string Id { get; set; }

    [Column("title")]
    [StringLength(250)]
    public string? Title { get; set; } = string.Empty;

    [Column("firstname")]
    [StringLength(250)]
    [Display(Name = ("ชื่อ/Name"))]
    [Required]
    public string Firstname { get; set; } = string.Empty;

    [Column("lastname")]
    [StringLength(250)]
    [Display(Name = ("นามสกุล/Last Name"))]
    public string Lastname { get; set; } = string.Empty;

    [Column("loginname")]
    [StringLength(250)]
    [Required]
    [Display(Name = ("ชื่อเข้าระบบ/Login Name"))]
    public string Loginname { get; set; } = string.Empty;

    [Column("password")]
    [StringLength(250)]
    [Required]
    public string? Password { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(250)]
    public string? Phone { get; set; } = string.Empty;

    [Column("mobilephone")]
    [StringLength(250)]
    [Display(Name = ("เบอร์มือถือ/Mobile"))]
    public string? Mobilephone { get; set; } = string.Empty;

    [Column("isforcechanged")]
    public bool Isforcechanged { get; set; } = false;

    [Column("IsCoachRequest")]
    [Display(Name = ("Apply to be Facililator"))]
    public bool IsCoachRequest { get; set; } = false;

    [Column("IsCoach")]
    [Display(Name = ("Facililator Status"))]
    public bool IsCoach { get; set; } = false;

    [Column("iscancel")]
    public bool Iscancel { get; set; } = false;

    [Column("pwdexpdate", TypeName = "datetime")]
    public DateTime? Pwdexpdate { get; set; } = null;

    [Column("lasttimelogin", TypeName = "datetime")]
    public DateTime? Lasttimelogin { get; set; } = null;

    [Column("invalidpwcount")]
    public int? Invalidpwcount { get; set; } = 0;

    [Column("lastinvalidpwd", TypeName = "datetime")]
    public DateTime? Lastinvalidpwd { get; set; } = null;

    [Column("remark")]
    public string? Remark { get; set; } = string.Empty;



    [Column("upperuserid")]
    public int? Upperuserid { get; set; }

    [Column("orgname")]
    [StringLength(250)]
    public string? Orgname { get; set; } = string.Empty;

    [Column("orgcode")]
    [StringLength(50)]
    public string? Orgcode { get; set; } = string.Empty;

    [Column("startdate")]
    public DateTime? Startdate { get; set; } = null;

    [Column("enddate")]
    public DateTime? Enddate { get; set; } = null;

    [Column("remindpwd")]
    [StringLength(250)]
    public string? Remindpwd { get; set; } = string.Empty;

    [Column("social")]
    [StringLength(250)]
    public string? Social { get; set; } = string.Empty;




    [Column("moddate", TypeName = "datetime")]
    public DateTime? Moddate { get; set; }

    [Column("modby")]
    [StringLength(250)]
    [Unicode(true)]
    public string? Modby { get; set; } = string.Empty;

    [Column("empid")]
    [StringLength(50)]
    [Unicode(true)]
    public string? Empid { get; set; } = string.Empty;



    [Column("email")]
    [StringLength(250)]
    [Display(Name = ("อีเมล์/Email"))]
    public string? Email { get; set; } = string.Empty;



    [Column("poscode")]
    [StringLength(50)]
    [Unicode(true)]
    public string? Poscode { get; set; } = string.Empty;

    [Column("pos_name")]
    [StringLength(250)]
    [Unicode(true)]
    public string? PosName { get; set; } = string.Empty;

    [Column("isActivate")]
    public bool IsActivate { get; set; } = true;

    [Display(Name = "คะแนน/Score")]
    public decimal? score { get; set; }


    [Column("supervisor")]
    [StringLength(250)]
    [Unicode(true)]
    public string? Supervisor { get; set; } = string.Empty;

    [Column("costcenter")]
    [StringLength(250)]
    [Unicode(true)]
    public string? Costcenter { get; set; } = string.Empty;

    [Column("verifycode")]
    [StringLength(250)]
    [Unicode(true)]
    public string? Verifycode { get; set; } = string.Empty;

    [Column("registerdate", TypeName = "datetime")]
    [Display(Name = ("วันที่ลงทะเบียน/Register Date"))]
    public DateTime? Registerdate { get; set; } = null;


    [Column("ref1")]
    [StringLength(500)]
    [Display(Name = ("คำอธิบาย คุณสมบัติเป็น Facililator/Write a description to get approval as a facilitator"))]

    public string? Ref1 { get; set; } = string.Empty;

    [Column("ref2")]
    [StringLength(500)]
    public string? Ref2 { get; set; } = string.Empty;


    //public virtual ICollection<ScUserRole> ScUserRoles { get; set; } = new List<ScUserRole>();

}
