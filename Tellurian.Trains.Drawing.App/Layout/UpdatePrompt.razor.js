// Tells .NET when a new version is waiting (see service-worker-registration.js), and applies it.
let listener = null;

export function subscribe(dotnet) {
    const state = window.tellurianDrawingUpdate;
    listener = () => dotnet.invokeMethodAsync('UpdateAvailable');
    window.addEventListener('tellurian-update-available', listener);
    return !!state?.waiting;
}

export function unsubscribe() {
    if (listener) window.removeEventListener('tellurian-update-available', listener);
    listener = null;
}

// The new service worker takes over; its controllerchange event reloads the page.
export function apply() {
    const worker = window.tellurianDrawingUpdate?.waiting;
    if (!worker) return false;
    worker.postMessage({ type: 'SKIP_WAITING' });
    return true;
}
