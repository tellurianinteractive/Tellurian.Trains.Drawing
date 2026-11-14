// Registers the service worker and keeps track of a new version that is waiting to take over (AP-1).
// The app shows the prompt (UpdatePrompt.razor) and applies the update through SafeReload (AP-7),
// so unsaved work is flushed before the page reloads.
(function () {
    const state = window.tellurianDrawingUpdate = { waiting: null };
    if (!('serviceWorker' in navigator)) return;
    let reloading = false;

    function available(worker) {
        state.waiting = worker;
        window.dispatchEvent(new CustomEvent('tellurian-update-available'));
    }

    navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).then(registration => {
        if (registration.waiting && navigator.serviceWorker.controller) available(registration.waiting);
        registration.addEventListener('updatefound', () => {
            const installing = registration.installing;
            installing?.addEventListener('statechange', () => {
                if (installing.state === 'installed' && navigator.serviceWorker.controller) available(installing);
            });
        });
    });

    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (reloading) return;
        reloading = true;
        window.location.reload();
    });
})();
