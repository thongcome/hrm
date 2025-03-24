using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

/// <summary>
/// ข้อมูลรายการเรียกเก็บ
/// </summary>
 
[Table("KPTEMPRECEIVE")]
 
public partial class Kptempreceive
{


    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    [Column("ID")]
    public long id { get; set; }
    /// <summary>
    /// รหัสสาขา
    /// </summary>

    [Column("companyid")]
    [StringLength(6)]
    
    public string companyid { get; set; } = null!;

    /// <summary>
    /// เลขที่รายการเรียกเก็บ(Invoice)
    /// </summary>
    
    [Column("KPSLIP_NO")]
    [StringLength(15)]
    
    public string KpslipNo { get; set; } = null!;

    [Column("MEMCOOP_ID")]
    [StringLength(6)]
    
    public string? MemcoopId { get; set; }

    /// <summary>
    /// เลขสมาชิก
    /// </summary>
    [Column("MEMBER_NO")]
    [StringLength(8)]
    
    public string? MemberNo { get; set; }

    /// <summary>
    /// งวดเรียกเก็บ
    /// </summary>
    [Column("RECV_PERIOD")]
    [StringLength(8)]
    
    public string? RecvPeriod { get; set; }

    [Column("REFMEMCOOP_ID")]
    [StringLength(6)]
    
    public string? RefmemcoopId { get; set; }

    /// <summary>
    /// อ้างอิงเลขสมาชิก
    /// </summary>
    [Column("REF_MEMBNO")]
    [StringLength(8)]
    
    public string? RefMembno { get; set; }

    /// <summary>
    /// ประเภทสมาชิก
    /// </summary>
    [Column("MEMBTYPE_CODE")]
    [StringLength(2)]
    
    public string? MembtypeCode { get; set; }

    /// <summary>
    /// แผนกสมาชิก
    /// </summary>
    [Column("DEPARTMENT_CODE")]
    [StringLength(15)]
    
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// สังกัดสมาชิก
    /// </summary>
    [Column("MEMBGROUP_CODE")]
    [StringLength(15)]
    
    public string? MembgroupCode { get; set; }

    /// <summary>
    /// วันที่ทำการผ่านรายการ
    /// </summary>
    [Column("PROCESS_DATE", TypeName = "DATE")]
    public DateTime? ProcessDate { get; set; }

    /// <summary>
    /// เลขที่ใบเสร็จเรียกเก็บ
    /// </summary>
    [Column("RECEIPT_NO")]
    [StringLength(15)]
    
    public string? ReceiptNo { get; set; }

    /// <summary>
    /// วันที่ใบเสร็จ
    /// </summary>
    [Column("RECEIPT_DATE", TypeName = "DATE")]
    public DateTime? ReceiptDate { get; set; }

    /// <summary>
    /// วันที่ทำรายการ
    /// </summary>
    [Column("OPERATE_DATE", TypeName = "DATE")]
    public DateTime? OperateDate { get; set; }

    /// <summary>
    /// หุ้นยกมาสะสม
    /// </summary>
    [Column("SHARESTKBF_VALUE", TypeName = "NUMBER(15,2)")]
    public decimal? SharestkbfValue { get; set; }

    /// <summary>
    /// หุ้นสะสม
    /// </summary>
    [Column("SHARESTK_VALUE", TypeName = "NUMBER(15,2)")]
    public decimal? SharestkValue { get; set; }

    /// <summary>
    /// ดอกเบี้ยสะสม
    /// </summary>
    [Column("INTEREST_ACCUM", TypeName = "NUMBER(15,2)")]
    public decimal? InterestAccum { get; set; }

    /// <summary>
    /// สถานะผ่านรายการ
    /// </summary>
    [Column("KEEPING_STATUS")]
    [Precision(2)]
    public Int32? KeepingStatus { get; set; }

    /// <summary>
    /// จำนวนเงินที่เก็บได้
    /// </summary>
    [Column("RECEIVE_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? ReceiveAmt { get; set; }

    /// <summary>
    /// จำนวนที่เก็บได้ตัวอักษร
    /// </summary>
    [Column("MONEY_TEXT")]
    [StringLength(200)]
    
    public string? MoneyText { get; set; }

    /// <summary>
    /// ลำดับรายการล่าสุด
    /// </summary>
    [Column("LAST_SEQ_NO")]
    [Precision(5)]
    public Int32? LastSeqNo { get; set; }

    /// <summary>
    /// ผู้ทำรายการ
    /// </summary>
    [Column("ENTRY_ID")]
    [StringLength(15)]
    
    public string? EntryId { get; set; }

   
}
