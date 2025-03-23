using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class Employee_datum
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    [StringLength(250)]
    
    public string Emp_key { get; set; } = null!;

    [StringLength(250)]
    
    public string? Name { get; set; }

    [StringLength(250)]
    
    public string? Surname { get; set; }

    [StringLength(250)]
    
    public string? Emp_no { get; set; }

    [StringLength(250)]
    
    public string? Emp_card { get; set; }

    [StringLength(250)]
    
    public string? Emp_Dept { get; set; }

    [StringLength(250)]
    
    public string? Passwd { get; set; }

    [StringLength(250)]
    
    public string? Head_Code { get; set; }

    [StringLength(250)]
    
    public string? Vacation { get; set; }

    [StringLength(250)]
    
    public string? sick { get; set; }

    [StringLength(250)]
    
    public string? business { get; set; }
}
