(() => {
    'use strict';
    const breadcrumb = document.querySelector('.private-breadcrumb');
    const header = breadcrumb?.closest('.site-header');
    const nav = header?.querySelector('.primary-nav');
    const button = header?.querySelector('.private-back-to-top');
    if (!header || !nav || !button) return;

    // Bruk faktisk menyhøyde, også når menyen går over flere linjer på mobil.
    const measure = () => {
        header.style.setProperty('--private-nav-height', `${nav.getBoundingClientRect().height}px`);
    };
    measure();
    new ResizeObserver(measure).observe(nav);

    const updateButton = () => { button.hidden = window.scrollY < 300; };
    updateButton();
    window.addEventListener('scroll', updateButton, { passive: true });
    button.addEventListener('click', () => {
        const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        nav.querySelector('a')?.focus({ preventScroll: true });
        window.scrollTo({ top: 0, behavior: reduceMotion ? 'instant' : 'smooth' });
    });
})();
