using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

// Single global row (no CompanyId — this is a deployment-wide UI concern,
// not a payroll-domain one: which languages the language switcher offers
// should not differ depending on which company's data a user happens to be
// viewing). Thai is not a column here because it is the hardcoded ultimate
// fallback in JsonLocalizationService and is always available regardless of
// this settings row's contents — only the optional languages are toggled.
// Deliberately no seeded row (same pattern as Talent_NineBoxSettings):
// SystemLanguageSettingsService returns an in-memory default (English on,
// Chinese/Japanese off) when none exists yet; a real row is only written
// once an admin saves the settings page.
[Table("SystemLanguageSettings")]
public class SystemLanguageSettings
{
    [Key]
    public long Id { get; set; }

    public bool EnableEnglish { get; set; } = true;

    public bool EnableChinese { get; set; }

    public bool EnableJapanese { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    public long? ModifiedByUserId { get; set; }
}
