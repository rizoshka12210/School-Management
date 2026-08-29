// Minimal service worker: makes the site installable as a PWA and caches
// static assets for faster repeat loads. It deliberately does NOT cache
// or intercept pages or API-like requests - grades, attendance, notifications
// and everything else always comes fresh from the network, and login/logout
// POSTs are never touched. This keeps the app correct while still letting
// browsers show the "Install app" prompt.

const CACHE_NAME = "school-static-v1";

const STATIC_ASSETS = [
    "/manifest.webmanifest",
    "/icons/icon-192.png",
    "/icons/icon-512.png",
    "/icons/icon-maskable-192.png",
    "/icons/icon-maskable-512.png",
    "/css/site.css",
    "/css/features.css",
    "/css/icons.css",
    "/css/polish.css",
    "/favicon.ico"
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => cache.addAll(STATIC_ASSETS))
            .catch(() => {
                // Some assets may 404 in a given environment - that's fine,
                // installability doesn't depend on every asset being cached.
            })
    );
    self.skipWaiting();
});

self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches.keys().then((names) =>
            Promise.all(
                names
                    .filter((name) => name !== CACHE_NAME)
                    .map((name) => caches.delete(name))
            )
        )
    );
    self.clients.claim();
});

function isStaticAsset(url) {
    return (
        url.pathname.startsWith("/css/") ||
        url.pathname.startsWith("/icons/") ||
        url.pathname.startsWith("/lib/") ||
        url.pathname === "/manifest.webmanifest" ||
        url.pathname === "/favicon.ico"
    );
}

self.addEventListener("fetch", (event) => {
    const request = event.request;

    // Only ever handle same-origin GET requests for static assets.
    // Everything else (pages, forms, POSTs, other origins) goes straight
    // to the network untouched.
    if (request.method !== "GET") {
        return;
    }

    const url = new URL(request.url);

    if (url.origin !== self.location.origin || !isStaticAsset(url)) {
        return;
    }

    event.respondWith(
        caches.match(request).then((cached) => {
            const network = fetch(request)
                .then((response) => {
                    if (response && response.ok) {
                        const copy = response.clone();
                        caches.open(CACHE_NAME).then((cache) => cache.put(request, copy));
                    }
                    return response;
                })
                .catch(() => cached);

            return cached || network;
        })
    );
});
