using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("sc_role")]
public partial class sc_role
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long roleid { get; set; }

 
    public long company_id { get; set; }

    [ForeignKey("company_id")]
    public virtual com_company? company { get; set; }

    //public long CompanyId { get; set; } // ไม่ nullable = ต้องมี
    //[ForeignKey("CompanyId")]
    //public virtual com_company Company { get; set; } = null!;



    [StringLength(250)]
    public string? name { get; set; }

    [StringLength(100)]
    
    public string? abbr { get; set; }

    [StringLength(50)]
    
    public string rolelevel { get; set; } = null!;

    [StringLength(50)]
    public string? upperrole { get; set; }

    [StringLength(50)]
    
    public string? rolecode { get; set; }

    [Required]
    public bool isactive { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime? moddate { get; set; }

    [StringLength(250)]
    
    public string? modby { get; set; }

    public bool isHeader { get; set; } = false;

    [StringLength(500)]
    public string? ref1 { get; set; }

    [StringLength(500)]
    public string? ref2 { get; set; }

 
    [InverseProperty("role")]
    public virtual ICollection<sc_role_menu> sc_role_menus { get; set; } = new List<sc_role_menu>();

    [InverseProperty("role")]
    public virtual ICollection<sc_role_program> sc_role_programs { get; set; } = new List<sc_role_program>();

    [InverseProperty("role")]
    public virtual ICollection<sc_user_role> sc_user_roles { get; set; } = new List<sc_user_role>();
}
