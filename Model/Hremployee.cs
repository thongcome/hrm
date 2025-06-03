using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;


[Table("HREMPLOYEE")]
public partial class Hremployee
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    [Column("ID")]
    public long id { get; set; }

    [Column("companyid")]
    [StringLength(6)]
    
    public string companyid { get; set; } = null!;

    
    [Column("EMP_NO")]
    [StringLength(6)]
    
    public string EmpNo { get; set; } = null!;

    [Column("PRENAME_CODE")]
    [StringLength(3)]
    
    public string? PrenameCode { get; set; }

    [Column("EMP_NAME")]
    [StringLength(250)]
    
    public string? EmpName { get; set; }

    [Column("EMP_SURNAME")]
    [StringLength(250)]
    
    public string? EmpSurname { get; set; }

    [Column("EMP_ENAME")]
    [StringLength(50)]
    
    public string? EmpEname { get; set; }

    [Column("EMP_ESURNAME")]
    [StringLength(50)]
    
    public string? EmpEsurname { get; set; }

    [Column("EMP_NICKNAME")]
    [StringLength(50)]
    
    public string? EmpNickname { get; set; }

    [Column("EMPTYPE_CODE")]
    [StringLength(2)]
    
    public string? EmptypeCode { get; set; }

    [Column("DEPTGRP_CODE")]
    [StringLength(5)]
    
    public string? DeptgrpCode { get; set; }

    [Column("POS_CODE")]
    [StringLength(3)]
    
    public string? PosCode { get; set; }

    [Column("SEX")]
    [StringLength(1)]
    
    public string? Sex { get; set; }

    [Column("WEIGHT", TypeName = "decimal(5,2)")]
    public decimal? Weight { get; set; }

    [Column("HEIGHT")]
    [Precision(3)]
    public decimal? Height { get; set; }

    [Column("NATION")]
    [StringLength(20)]
    
    public string? Nation { get; set; }

    [Column("RELIGION")]
    [StringLength(20)]
    
    public string? Religion { get; set; }

    [Column("ADN_NO")]
    [StringLength(150)]
    
    public string? AdnNo { get; set; }

    [Column("ADN_TAMBOL")]
    [StringLength(20)]
    
    public string? AdnTambol { get; set; }

    [Column("ADN_AMPHUR")]
    [StringLength(20)]
    
    public string? AdnAmphur { get; set; }

    [Column("ADN_PROVINCE")]
    [StringLength(20)]
    
    public string? AdnProvince { get; set; }

    [Column("ADN_POSTCODE")]
    [StringLength(10)]
    
    public string? AdnPostcode { get; set; }

    [Column("ADN_TEL")]
    [StringLength(50)]
    
    public string? AdnTel { get; set; }

    [Column("ADN_EMAIL")]
    [StringLength(50)]
    
    public string? AdnEmail { get; set; }

    [Column("ADR_NO")]
    [StringLength(150)]
    
    public string? AdrNo { get; set; }

    [Column("ADR_TAMBOL")]
    [StringLength(20)]
    
    public string? AdrTambol { get; set; }

    [Column("ADR_AMPHUR")]
    [StringLength(20)]
    
    public string? AdrAmphur { get; set; }

    [Column("ADR_PROVINCE")]
    [StringLength(20)]
    
    public string? AdrProvince { get; set; }

    [Column("ADR_POSTCODE")]
    [StringLength(20)]
    
    public string? AdrPostcode { get; set; }

    [Column("ID_CARD")]
    [StringLength(13)]
    
    public string? IdCard { get; set; }

    [Column("BIRTH_DATE", TypeName = "DATE")]
    public DateTime? BirthDate { get; set; }

    [Column("WORK_DATE", TypeName = "DATE")]
    public DateTime? WorkDate { get; set; }

    [Column("RESIGN_DATE", TypeName = "DATE")]
    public DateTime? ResignDate { get; set; }

    [Column("CONTAIN_DATE", TypeName = "DATE")]
    public DateTime? ContainDate { get; set; }

    [Column("TERM_DATE", TypeName = "DATE")]
    public DateTime? TermDate { get; set; }

    [Column("SALARY_AMT", TypeName = "decimal(15,2)")]
    public decimal? SalaryAmt { get; set; }

    [Column("DAILY_WAGE", TypeName = "decimal(10,2)")]
    public decimal? DailyWage { get; set; }

    [Column("SALEXP_CODE")]
    [StringLength(3)]
    
    public string? SalexpCode { get; set; }

    [Column("SALEXP_BANK")]
    [StringLength(3)]
    
    public string? SalexpBank { get; set; }

    [Column("SALEXP_BRANCH")]
    [StringLength(4)]
    
    public string? SalexpBranch { get; set; }

    [Column("SALEXP_ACCID")]
    [StringLength(15)]
    
    public string? SalexpAccid { get; set; }

    [Column("TAX_CALCODE")]
    [StringLength(3)]
    
    public string? TaxCalcode { get; set; }

    [Column("TAX_BFAMT", TypeName = "decimal(15,2)")]
    public decimal? TaxBfamt { get; set; }

    [Column("SS_STATUS")]
    [Precision(2)]
    public decimal? SsStatus { get; set; }

    [Column("SS_APPFIRSTSTS")]
    [Precision(2)]
    public decimal? SsAppfirststs { get; set; }

    [Column("SS_BFAMT", TypeName = "decimal(15,2)")]
    public decimal? SsBfamt { get; set; }

    [Column("SS_APPDATE", TypeName = "DATE")]
    public DateTime? SsAppdate { get; set; }

    [Column("SS_RATE", TypeName = "decimal(5,2)")]
    public decimal? SsRate { get; set; }

    [Column("SS_HOSPITAL")]
    [StringLength(100)]
    
    public string? SsHospital { get; set; }

    [Column("PROVF_STATUS")]
    [Precision(2)]
    public decimal? ProvfStatus { get; set; }

    [Column("PROVF_CORPRATE", TypeName = "decimal(5,2)")]
    public decimal? ProvfCorprate { get; set; }

    [Column("PROVF_EMPRATE", TypeName = "decimal(5,2)")]
    public decimal? ProvfEmprate { get; set; }

    [Column("PROVF_APPDATE", TypeName = "DATE")]
    public DateTime? ProvfAppdate { get; set; }

    [Column("PROVF_RESIGNDATE", TypeName = "DATE")]
    public DateTime? ProvfResigndate { get; set; }

    [Column("PROVF_BFAMT", TypeName = "decimal(15,2)")]
    public decimal? ProvfBfamt { get; set; }

    [Column("REF_MEMBNO")]
    [StringLength(8)]
    
    public string? RefMembno { get; set; }

    [Column("EMP_STATUS")]
    [Precision(2)]
    public decimal? EmpStatus { get; set; }

    [Column("BLOODTYPE_CODE")]
    [StringLength(3)]
    
    public string? BloodtypeCode { get; set; }

    [Column("EMPLEVEL_CODE")]
    [StringLength(2)]
    
    public string? EmplevelCode { get; set; }

    [Column("PROFESSIONAL_AMT", TypeName = "decimal(10,2)")]
    public decimal? ProfessionalAmt { get; set; }

    [Column("COMPENSATION_AMT1", TypeName = "decimal(10,2)")]
    public decimal? CompensationAmt1 { get; set; }

    [Column("COMPENSATION_AMT2", TypeName = "decimal(10,2)")]
    public decimal? CompensationAmt2 { get; set; }

    [Column("EMER_NAME")]
    [StringLength(50)]
    
    public string? EmerName { get; set; }

    [Column("EMER_SURNAME")]
    [StringLength(50)]
    
    public string? EmerSurname { get; set; }

    [Column("CONCERN_CODE")]
    [StringLength(2)]
    
    public string? ConcernCode { get; set; }

    [Column("EMER_PHONE")]
    [StringLength(12)]
    
    public string? EmerPhone { get; set; }

    [Column("EMER_ADDRESS")]
    [StringLength(255)]
    
    public string? EmerAddress { get; set; }

    [Column("LEVEL_CODE")]
    [StringLength(4)]
    
    public string? LevelCode { get; set; }

    [Column("WORKTIME_CODE")]
    [StringLength(2)]
    
    public string? WorktimeCode { get; set; }

    [Column("LEAVE_BF", TypeName = "decimal(10,2)")]
    public decimal? LeaveBf { get; set; }

    [Column("OVERLEAVE_FLAG")]
    public Int32 OverleaveFlag { get; set; }

    [Column("EMP_SPECIAL")]
    [StringLength(200)]
    
    public string? EmpSpecial { get; set; }

    [Column("UPDATE_BYENTRYID")]
    [StringLength(32)]
    
    public string? UpdateByentryid { get; set; }

    [Column("UPDATE_BYENTRYIP")]
    [StringLength(50)]
    
    public string? UpdateByentryip { get; set; }

    [Column("DEPT_FIRST", TypeName = "decimal(10,2)")]
    public decimal? DeptFirst { get; set; }

    [Column("SCAN_NO")]
    [StringLength(3)]
    
    public string? ScanNo { get; set; }

    [Column("LAVEL_ACTIVE_DATE", TypeName = "DATE")]
    public DateTime? LavelActiveDate { get; set; }

    [Column("CONTRACT_NO")]
    [StringLength(3)]
    
    public string? ContractNo { get; set; }

    [Column("CONTRACT_DATE", TypeName = "DATE")]
    public DateTime? ContractDate { get; set; }

    [Column("HEALTH_CHECK1", TypeName = "decimal(12,2)")]
    public decimal? HealthCheck1 { get; set; }

    [Column("HEALTH_CHECK2", TypeName = "decimal(12,2)")]
    public decimal? HealthCheck2 { get; set; }

    [Column("VACCINE_AMT", TypeName = "decimal(12,2)")]
    public decimal? VaccineAmt { get; set; }

    [Column("MATE_CODE")]
    [StringLength(2)]
    
    public string? MateCode { get; set; }

    [Column("HR_TYPE")]
    [StringLength(2)]
    
    public string? HrType { get; set; }

    [Column("PROVFCORP_BFAMT", TypeName = "decimal(15,2)")]
    public decimal? ProvfcorpBfamt { get; set; }

    [Column("PROVF_AMT", TypeName = "decimal(15,2)")]
    public decimal? ProvfAmt { get; set; }

    [Column("PROVFCORP_AMT", TypeName = "decimal(15,2)")]
    public decimal? ProvfcorpAmt { get; set; }

    [Column("ASSIST_BF", TypeName = "decimal(15,2)")]
    public decimal? AssistBf { get; set; }
}
