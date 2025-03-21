using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("ConsentHistory")]
public partial class ConsentHistory
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(500)]
    public string? email { get; set; }

    [StringLength(500)]
    public string? name { get; set; }

    [StringLength(500)]
    public string? lastname { get; set; }

    [StringLength(250)]
    
    public string? mobile { get; set; }

    [StringLength(250)]
    
    public string? line { get; set; }

    [StringLength(50)]
    
    public string? card_id { get; set; }

    [StringLength(50)]
    
    public string? card_type { get; set; }
}
