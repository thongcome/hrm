using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("HRW_OT")]
public partial class HrwOt
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long ID { get; set; }


    
    [Column("companyid")]
    [StringLength(6)]
    
    public string companyid { get; set; } = null!;

    
    [Column("EMP_NO")]
    [StringLength(6)]
    
    public string EmpNo { get; set; } = null!;

    
    [Column("SEQ_NO")]
    [Precision(10)]
    public int SeqNo { get; set; }

    [Column("REMARK")]
    [StringLength(100)]
    
    public string? Remark { get; set; }

    [Column("DATE_WORK", TypeName = "DATE")]
    public DateTime? DateWork { get; set; }

    [Column("WORK_IN")]
    [StringLength(5)]
    
    public string? WorkIn { get; set; }

    [Column("WORK_OUT")]
    [StringLength(5)]
    
    public string? WorkOut { get; set; }

    [Column("WORKOT_IN")]
    [StringLength(5)]
    
    public string? WorkotIn { get; set; }

    [Column("WORKOT_OUT")]
    [StringLength(5)]
    
    public string? WorkotOut { get; set; }

    [Column("OT_P")]
    [StringLength(5)]
    
    public string? OtP { get; set; }

    [Column("APV_OT_STATUS")]
    [StringLength(2)]
    
    public string? ApvOtStatus { get; set; }

    [Column("OT_CODE")]
    [StringLength(2)]
    
    public string? OtCode { get; set; }

   
    [Column("OT_DOCNO")]
    [StringLength(10)]
    
    public string OtDocno { get; set; } = null!;

    [Column("OT_DOCNO_DATE", TypeName = "DATE")]
    public DateTime? OtDocnoDate { get; set; }

    [Column("OT_AMT", TypeName = "decimal(10,2)")]
    public decimal? OtAmt { get; set; }

    [Column("MONEY_HOUR", TypeName = "decimal(12,2)")]
    public decimal? MoneyHour { get; set; }

    [Column("MONEY_MINUTE", TypeName = "decimal(12,2)")]
    public decimal? MoneyMinute { get; set; }

    [Column("OT_P_MINUTE", TypeName = "decimal(12,2)")]
    public decimal? OtPMinute { get; set; }

    [Column("DEPTGRP_CODE")]
    [StringLength(2)]
    
    public string? DeptgrpCode { get; set; }

    [Column("WORKDEFAULT_IN")]
    [StringLength(5)]
    
    public string? WorkdefaultIn { get; set; }

    [Column("WORKDEFAULT_OUT")]
    [StringLength(5)]
    
    public string? WorkdefaultOut { get; set; }

    [Column("POST_STATUS")]
    public Int32? PostStatus { get; set; }

    [Column("DATE_WORK_TO", TypeName = "DATE")]
    public DateTime? DateWorkTo { get; set; }

    [Column("SIGNATURE1")]
    [StringLength(6)]
    
    public string? Signature1 { get; set; }

    [Column("SIGNATURE2")]
    [StringLength(6)]
    
    public string? Signature2 { get; set; }

    [Column("SIGNATURE3")]
    [StringLength(6)]
    
    public string? Signature3 { get; set; }

    [Column("SALARY_CAL_SEQ")]
    [Precision(4)]
    public decimal? SalaryCalSeq { get; set; }
}
