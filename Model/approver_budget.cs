using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("approver_budget")]
public partial class approver_budget
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long budgetid { get; set; }

    // Stable human-facing code for this record (audit: every master/document table needs one beyond the surrogate Id).
    [StringLength(20)]

    public string? budgetcode { get; set; }

    [StringLength(50)]

    public string approvelevel { get; set; } = null!;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? budget_min { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? noreceipt { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? advance { get; set; }

    [StringLength(250)]
    
    public string? comparesign { get; set; }

    public byte[]? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isActive { get; set; }
}
