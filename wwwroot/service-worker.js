// Minimal service worker for HumanOk PWA.
//
// This app is Blazor Server — its interactive UI needs a live SignalR
// connection, so we deliberately do NOT cache the app shell for offline use
// (that would only show a frozen, non-functional page). The service worker
// exists to satisfy PWA installability (an install + fetch handler is required)
// and to show a friendly offline page when a navigation fails with no network,
// instead of the browser's raw error. Everything else passes straight through
// to the network so the app is never served stale.
const CACHE = 'humanok-shell-v1';
const OFFLINE_URL = '/offline.html';

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE).then((cache) => cache.add(OFFLINE_URL)));
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  // Only intercept top-level navigations; let all other requests (framework,
  // SignalR, assets, API) go to the network untouched.
  if (event.request.mode === 'navigate') {
    event.respondWith(fetch(event.request).catch(() => caches.match(OFFLINE_URL)));
  }
});
