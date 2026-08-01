using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Registry row recording a table that was generated through the
// WorkflowDesign drag-and-drop screen builder (Components/Pages/Admin/
// WorkflowDesign.razor) — the actual generated table lives separately in
// the database (created via raw CREATE TABLE DDL); this row exists only so
// the designer page can list what it has already created and show the
// field layout used, since that layout isn't derivable from the generated
// table's column list alone (labels, original field order intent, etc.).
[Table("WorkflowDesignTable")]
public class WorkflowDesignTable
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(128)]
    public string TableName { get; set; } = null!;

    // JSON-serialized List<WorkflowDesignField> (see WorkflowDesign.razor) —
    // kept as a single JSON blob rather than a child table since this is a
    // read-mostly design record, not something queried per-field.
    [Required]
    public string FieldsJson { get; set; } = null!;

    public long CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
