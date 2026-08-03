using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// ประเภทสังกัด (org level master) — ported from legacy PIS "sectiontype" table
// (see SQL_PIS_สำหรับversionJSP.txt). Acts as an org-level classification: each
// com_organization row points at one of these to say "what kind of level am I"
// (e.g. กรมการผู้จัดการใหญ่/ฝ่าย/ส่วน). levelno + istoplevel let code walk the
// hierarchy without hardcoding level names, matching the legacy comment
// "ระดับสูงสุดมีไว้เพื่อให้โปรแกรมอ่านตอน loop".
[Table("Com_SectionType")]
public class Com_SectionType
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string? AbbName { get; set; }

    [StringLength(200)]
    public string? NameEn { get; set; }

    [StringLength(50)]
    public string? AbbNameEn { get; set; }

    public int LevelNo { get; set; }

    public bool IsTopLevel { get; set; }

    // Self-referencing: legacy "uppersectype" — optional parent level type
    // (e.g. ฝ่าย is "above" ส่วน). Nullable soft-link by Code, not FK, matching
    // the string-code convention used elsewhere (parent_code on com_organization).
    [StringLength(20)]
    public string? UpperSecType { get; set; }

    [StringLength(1000)]
    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreateDate { get; set; }
    [StringLength(50)]
    public string? CreateBy { get; set; }
    public DateTime? ModDate { get; set; }
    [StringLength(50)]
    public string? ModBy { get; set; }
}
