using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("wf_button_master")]
public partial class wf_button_master
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(2000)]
    public string? class_style { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? actiontypecode { get; set; }

    [StringLength(250)]
    
    public string? value { get; set; }

    [StringLength(250)]
    
    public string? idname { get; set; }

    public int? wlevelType { get; set; }

    [StringLength(250)]
    
    public string? controller { get; set; }

    [StringLength(250)]
    
    public string? action { get; set; }

    [StringLength(250)]
    
    public string? btnType { get; set; }

    [StringLength(2000)]
    public string? btnTag { get; set; }

    public int? orderth { get; set; }

    [InverseProperty("button_master")]
    public virtual ICollection<wf_button> wf_buttons { get; set; } = new List<wf_button>();
}
