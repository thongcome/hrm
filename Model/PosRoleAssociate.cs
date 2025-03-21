using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("PosRoleAssociate")]
public partial class PosRoleAssociate
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    public long roleid { get; set; }

    public long posid { get; set; }

    [StringLength(250)]
    
    public string rolecode { get; set; } = null!;

    [MaxLength(250)]
    public byte[] poscode { get; set; } = null!;

    public byte? isactive { get; set; }

    [MaxLength(250)]
    public byte[]? remark { get; set; }

    public DateOnly? startdate { get; set; }

    public DateOnly? enddate { get; set; }
}
