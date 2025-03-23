using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("Currency")]
public partial class Currency
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(50)]
    
    public string code { get; set; } = null!;

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(250)]
    
    public string? name_en { get; set; }

    public bool? isactive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [InverseProperty("BankGuaranteeCurrency")]
    public virtual ICollection<CT_Contract> CT_ContractBankGuaranteeCurrencies { get; set; } = new List<CT_Contract>();

    [InverseProperty("InsurancePolicyCurrency")]
    public virtual ICollection<CT_Contract> CT_ContractInsurancePolicyCurrencies { get; set; } = new List<CT_Contract>();

    [InverseProperty("ProjectCurency")]
    public virtual ICollection<CT_Contract> CT_ContractProjectCurencies { get; set; } = new List<CT_Contract>();

    [InverseProperty("capitalCurrency")]
    public virtual ICollection<vd_general_info> vd_general_infos { get; set; } = new List<vd_general_info>();
}
