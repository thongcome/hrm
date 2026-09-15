// Writes the hrm_lang cookie from within a live Blazor Server circuit.
// JsonLocalizationService can't set a Set-Cookie response header mid-circuit
// (same reason /login-handler exists as a real endpoint instead of a
// component event handler), but document.cookie is a plain DOM write and
// works fine at any time. Only persists the choice for the NEXT circuit
// (page refresh, new tab, re-login) — the current circuit's live re-render
// is driven separately by LanguageState.NotifyAsync().
window.setLanguageCookie = (name, value) => {
    const oneYear = 365 * 24 * 60 * 60;
    document.cookie = `${name}=${value}; path=/; max-age=${oneYear}; samesite=lax`;
};

// Submits the <select>'s own <form> on change (CompanySwitcher.razor). An
// inline onchange="this.form.submit()" HTML attribute would be simplest,
// but this app's CSP (script-src 'self', no unsafe-inline) blocks inline
// event-handler attributes exactly like it blocks inline <script> tags —
// confirmed live: selecting a new company silently did nothing until this
// moved to a real @onchange (Blazor's own event wiring, not an inline
// attribute) calling out to this external, CSP-compliant function instead.
window.submitElementForm = (selectElement) => {
    selectElement.form?.submit();
};
