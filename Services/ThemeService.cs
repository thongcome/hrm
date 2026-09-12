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
    public const string MenuStyleCookieName = "hrm_menu_style";

    // "a" (เส้นกรอบซ้าย), "b" (กล่องแยกกลุ่ม), "c" (เส้นคั่นบาง + จุดนำ) — three
    // answers to the same CEO complaint (13 ก.ย. 2569: menu reads as one
    // undifferentiated block, "ลอย"/"เป็นพืด") previewed as a mockup first,
    // then shipped as a live switcher (like the color theme picker) instead
    // of picking one and forcing everyone to it. See DbNavMenu.razor.css.
    public static readonly (string Id, string Label)[] MenuStyles =
    [
        ("a", "เส้นกรอบซ้าย"),
        ("b", "กล่องแยกกลุ่ม"),
        ("c", "เส้นคั่นบาง"),
    ];

    private readonly ThemeState _state;
    private readonly IJSRuntime _js;

    public string CurrentThemeId { get; private set; } = ThemeCatalog.Options[0].Id;
    public bool IsDarkMode { get; private set; }
    public string MenuStyleId { get; private set; } = MenuStyles[0].Id;

    public ThemeCatalog.ThemeOption CurrentTheme => ThemeCatalog.GetById(CurrentThemeId);

    public ThemeService(ThemeState state, IHttpContextAccessor httpContextAccessor, IJSRuntime js)
    {
        _state = state;
        _js = js;

        var themeCookie = httpContextAccessor.HttpContext?.Request.Cookies[ThemeCookieName];
        CurrentThemeId = ThemeCatalog.GetById(themeCookie).Id;

        var darkCookie = httpContextAccessor.HttpContext?.Request.Cookies[DarkModeCookieName];
        IsDarkMode = darkCookie == "1";

        var menuStyleCookie = httpContextAccessor.HttpContext?.Request.Cookies[MenuStyleCookieName];
        MenuStyleId = MenuStyles.Any(m => m.Id == menuStyleCookie) ? menuStyleCookie! : MenuStyles[0].Id;
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

    public async Task SetMenuStyleAsync(string menuStyleId)
    {
        MenuStyleId = MenuStyles.Any(m => m.Id == menuStyleId) ? menuStyleId : MenuStyles[0].Id;
        await _js.InvokeVoidAsync("setLanguageCookie", MenuStyleCookieName, MenuStyleId);
        await _state.NotifyAsync();
    }
}
