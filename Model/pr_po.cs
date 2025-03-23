using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("pr_po")]
public partial class pr_po
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(250)]
    
    public string? org_id { get; set; }

    [StringLength(250)]
    
    public string? req_number { get; set; }

    [StringLength(250)]
    
    public string? req_description { get; set; }

    [StringLength(250)]
    
    public string? vendor_code { get; set; }

    [StringLength(250)]
    
    public string? vendor_name { get; set; }

    [StringLength(250)]
    
    public string? creation_date { get; set; }

    [StringLength(250)]
    
    public string? type_code { get; set; }

    [StringLength(250)]
    
    public string? req_line_num { get; set; }

    [StringLength(250)]
    
    public string? item_description { get; set; }

    [StringLength(250)]
    
    public string? uom_code { get; set; }

    [StringLength(250)]
    
    public string? unit_price { get; set; }

    [StringLength(250)]
    
    public string? quantity { get; set; }

    [StringLength(250)]
    
    public string? line_amount { get; set; }

    [StringLength(250)]
    
    public string? cancel_flag { get; set; }

    [StringLength(250)]
    
    public string? requestor { get; set; }

    [StringLength(250)]
    
    public string? user_name { get; set; }

    [StringLength(250)]
    
    public string? user_description { get; set; }

    [StringLength(250)]
    
    public string? authorization_status { get; set; }

    [StringLength(250)]
    
    public string? approved_date { get; set; }

    [StringLength(250)]
    
    public string? company_code { get; set; }

    [StringLength(250)]
    
    public string? company_desc { get; set; }

    [StringLength(250)]
    
    public string? costcenter_code { get; set; }

    [StringLength(250)]
    
    public string? costcenter_desc { get; set; }

    [StringLength(250)]
    
    public string? product_code { get; set; }

    [StringLength(250)]
    
    public string? product_desc { get; set; }

    [StringLength(250)]
    
    public string? po_number { get; set; }

    [StringLength(250)]
    
    public string? requisition_header_id { get; set; }

    [StringLength(250)]
    
    public string? requisition_line_id { get; set; }

    [StringLength(250)]
    
    public string? distribution_id { get; set; }

    [StringLength(250)]
    
    public string? po_header_id { get; set; }

    [StringLength(250)]
    
    public string? po_line_id { get; set; }

    [StringLength(250)]
    
    public string? po_distribution_id { get; set; }
}
