using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("CT_Contract")]
public partial class CT_Contract
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string po_no { get; set; } = null!;

    [StringLength(50)]
    
    public string? constract_no { get; set; }

    [StringLength(50)]
    
    public string? vendor_code { get; set; }

    [StringLength(250)]
    
    public string? vendor_name { get; set; }

    [StringLength(20)]
    
    public string? tax_id { get; set; }

    [StringLength(250)]
    
    public string? requester { get; set; }

    [StringLength(50)]
    
    public string? requestercode { get; set; }

    [StringLength(250)]
    
    public string? requesterOrg { get; set; }

    [StringLength(250)]
    
    public string? requester_email { get; set; }

    public DateOnly? start_date { get; set; }

    public DateOnly? expired_date { get; set; }

    public bool isNeedExtends { get; set; }

    public string? comment { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isActive { get; set; }

    public string? remark { get; set; }

    [Column(TypeName = "money")]
    public decimal? ProjectAmount { get; set; }

    [Column(TypeName = "money")]
    public decimal? ProjectUtilization { get; set; }

    [Column(TypeName = "money")]
    public decimal? ProjectRemaining { get; set; }

    public long? ProjectCurencyID { get; set; }

    public bool? isWarrantyRequired { get; set; }

    public long? WarrantyType { get; set; }

    public DateOnly? WarrantyStartDate { get; set; }

    public DateOnly? WarrantyEnddate { get; set; }

    public bool? isBankGuarantee { get; set; }

    [Column(TypeName = "money")]
    public decimal? BankGuaranteeAmount { get; set; }

    public DateOnly? BankGuaranteeEffectiveDate { get; set; }

    public DateOnly? BankGuaranteeExpiryDate { get; set; }

    [StringLength(1000)]
    public string? BankGuaranteeRemark { get; set; }

    public long? BankGuaranteeCurrencyID { get; set; }

    public bool? isInsurancePolicyRequired { get; set; }

    public long? InsurancePolicyType { get; set; }

    [Column(TypeName = "money")]
    public decimal? InsurancePolicyAmountCoverage { get; set; }

    public DateOnly? InsurancePolicyEffectiveDate { get; set; }

    public DateOnly? InsurancePolicyExpiryDate { get; set; }

    [StringLength(1000)]
    public string? InsurancePolicyRemark { get; set; }

    public long? InsurancePolicyCurrencyID { get; set; }

    public bool? isContractDocumentStatusComplete { get; set; }

    [StringLength(50)]
    
    public string? DateNotice { get; set; }

    public TimeOnly? RemainPeriod { get; set; }

    [StringLength(250)]
    
    public string? ProjectName { get; set; }

    [ForeignKey("BankGuaranteeCurrencyID")]
    [InverseProperty("CT_ContractBankGuaranteeCurrencies")]
    public virtual Currency? BankGuaranteeCurrency { get; set; }

    [ForeignKey("InsurancePolicyCurrencyID")]
    [InverseProperty("CT_ContractInsurancePolicyCurrencies")]
    public virtual Currency? InsurancePolicyCurrency { get; set; }

    [ForeignKey("ProjectCurencyID")]
    [InverseProperty("CT_ContractProjectCurencies")]
    public virtual Currency? ProjectCurency { get; set; }
}
