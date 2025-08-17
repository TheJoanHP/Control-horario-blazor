/* Manifest version: y2eyn3YW */
// Service Worker para Sphere Admin PWA
const CACHE_NAME = 'sphere-admin-v1.0.0';
const STATIC_CACHE_URLS = [
    '/',
    '/manifest.json',
    '/favicon.png'
];

// Instalar Service Worker
self.addEventListener('install', event => {
    console.log('[SW] Installing Service Worker');
    
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[SW] Caching static assets');
                return cache.addAll(STATIC_CACHE_URLS);
            })
    );
});

// Activar Service Worker
self.addEventListener('activate', event => {
    console.log('[SW] Activating Service Worker');
    
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME) {
                        console.log('[SW] Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
});

// Interceptar requests
self.addEventListener('fetch', event => {
    // Solo manejar requests GET
    if (event.request.method !== 'GET') {
        return;
    }

    // Estrategia Network First para la API
    if (event.request.url.includes('/api/')) {
        event.respondWith(
            fetch(event.request).catch(() => {
                // Si falla la red, podrías retornar un response offline
                return new Response(JSON.stringify({ error: 'Offline' }), {
                    headers: { 'Content-Type': 'application/json' }
                });
            })
        );
        return;
    }

    // Estrategia Cache First para assets estáticos
    event.respondWith(
        caches.match(event.request)
            .then(response => {
                if (response) {
                    return response;
                }
                return fetch(event.request);
            })
    );
});