using HRM.Interface;

using HRM.Services;

using Microsoft.AspNetCore.Http;
using System.Text.Json;

public class JsonLocalizationService : IJsonLocalizationService
{
    public const string CookieName = "hrm_lang";

    private readonly IWebHostEnvironment _env;
    private readonly LanguageState _state;
    private Dictionary<string, string> _translations = new();
    public string CurrentLanguage { get; private set; } = "th";

    public JsonLocalizationService(IWebHostEnvironment env, LanguageState state, IHttpContextAccessor httpContextAccessor)
    {
        _env = env;
        _state = state;

        // Cookie set by Endpoints/LanguageEndpoints.cs — this only reads it
        // once at circuit start, so the toggle in MainLayout.razor does a
        // real navigation (not a live SignalR state change) to guarantee a
        // fresh circuit picks up the new value from the start.
        var cookieValue = httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        CurrentLanguage = cookieValue == "en" ? "en" : "th";
        LoadLanguage(CurrentLanguage);
    }

    public async Task SetLanguageAsync(string culture)
    {
        CurrentLanguage = culture;
        LoadLanguage(culture);
        await _state.NotifyAsync(); // ใช้ async version
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
