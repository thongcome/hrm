using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pos_position_level")]
public partial class pos_position_level
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public int id { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(250)]
    
    public string? pol_note { get; set; }

    [StringLength(7)]
    
    public string? update_by { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? update_date { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    [StringLength(250)]
    
    public string? engname { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? plevel { get; set; }
}
