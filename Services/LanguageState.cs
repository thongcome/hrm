using System.Linq;

namespace HRM.Services
{
   
    public class LanguageState
    {
        public event Func<Task>? OnLanguageChangedAsync;

        // Calling a multicast Func<Task> delegate directly via .Invoke()
        // only awaits the LAST subscriber's Task — every earlier subscriber
        // is started but never awaited (exceptions swallowed, completion not
        // guaranteed before this method returns). Harmless while only one
        // page ever subscribed at a time, but now that every localized page
        // subscribes alongside MainLayout, multiple simultaneous subscribers
        // is the normal case, so invoke each handler explicitly.
        public async Task NotifyAsync()
        {
            if (OnLanguageChangedAsync is null) return;
            foreach (var handler in OnLanguageChangedAsync.GetInvocationList().Cast<Func<Task>>())
            {
                await handler();
            }
        }
    }

}
