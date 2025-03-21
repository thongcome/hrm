using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("upload_center")]
public partial class upload_center
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? datasource { get; set; }

    [StringLength(250)]
    
    public string? refId { get; set; }

    [StringLength(250)]
    
    public string? ref2 { get; set; }

    [StringLength(250)]
    
    public string? files { get; set; }

    [StringLength(250)]
    
    public string? path { get; set; }

    public long? userid { get; set; }

    [StringLength(50)]
    
    public string? empid { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(1000)]
    public string? remark { get; set; }

    [StringLength(1000)]
    public string? description { get; set; }

    [StringLength(250)]
    
    public string? table_ref { get; set; }

    public long? taskid { get; set; }

    public long? checkinid { get; set; }
}
