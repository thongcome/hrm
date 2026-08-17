using HRM.Services;
using Microsoft.AspNetCore.Components;

namespace HRM.Components.Shared;

// Inherit with @inherits LocalizedComponentBase on any page that calls
// JsonLocalizationService.Translate(...) so it re-renders live when the
// language is switched elsewhere (e.g. the toggle button in MainLayout,
// which is a separate component instance) — without this, a page keeps
// showing stale-language text until the user navigates away and back.
// MainLayout.razor itself can't use this (it already inherits
// LayoutComponentBase and C# doesn't allow multiple base classes) — it
// wires the same subscribe/unsubscribe directly in its own @code block.
public abstract class LocalizedComponentBase : ComponentBase, IDisposable
{
    [Inject] protected LanguageState LangState { get; set; } = default!;

    protected override void OnInitialized() => LangState.OnLanguageChangedAsync += HandleLanguageChangedAsync;

    private Task HandleLanguageChangedAsync() => InvokeAsync(StateHasChanged);

    public void Dispose() => LangState.OnLanguageChangedAsync -= HandleLanguageChangedAsync;
}
