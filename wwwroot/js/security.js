/* ─────────────────────────────────────────────────────────────────────────
   security.js — front-end zero-trust + UX primitives
   Exposes a single global: window.MakauSecurity

   Design principles:
   • Tokens never leave the cookie jar. Auth state is server-only.
   • Every state-mutating fetch automatically carries the antiforgery header.
   • Inputs are schema-validated client-side BEFORE touching the network
     (defence-in-depth — server still validates on `LoginViewModel`).
   • All dynamic strings rendered to the DOM go through escapeHtml().
   • Loading states use a slim top-of-screen bar — no blocking overlays.
   ───────────────────────────────────────────────────────────────────────── */
(function (global) {
    'use strict';

    /* ───── 1. CSRF token bootstrap ───────────────────────────────────────
       The layout renders <meta name="csrf-token" content="..."> on every
       page, sourced from IAntiforgery.GetAndStoreTokens(). The cookie half
       is HttpOnly + __Host-, the header half is exposed here only.        */
    function getCsrfToken() {
        var meta = document.querySelector('meta[name="csrf-token"]');
        if (meta && meta.content) return meta.content;
        // Razor's @Html.AntiForgeryToken() emits a hidden input as fallback.
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    /* ───── 2. csrfFetch — drop-in fetch() with auto antiforgery + creds ──
       Use for ALL state-mutating AJAX (POST/PUT/PATCH/DELETE).
       GETs pass through unchanged.                                         */
    function csrfFetch(url, options) {
        options = options || {};
        var method = (options.method || 'GET').toUpperCase();
        var headers = new Headers(options.headers || {});

        // Always send cookies (HttpOnly session lives there).
        options.credentials = options.credentials || 'same-origin';

        // Belt-and-braces marker so a sniffed request can't masquerade as a form.
        headers.set('X-Requested-With', 'XMLHttpRequest');

        if (method !== 'GET' && method !== 'HEAD' && method !== 'OPTIONS') {
            var token = getCsrfToken();
            if (!token) {
                return Promise.reject(new Error('CSRF token missing — refusing to send mutation.'));
            }
            headers.set('X-CSRF-TOKEN', token);
        }

        options.headers = headers;
        return fetch(url, options);
    }

    // Loading bars intentionally removed (product direction). Kept as a no-op
    // so page code can still call MakauSecurity.progress.start() safely.
    var progress = { start: function () {}, done: function () {} };

    /* ───── 4. MkSchema — Zod-style fluent validator (no dependency) ──────
       Tiny on purpose. Mirrors the ergonomic surface of Zod / Yup so the
       schemas read identically to a Next.js codebase.

         var loginSchema = MkSchema.object({
             email:    MkSchema.string().email().max(256),
             password: MkSchema.string().min(6).max(128)
         });
         var result = loginSchema.safeParse(formData);
         // result = { ok: true, data } | { ok: false, errors: { field: msg } }
       ───────────────────────────────────────────────────────────────── */
    var EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    function chain(rules) {
        return {
            _rules: rules,
            min: function (n, msg) {
                rules.push(function (v) {
                    return (v == null || String(v).length < n)
                        ? (msg || ('Must be at least ' + n + ' characters.'))
                        : null;
                });
                return chain(rules);
            },
            max: function (n, msg) {
                rules.push(function (v) {
                    return (v != null && String(v).length > n)
                        ? (msg || ('Must be at most ' + n + ' characters.'))
                        : null;
                });
                return chain(rules);
            },
            email: function (msg) {
                rules.push(function (v) {
                    return EMAIL_RE.test(String(v || '').trim())
                        ? null
                        : (msg || 'Enter a valid email address.');
                });
                return chain(rules);
            },
            regex: function (re, msg) {
                rules.push(function (v) {
                    return re.test(String(v || ''))
                        ? null
                        : (msg || 'Invalid format.');
                });
                return chain(rules);
            },
            required: function (msg) {
                rules.push(function (v) {
                    return (v == null || String(v).trim() === '')
                        ? (msg || 'This field is required.')
                        : null;
                });
                return chain(rules);
            },
            safeParse: function (value) {
                for (var i = 0; i < rules.length; i++) {
                    var err = rules[i](value);
                    if (err) return { ok: false, error: err };
                }
                return { ok: true, data: value };
            }
        };
    }

    var MkSchema = {
        string: function () { return chain([function (v) {
            return (v == null || typeof v === 'string') ? null : 'Must be text.';
        }]); },
        object: function (shape) {
            return {
                safeParse: function (data) {
                    var errors = {};
                    var clean = {};
                    var ok = true;
                    Object.keys(shape).forEach(function (key) {
                        var raw = data ? data[key] : undefined;
                        var res = shape[key].safeParse(raw);
                        if (!res.ok) { ok = false; errors[key] = res.error; }
                        else { clean[key] = res.data; }
                    });
                    return ok ? { ok: true, data: clean } : { ok: false, errors: errors };
                }
            };
        }
    };

    /* ───── 5. escapeHtml — for any dynamic DOM injection ─────────────────
       Razor encodes server-side already, but JS-driven insertions must be
       routed through this to neutralise XSS payloads in user-controlled
       strings (review text, place names, AI chat output, etc.).            */
    function escapeHtml(input) {
        if (input == null) return '';
        return String(input)
            .replace(/&/g,  '&amp;')
            .replace(/</g,  '&lt;')
            .replace(/>/g,  '&gt;')
            .replace(/"/g,  '&quot;')
            .replace(/'/g,  '&#39;')
            .replace(/\//g, '&#x2F;');
    }

    // No global navigation progress hooks (loading bars removed).

    global.MakauSecurity = {
        csrfFetch: csrfFetch,
        getCsrfToken: getCsrfToken,
        progress: progress,
        MkSchema: MkSchema,
        escapeHtml: escapeHtml
    };
}(window));
