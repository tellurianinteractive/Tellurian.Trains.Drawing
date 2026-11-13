// Registers the service worker and shows the "update available" banner (AP-1)
// when a new version has been installed and is waiting to take over.
(function () {
    if (!('serviceWorker' in navigator)) return;
    const banner = document.getElementById('update-available');
    const button = document.getElementById('update-now');
    let reloading = false;

    function showBanner(worker) {
        banner.hidden = false;
        button.onclick = () => worker.postMessage({ type: 'SKIP_WAITING' });
    }

    navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).then(registration => {
        if (registration.waiting && navigator.serviceWorker.controller) showBanner(registration.waiting);
        registration.addEventListener('updatefound', () => {
            const installing = registration.installing;
            installing?.addEventListener('statechange', () => {
                if (installing.state === 'installed' && navigator.serviceWorker.controller) showBanner(installing);
            });
        });
    });

    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (reloading) return;
        reloading = true;
        window.location.reload();
    });
})();
