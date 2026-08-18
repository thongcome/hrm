namespace HRM.Services;

// Static catalog of languages the app knows how to render (i.e. has a
// wwwroot/Resources/{code}.json for). Adding a new language requires both a
// new entry here and a matching JSON translation file — this list is not
// itself database-driven because each entry needs actual translated content
// shipped with the app, not just a toggle. What IS database-driven (via
// SystemLanguageSettings/SystemLanguageSettingsService) is which of these
// already-supported languages an admin has chosen to expose.
public static class LanguageCatalog
{
    public const string DefaultCulture = "th";

    // Thai is intentionally absent from this list — it is the hardcoded,
    // non-optional fallback (see JsonLocalizationService), not a toggle.
    public static readonly IReadOnlyList<(string Code, string Label)> OptionalLanguages = new[]
    {
        ("en", "EN"),
        ("zh", "中文"),
        ("ja", "日本語"),
    };

    public static readonly IReadOnlyDictionary<string, string> AllLabels = new Dictionary<string, string>
    {
        [DefaultCulture] = "ไทย",
        ["en"] = "EN",
        ["zh"] = "中文",
        ["ja"] = "日本語",
    };
}
