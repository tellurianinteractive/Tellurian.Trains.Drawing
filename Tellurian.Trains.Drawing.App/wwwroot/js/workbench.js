// The IndexedDB workbench (spec §6, ADR 0002): drafts, recent files with their file handles, and values
// such as settings and the user catalogue. Values cross the interop boundary as JSON strings,
// so the .NET side needs no reflection.

const databaseName = 'tellurian-drawing';
const databaseVersion = 1;
const stores = { drafts: 'drafts', recent: 'recent', handles: 'handles', values: 'values' };

let database = null;

function open() {
    if (database) return Promise.resolve(database);
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(databaseName, databaseVersion);
        request.onupgradeneeded = () => {
            const db = request.result;
            for (const name of Object.values(stores)) {
                if (!db.objectStoreNames.contains(name)) db.createObjectStore(name, { keyPath: 'id' });
            }
        };
        request.onsuccess = () => {
            database = request.result;
            database.onversionchange = () => { database.close(); database = null; };
            resolve(database);
        };
        request.onerror = () => reject(request.error);
        request.onblocked = () => reject(new Error('The database is blocked by another tab.'));
    });
}

async function run(storeName, mode, action) {
    const db = await open();
    return new Promise((resolve, reject) => {
        const transaction = db.transaction(storeName, mode);
        const request = action(transaction.objectStore(storeName));
        transaction.oncomplete = () => resolve(request?.result);
        transaction.onerror = () => reject(transaction.error);
        transaction.onabort = () => reject(transaction.error ?? new Error('The transaction was aborted.'));
    });
}

export async function put(storeName, json) {
    await run(storeName, 'readwrite', store => store.put(JSON.parse(json)));
}

export async function get(storeName, id) {
    const item = await run(storeName, 'readonly', store => store.get(id));
    return item ? JSON.stringify(item) : null;
}

export async function getAll(storeName) {
    const items = await run(storeName, 'readonly', store => store.getAll());
    return JSON.stringify(items ?? []);
}

export async function remove(storeName, id) {
    await run(storeName, 'readwrite', store => store.delete(id));
}

// File handles can't be serialized to JSON; they are stored as they are, where the browser allows it.
export async function putHandle(id, handle) {
    try {
        await run(stores.handles, 'readwrite', store => store.put({ id, handle }));
        return true;
    } catch (e) {
        console.warn(`The file handle ${id} couldn't be stored:`, e);
        return false;
    }
}

export async function getHandle(id) {
    const item = await run(stores.handles, 'readonly', store => store.get(id));
    return item?.handle ?? null;
}

export async function removeHandle(id) {
    await run(stores.handles, 'readwrite', store => store.delete(id));
}

export async function isPersisted() {
    return await navigator.storage?.persisted?.() ?? false;
}

export async function requestPersistence() {
    return await navigator.storage?.persist?.() ?? false;
}
