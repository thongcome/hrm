using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("vd_signed")]
public partial class vd_signed
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? signedName { get; set; }

    [StringLength(250)]
    
    public string? signedPosition { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? signedDate { get; set; }

    public long? vendorid { get; set; }

    public long? userid { get; set; }
}
