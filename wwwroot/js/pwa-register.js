// Register the PWA service worker so HumanOk can be installed to the home screen
// (see wwwroot/service-worker.js for why it's minimal). A file, not an inline <script>: the
// Content-Security-Policy (Middleware/SecurityHeadersMiddleware, script-src 'self') blocks inline
// scripts, so the inline version never ran.
if ('serviceWorker' in navigator) {
    window.addEventListener('load', function () {
        navigator.serviceWorker.register('/service-worker.js').catch(function () { });
    });
}
