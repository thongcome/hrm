using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Company holiday calendar — used by LeaveDayCalculator to exclude holidays
// (along with weekends) from a leave request's day count. A separate,
// modern-convention table rather than reusing the legacy Model/Holiday.cs
// scaffold: that table has an awkward NOT NULL hol_month/hol_day/year shape
// (no single date column, no CompanyId) and is otherwise unreferenced
// anywhere in the app (0 rows) — not a clean fit to build on.
[Table("Lve_CompanyHoliday")]
public class Lve_CompanyHoliday
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(6)]
    public string CompanyId { get; set; } = null!;

    public DateOnly HolidayDate { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
