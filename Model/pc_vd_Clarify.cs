using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pc_vd_Clarify")]
public partial class pc_vd_Clarify
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long rfqid { get; set; }

    public long vendorid { get; set; }

    [StringLength(2000)]
    public string? text1 { get; set; }

    [StringLength(250)]
    
    public string? file1 { get; set; }

    [StringLength(250)]
    
    public string? path { get; set; }

    [StringLength(250)]
    
    public string? filerResp { get; set; }

    [StringLength(250)]
    
    public string? pathResp { get; set; }

    public long? modbyVdID { get; set; }

    [StringLength(250)]
    
    public string? modbyVdName { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddateVD { get; set; }

    public long? useridVD { get; set; }

    [StringLength(500)]
    public string? subject { get; set; }

    public bool? isActive { get; set; }

    public bool? isReadOnly { get; set; }

    public bool? isApprove { get; set; }

    [StringLength(500)]
    public string? approveBy { get; set; }

    [StringLength(500)]
    public string? text2 { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? approvedate { get; set; }

    public bool? isVendorState { get; set; }

    [StringLength(2000)]
    public string? vdText1 { get; set; }

    public bool? isprocurementState { get; set; }

    [StringLength(2000)]
    public string? vdText2 { get; set; }

    [StringLength(250)]
    
    public string? file2 { get; set; }

    [StringLength(250)]
    
    public string? file3 { get; set; }

    [StringLength(250)]
    
    public string? path2 { get; set; }

    [StringLength(250)]
    
    public string? path3 { get; set; }

    [StringLength(50)]
    
    public string? status { get; set; }

    [StringLength(250)]
    
    public string? statusName { get; set; }

    public long? modbyReqID { get; set; }

    [StringLength(250)]
    
    public string? modbyReqName { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddateReq { get; set; }

    public long useridReqCreate { get; set; }
}
