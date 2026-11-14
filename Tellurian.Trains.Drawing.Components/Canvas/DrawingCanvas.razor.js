// Pan and zoom for DrawingCanvas (ADR 0001): the viewBox is changed here, and .NET is told the final view,
// debounced. Pointer events for tools go to .NET in world coordinates, moves at most once per animation frame.
// Only asynchronous interop is used, so this works under Blazor Server too.

const minimumScale = 0.002;
const maximumScale = 200;
const wheelZoomPerPixel = 0.0015;
const reportDelay = 150;

export function attach(host, svg, dotnet, view, panWithPrimaryButton) {
    const state = {
        cx: view[0], cy: view[1], scale: view[2],
        width: host.clientWidth || 1, height: host.clientHeight || 1,
        pointers: new Map(), panning: null, pinch: null, spaceDown: false,
        pendingMove: null, frame: 0, reportTimer: 0,
    };

    const clamp = s => Math.min(maximumScale, Math.max(minimumScale, s));
    const toWorld = (sx, sy) => ({ x: state.cx + (sx - state.width / 2) / state.scale, y: state.cy - (sy - state.height / 2) / state.scale });
    const local = e => {
        const r = svg.getBoundingClientRect();
        return { x: e.clientX - r.left, y: e.clientY - r.top };
    };

    function apply() {
        const w = state.width / state.scale, h = state.height / state.scale;
        svg.setAttribute('viewBox', `${state.cx - w / 2} ${-(state.cy + h / 2)} ${w} ${h}`);
    }

    function report() {
        clearTimeout(state.reportTimer);
        state.reportTimer = setTimeout(() => {
            dotnet.invokeMethodAsync('ViewChanged', state.cx, state.cy, state.scale, state.width, state.height).catch(() => { });
        }, reportDelay);
    }

    function zoomAt(sx, sy, factor) {
        const before = toWorld(sx, sy);
        state.scale = clamp(state.scale * factor);
        const after = toWorld(sx, sy);
        state.cx += before.x - after.x;
        state.cy += before.y - after.y;
        apply();
        report();
    }

    function panBy(dx, dy) {
        state.cx -= dx / state.scale;
        state.cy += dy / state.scale;
        apply();
        report();
    }

    function send(method, e) {
        const p = local(e);
        const w = toWorld(p.x, p.y);
        const modifiers = (e.shiftKey ? 1 : 0) | (e.ctrlKey ? 2 : 0) | (e.altKey ? 4 : 0) | (e.metaKey ? 8 : 0);
        dotnet.invokeMethodAsync(method, w.x, w.y, p.x, p.y, e.button ?? -1, e.buttons ?? 0, modifiers, e.pointerType ?? 'mouse').catch(() => { });
    }

    const isPanGesture = e => e.button === 1 || state.spaceDown || (panWithPrimaryButton && e.button === 0) || (e.pointerType === 'touch' && panWithPrimaryButton);

    function onPointerDown(e) {
        svg.focus({ preventScroll: true });
        const p = local(e);
        state.pointers.set(e.pointerId, p);
        if (state.pointers.size === 2) {
            const [a, b] = [...state.pointers.values()];
            state.pinch = { distance: Math.hypot(a.x - b.x, a.y - b.y), center: { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 } };
            state.panning = null;
            return;
        }
        if (isPanGesture(e)) {
            state.panning = p;
            svg.setPointerCapture(e.pointerId);
            e.preventDefault();
            return;
        }
        svg.setPointerCapture(e.pointerId);
        send('PointerDown', e);
    }

    function onPointerMove(e) {
        const p = local(e);
        if (state.pointers.has(e.pointerId)) state.pointers.set(e.pointerId, p);
        if (state.pinch && state.pointers.size === 2) {
            const [a, b] = [...state.pointers.values()];
            const distance = Math.hypot(a.x - b.x, a.y - b.y);
            const center = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
            panBy(center.x - state.pinch.center.x, center.y - state.pinch.center.y);
            if (state.pinch.distance > 0) zoomAt(center.x, center.y, distance / state.pinch.distance);
            state.pinch = { distance, center };
            return;
        }
        if (state.panning) {
            panBy(p.x - state.panning.x, p.y - state.panning.y);
            state.panning = p;
            return;
        }
        state.pendingMove = e;
        if (!state.frame) {
            state.frame = requestAnimationFrame(() => {
                state.frame = 0;
                if (state.pendingMove) send('PointerMove', state.pendingMove);
                state.pendingMove = null;
            });
        }
    }

    function onPointerUp(e) {
        const wasGesture = state.panning || state.pinch;
        state.pointers.delete(e.pointerId);
        if (state.pointers.size < 2) state.pinch = null;
        if (svg.hasPointerCapture(e.pointerId)) svg.releasePointerCapture(e.pointerId);
        if (state.panning) {
            state.panning = null;
            return;
        }
        if (!wasGesture) send('PointerUp', e);
    }

    function onWheel(e) {
        e.preventDefault();
        const p = local(e);
        const delta = e.deltaMode === 1 ? e.deltaY * 16 : e.deltaY;
        zoomAt(p.x, p.y, Math.exp(-delta * wheelZoomPerPixel));
    }

    function onKey(e) {
        if (e.code === 'Space') {
            state.spaceDown = e.type === 'keydown';
            if (state.spaceDown) e.preventDefault();
        }
    }

    const resize = new ResizeObserver(() => {
        const width = host.clientWidth, height = host.clientHeight;
        if (width === 0 || height === 0 || (width === state.width && height === state.height)) return;
        state.width = width;
        state.height = height;
        apply();
        report();
    });

    svg.addEventListener('pointerdown', onPointerDown);
    svg.addEventListener('pointermove', onPointerMove);
    svg.addEventListener('pointerup', onPointerUp);
    svg.addEventListener('pointercancel', onPointerUp);
    svg.addEventListener('wheel', onWheel, { passive: false });
    svg.addEventListener('keydown', onKey);
    svg.addEventListener('keyup', onKey);
    svg.addEventListener('contextmenu', e => e.preventDefault());
    resize.observe(host);
    apply();
    report();

    return {
        setView(view) {
            state.cx = view[0];
            state.cy = view[1];
            state.scale = clamp(view[2]);
            apply();
        },
        detach() {
            resize.disconnect();
            clearTimeout(state.reportTimer);
            if (state.frame) cancelAnimationFrame(state.frame);
            svg.removeEventListener('pointerdown', onPointerDown);
            svg.removeEventListener('pointermove', onPointerMove);
            svg.removeEventListener('pointerup', onPointerUp);
            svg.removeEventListener('pointercancel', onPointerUp);
            svg.removeEventListener('wheel', onWheel);
            svg.removeEventListener('keydown', onKey);
            svg.removeEventListener('keyup', onKey);
        },
    };
}
