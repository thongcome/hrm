using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("KPTEMPRECEIVEDET")]
public partial class Kptempreceivedet
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    [Column("ID")]
    public long id { get; set; }

    
    [Column("companyid")]
    [StringLength(6)]
    
    public string companyid { get; set; } = null!;

  
    [Column("KPSLIP_NO")]
    [StringLength(15)]
    
    public string KpslipNo { get; set; } = null!;

 
    [Column("SEQ_NO")]
    [Precision(5)]
    public Int32 SeqNo { get; set; }

    [Column("MEMCOOP_ID")]
    [StringLength(6)]
    
    public string? MemcoopId { get; set; }

    [Column("MEMBER_NO")]
    [StringLength(8)]
    
    public string? MemberNo { get; set; }

    [Column("RECV_PERIOD")]
    [StringLength(8)]
    
    public string? RecvPeriod { get; set; }

    [Column("REFMEMCOOP_ID")]
    [StringLength(6)]
    
    public string? RefmemcoopId { get; set; }

    [Column("REF_MEMBNO")]
    [StringLength(8)]
    
    public string? RefMembno { get; set; }

    [Column("KEEPITEMTYPE_CODE")]
    [StringLength(3)]
    
    public string? KeepitemtypeCode { get; set; }

    [Column("SHRLONTYPE_CODE")]
    [StringLength(5)]
    
    public string? ShrlontypeCode { get; set; }

    [Column("ITEM_SEQNO")]
    [Precision(5)]
    public Int32? ItemSeqno { get; set; }

    [Column("LOANCONTRACT_NO")]
    [StringLength(15)]
    
    public string? LoancontractNo { get; set; }

    [Column("DESCRIPTION")]
    [StringLength(200)]
    
    public string? Description { get; set; }

    [Column("PERIOD")]
    [Precision(5)]
    public Int32? Period { get; set; }

    [Column("PRINCIPAL_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? PrincipalPayment { get; set; }

    [Column("INTEREST_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? InterestPayment { get; set; }

    [Column("INTARREAR_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? IntarrearPayment { get; set; }

    [Column("ITEM_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? ItemPayment { get; set; }

    [Column("ITEM_BALANCE", TypeName = "NUMBER(15,2)")]
    public decimal? ItemBalance { get; set; }

    [Column("PRINCIPAL_BALANCE", TypeName = "NUMBER(15,2)")]
    public decimal? PrincipalBalance { get; set; }

    [Column("CALINTFROM_DATE", TypeName = "DATE")]
    public DateTime? CalintfromDate { get; set; }

    [Column("CALINTTO_DATE", TypeName = "DATE")]
    public DateTime? CalinttoDate { get; set; }

    [Column("PRINCIPAL_PERIOD", TypeName = "NUMBER(15,2)")]
    public decimal? PrincipalPeriod { get; set; }

    [Column("INTEREST_PERIOD", TypeName = "NUMBER(15,2)")]
    public decimal? InterestPeriod { get; set; }

    [Column("BFPRINBALANCE_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? BfprinbalanceAmt { get; set; }

    [Column("BFPERIOD")]
    [Precision(5)]
    public Int32? Bfperiod { get; set; }

    [Column("BFLOANPAYMENT_TYPE")]
    [Precision(2)]
    public Int32? BfloanpaymentType { get; set; }

    [Column("BFPERIOD_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? BfperiodPayment { get; set; }

    [Column("BFMAXPERIOD_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? BfmaxperiodPayment { get; set; }

    [Column("BFPAYMENT_STATUS")]
    [Precision(2)]
    public Int32? BfpaymentStatus { get; set; }

    [Column("BFLASTCALINT_DATE", TypeName = "DATE")]
    public DateTime? BflastcalintDate { get; set; }

    [Column("BFLASTPAY_DATE", TypeName = "DATE")]
    public DateTime? BflastpayDate { get; set; }

    [Column("BFINTEREST_ARREAR", TypeName = "NUMBER(15,2)")]
    public decimal? BfinterestArrear { get; set; }

    [Column("BFINTMONTH_ARREAR", TypeName = "NUMBER(15,2)")]
    public decimal? BfintmonthArrear { get; set; }

    [Column("BFINTYEAR_ARREAR", TypeName = "NUMBER(15,2)")]
    public decimal? BfintyearArrear { get; set; }

    [Column("BFPRINCIPAL_ARREAR", TypeName = "NUMBER(15,2)")]
    public decimal? BfprincipalArrear { get; set; }

    [Column("BFINTEREST_RETURN", TypeName = "NUMBER(15,2)")]
    public decimal? BfinterestReturn { get; set; }

    [Column("BFCONTRACT_STATUS")]
    [Precision(2)]
    public Int32? BfcontractStatus { get; set; }

    [Column("BFCONTLAW_STATUS")]
    [Precision(2)]
    public Int32? BfcontlawStatus { get; set; }

    [Column("KEEPITEM_STATUS")]
    [Precision(2)]
    public Int32? KeepitemStatus { get; set; }

    [Column("POSTING_STATUS")]
    [Precision(2)]
    public Int32? PostingStatus { get; set; }

    [Column("POSTING_DATE", TypeName = "DATE")]
    public DateTime? PostingDate { get; set; }

    [Column("CANCEL_ID")]
    [StringLength(15)]
    
    public string? CancelId { get; set; }

    [Column("CANCEL_DATE", TypeName = "DATE")]
    public DateTime? CancelDate { get; set; }

    [Column("BFADJUST_PRNAMT", TypeName = "NUMBER(15,2)")]
    public decimal? BfadjustPrnamt { get; set; }

    [Column("BFADJUST_INTAMT", TypeName = "NUMBER(15,2)")]
    public decimal? BfadjustIntamt { get; set; }

    [Column("BFADJUST_ITEMAMT", TypeName = "NUMBER(15,2)")]
    public decimal? BfadjustItemamt { get; set; }

    [Column("BFREAL_INTPAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? BfrealIntpayment { get; set; }

    [Column("INTERESTRECAL_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? InterestrecalPayment { get; set; }

    [Column("KEPPAYMENT_TYPE")]
    [StringLength(3)]
    
    public string? KeppaymentType { get; set; }

    [Column("MISSPAY_FLAG")]
    [Precision(2)]
    public Int32? MisspayFlag { get; set; }

    [Column("OVERPAY_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? OverpayAmt { get; set; }

    [Column("OVERPAY_PRNAMT", TypeName = "NUMBER(15,2)")]
    public decimal? OverpayPrnamt { get; set; }

    [Column("OVERPAY_INTAMT", TypeName = "NUMBER(15,2)")]
    public decimal? OverpayIntamt { get; set; }

    [Column("RETOVERPAY_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? RetoverpayAmt { get; set; }

    [Column("RETOVERPAY_PRNAMT", TypeName = "NUMBER(15,2)")]
    public decimal? RetoverpayPrnamt { get; set; }

    [Column("RETOVERPAY_INTAMT", TypeName = "NUMBER(15,2)")]
    public decimal? RetoverpayIntamt { get; set; }

    [Column("ADJUST_ITEMAMT", TypeName = "NUMBER(15,2)")]
    public decimal? AdjustItemamt { get; set; }

    [Column("ADJUST_PRNAMT", TypeName = "NUMBER(15,2)")]
    public decimal? AdjustPrnamt { get; set; }

    [Column("ADJUST_INTAMT", TypeName = "NUMBER(15,2)")]
    public decimal? AdjustIntamt { get; set; }

    [Column("AFKEPPAY_ITEMAMT", TypeName = "NUMBER(15,2)")]
    public decimal? AfkeppayItemamt { get; set; }

    [Column("AFKEPPAY_PRNAMT", TypeName = "NUMBER(15,2)")]
    public decimal? AfkeppayPrnamt { get; set; }

    [Column("AFKEPPAY_INTAMT", TypeName = "NUMBER(15,2)")]
    public decimal? AfkeppayIntamt { get; set; }

    [Column("AFKEPPAY_INTRETAMT", TypeName = "NUMBER(15,2)")]
    public decimal? AfkeppayIntretamt { get; set; }

    [Column("BFKPPOST_ITEMAMT", TypeName = "NUMBER(15,2)")]
    public decimal? BfkppostItemamt { get; set; }

    [Column("BFKPPOST_PRNAMT", TypeName = "NUMBER(15,2)")]
    public decimal? BfkppostPrnamt { get; set; }

    [Column("BFKPPOST_INTAMT", TypeName = "NUMBER(15,2)")]
    public decimal? BfkppostIntamt { get; set; }

    [Column("KPRECAL_ITEMAMT", TypeName = "NUMBER(15,2)")]
    public decimal? KprecalItemamt { get; set; }

    [Column("KPRECAL_PRNAMT", TypeName = "NUMBER(15,2)")]
    public decimal? KprecalPrnamt { get; set; }

    [Column("KPRECAL_INTAMT", TypeName = "NUMBER(15,2)")]
    public decimal? KprecalIntamt { get; set; }

    [Column("CASE_POST")]
    [StringLength(30)]
    
    public string? CasePost { get; set; }

    [Column("DIINT_PERIOD", TypeName = "NUMBER(15,2)")]
    public decimal? DiintPeriod { get; set; }

    [Column("RECALINTAFKP_AMT", TypeName = "NUMBER(15,2)")]
    public decimal? RecalintafkpAmt { get; set; }

    [Column("ADJUST_ID")]
    [StringLength(25)]
    
    public string? AdjustId { get; set; }

    [Column("ADJUST_DATE", TypeName = "DATE")]
    public DateTime? AdjustDate { get; set; }

    [Column("REALITEM_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? RealitemPayment { get; set; }

    [Column("REALPRINCIPAL_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? RealprincipalPayment { get; set; }

    [Column("REALINTEREST_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? RealinterestPayment { get; set; }

    [Column("CCLKEPPRN_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? CclkepprnPayment { get; set; }

    [Column("CCLKEPINT_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? CclkepintPayment { get; set; }

    [Column("CCLKEPITEM_PAYMENT", TypeName = "NUMBER(15,2)")]
    public decimal? CclkepitemPayment { get; set; }

    [Column("AFKEPPAY_REFSLIP")]
    [StringLength(15)]
    
    public string? AfkeppayRefslip { get; set; }

    [Column("AFKEPPAY_DATE", TypeName = "DATE")]
    public DateTime? AfkeppayDate { get; set; }

    [Column("KEPEXENSE_CODE")]
    [StringLength(3)]
    
    public string? KepexenseCode { get; set; }

    [Column("KEPEXENSE_ACCID")]
    [StringLength(15)]
    
    public string? KepexenseAccid { get; set; }

    [Column("TRANTOVC_DATE", TypeName = "DATE")]
    public DateTime? TrantovcDate { get; set; }

}
