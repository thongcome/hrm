using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pc_te")]
public partial class pc_te
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string TEName { get; set; } = null!;

    public long? jobmasterid { get; set; }

    public long? workflowid { get; set; }

    public int? wleve { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [StringLength(250)]
    
    public string? crateby { get; set; }

    [StringLength(250)]
    
    public string? status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? prNo { get; set; }

    public int? prItem { get; set; }

    [StringLength(250)]
    
    public string? approveBy { get; set; }

    [InverseProperty("pc_te")]
    public virtual ICollection<pc_te_item> pc_te_items { get; set; } = new List<pc_te_item>();
}
