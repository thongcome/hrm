using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("emp_checkin")]
public partial class emp_checkin
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long userid { get; set; }

    [StringLength(50)]
    
    public string? empid { get; set; }

    [StringLength(250)]
    
    public string? orgcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? checkin_time { get; set; }

    [StringLength(50)]
    
    public string? lat { get; set; }

    [StringLength(50)]
    
    public string? lon { get; set; }

    [StringLength(500)]
    public string? place { get; set; }

    [StringLength(2500)]
    public string? remark { get; set; }

    public bool? ismain { get; set; }

    public bool? isActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool? isAdjust { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? checkin_time_stamp { get; set; }

    public bool? isIn { get; set; }

    public bool? isOut { get; set; }

    [StringLength(500)]
    public string? macAddress { get; set; }

    [StringLength(250)]
    public string? orgName { get; set; }

    [StringLength(250)]
    
    public string? files { get; set; }

    [StringLength(250)]
    
    public string? path { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? checkouttime { get; set; }

    public long? checkoutid { get; set; }

    [StringLength(50)]
    
    public string? workhour { get; set; }

    [StringLength(50)]
    
    public string? workminute { get; set; }

    public DateOnly? checkinasDate { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [ForeignKey("userid")]
    [InverseProperty("emp_checkins")]
    public virtual sc_user user { get; set; } = null!;
}
