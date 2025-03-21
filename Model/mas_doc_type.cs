using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("mas_doc_type")]
public partial class mas_doc_type
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(2000)]
    public string? name { get; set; }

    [StringLength(250)]
    
    public string? notes { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? code { get; set; }

    public bool isActive { get; set; }

    [StringLength(1000)]
    public string? remark { get; set; }

    [StringLength(50)]
    
    public string? group_ref { get; set; }

    [StringLength(250)]
    
    public string? name_en { get; set; }

    [StringLength(50)]
    
    public string? subMode1 { get; set; }

    public int? orderTh { get; set; }

    [InverseProperty("masdoc")]
    public virtual ICollection<DocCheckList> DocCheckLists { get; set; } = new List<DocCheckList>();

    [InverseProperty("mas_doc_type")]
    public virtual ICollection<doc_center> doc_centers { get; set; } = new List<doc_center>();

    [InverseProperty("doctype")]
    public virtual ICollection<his_doc> his_docs { get; set; } = new List<his_doc>();

    [InverseProperty("doctype")]
    public virtual ICollection<pc_RFQDocRequest> pc_RFQDocRequests { get; set; } = new List<pc_RFQDocRequest>();

    [InverseProperty("doctype")]
    public virtual ICollection<pc_vendor_RFQDocRequest> pc_vendor_RFQDocRequests { get; set; } = new List<pc_vendor_RFQDocRequest>();

    [InverseProperty("doctype")]
    public virtual ICollection<util_document> util_documents { get; set; } = new List<util_document>();

    [InverseProperty("doctype")]
    public virtual ICollection<vd_doc> vd_docs { get; set; } = new List<vd_doc>();
}
