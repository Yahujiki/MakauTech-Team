/* First-visit arcade boot bar + optional post-login strip (wired from _Layout.cshtml). */
(function () {
    'use strict';

    /** Once per tab session — full-screen pixel bar before the user explores (tap/click skips). */
    function runBootSplash() {
        var boot = document.getElementById('mk-pixel-boot');
        if (!boot) return;
        try {
            if (sessionStorage.getItem('mk-pixel-boot-v1')) {
                boot.remove();
                return;
            }
        } catch (_) {
            boot.remove();
            return;
        }

        var track = boot.querySelector('.mk-pixel-fill');
        var label = boot.querySelector('.mk-pixel-boot-label-state');
        if (!track || !label) return;

        var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var bootDone = false;
        var pendingTimer = null;

        function close() {
            if (bootDone) return;
            bootDone = true;
            try {
                sessionStorage.setItem('mk-pixel-boot-v1', '1');
            } catch (_) {}
            boot.classList.add('is-done');
            setTimeout(function () {
                if (boot.parentNode) boot.parentNode.removeChild(boot);
            }, 500);
        }

        function finishNow() {
            if (pendingTimer) clearTimeout(pendingTimer);
            track.style.width = '100%';
            label.textContent = 'COMPLETE!';
            boot.classList.add('is-complete');
            setTimeout(close, reduce ? 120 : 350);
        }

        var w = 0;
        function tick() {
            if (bootDone) return;
            if (reduce) {
                finishNow();
                return;
            }
            w += Math.random() * 9 + 4;
            if (w >= 100) {
                finishNow();
                return;
            }
            track.style.width = w + '%';
            pendingTimer = setTimeout(function () {
                requestAnimationFrame(tick);
            }, 55 + Math.random() * 40);
        }

        boot.style.cursor = 'pointer';
        boot.addEventListener('click', finishNow);

        boot.removeAttribute('hidden');
        boot.removeAttribute('aria-hidden');
        tick();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', runBootSplash);
    } else {
        runBootSplash();
    }

    window.MakauPixel = window.MakauPixel || {};

    /** Run slim post-login LOAD bar → COMPLETE, then call `thenFn`. */
    window.MakauPixel.runPostLoginStrip = function (thenFn) {
        var wrap = document.getElementById('mk-pixel-login-strip');
        if (!wrap) {
            if (typeof thenFn === 'function') thenFn();
            return;
        }
        var track = wrap.querySelector('.mk-pixel-fill');
        var label = wrap.querySelector('.mk-pixel-login-label');
        if (!track || !label) {
            if (typeof thenFn === 'function') thenFn();
            return;
        }

        wrap.setAttribute('data-active', 'true');
        wrap.setAttribute('aria-hidden', 'false');
        label.textContent = 'LOADING...';
        var w = 0;
        var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        function done() {
            label.textContent = 'COMPLETE!';
            wrap.classList.add('is-complete');
            setTimeout(function () {
                wrap.setAttribute('data-active', 'false');
                wrap.classList.remove('is-complete');
                wrap.setAttribute('aria-hidden', 'true');
                if (typeof thenFn === 'function') thenFn();
            }, 680);
        }

        function tick() {
            if (reduce) {
                track.style.width = '100%';
                done();
                return;
            }
            w += Math.random() * 12 + 6;
            if (w >= 100) {
                track.style.width = '100%';
                done();
                return;
            }
            track.style.width = w + '%';
            requestAnimationFrame(function () {
                setTimeout(tick, 36);
            });
        }

        tick();
    };
})();
