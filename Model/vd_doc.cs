using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("vd_doc")]
public partial class vd_doc
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? doc_type { get; set; }

    [StringLength(250)]
    
    public string? doc_name { get; set; }

    public DateOnly? doc_date { get; set; }

    public DateOnly? doc_expire_date { get; set; }

    [StringLength(250)]
    
    public string? filename { get; set; }

    [StringLength(1000)]
    public string? file_path { get; set; }

    public string? comment { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(50)]
    
    public string? secureLevel { get; set; }

    public bool isActive { get; set; }

    public bool isMandatory { get; set; }

    [StringLength(50)]
    
    public string? taxid { get; set; }

    public long vendorid { get; set; }

    public long doctypeid { get; set; }

    [StringLength(50)]
    
    public string? reftype { get; set; }

    [ForeignKey("doctypeid")]
    [InverseProperty("vd_docs")]
    public virtual mas_doc_type doctype { get; set; } = null!;

    [ForeignKey("vendorid")]
    [InverseProperty("vd_docs")]
    public virtual vd_general_info vendor { get; set; } = null!;
}
