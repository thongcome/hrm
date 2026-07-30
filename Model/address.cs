using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

// Normalized, typed address store — one employee can have multiple rows,
// distinguished by address_type_id (registered / current-contact / etc.,
// see mas_address_type). Modeled after the existing vd_address/mas_address_type
// pattern (vendor addresses) rather than inventing a new shape; supersedes
// Hremployee's flat ADN_*/ADR_* columns going forward (those are left in
// place — see the one-time data-copy migration — since the actively-used
// PayrollEmployeeAdmin.razor never touched them, only the legacy scaffolded
// HremployeePages/ CRUD pages do, and migrating those is a separate task).
[Table("address")]
public partial class address
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long id { get; set; }

    public long hremployeeid { get; set; }

    public long? address_type_id { get; set; }

    [StringLength(100)]
    public string? no { get; set; }

    [StringLength(250)]
    public string? road { get; set; }

    [StringLength(250)]
    public string? soi { get; set; }

    [StringLength(250)]
    public string? moo { get; set; }

    [StringLength(250)]
    public string? buildingname { get; set; }

    [StringLength(100)]
    public string? village { get; set; }

    [StringLength(100)]
    public string? subdistrict { get; set; }

    // free text, not an FK — no district/amphur master table exists in this
    // system yet (same looseness as vd_address's district/sub_district)
    [StringLength(100)]
    public string? districtid { get; set; }

    public long? provinceid { get; set; }

    [StringLength(20)]
    public string? province { get; set; }

    [StringLength(10)]
    public string? postcode { get; set; }

    [StringLength(50)]
    public string? tel { get; set; }

    [StringLength(50)]
    public string? mobileno { get; set; }

    [StringLength(18)]
    public string? officeno { get; set; }

    [StringLength(18)]
    public string? fax { get; set; }

    [StringLength(250)]
    public string? email { get; set; }

    [StringLength(1000)]
    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? createdate { get; set; }

    public long? createby { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    public long? modby { get; set; }

    // not in the legacy DDL — added so a superseded row can be soft-retired
    // later without deleting history
    public bool isactive { get; set; } = true;

    [ForeignKey("hremployeeid")]
    [InverseProperty("addresses")]
    public virtual Hremployee Hremployee { get; set; } = null!;

    [ForeignKey("address_type_id")]
    [InverseProperty("addresses")]
    public virtual mas_address_type? address_typeNavigation { get; set; }

    [ForeignKey("provinceid")]
    [InverseProperty("addresses")]
    public virtual mas_province? provinceNavigation { get; set; }
}
