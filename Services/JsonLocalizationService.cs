using HRM.Interface;
using HRM.Models;

using HRM.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Text.Json;

public class JsonLocalizationService : IJsonLocalizationService
{
    public const string CookieName = "hrm_lang";

    private readonly IWebHostEnvironment _env;
    private readonly LanguageState _state;
    private readonly IJSRuntime _js;
    private Dictionary<string, string> _translations = new();
    public string CurrentLanguage { get; private set; } = "th";

    // Which languages the admin-configured SystemLanguageSettings row
    // currently exposes (Thai always first/present — see
    // SystemLanguageSettingsService.ResolveAvailableLanguages). Computed
    // once per circuit at construction, same lifetime as the cookie read
    // below, so a language an admin disables mid-session still lingers
    // until the next circuit (refresh/re-login) — acceptable for a setting
    // that changes rarely.
    public IReadOnlyList<string> AvailableLanguages { get; private set; } = new List<string> { "th", "en" };

    public JsonLocalizationService(IWebHostEnvironment env, LanguageState state, IHttpContextAccessor httpContextAccessor, IJSRuntime js, IDbContextFactory<HRMContext> dbFactory)
    {
        _env = env;
        _state = state;
        _js = js;

        // Sync DB read at construction — consistent with the sync file read
        // in LoadLanguage below (this constructor already does blocking I/O;
        // adding one more small, rarely-changing settings-row query doesn't
        // meaningfully change that).
        using var context = dbFactory.CreateDbContext();
        var settings = context.SystemLanguageSettingsList.FirstOrDefault();
        AvailableLanguages = SystemLanguageSettingsService.ResolveAvailableLanguages(settings);

        // Cookie set by SetLanguageAsync below (via JS interop, see there) —
        // read once here at circuit start so a fresh circuit (new tab,
        // refresh, re-login) opens in the language the user picked last time.
        var cookieValue = httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        CurrentLanguage = (cookieValue is not null && AvailableLanguages.Contains(cookieValue))
            ? cookieValue
            : LanguageCatalog.DefaultCulture;
        LoadLanguage(CurrentLanguage);
    }

    // Live switch: updates state + re-renders every subscribed component in
    // THIS circuit immediately (no page reload), and separately persists
    // the choice as a real cookie via JS interop (document.cookie — a
    // Blazor Server circuit can't set a Set-Cookie response header mid-
    // circuit) so the NEXT circuit opens in the same language. Falls back
    // to Thai if asked for a culture that isn't currently enabled (e.g. a
    // stale request for a language an admin has since disabled).
    public async Task SetLanguageAsync(string culture)
    {
        culture = AvailableLanguages.Contains(culture) ? culture : LanguageCatalog.DefaultCulture;
        CurrentLanguage = culture;
        LoadLanguage(culture);
        await _js.InvokeVoidAsync("setLanguageCookie", CookieName, culture);
        await _state.NotifyAsync();
    }


    private void LoadLanguage(string culture)
    {
        var filePath = Path.Combine(_env.WebRootPath, "Resources", $"{culture}.json");
      //  var filePath = Path.Combine(_env.ContentRootPath, "./Resources", $"{culture}.json");
        Console.WriteLine($"[Localization] Loading language file: {filePath}");

        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                           ?? new Dictionary<string, string>();
            Console.WriteLine($"[Localization] Loaded {_translations.Count} keys");
        }
        else
        {
            Console.WriteLine("[Localization] Language file not found.");
        }
    }

    public void SetLanguage(string culture)
    {
        CurrentLanguage = culture;
        LoadLanguage(culture);
    }

    public string Translate(string key)
    {
        if (_translations.TryGetValue(key, out var value))
        {
            return value;
        }

        return $"#{key}#"; // ถ้าไม่เจอ key จะ return ข้อความระบุว่าไม่เจอ
    }

}
