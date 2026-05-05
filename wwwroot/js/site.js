// MakauTech — shared client scripts

// ═══════════════════════════════════════════════════════
// 1. WEB AUDIO API — SOUND ENGINE (lightweight beeps)
// ═══════════════════════════════════════════════════════
const MakauSound = (() => {
    let ctx = null;
    function getCtx() {
        if (!ctx) ctx = new (window.AudioContext || window.webkitAudioContext)();
        return ctx;
    }
    function beep(freq, duration, type, vol) {
        try {
            const c = getCtx();
            const osc = c.createOscillator();
            const gain = c.createGain();
            osc.type = type || 'sine';
            osc.frequency.value = freq;
            gain.gain.value = vol || 0.15;
            gain.gain.exponentialRampToValueAtTime(0.001, c.currentTime + duration);
            osc.connect(gain);
            gain.connect(c.destination);
            osc.start(c.currentTime);
            osc.stop(c.currentTime + duration);
        } catch (e) { /* silent fallback */ }
    }
    return {
        catch:   () => beep(660, 0.12, 'sine', 0.18),
        rare:    () => beep(880, 0.18, 'sine', 0.22),
        golden:  () => { beep(880, 0.1, 'sine', 0.2); setTimeout(() => beep(1100, 0.15, 'sine', 0.2), 100); },
        hazard:  () => beep(180, 0.25, 'sawtooth', 0.15),
        miss:    () => beep(220, 0.15, 'triangle', 0.1),
        shoot:   () => beep(440, 0.06, 'square', 0.08),
        hit:     () => beep(700, 0.1, 'sine', 0.15),
        combo:   () => { beep(660, 0.08, 'sine', 0.15); setTimeout(() => beep(880, 0.1, 'sine', 0.15), 80); },
        gameOver:() => { beep(400, 0.15, 'sine', 0.18); setTimeout(() => beep(300, 0.2, 'sine', 0.18), 150); setTimeout(() => beep(200, 0.3, 'sine', 0.18), 300); },
        pay:     () => beep(520, 0.1, 'sine', 0.12),
        click:   () => beep(800, 0.04, 'sine', 0.08)
    };
})();

// ═══════════════════════════════════════════════════════
// 2. DARK MODE — unified via data-theme attribute
// ═══════════════════════════════════════════════════════
const MakauDark = (() => {
    const KEY = 'makautech-theme';
    function apply(dark) {
        const html = document.documentElement;
        if (dark) {
            html.setAttribute('data-theme', 'dark');
        } else {
            html.removeAttribute('data-theme');
        }
        // Update any toggle buttons
        document.querySelectorAll('.dark-mode-toggle').forEach(btn => {
            btn.textContent = dark ? '☀️ Light' : '🌙 Dark';
            btn.setAttribute('aria-pressed', dark);
        });
        // Sync settings checkbox if present
        var chk = document.getElementById('gsDarkCheck');
        if (chk) chk.checked = dark;
    }
    function get() {
        return localStorage.getItem(KEY) === 'dark';
    }
    function set(v) {
        localStorage.setItem(KEY, v ? 'dark' : 'light');
        apply(v);
    }
    function toggle() { set(!get()); MakauSound.click(); }
    // Apply on load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => apply(get()));
    } else {
        apply(get());
    }
    return { toggle, get, set };
})();

// Wire up all .dark-mode-toggle buttons
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.dark-mode-toggle').forEach(btn => {
        btn.addEventListener('click', MakauDark.toggle);
    });
});

