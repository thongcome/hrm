using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("app_serverInfo")]
public partial class app_serverInfo
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(250)]
    
    public string? nameEn { get; set; }

    public long appID { get; set; }

    [StringLength(50)]
    
    public string? appCode { get; set; }

    [StringLength(250)]
    
    public string? ipAddress { get; set; }

    [StringLength(50)]
    
    public string? serverType { get; set; }

    [StringLength(250)]
    
    public string? instantName { get; set; }

    [StringLength(250)]
    
    public string? serviceName { get; set; }

    [StringLength(50)]
    
    public string? userName { get; set; }

    [StringLength(500)]
    public string? confident_info { get; set; }

    [StringLength(50)]
    
    public string? routeNo { get; set; }

    [StringLength(500)]
    public string? description { get; set; }

    [StringLength(500)]
    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }
}
