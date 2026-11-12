// Shows Disci in English for users who picked it (App.razor loads this only then, after en.js).
// The program renders Turkish; this swaps texts and a few attributes for their English version as the page is built
// and whenever Blazor changes it. Unknown texts stay Turkish. Numbers, dates and the clinic's own data are left as is,
// except Turkish month/day names inside dates.
(function () {
    'use strict';
    var d = window.__disciEn;
    if (!d) return;
    var exact = d.exact;
    var ATTRS = ['placeholder', 'title', 'aria-label', 'alt'];
    var SKIP = { SCRIPT: 1, STYLE: 1, TEXTAREA: 1, CODE: 1, PRE: 1, NOSCRIPT: 1 };
    var LETTER = 'A-Za-zÇĞİÖŞÜçğıöşü';

    function esc(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }

    var patterns = d.patterns.map(function (p) {
        // {0} matches any text, {#0} only numbers (so "Diş {#0}" leaves the procedure "Diş çekimi" alone).
        var parts = p[0].split(/\{(#?\d)\}/), re = '^', order = [];
        for (var i = 0; i < parts.length; i++) {
            if (i % 2 === 0) re += esc(parts[i]);
            else {
                re += parts[i].charAt(0) === '#' ? '([\\d.,:%₺+\\- ]+?)' : '(.+?)';
                order.push(+parts[i].replace('#', ''));
            }
        }
        return { re: new RegExp(re + '$'), en: p[1], order: order };
    });

    var words = Object.keys(d.words).sort(function (a, b) { return b.length - a.length; });
    var wordRe = new RegExp('(^|[^' + LETTER + '])(' + words.map(esc).join('|') + ')(?=$|[^' + LETTER + '])', 'g');

    function translate(text) {
        var key = text.replace(/\s+/g, ' ').trim();
        if (!key || !/[A-Za-zÇĞİÖŞÜçğıöşü]/.test(key)) return null;
        if (Object.prototype.hasOwnProperty.call(exact, key)) return exact[key];
        for (var i = 0; i < patterns.length; i++) {
            var p = patterns[i], m = key.match(p.re);
            if (!m) continue;
            return p.en.replace(/\{(\d)\}/g, function (_, n) {
                var part = m[p.order.indexOf(+n) + 1] || '';
                return Object.prototype.hasOwnProperty.call(exact, part) ? exact[part] : part;
            });
        }
        // Dates such as "9 Kasım 2026, Pazartesi" or "Pzt 9 Kas".
        if (/\d/.test(key) && wordRe.test(key)) {
            wordRe.lastIndex = 0;
            return key.replace(wordRe, function (_, before, w) { return before + d.words[w]; });
        }
        return null;
    }

    var done = new WeakMap();   // node → the English text we put there (so our own change is not translated again)

    function skipped(el) {
        for (var e = el; e && e.nodeType === 1; e = e.parentNode) {
            if (SKIP[e.nodeName] || e.isContentEditable || e.getAttribute('translate') === 'no') return true;
        }
        return false;
    }

    function textNode(n) {
        var v = n.nodeValue;
        if (!v || done.get(n) === v || skipped(n.parentNode)) return;
        var t = translate(v);
        if (t === null) return;
        var out = v.match(/^\s*/)[0] + t + v.match(/\s*$/)[0];
        done.set(n, out);
        if (out !== v) n.nodeValue = out;
    }

    function attributes(el) {
        if (skipped(el)) return;
        for (var i = 0; i < ATTRS.length; i++) {
            var a = ATTRS[i], v = el.getAttribute(a);
            if (!v) continue;
            var t = translate(v);
            if (t !== null && t !== v) el.setAttribute(a, t);
        }
    }

    function walk(root) {
        if (root.nodeType === 3) { textNode(root); return; }
        if (root.nodeType !== 1 && root.nodeType !== 9 && root.nodeType !== 11) return;
        if (root.nodeType === 1) { if (SKIP[root.nodeName]) return; attributes(root); }
        var it = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT | NodeFilter.SHOW_TEXT);
        for (var n = it.nextNode(); n; n = it.nextNode()) {
            if (n.nodeType === 3) textNode(n); else attributes(n);
        }
    }

    var observer = new MutationObserver(function (records) {
        for (var i = 0; i < records.length; i++) {
            var r = records[i];
            if (r.type === 'characterData') textNode(r.target);
            else if (r.type === 'attributes') attributes(r.target);
            else for (var j = 0; j < r.addedNodes.length; j++) walk(r.addedNodes[j]);
        }
    });
    observer.observe(document.documentElement, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ATTRS });
    walk(document.documentElement);
    document.addEventListener('DOMContentLoaded', function () { walk(document.documentElement); });
})();