// ═══════════════════════════════════════════════════════
// 3b. TEXT SIZE (A− / A+) — warga emas
// ═══════════════════════════════════════════════════════
const MakauTextScale = (() => {
    const KEY = 'makau-text-scale';
    const LEVELS = [90, 100, 110, 125];
    function apply(value) {
        const v = LEVELS.includes(value) ? value : 100;
        document.documentElement.setAttribute('data-text-scale', String(v));
        try { localStorage.setItem(KEY, String(v)); } catch (e) { /* ignore */ }
    }
    function load() {
        let v = 100;
        try {
            const s = localStorage.getItem(KEY);
            if (s) v = parseInt(s, 10);
        } catch (e) { /* ignore */ }
        if (!LEVELS.includes(v)) v = 100;
        apply(v);
    }
    function delta(dir) {
        const cur = parseInt(document.documentElement.getAttribute('data-text-scale') || '100', 10);
        let idx = LEVELS.indexOf(cur);
        if (idx < 0) idx = 1;
        idx = Math.max(0, Math.min(LEVELS.length - 1, idx + dir));
        apply(LEVELS[idx]);
        MakauSound.click();
    }
    return { load, delta };
})();
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => MakauTextScale.load());
} else {
    MakauTextScale.load();
}
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.text-size-down').forEach(btn => {
        btn.addEventListener('click', () => MakauTextScale.delta(-1));
    });
    document.querySelectorAll('.text-size-up').forEach(btn => {
        btn.addEventListener('click', () => MakauTextScale.delta(1));
    });
});

// ═══════════════════════════════════════════════════════
// 4. IMAGE SKELETON SHIMMER
// ═══════════════════════════════════════════════════════
// ═══════════════════════════════════════════════════════
// 5. SIDEBAR (mobile drawer)
// ═══════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', () => {
    const body = document.body;
    const openBtn = document.getElementById('sidebarOpen');
    const closeBtn = document.getElementById('sidebarClose');
    const backdrop = document.getElementById('sidebarBackdrop');
    const mq = window.matchMedia('(max-width: 960px)');

    function openSidebar() {
        body.classList.add('sidebar-open');
        openBtn?.setAttribute('aria-expanded', 'true');
    }
    function closeSidebar() {
        body.classList.remove('sidebar-open');
        openBtn?.setAttribute('aria-expanded', 'false');
    }

    openBtn?.addEventListener('click', () => {
        if (mq.matches) openSidebar();
        else window.scrollTo({ top: 0, behavior: 'smooth' });
    });
    closeBtn?.addEventListener('click', closeSidebar);
    backdrop?.addEventListener('click', closeSidebar);

    document.querySelectorAll('#app-sidebar .sidebar-link, #app-sidebar .sidebar-brand, #app-sidebar .sidebar-btn, #app-sidebar .sidebar-profile').forEach(el => {
        el.addEventListener('click', () => {
            if (mq.matches) closeSidebar();
        });
    });

    mq.addEventListener('change', e => {
        if (!e.matches) closeSidebar();
    });

    document.addEventListener('keydown', e => {
        if (e.key === 'Escape') closeSidebar();
    });
});

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.place-image-wrap').forEach(wrap => {
        wrap.classList.add('img-loading');
        const img = wrap.querySelector('img');
        if (img) {
            if (img.complete) { wrap.classList.remove('img-loading'); }
            else {
                img.addEventListener('load', () => wrap.classList.remove('img-loading'));
                img.addEventListener('error', () => wrap.classList.remove('img-loading'));
            }
        }
    });
});

// ═══════════════════════════════════════════════════════
// 6. SCROLL-REVEAL ANIMATIONS
// ═══════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', () => {
    const els = document.querySelectorAll('.scroll-reveal');
    if (!els.length) return;
    const io = new IntersectionObserver((entries) => {
        entries.forEach((entry, idx) => {
            if (entry.isIntersecting) {
                const el = entry.target;
                const siblings = Array.from(el.parentElement?.children || []).filter(c => c.classList.contains('scroll-reveal'));
                const i = siblings.indexOf(el);
                el.style.transitionDelay = (i >= 0 ? i * 0.12 : 0) + 's';
                el.classList.add('revealed');
                io.unobserve(el);
            }
        });
    }, { threshold: 0.15, rootMargin: '0px 0px -40px 0px' });
    els.forEach(el => io.observe(el));
});
