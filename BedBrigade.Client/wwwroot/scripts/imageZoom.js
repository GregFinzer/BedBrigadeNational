(function () {
    const clickText = 'click';
    function ensureStyle() {
        if (document.getElementById('bb-image-zoom-style')) return;
        const style = document.createElement('style');
        style.id = 'bb-image-zoom-style';
        style.textContent = `
/* Enable hover magnifier on images inside the enabled container (and fallback to #zoom-body) */
#zoom-body img,
#zoom-body a img,
.bb-image-zoom-enabled img,
.bb-image-zoom-enabled a img { cursor: zoom-in !important; }

/* Overlay fade/zoom transitions */
.bb-image-zoom-overlay {
 position: fixed; inset:0; background: rgba(0,0,0,0.92);
 display: flex; align-items: center; justify-content: center;
 z-index:2147483647; cursor: zoom-out !important; opacity:0;
 transition: opacity 220ms ease;
}
.bb-image-zoom-overlay.open { opacity:1; }
.bb-image-zoom-overlay img {
 max-width:98%; max-height:98%; object-fit: contain;
 box-shadow: 0 10px 40px rgba(0,0,0,0.6); border-radius:4px;
 transform: scale(0.95); transform-origin: center center;
 transition: transform 220ms ease;
 will-change: transform;
 cursor: zoom-out !important; /* Ensure cursor shows zoom-out when zoomed */
}
.bb-image-zoom-overlay.open img { transform: scale(1); }

@media (prefers-reduced-motion: reduce) {
 .bb-image-zoom-overlay { transition: none; }
 .bb-image-zoom-overlay img { transition: none; }
}
`;
        document.head.appendChild(style);
    }

    function createOverlay(src, srcset, alt) {
        const overlay = document.createElement('div');
        overlay.className = 'bb-image-zoom-overlay';
        overlay.tabIndex = 0;

        const img = document.createElement('img');
        img.src = src || '';
        if (srcset) img.srcset = srcset;
        if (alt) img.alt = alt;

        overlay.appendChild(img);

        // clicking the overlay (anywhere) closes it (zoom out)
        overlay.addEventListener(clickText, function (e) {
            e.stopPropagation();
            removeOverlay();
        });

        // clicking the image also closes it (zoom out)
        img.addEventListener(clickText, function (e) {
            e.stopPropagation();
            removeOverlay();
        });

        return overlay;
    }

    let currentOverlay = null;
    let isClosing = false;
    let justOpened = false;
    let justOpenedTimer = null;
    let cursorObserver = null;

    function ensureCursorHint() {
        const container = document.getElementById('zoom-body');
        if (container && !container.classList.contains('bb-image-zoom-enabled')) {
            container.classList.add('bb-image-zoom-enabled');
        }
    }

    function startCursorObserver() {
        if (cursorObserver || !window.MutationObserver) return;
        cursorObserver = new MutationObserver(() => {
            ensureCursorHint();
        });
        cursorObserver.observe(document.documentElement, { childList: true, subtree: true });
    }

    function removeOverlay() {
        if (!currentOverlay || isClosing) return;
        isClosing = true;
        const overlay = currentOverlay;
        // start close animation (fade/zoom out)
        overlay.classList.remove('open');

        const cleanup = () => {
            if (!currentOverlay) return; // already cleaned
            overlay.remove();
            currentOverlay = null;
            isClosing = false;
            document.body.style.overflow = '';
        };

        // Only remove after the overlay's opacity transition finishes
        const handleTransitionEnd = (ev) => {
            if (ev.target !== overlay) return; // ignore img transition
            overlay.removeEventListener('transitionend', handleTransitionEnd);
            cleanup();
        };

        // Fallback in case transitionend doesn't fire
        const fallback = setTimeout(() => {
            overlay.removeEventListener('transitionend', handleTransitionEnd);
            cleanup();
        }, 300);

        overlay.addEventListener('transitionend', function (ev) {
            clearTimeout(fallback);
            handleTransitionEnd(ev);
        });
    }

    // Robustly resolve the image element from any event target (works for <picture>, links, wrappers)
    function getImageFromEvent(e) {
        const t = e && e.target;
        if (!t) return null;
        // Direct img
        if (t.tagName === 'IMG') return t;
        // Walk composed path if available
        const path = typeof e.composedPath === 'function' ? e.composedPath() : (e.path || []);
        if (path && path.length) {
            for (const node of path) {
                if (node && node.tagName === 'IMG') return node;
                if (node === window || node === document) break;
            }
        }
        // Find nearest likely container and search inside
        const container = t.closest && t.closest('picture, a, figure');
        if (container) {
            const img = container.querySelector && container.querySelector('img');
            if (img) return img;
        }
        return null;
    }

    function openFromImage(imgEl) {
        // figure out the best source (handle responsive or lazy images)
        const src = imgEl.currentSrc || imgEl.getAttribute('src') || (imgEl.dataset ? imgEl.dataset.src : '') || '';
        const srcset = imgEl.getAttribute('srcset') || '';
        const alt = imgEl.getAttribute('alt') || '';

        ensureStyle();

        currentOverlay = createOverlay(src, srcset, alt);
        document.body.appendChild(currentOverlay);
        // prevent background scroll while zoomed
        document.body.style.overflow = 'hidden';
        currentOverlay.focus();

        // next frame: trigger opening transition (zoom/fade in)
        requestAnimationFrame(() => {
            currentOverlay.classList.add('open');
        });

        // guard to ignore the synthetic click that follows pointerdown
        justOpened = true;
        if (justOpenedTimer) clearTimeout(justOpenedTimer);
        justOpenedTimer = setTimeout(() => { justOpened = false; justOpenedTimer = null; }, 350);
    }

    function onWindowClick(e) {
        // Ignore clicks inside overlay
        if ((e.target.closest && e.target.closest('.bb-image-zoom-overlay'))) {
            return;
        }
        // Prevent immediate close if we just opened from pointerdown
        if (justOpened) {
            e.preventDefault();
            return;
        }
        // If overlay already open, close it on any click outside overlay
        if (currentOverlay) {
            e.preventDefault();
            removeOverlay();
            return;
        }
        // Resolve image from event
        const imgEl = getImageFromEvent(e);
        if (!imgEl) return;
        // If inside a link, prevent navigation
        const link = imgEl.closest && imgEl.closest('a');
        if (link) { e.preventDefault(); }
        openFromImage(imgEl);
    }

    function onPointerDown(e) {
        // Ignore if overlay open; click handler will manage closing
        if (currentOverlay) return;
        // Resolve image from event early in capture
        const imgEl = getImageFromEvent(e);
        if (!imgEl) return;
        // If inside a link, prevent navigation immediately
        const link = imgEl.closest && imgEl.closest('a');
        if (link) { e.preventDefault(); }
        // Open now to avoid other handlers swallowing the click
        openFromImage(imgEl);
        // Stop further propagation if we opened overlay to avoid interference
        e.stopPropagation();
    }

    function init() {
        ensureStyle();
        ensureCursorHint();
        startCursorObserver();
        // Set cursor hint on body as best-effort fallback if container absent
        if (!document.getElementById('zoom-body')) {
            document.body.classList.add('bb-image-zoom-enabled');
        }
        // Capture to see events even if inner handlers stopPropagation; use non-passive to allow preventDefault on touch
        try {
            window.addEventListener('pointerdown', onPointerDown, { capture: true, passive: false });
            window.addEventListener(clickText, onWindowClick, { capture: true, passive: false });
        } catch (_) {
            // Fallback for very old browsers
            window.addEventListener('pointerdown', onPointerDown, true);
            window.addEventListener(clickText, onWindowClick, true);
        }
        // Close overlay with Escape
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && currentOverlay) removeOverlay();
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
