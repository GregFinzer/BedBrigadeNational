let _timerId = null;
let _bound = false;

function _resetTimer(timeoutMs, logoutUrl) {
    if (_timerId) clearTimeout(_timerId);
    _timerId = setTimeout(() => {
        // Hard navigate so the server-side session + client token get cleared by your existing logout page
        window.location.href = logoutUrl || "/logout/idle";
    }, timeoutMs);
}

function _onActivity(timeoutMs, logoutUrl) {
    _resetTimer(timeoutMs, logoutUrl);
}

export function startIdleTimer(timeoutMs, logoutUrl) {
    // Attach once per page
    if (!_bound) {
        const events = ["mousemove", "mousedown", "keydown", "scroll", "touchstart", "wheel", "visibilitychange"];
        events.forEach(e =>
            window.addEventListener(e, () => _onActivity(timeoutMs, logoutUrl), { passive: true })
        );
        _bound = true;
    }
    _resetTimer(timeoutMs, logoutUrl);
}

export function stopIdleTimer() {
    if (_timerId) {
        clearTimeout(_timerId);
        _timerId = null;
    }
    // listeners remain; next startIdleTimer() reuses them
}