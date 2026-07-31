using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

[Table("sc_role_menu")]
public partial class sc_role_menu
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long rolemenuid { get; set; }

    public long menuid { get; set; }

    public long roleid { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    // Read-only lock: grants menu access but hides Create/Edit affordances
    // in pages built on Components/Shared/CrudScaffold.razor (checked via
    // the "menu_edit" claim added in ScUserClaimsPrincipalFactory). Default
    // true so every existing grant keeps working exactly as before.
    [Required]
    public bool canedit { get; set; } = true;

    [Column(TypeName = "text")]
    public string? remark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    [ForeignKey("menuid")]
    [InverseProperty("sc_role_menus")]
    public virtual sc_menu menu { get; set; } = null!;

    [ForeignKey("roleid")]
    [InverseProperty("sc_role_menus")]
    public virtual sc_role role { get; set; } = null!;
}
