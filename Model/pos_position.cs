using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pos_position")]
public partial class pos_position
{
    [StringLength(15)]
    
    public string pos_code { get; set; } = null!;

    public int? code { get; set; }

    [StringLength(50)]
    
    public string? pos_exec_code { get; set; }

    [StringLength(15)]
    
    public string? sec_code { get; set; }

    [StringLength(250)]
    
    public string? name { get; set; }

    [StringLength(20)]
    
    public string? abbname { get; set; }

    [StringLength(250)]
    
    public string? dopaname { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? startdate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? expiredate { get; set; }

    [StringLength(50)]
    
    public string? min_c_level { get; set; }

    [StringLength(50)]
    
    public string? max_c_level { get; set; }

    [StringLength(40)]
    
    public string? salary_level { get; set; }

    [StringLength(1)]
    
    public string? isvacancy { get; set; }

    [StringLength(1)]
    
    public string? status { get; set; }

    
    public string? remark { get; set; }

    [StringLength(250)]
    
    public string? min_graduate { get; set; }

    public float? normal_salary { get; set; }

    public int? paymentrunid { get; set; }

    public int? lperiodid { get; set; }

    [StringLength(15)]
    
    public string? paymentno { get; set; }

    public int? ptp_code { get; set; }

    public int? employeetype { get; set; }

    public int posid { get; set; }

    [StringLength(1)]
    
    public string? ismanpower { get; set; }

    public float? max_salary { get; set; }

    
    public string? jobsummary { get; set; }

    [StringLength(50)]
    
    public string? personid { get; set; }

    [StringLength(250)]
    
    public string? engname { get; set; }

    [StringLength(250)]
    
    public string? engabbname { get; set; }

    public float? min_salary { get; set; }

    public float? real_salary { get; set; }

    [StringLength(1)]
    
    public string? is_boss { get; set; }

    [StringLength(50)]
    
    public string? worklineid { get; set; }

    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }
}
