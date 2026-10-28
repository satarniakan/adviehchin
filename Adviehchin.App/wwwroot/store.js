(function () {
    'use strict';

    // سبد خرید: فقط رشتهٔ JSON جابه‌جا می‌شود؛ منطق در CartState است.
    window.storeCart = {
        get: function (key) {
            try { return localStorage.getItem(key || 'adviehchin.cart.v1') || '[]'; }
            catch (e) { return '[]'; }
        },
        set: function (key, json) {
            try { localStorage.setItem(key, json); } catch (e) { /* حافظهٔ مرورگر در دسترس نیست */ }
        }
    };

    function initHero() {
        var heroes = document.querySelectorAll('[data-hero]');
        heroes.forEach(function (hero) {
            if (hero.dataset.heroReady === '1') return;
            hero.dataset.heroReady = '1';

            var slides = Array.prototype.slice.call(hero.querySelectorAll('.st-hero-slide'));
            var dots = Array.prototype.slice.call(hero.querySelectorAll('[data-hero-dot]'));
            if (slides.length < 2) return;

            var index = slides.findIndex(function (s) { return s.classList.contains('is-active'); });
            if (index < 0) index = 0;
            var timer = null;

            function show(next) {
                index = (next + slides.length) % slides.length;
                slides.forEach(function (s, i) { s.classList.toggle('is-active', i === index); });
                dots.forEach(function (d, i) { d.classList.toggle('is-active', i === index); });
            }

            function restart() {
                if (timer) clearInterval(timer);
                timer = setInterval(function () { show(index + 1); }, 6500);
            }

            hero.querySelectorAll('[data-hero-next]').forEach(function (b) {
                b.addEventListener('click', function () { show(index + 1); restart(); });
            });
            hero.querySelectorAll('[data-hero-prev]').forEach(function (b) {
                b.addEventListener('click', function () { show(index - 1); restart(); });
            });
            dots.forEach(function (d, i) {
                d.addEventListener('click', function () { show(i); restart(); });
            });

            show(index);
            restart();
        });
    }

    function initAll() {
        initHero();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }
    document.addEventListener('enhancedload', initAll);
})();
