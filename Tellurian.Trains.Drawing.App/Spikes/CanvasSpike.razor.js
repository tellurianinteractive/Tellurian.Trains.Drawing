// Spike A: pan/zoom/drag on an SVG canvas, with fps and drag-latency measurement.
// Pan and zoom change the viewBox directly; .NET is not involved.
// Drag mode "js": JS moves the element and tells .NET on drop.
// Drag mode "dotnet": JS sends the position to .NET at most once per animation frame, and .NET re-renders.

export function init(svg, dotnet, initialMode) {
    let mode = initialMode;
    let viewBox = svg.viewBox.baseVal;
    let action = null; // { kind: 'pan' | 'drag', ... }
    const pointers = new Map(); // for pinch-zoom
    let pinchStart = null;

    const fpsLabel = document.getElementById('spike-fps');
    const latencyLabel = document.getElementById('spike-latency');

    // --- fps meter: counts frames and the longest frame in each second ---
    let frames = 0, last = performance.now(), worst = 0, prev = last, running = true;
    function tick(now) {
        if (!running) return;
        frames++;
        worst = Math.max(worst, now - prev);
        prev = now;
        if (now - last >= 1000) {
            fpsLabel.textContent = `fps: ${Math.round(frames * 1000 / (now - last))} (worst frame ${Math.round(worst)} ms)`;
            frames = 0; worst = 0; last = now;
        }
        requestAnimationFrame(tick);
    }
    requestAnimationFrame(tick);

    // --- latency statistics for .NET drag mode ---
    const latencies = [];
    function recordLatency(ms) {
        latencies.push(ms);
        if (latencies.length > 120) latencies.shift();
        const sorted = [...latencies].sort((a, b) => a - b);
        const p50 = sorted[Math.floor(sorted.length / 2)];
        const p95 = sorted[Math.floor(sorted.length * 0.95)];
        latencyLabel.textContent = `drag latency: median ${p50.toFixed(1)} ms, p95 ${p95.toFixed(1)} ms`;
    }

    function toSvg(clientX, clientY) {
        const p = svg.createSVGPoint();
        p.x = clientX; p.y = clientY;
        return p.matrixTransform(svg.getScreenCTM().inverse());
    }

    function parseTransform(g) {
        const m = /translate\(([-\d.]+)[ ,]([-\d.]+)\)\s*rotate\(([-\d.]+)\)/.exec(g.getAttribute('transform'));
        return { x: parseFloat(m[1]), y: parseFloat(m[2]), r: parseFloat(m[3]) };
    }

    function zoomAt(clientX, clientY, factor) {
        const p = toSvg(clientX, clientY);
        const w = viewBox.width * factor, h = viewBox.height * factor;
        viewBox.x = p.x - (p.x - viewBox.x) * factor;
        viewBox.y = p.y - (p.y - viewBox.y) * factor;
        viewBox.width = w; viewBox.height = h;
    }

    function onWheel(e) {
        e.preventDefault();
        zoomAt(e.clientX, e.clientY, Math.exp(e.deltaY * 0.0015));
    }

    function onPointerDown(e) {
        svg.setPointerCapture(e.pointerId);
        pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
        if (pointers.size === 2) {
            const [a, b] = [...pointers.values()];
            pinchStart = { distance: Math.hypot(a.x - b.x, a.y - b.y), cx: (a.x + b.x) / 2, cy: (a.y + b.y) / 2 };
            action = null;
            return;
        }
        const g = e.target.closest('g.module');
        const start = toSvg(e.clientX, e.clientY);
        if (g) {
            const t = parseTransform(g);
            action = { kind: 'drag', g, id: parseInt(g.dataset.id), start, t, x: t.x, y: t.y, pending: false };
            g.classList.add('dragging');
        } else {
            action = { kind: 'pan', startClientX: e.clientX, startClientY: e.clientY, vx: viewBox.x, vy: viewBox.y };
        }
    }

    function onPointerMove(e) {
        if (!pointers.has(e.pointerId)) return;
        pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
        if (pointers.size === 2 && pinchStart) {
            const [a, b] = [...pointers.values()];
            const distance = Math.hypot(a.x - b.x, a.y - b.y);
            zoomAt(pinchStart.cx, pinchStart.cy, pinchStart.distance / distance);
            pinchStart.distance = distance;
            return;
        }
        if (!action) return;
        if (action.kind === 'pan') {
            // Panning moves the viewBox, so measure against the start in screen units.
            const scale = viewBox.width / svg.getBoundingClientRect().width;
            const dx = (e.clientX - action.startClientX) * scale;
            const dy = (e.clientY - action.startClientY) * scale;
            viewBox.x = action.vx - dx;
            viewBox.y = action.vy - dy;
        } else {
            const p = toSvg(e.clientX, e.clientY);
            action.x = action.t.x + (p.x - action.start.x);
            action.y = action.t.y + (p.y - action.start.y);
            if (mode === 'js') {
                action.g.setAttribute('transform', `translate(${action.x.toFixed(1)} ${action.y.toFixed(1)}) rotate(${action.t.r})`);
            } else if (!action.pending) {
                action.pending = true;
                const current = action;
                requestAnimationFrame(async () => {
                    const t0 = performance.now();
                    await dotnet.invokeMethodAsync('ModuleDragged', current.id, current.x, current.y);
                    requestAnimationFrame(() => recordLatency(performance.now() - t0));
                    current.pending = false;
                });
            }
        }
    }

    async function onPointerUp(e) {
        pointers.delete(e.pointerId);
        if (pointers.size < 2) pinchStart = null;
        if (action?.kind === 'drag') {
            action.g.classList.remove('dragging');
            const done = action;
            action = null;
            await dotnet.invokeMethodAsync('ModuleDropped', done.id, done.x, done.y);
        } else {
            action = null;
        }
    }

    svg.addEventListener('wheel', onWheel, { passive: false });
    svg.addEventListener('pointerdown', onPointerDown);
    svg.addEventListener('pointermove', onPointerMove);
    svg.addEventListener('pointerup', onPointerUp);
    svg.addEventListener('pointercancel', onPointerUp);

    return {
        setMode(m) { mode = m; latencies.length = 0; latencyLabel.textContent = 'drag latency: –'; },
        dispose() {
            running = false;
            svg.removeEventListener('wheel', onWheel);
            svg.removeEventListener('pointerdown', onPointerDown);
            svg.removeEventListener('pointermove', onPointerMove);
            svg.removeEventListener('pointerup', onPointerUp);
            svg.removeEventListener('pointercancel', onPointerUp);
        }
    };
}
