// Opening and saving files (ADR 0002). Chromium browsers use the File System Access API, so files can be
// saved in place and re-opened from stored handles. Other browsers upload and download.
// Results cross the interop boundary as JSON strings; a cancelled picker returns null.

import { putHandle, getHandle } from './workbench.js';

export function capabilities() {
    return JSON.stringify({
        fileSystemAccess: 'showOpenFilePicker' in window && 'showSaveFilePicker' in window,
        directoryPicker: 'showDirectoryPicker' in window,
        launchQueue: 'launchQueue' in window,
    });
}

function pickerTypes(extensions) {
    return [{ description: 'Tellurian Trains Drawing', accept: { 'application/json': extensions } }];
}

function isAbort(e) {
    return e?.name === 'AbortError';
}

async function newHandleId(handle) {
    const id = crypto.randomUUID();
    return (await putHandle(id, handle)) ? id : null;
}

async function hasPermission(handle, mode) {
    return (await handle.queryPermission({ mode })) === 'granted' || (await handle.requestPermission({ mode })) === 'granted';
}

// ---------- File System Access API ----------

export async function openFiles(extensions, multiple) {
    try {
        const handles = await window.showOpenFilePicker({ types: pickerTypes(extensions), multiple });
        const files = [];
        for (const handle of handles) {
            const file = await handle.getFile();
            files.push({ name: file.name, text: await file.text(), handleId: await newHandleId(handle) });
        }
        return JSON.stringify(files);
    } catch (e) {
        if (isAbort(e)) return null;
        throw e;
    }
}

export async function openArchive() {
    try {
        const [handle] = await window.showOpenFilePicker({ types: [{ description: 'ZIP', accept: { 'application/zip': ['.zip'] } }] });
        return new Uint8Array(await (await handle.getFile()).arrayBuffer());
    } catch (e) {
        if (isAbort(e)) return null;
        throw e;
    }
}

export async function reopen(handleId) {
    const handle = await getHandle(handleId);
    if (!handle || !(await hasPermission(handle, 'read'))) return null;
    const file = await handle.getFile();
    return JSON.stringify({ name: file.name, text: await file.text(), handleId });
}

export async function save(text, suggestedName, extension, handleId, saveAs) {
    try {
        let handle = !saveAs && handleId ? await getHandle(handleId) : null;
        if (!handle) {
            handle = await window.showSaveFilePicker({ suggestedName, types: pickerTypes([extension]) });
            handleId = await newHandleId(handle);
        }
        if (!(await hasPermission(handle, 'readwrite'))) throw new Error('Permission to write the file was denied.');
        const writable = await handle.createWritable();
        await writable.write(text);
        await writable.close();
        return JSON.stringify({ name: handle.name, handleId });
    } catch (e) {
        if (isAbort(e)) return null;
        throw e;
    }
}

export async function pickFolder(extensions) {
    try {
        const folder = await window.showDirectoryPicker({ mode: 'read' });
        return await readFolder(folder, await newHandleId(folder), extensions);
    } catch (e) {
        if (isAbort(e)) return null;
        throw e;
    }
}

export async function rereadFolder(handleId, extensions) {
    const folder = await getHandle(handleId);
    if (!folder || !(await hasPermission(folder, 'read'))) return null;
    return await readFolder(folder, handleId, extensions);
}

function hasExtension(name, extensions) {
    const lower = name.toLowerCase();
    return extensions.some(e => lower.endsWith(e));
}

async function readFolder(folder, handleId, extensions) {
    const files = [];
    for await (const [name, handle] of folder.entries()) {
        if (handle.kind !== 'file' || !hasExtension(name, extensions)) continue;
        const file = await handle.getFile();
        files.push({ name, text: await file.text() });
    }
    return JSON.stringify({ name: folder.name, handleId, files });
}

// ---------- Upload and download ----------

function pick(accept, options = {}) {
    return new Promise(resolve => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = accept;
        input.multiple = !!options.multiple;
        if (options.folder) input.webkitdirectory = true;
        input.addEventListener('change', () => resolve([...input.files]));
        input.addEventListener('cancel', () => resolve([]));
        input.click();
    });
}

export async function uploadFiles(extensions, multiple) {
    const files = await pick(extensions.join(','), { multiple });
    if (files.length === 0) return null;
    return JSON.stringify(await Promise.all(files.map(async f => ({ name: f.name, text: await f.text() }))));
}

export async function uploadArchive() {
    const [file] = await pick('.zip');
    return file ? new Uint8Array(await file.arrayBuffer()) : null;
}

export async function uploadFolder(extensions) {
    const files = (await pick('', { folder: true })).filter(f => hasExtension(f.name, extensions));
    if (files.length === 0) return null;
    const name = files[0].webkitRelativePath.split('/')[0];
    return JSON.stringify({ name, files: await Promise.all(files.map(async f => ({ name: f.webkitRelativePath || f.name, text: await f.text() }))) });
}

export function download(text, fileName) {
    const url = URL.createObjectURL(new Blob([text], { type: 'application/json' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10_000);
    return JSON.stringify({ name: fileName });
}
