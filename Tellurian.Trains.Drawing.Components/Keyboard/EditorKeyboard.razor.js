// Keyboard shortcuts for an editor (EditorKeyboard.razor): listened for on the whole page, so they work wherever the
// focus is, except while the user types in a field, where the keys edit the text. Which keys are shortcuts is decided
// here, synchronously, so that their browser default (scrolling, going back) can be prevented; what they do is decided
// in .NET (EditorShortcuts). Only asynchronous interop is used, so this works under Blazor Server too.

const editable = 'input, textarea, select, [contenteditable=""], [contenteditable="true"]';

export function attach(dotnet, canNudge) {
    const state = { canNudge };

    function isShortcut(e) {
        const command = e.ctrlKey || e.metaKey;
        if (command) return !e.altKey && /^[zy]$/i.test(e.key);
        if (e.altKey || e.metaKey) return false;
        switch (e.key) {
            case 'Delete':
            case 'Backspace':
                return !e.shiftKey;
            case 'Escape':
                // A help popover or another dialog closes on Escape by itself.
                return !e.target.closest('[role="dialog"]');
            case 'Enter':
                // Enter on a button or a link presses it.
                return !e.shiftKey && (e.target === document.body || e.target.closest('svg') !== null);
            case 'ArrowLeft':
            case 'ArrowRight':
            case 'ArrowUp':
            case 'ArrowDown':
                return state.canNudge;
            default:
                return !e.shiftKey && /^[0-9]$/.test(e.key);
        }
    }

    function onKey(e) {
        if (e.defaultPrevented || e.isComposing) return;
        if (e.target instanceof Element && e.target.closest(editable)) return;
        if (!isShortcut(e)) return;
        e.preventDefault();
        dotnet.invokeMethodAsync('KeyDown', e.key, e.ctrlKey, e.shiftKey, e.altKey, e.metaKey);
    }

    document.addEventListener('keydown', onKey);
    return {
        setNudge: value => { state.canNudge = value; },
        detach: () => document.removeEventListener('keydown', onKey),
    };
}
