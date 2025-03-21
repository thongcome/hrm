using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("DocCheckList")]
public partial class DocCheckList
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long masdocid { get; set; }

    public bool isMandatory { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(250)]
    
    public string? mode { get; set; }

    [StringLength(1000)]
    public string? remark { get; set; }

    [StringLength(250)]
    
    public string? refcode1 { get; set; }

    [StringLength(250)]
    
    public string? refcode2 { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? expiredate { get; set; }

    public bool isactive { get; set; }

    [StringLength(25)]
    
    public string? secretLevel { get; set; }

    [StringLength(1000)]
    public string? fileref { get; set; }

    [StringLength(50)]
    
    public string? subMode1 { get; set; }

    public int orderTH { get; set; }

    [ForeignKey("masdocid")]
    [InverseProperty("DocCheckLists")]
    public virtual mas_doc_type masdoc { get; set; } = null!;
}
