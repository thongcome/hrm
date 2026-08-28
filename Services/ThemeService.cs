using HRM.Components.Layout;

using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace HRM.Services;

// Mirrors JsonLocalizationService's cookie pattern exactly: reads the saved
// choice once at circuit construction (a Blazor Server circuit can read
// request cookies fine at start, just can't set a Set-Cookie response
// header mid-circuit), and persists changes via the same JS interop cookie
// writer the language switcher already uses (window.setLanguageCookie is a
// plain (name, value) cookie setter despite its name — reused here rather
// than adding a near-duplicate JS file for one more string cookie).
public class ThemeService
{
    public const string ThemeCookieName = "hrm_theme";
    public const string DarkModeCookieName = "hrm_dark_mode";

    private readonly ThemeState _state;
    private readonly IJSRuntime _js;

    public string CurrentThemeId { get; private set; } = ThemeCatalog.Options[0].Id;
    public bool IsDarkMode { get; private set; }

    public ThemeCatalog.ThemeOption CurrentTheme => ThemeCatalog.GetById(CurrentThemeId);

    public ThemeService(ThemeState state, IHttpContextAccessor httpContextAccessor, IJSRuntime js)
    {
        _state = state;
        _js = js;

        var themeCookie = httpContextAccessor.HttpContext?.Request.Cookies[ThemeCookieName];
        CurrentThemeId = ThemeCatalog.GetById(themeCookie).Id;

        var darkCookie = httpContextAccessor.HttpContext?.Request.Cookies[DarkModeCookieName];
        IsDarkMode = darkCookie == "1";
    }

    public async Task SetThemeAsync(string themeId)
    {
        CurrentThemeId = ThemeCatalog.GetById(themeId).Id;
        await _js.InvokeVoidAsync("setLanguageCookie", ThemeCookieName, CurrentThemeId);
        await _state.NotifyAsync();
    }

    public async Task SetDarkModeAsync(bool value)
    {
        IsDarkMode = value;
        await _js.InvokeVoidAsync("setLanguageCookie", DarkModeCookieName, value ? "1" : "0");
        await _state.NotifyAsync();
    }
}
