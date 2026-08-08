using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// OrganizationId == null means the budget row covers the whole company for
// that fiscal year.
[Table("Lms_TrainingBudget")]
public class Lms_TrainingBudget
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public int FiscalYear { get; set; }

    public long? OrganizationId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal BudgetAmount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
