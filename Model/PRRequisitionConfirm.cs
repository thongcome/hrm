using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Keyless]
[Table("PRRequisitionConfirm")]
public partial class PRRequisitionConfirm
{
    public long id { get; set; }

    public long? jobmasterid { get; set; }

    public long rfqid { get; set; }

    public long? prid { get; set; }

    [StringLength(250)]
    
    public string? PrNo { get; set; }

    [StringLength(50)]
    
    public string? pritem { get; set; }

    public int? pritemNo { get; set; }

    public long vendorid { get; set; }

    [StringLength(50)]
    
    public string? vendorcode { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? modate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool? isApprove { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approvedate { get; set; }

    public bool? isActive { get; set; }

    [StringLength(500)]
    public string? remark { get; set; }

    [StringLength(250)]
    
    public string? ref1 { get; set; }

    public long? rfqitemid { get; set; }

    [StringLength(250)]
    
    public string? approveby { get; set; }

    public long? approveUserid { get; set; }

    [StringLength(500)]
    public string? ref2 { get; set; }

    [StringLength(250)]
    
    public string? RequisitionerName { get; set; }

    [StringLength(250)]
    
    public string? ApproverName { get; set; }

    public bool? isSendMember { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? SendMemberDate { get; set; }

    public bool? isAllowReApprove { get; set; }

    [StringLength(250)]
    
    public string? status { get; set; }

    [StringLength(250)]
    
    public string? statusName { get; set; }

    public bool? isMandatory { get; set; }

    public long? prServiceid { get; set; }

    public long? quoteID { get; set; }

    public long? quoteItemID { get; set; }

    [StringLength(250)]
    
    public string? RequisitionerEmpID { get; set; }
}
