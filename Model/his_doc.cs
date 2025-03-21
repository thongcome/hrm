using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("his_doc")]
public partial class his_doc
{
    public long id { get; set; }

    public long jobmasterid { get; set; }

    [StringLength(50)]
    
    public string? wlevel { get; set; }

    [StringLength(250)]
    
    public string? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [StringLength(20)]
    
    public string? secretlevel { get; set; }

    [StringLength(250)]
    
    public string path { get; set; } = null!;

    [StringLength(250)]
    
    public string filename { get; set; } = null!;

    [StringLength(250)]
    
    public string? filetype { get; set; }

    [StringLength(250)]
    
    public string? servername { get; set; }

    public int? order_th { get; set; }

    [StringLength(50)]
    
    public string? role_allow { get; set; }

    public bool isAllAccess { get; set; }

    public bool isActive { get; set; }

    [StringLength(250)]
    
    public string? approveby { get; set; }

    public long? doctype_id { get; set; }

    [StringLength(250)]
    
    public string? job_type { get; set; }

    public long userid { get; set; }

    [StringLength(250)]
    
    public string? sourceName { get; set; }

    [StringLength(250)]
    
    public string? sourceID { get; set; }

    [StringLength(250)]
    
    public string? sourceNameSub1 { get; set; }

    [StringLength(250)]
    
    public string? sourceNameSub2 { get; set; }

    [StringLength(250)]
    
    public string? sourceNameSub3 { get; set; }

    [StringLength(250)]
    
    public string? sourceID2 { get; set; }

    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long his_id { get; set; }

    [ForeignKey("doctype_id")]
    [InverseProperty("his_docs")]
    public virtual mas_doc_type? doctype { get; set; }
}
