using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("stoa")]
public partial class stoa
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public int stoaid { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(20)]

    public string? stoa_code { get; set; }

    [StringLength(125)]

    public string? comcode { get; set; }

    [StringLength(250)]
    
    public string? expenseType { get; set; }

    [StringLength(50)]
    
    public string? glAccount { get; set; }

    public string? description { get; set; }

    [StringLength(250)]
    
    public string? specialType { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? dateFrom { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? dateTo { get; set; }

    public bool? isactive { get; set; }

    public DateTime? moddate { get; set; }

    [StringLength(50)]
    
    public string? modby { get; set; }
}
