using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("doc_center")]
public partial class doc_center
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? refno { get; set; }

    public bool? isRequired { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public long? mas_doc_type_id { get; set; }

    [StringLength(250)]
    
    public string? doctypecode { get; set; }

    public long? refid { get; set; }

    public bool? isActive { get; set; }

    public bool? isInclude { get; set; }

    [StringLength(2000)]
    public string? remark { get; set; }

    [StringLength(500)]
    public string? files { get; set; }

    [StringLength(500)]
    public string? path { get; set; }

    [StringLength(50)]
    
    public string? subMode1 { get; set; }

    public bool isDefaultRequired { get; set; }

    [ForeignKey("mas_doc_type_id")]
    [InverseProperty("doc_centers")]
    public virtual mas_doc_type? mas_doc_type { get; set; }
}
