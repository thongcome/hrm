using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("vd_general_info")]
public partial class vd_general_info
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string taxid { get; set; } = null!;

    [StringLength(50)]
    
    public string? vendorcode { get; set; }

    [StringLength(250)]
    
    public string first_name { get; set; } = null!;

    [StringLength(250)]
    
    public string? mid_name { get; set; }

    [StringLength(250)]
    
    public string? last_name { get; set; }

    [StringLength(250)]
    
    public string? first_name_en { get; set; }

    [StringLength(250)]
    
    public string? midname_en { get; set; }

    [StringLength(250)]
    
    public string? last_name_en { get; set; }

    public long? title { get; set; }

    [StringLength(50)]
    
    public string? title_code { get; set; }

    [StringLength(50)]
    
    public string? type { get; set; }

    [StringLength(250)]
    
    public string email { get; set; } = null!;

    [StringLength(250)]
    
    public string? email2 { get; set; }

    [StringLength(250)]
    
    public string? username { get; set; }

    [StringLength(250)]
    
    public string? code { get; set; }

    public bool? islocal { get; set; }

    [StringLength(50)]
    
    public string? countrycode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(250)]
    
    public string? website { get; set; }

    public bool? isActive { get; set; }

    [StringLength(50)]
    
    public string? mobile { get; set; }

    [StringLength(50)]
    
    public string? mobile2 { get; set; }

    [StringLength(250)]
    
    public string? phone { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? register_date { get; set; }

    public DateOnly? effective_date { get; set; }

    public DateOnly? enddate { get; set; }

    [StringLength(250)]
    public string? remark_status { get; set; }

    public long? jobmasterid { get; set; }

    [StringLength(250)]
    
    public string? areatype { get; set; }

    [StringLength(50)]
    
    public string? branchcode { get; set; }

    public bool? isApprove { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApproveDate { get; set; }

    [StringLength(250)]
    
    public string? ApproveBy { get; set; }

    public bool? isLast { get; set; }

    public DateOnly? birthdate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? capitalRegister { get; set; }

    public long? capitalCurrencyid { get; set; }

    [StringLength(250)]
    
    public string? signedName { get; set; }

    [StringLength(250)]
    
    public string? signedPosition { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? signedDate { get; set; }

    [StringLength(250)]
    
    public string? fax { get; set; }

    [StringLength(250)]
    
    public string? extPhone { get; set; }

    [StringLength(250)]
    
    public string? extFax { get; set; }

    public bool? isAcceptCondition1 { get; set; }

    public bool? isAcceptCondition2 { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? isAccept1Date { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? isAccept2Date { get; set; }

    [ForeignKey("capitalCurrencyid")]
    [InverseProperty("vd_general_infos")]
    public virtual Currency? capitalCurrency { get; set; }

    [InverseProperty("vendor")]
    public virtual ICollection<pc_RFQServiceVendor> pc_RFQServiceVendors { get; set; } = new List<pc_RFQServiceVendor>();

    [InverseProperty("vendor")]
    public virtual ICollection<vd_doc> vd_docs { get; set; } = new List<vd_doc>();
}
