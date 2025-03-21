using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pc_rfq")]
public partial class pc_rfq
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string? rfq_no { get; set; }

    [StringLength(250)]
    
    public string? subject { get; set; }

    [StringLength(50)]
    
    public string? type { get; set; }

    public DateOnly? rfqdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    public string? remark { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? total { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? total_net { get; set; }

    [StringLength(250)]
    
    public string? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? vat { get; set; }

    public bool? isVat { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? vat_amount { get; set; }

    public long? jobmasterid { get; set; }

    [StringLength(50)]
    
    public string? quotation_ref { get; set; }

    [StringLength(50)]
    
    public string? vendor_code { get; set; }

    [MaxLength(50)]
    public byte[]? vendor_taxid { get; set; }

    [StringLength(250)]
    
    public string? vendor_name { get; set; }

    [StringLength(250)]
    
    public string? rfqName { get; set; }

    public long? vendorid { get; set; }

    public bool? isApprove { get; set; }

    [StringLength(250)]
    
    public string? file1 { get; set; }

    [StringLength(250)]
    
    public string? file2 { get; set; }

    [StringLength(250)]
    
    public string? file3 { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? deadline_date { get; set; }

    [StringLength(250)]
    
    public string? requestor { get; set; }

    [StringLength(250)]
    
    public string? requestorOrg { get; set; }

    [StringLength(250)]
    
    public string? requestorCostCenter { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? start_date { get; set; }

    [StringLength(250)]
    
    public string? requestorOrgName { get; set; }

    [StringLength(250)]
    
    public string? requestorName { get; set; }

    public int? showplusday { get; set; }

    public bool? isBidClosed { get; set; }

    public bool? isBidding { get; set; }

    public long? pridAsTe { get; set; }

    public bool? isTE { get; set; }

    [StringLength(500)]
    public string? fileTe { get; set; }

    [StringLength(500)]
    public string? pathTe { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApproveDate { get; set; }

    [StringLength(250)]
    
    public string? statusName { get; set; }

    [StringLength(500)]
    public string? pr_no { get; set; }

    [StringLength(2500)]
    public string? condition { get; set; }

    [StringLength(500)]
    public string? remark2 { get; set; }

    [StringLength(500)]
    public string? ReviewerBy { get; set; }

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

    [InverseProperty("rfq")]
    public virtual ICollection<pc_RFQDocRequest> pc_RFQDocRequests { get; set; } = new List<pc_RFQDocRequest>();

    [InverseProperty("rfq")]
    public virtual ICollection<pc_RFQServiceVendor> pc_RFQServiceVendors { get; set; } = new List<pc_RFQServiceVendor>();

    [InverseProperty("rfq")]
    public virtual ICollection<pc_rfqItem> pc_rfqItems { get; set; } = new List<pc_rfqItem>();
}
