using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("po_inv")]
public partial class po_inv
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(10)]
    public string? org_id { get; set; }

    [StringLength(10)]
    public string? po_date { get; set; }

    [StringLength(10)]
    public string? po_number { get; set; }

    [StringLength(10)]
    public string? po_description { get; set; }

    [StringLength(10)]
    public string? vendor_code { get; set; }

    [StringLength(10)]
    public string? vendor_name { get; set; }

    [StringLength(10)]
    public string? authorization_status { get; set; }

    [StringLength(10)]
    public string? line_num { get; set; }

    [StringLength(10)]
    public string? item_description { get; set; }

    [StringLength(10)]
    public string? quantity { get; set; }

    [StringLength(10)]
    public string? unit_price { get; set; }

    [StringLength(10)]
    public string? line_amount { get; set; }

    [StringLength(10)]
    public string? user_name { get; set; }

    [StringLength(10)]
    public string? user_desc { get; set; }

    [StringLength(10)]
    public string? approved_flag { get; set; }

    [StringLength(10)]
    public string? unit_meas_lookup_code { get; set; }

    [StringLength(10)]
    public string? currency_code { get; set; }

    [StringLength(10)]
    public string? rate_type { get; set; }

    [StringLength(10)]
    public string? rate_date { get; set; }

    [StringLength(10)]
    public string? rate { get; set; }

    [StringLength(10)]
    public string? cancel_flag { get; set; }

    [StringLength(10)]
    public string? company_code { get; set; }

    [StringLength(10)]
    public string? company_desc { get; set; }

    [StringLength(10)]
    public string? costcenter_code { get; set; }

    [StringLength(10)]
    public string? costcenter_desc { get; set; }

    [StringLength(10)]
    public string? account_code { get; set; }

    [StringLength(10)]
    public string? account_desc { get; set; }

    [StringLength(10)]
    public string? product_code { get; set; }

    [StringLength(10)]
    public string? product_desc { get; set; }

    [StringLength(10)]
    public string? invoice_id { get; set; }

    [StringLength(10)]
    public string? invoice_number { get; set; }

    [StringLength(10)]
    public string? invoice_description { get; set; }

    [StringLength(10)]
    public string? invoice_line_number { get; set; }

    [StringLength(10)]
    public string? invoice_line_desc { get; set; }
}
