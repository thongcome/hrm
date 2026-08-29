using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_rfq_doc")]
public partial class pc_rfq_doc
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(20)]

    public string? doc_code { get; set; }

    [StringLength(250)]

    public string doc_type { get; set; } = null!;

    [StringLength(250)]
    
    public string? doc_name { get; set; }

    public DateOnly? doc_date { get; set; }

    public DateOnly? doc_expire_date { get; set; }

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

    public bool? isMandatory { get; set; }

    public bool? remark { get; set; }

    [StringLength(1000)]
    public string? doc_url { get; set; }
}
