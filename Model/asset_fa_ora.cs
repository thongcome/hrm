using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("asset_fa_ora")]
public partial class asset_fa_ora
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? book_type_code { get; set; }

    [StringLength(250)]
    
    public string? asset_number { get; set; }

    [StringLength(250)]
    
    public string? asset_desc { get; set; }

    [StringLength(250)]
    
    public string? tag_number { get; set; }

    [StringLength(250)]
    
    public string? serial_number { get; set; }

    [StringLength(250)]
    
    public string? date_placed_in_service { get; set; }

    [StringLength(250)]
    
    public string? units_assigned { get; set; }

    [StringLength(250)]
    
    public string? original_cost { get; set; }

    [StringLength(250)]
    
    public string? COST { get; set; }

    [StringLength(250)]
    
    public string? salvage_value { get; set; }

    [StringLength(250)]
    
    public string? life_in_months { get; set; }

    [StringLength(250)]
    
    public string? asset_year { get; set; }

    [StringLength(250)]
    
    public string? asset_month { get; set; }

    [StringLength(250)]
    
    public string? asset_category { get; set; }

    [StringLength(250)]
    
    public string? asset_category_desc { get; set; }

    [StringLength(250)]
    
    public string? date_effective { get; set; }

    [StringLength(250)]
    
    public string? employee_number { get; set; }

    [StringLength(250)]
    
    public string? full_name { get; set; }

    [StringLength(250)]
    
    public string? location_code { get; set; }

    [StringLength(250)]
    
    public string? company_code { get; set; }

    [StringLength(250)]
    
    public string? company_desc { get; set; }

    [StringLength(250)]
    
    public string? costcenter_code { get; set; }

    [StringLength(250)]
    
    public string? account_code { get; set; }

    [StringLength(250)]
    
    public string? account_desc { get; set; }

    [StringLength(250)]
    
    public string? product_code { get; set; }

    [StringLength(250)]
    
    public string? product_desc { get; set; }

    [StringLength(250)]
    
    public string? asset_id { get; set; }

    [StringLength(250)]
    
    public string? distribution_id { get; set; }

    [StringLength(250)]
    
    public string? transaction_header_id_in { get; set; }

    [StringLength(250)]
    
    public string? transaction_header_id_out { get; set; }
}
