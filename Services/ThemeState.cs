using System.Linq;

namespace HRM.Services
{
    // Pure pub-sub notifier, no data of its own — mirrors LanguageState.cs
    // exactly. ThemeService (not this class) owns the actual CurrentThemeId/
    // IsDarkMode values; this just tells every subscribed component (mainly
    // MainLayout, which hosts MudThemeProvider) to re-render when they change.
    public class ThemeState
    {
        public event Func<Task>? OnThemeChangedAsync;

        // Same reasoning as LanguageState.NotifyAsync: invoking a multicast
        // Func<Task> delegate via .Invoke() only awaits the last subscriber,
        // so invoke each handler explicitly instead.
        public async Task NotifyAsync()
        {
            if (OnThemeChangedAsync is null) return;
            foreach (var handler in OnThemeChangedAsync.GetInvocationList().Cast<Func<Task>>())
            {
                await handler();
            }
        }
    }
}
