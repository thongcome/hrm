using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace hrm.Models;

[Table("pdpa_log_convertEndDec")]
public partial class pdpa_log_convertEndDec
{
    [Key]    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ กำหนดให้เป็น Auto-Increment
    public long id { get; set; }

    [StringLength(4000)]
    public string? subject { get; set; }

    [StringLength(4000)]
    public string? str_source { get; set; }

    [StringLength(4000)]
    public string? str_target { get; set; }

    [StringLength(4000)]
    public string? col_source { get; set; }

    [StringLength(4000)]
    public string? col_target { get; set; }

    [StringLength(4000)]
    public string? tab_target { get; set; }

    [StringLength(4000)]
    public string? tab_source { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? converttime { get; set; }

    [StringLength(4000)]
    public string? sys_name { get; set; }

    [StringLength(4000)]
    public string? uri_calling { get; set; }
}
