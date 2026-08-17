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
