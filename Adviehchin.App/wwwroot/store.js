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

    // کشوی سبد خرید: باز/بسته شدن سمت کلاینت است تا در همهٔ مدارها
    // (حتی صفحات با رندر استاتیک) بدون هماهنگی سرور کار کند.
    function setCartOpen(open) {
        document.body.classList.toggle('st-cart-open', open);
        if (open) {
            requestAnimationFrame(function () {
                var closeBtn = document.querySelector('.st-cart-drawer-close');
                if (closeBtn) closeBtn.focus();
            });
        }
    }
    document.addEventListener('click', function (e) {
        var t = e.target;
        if (!t || !t.closest) return;
        var opener = t.closest('[data-cart-open]');
        if (opener) { e.preventDefault(); setCartOpen(true); return; }
        if (t.closest('[data-cart-close]')) setCartOpen(false);
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') setCartOpen(false);
    });
    document.addEventListener('enhancedload', function () { setCartOpen(false); });

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

    // کاروسل افقی مشترک: دسته‌بندی‌ها و پک‌های قومی فقط کلاس/اتریبیوت متفاوت دارند.
    function initRail(config) {
        document.querySelectorAll(config.track).forEach(function (track) {
            if (track.dataset.railReady === '1') return;
            track.dataset.railReady = '1';

            var wrap = track.closest(config.wrap);
            if (!wrap) return;

            var prev = wrap.querySelector(config.prev);
            var next = wrap.querySelector(config.next);

            function step() {
                var card = track.querySelector(config.card);
                if (!card) return track.clientWidth;
                var styles = getComputedStyle(track);
                var gap = parseFloat(styles.columnGap || styles.gap) || 0;
                return card.getBoundingClientRect().width + gap;
            }

            function update() {
                // در RTL کروم scrollLeft از صفر شروع و منفی می‌شود؛ قدرمطلق برای هر دو حالت
                var max = track.scrollWidth - track.clientWidth;
                var pos = Math.abs(track.scrollLeft);
                if (prev) prev.disabled = pos <= 2;
                if (next) next.disabled = pos >= max - 2;
            }

            if (prev) prev.addEventListener('click', function () {
                track.scrollBy({ left: step(), behavior: 'smooth' });
            });
            if (next) next.addEventListener('click', function () {
                track.scrollBy({ left: -step(), behavior: 'smooth' });
            });

            track.addEventListener('scroll', update, { passive: true });
            window.addEventListener('resize', update);
            update();
        });
    }

    function initCatsCarousel() {
        initRail({
            wrap: '.st-cats-carousel',
            track: '[data-cats-track]',
            card: '.st-cat',
            prev: '[data-cats-prev]',
            next: '[data-cats-next]'
        });
    }

    function initEthnicCarousel() {
        initRail({
            wrap: '.st-eth-carousel',
            track: '[data-eth-track]',
            card: '.st-eth-card',
            prev: '[data-eth-prev]',
            next: '[data-eth-next]'
        });
    }

    function initQuotes() {
        document.querySelectorAll('[data-quotes]').forEach(function (box) {
            if (box.dataset.quotesReady === '1') return;
            box.dataset.quotesReady = '1';

            var items = Array.prototype.slice.call(box.querySelectorAll('[data-quote]'));
            if (!items.length) return;

            var paragraphs = items.map(function (item) {
                var p = item.querySelector('[data-quote-text]');
                if (p) p.dataset.full = (p.textContent || '').replace(/\s+/g, ' ').trim();
                return p;
            });

            if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

            var started = false;

            function show(n) {
                items.forEach(function (item, i) { item.classList.toggle('is-active', i === n); });

                var p = paragraphs[n];
                if (!p) { setTimeout(function () { show((n + 1) % items.length); }, 4500); return; }

                var full = p.dataset.full || '';
                p.textContent = '';
                p.classList.add('is-typing');

                var i = 0;
                var timer = setInterval(function () {
                    i++;
                    p.textContent = full.slice(0, i);
                    if (i >= full.length) {
                        clearInterval(timer);
                        p.classList.remove('is-typing');
                        setTimeout(function () { show((n + 1) % items.length); }, 4200);
                    }
                }, 30);
            }

            var observer = new IntersectionObserver(function (entries) {
                if (!entries.some(function (e) { return e.isIntersecting; })) return;
                observer.disconnect();
                if (started) return;
                started = true;
                box.classList.add('is-live');
                show(0);
            }, { threshold: .35 });

            observer.observe(box);
        });
    }

    function initAll() {
        initHero();
        initCatsCarousel();
        initEthnicCarousel();
        initQuotes();
    }

    let initTimer = null;
    function scheduleInit() {
        clearTimeout(initTimer);
        initTimer = setTimeout(initAll, 150);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }
    document.addEventListener('enhancedload', initAll);

    // بلازور هنگام اتصال اولیه گره‌های DOM را از نو می‌سازد؛ پس از هر تغییر،
    // بخش‌های راه‌اندازی‌نشده دوباره مقداردهی می‌شوند (init‌ها با data-flag محافظت شده‌اند).
    if (document.body) {
        new MutationObserver(scheduleInit).observe(document.body, { childList: true, subtree: true });
    }
})();
