using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("sc_program_access")]
public partial class sc_program_access
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long accessid { get; set; }

    public long programid { get; set; }

    [StringLength(50)]
    public string? programcode { get; set; }

    public long? roleid { get; set; }

    [StringLength(50)]
    
    public string? rolecode { get; set; }

    [Required]
    public bool? isCreate { get; set; }

    [Required]
    public bool? isRead { get; set; }

    [Required]
    public bool? isUpdate { get; set; }

    [Required]
    public bool? isDelete { get; set; }

    public long? menuid { get; set; }

    [StringLength(20)]
    public string? menucode { get; set; }

    public bool? isSearch { get; set; }

    [Required]
    public bool? isactive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [ForeignKey("menuid")]
    [InverseProperty("sc_program_accesses")]
    public virtual sc_menu? menu { get; set; }

    [ForeignKey("roleid")]
    [InverseProperty("sc_program_accesses")]
    public virtual sc_role? role { get; set; }
}
