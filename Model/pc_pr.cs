using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pc_pr")]
[Index("PRNo", Name = "IX_pc_pr")]
public partial class pc_pr
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? PRNo { get; set; }

    [StringLength(250)]
    
    public string? subject { get; set; }

    [StringLength(50)]
    
    public string? type { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    public string? remark { get; set; }

    [Column(TypeName = "money")]
    public decimal? total { get; set; }

    [Column(TypeName = "money")]
    public decimal? total_net { get; set; }

    [StringLength(250)]
    
    public string? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [Column(TypeName = "money")]
    public decimal? vat { get; set; }

    public bool? isVat { get; set; }

    [Column(TypeName = "money")]
    public decimal? vat_amount { get; set; }

    public long? jobmasterid { get; set; }

    [StringLength(50)]
    
    public string? orgcode { get; set; }

    [StringLength(50)]
    
    public string? costcenter { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? prCreateDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? prReleaseDate { get; set; }

    [StringLength(50)]
    
    public string? prStatus { get; set; }

    [StringLength(50)]
    
    public string? prSAPstatus { get; set; }

    public long? pr_service_type_id { get; set; }

    [StringLength(50)]
    
    public string? requestor { get; set; }

    [StringLength(250)]
    
    public string? requestor_empid { get; set; }

    [StringLength(500)]
    public string? fileTe { get; set; }

    [StringLength(500)]
    public string? path { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approveTicketDate { get; set; }

    public bool? isApproved { get; set; }

    public long? createid { get; set; }

    public long? requstorid { get; set; }

    [StringLength(250)]
    
    public string? SAPRequisitioner { get; set; }

    public long? userid { get; set; }

    [StringLength(250)]
    
    public string? approvedby { get; set; }

    [StringLength(500)]
    public string? doc1 { get; set; }

    [StringLength(500)]
    public string? doc1path { get; set; }

    [StringLength(500)]
    public string? doc2 { get; set; }

    [StringLength(500)]
    public string? doc2path { get; set; }

    [StringLength(500)]
    public string? doc3 { get; set; }

    [StringLength(500)]
    public string? doc3path { get; set; }

    [StringLength(500)]
    public string? doc4 { get; set; }

    [StringLength(500)]
    public string? doc4path { get; set; }

    [StringLength(500)]
    public string? doc5 { get; set; }

    [StringLength(500)]
    public string? doc5path { get; set; }

    [InverseProperty("pc_pr")]
    public virtual ICollection<pc_pr_item> pc_pr_items { get; set; } = new List<pc_pr_item>();

    [ForeignKey("pr_service_type_id")]
    [InverseProperty("pc_prs")]
    public virtual pr_service_type? pr_service_type { get; set; }
}
