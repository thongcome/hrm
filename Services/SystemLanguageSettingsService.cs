using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

// Backs the admin "how many languages" config page. Deliberately no seeded
// row (same pattern as Talent_NineBoxSettings) — GetSettingsAsync returns an
// in-memory default (English on, Chinese/Japanese off, matching this app's
// historical TH/EN-only behavior) until an admin actually saves the page.
public class SystemLanguageSettingsService(IDbContextFactory<HRMContext> dbFactory)
{
    public async Task<SystemLanguageSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.SystemLanguageSettingsList.FirstOrDefaultAsync(ct);
        return existing ?? new SystemLanguageSettings();
    }

    public async Task SaveSettingsAsync(bool enableEnglish, bool enableChinese, bool enableJapanese, long actorUserId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);
        var existing = await context.SystemLanguageSettingsList.FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            existing.EnableEnglish = enableEnglish;
            existing.EnableChinese = enableChinese;
            existing.EnableJapanese = enableJapanese;
            existing.ModifiedDate = DateTime.Now;
            existing.ModifiedByUserId = actorUserId;
        }
        else
        {
            context.SystemLanguageSettingsList.Add(new SystemLanguageSettings
            {
                EnableEnglish = enableEnglish,
                EnableChinese = enableChinese,
                EnableJapanese = enableJapanese,
                ModifiedDate = DateTime.Now,
                ModifiedByUserId = actorUserId,
            });
        }

        await context.SaveChangesAsync(ct);
    }

    // Ordered list of active culture codes: Thai first (always present —
    // the hardcoded JsonLocalizationService fallback), then whichever
    // optional languages are enabled. null settings (no row saved yet)
    // resolves to the historical TH/EN-only default.
    public static List<string> ResolveAvailableLanguages(SystemLanguageSettings? settings)
    {
        var result = new List<string> { LanguageCatalog.DefaultCulture };
        if (settings?.EnableEnglish ?? true) result.Add("en");
        if (settings?.EnableChinese ?? false) result.Add("zh");
        if (settings?.EnableJapanese ?? false) result.Add("ja");
        return result;
    }
}
