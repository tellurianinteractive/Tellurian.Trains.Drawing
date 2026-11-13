// Spike C: files first, IndexedDB as workbench.
// Everything returned to .NET is a string (often JSON) or a boolean, so the interop needs no reflection.

const fileTypes = [{
    description: 'Tellurian Trains drawing',
    accept: { 'application/json': ['.tdmodule', '.tdendprofile', '.tdhall', '.tdhallplan', '.json'] }
}];

let currentHandle = null;

export function capabilities() {
    return JSON.stringify({
        openFilePicker: 'showOpenFilePicker' in window,
        saveFilePicker: 'showSaveFilePicker' in window,
        directoryPicker: 'showDirectoryPicker' in window,
        launchQueue: 'launchQueue' in window,
        storagePersist: !!navigator.storage?.persist,
        indexedDb: 'indexedDB' in window,
        standalone: window.matchMedia('(display-mode: standalone)').matches
    });
}

// ---------- Single files ----------

export async function openFile() {
    const [handle] = await window.showOpenFilePicker({ types: fileTypes, multiple: false });
    currentHandle = handle;
    await rememberHandle('recent-file', handle);
    const file = await handle.getFile();
    return JSON.stringify({ name: file.name, text: await file.text() });
}

export async function save(text, suggestedName, saveAs) {
    if (saveAs || !currentHandle) {
        currentHandle = await window.showSaveFilePicker({ suggestedName, types: fileTypes });
    }
    if ((await currentHandle.queryPermission({ mode: 'readwrite' })) !== 'granted' &&
        (await currentHandle.requestPermission({ mode: 'readwrite' })) !== 'granted') {
        throw new Error('Write permission denied');
    }
    const writable = await currentHandle.createWritable();
    await writable.write(text);
    await writable.close();
    await rememberHandle('recent-file', currentHandle);
    return currentHandle.name;
}

export function currentFileName() {
    return currentHandle?.name ?? '';
}

export async function reopenRecentFile() {
    const handle = await recallHandle('recent-file');
    if (!handle) return '';
    if ((await handle.queryPermission({ mode: 'read' })) !== 'granted' &&
        (await handle.requestPermission({ mode: 'read' })) !== 'granted') {
        return JSON.stringify({ error: 'Read permission denied' });
    }
    currentHandle = handle;
    const file = await handle.getFile();
    return JSON.stringify({ name: file.name, text: await file.text() });
}

// Fallback for browsers without the File System Access API.
export function downloadText(text, fileName) {
    downloadBlob(new Blob([text], { type: 'application/json' }), fileName);
}

export async function downloadStream(fileName, streamRef) {
    const buffer = await streamRef.arrayBuffer();
    downloadBlob(new Blob([buffer], { type: 'application/zip' }), fileName);
}

function downloadBlob(blob, fileName) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10_000);
}

// ---------- Folders ----------

export async function pickFolder() {
    const folder = await window.showDirectoryPicker({ mode: 'read' });
    await rememberHandle('folder', folder);
    return readFolder(folder);
}

export async function rereadFolder() {
    const folder = await recallHandle('folder');
    if (!folder) return JSON.stringify({ error: 'No folder remembered' });
    if ((await folder.queryPermission({ mode: 'read' })) !== 'granted' &&
        (await folder.requestPermission({ mode: 'read' })) !== 'granted') {
        return JSON.stringify({ error: 'Read permission denied' });
    }
    return readFolder(folder);
}

async function readFolder(folder) {
    const t0 = performance.now();
    const files = [];
    for await (const [name, handle] of folder.entries()) {
        if (handle.kind !== 'file' || !/\.(tdmodule|tdendprofile|json)$/i.test(name)) continue;
        const file = await handle.getFile();
        files.push({ name, lastModified: file.lastModified, text: await file.text() });
    }
    return JSON.stringify({ folder: folder.name, milliseconds: Math.round(performance.now() - t0), files });
}

// ---------- IndexedDB: drafts and remembered handles ----------

function openDb() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('tellurian-drawing-spike', 1);
        request.onupgradeneeded = () => {
            const db = request.result;
            db.createObjectStore('drafts', { keyPath: 'id' });
            db.createObjectStore('handles', { keyPath: 'id' });
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

async function withStore(storeName, mode, action) {
    const db = await openDb();
    try {
        return await new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, mode);
            const request = action(tx.objectStore(storeName));
            tx.oncomplete = () => resolve(request?.result);
            tx.onerror = () => reject(tx.error);
            tx.onabort = () => reject(tx.error);
        });
    } finally {
        db.close();
    }
}

export async function saveDraft(id, json) {
    await withStore('drafts', 'readwrite', s => s.put({ id, json, modified: new Date().toISOString() }));
}

export async function listDrafts() {
    const all = await withStore('drafts', 'readonly', s => s.getAll());
    return JSON.stringify((all ?? []).map(d => ({ id: d.id, modified: d.modified, size: d.json.length })));
}

export async function loadDraft(id) {
    const draft = await withStore('drafts', 'readonly', s => s.get(id));
    return draft?.json ?? '';
}

export async function deleteDraft(id) {
    await withStore('drafts', 'readwrite', s => s.delete(id));
}

async function rememberHandle(id, handle) {
    try {
        await withStore('handles', 'readwrite', s => s.put({ id, handle }));
    } catch (e) {
        console.warn(`Could not store handle '${id}':`, e); // e.g. DataCloneError where handles can't be stored
    }
}

async function recallHandle(id) {
    const item = await withStore('handles', 'readonly', s => s.get(id));
    return item?.handle ?? null;
}

// ---------- Storage persistence ----------

export async function storageInfo() {
    const persisted = await navigator.storage?.persisted?.() ?? false;
    const estimate = await navigator.storage?.estimate?.();
    return JSON.stringify({ persisted, usage: estimate?.usage ?? 0, quota: estimate?.quota ?? 0 });
}

export async function requestPersistence() {
    return await navigator.storage?.persist?.() ?? false;
}

// ---------- Opening files from the operating system (manifest file_handlers) ----------

export function initLaunchQueue(dotnet) {
    if (!('launchQueue' in window)) return false;
    window.launchQueue.setConsumer(async params => {
        for (const handle of params.files ?? []) {
            const file = await handle.getFile();
            currentHandle = handle;
            await dotnet.invokeMethodAsync('FileLaunched', file.name, await file.text());
        }
    });
    return true;
}
