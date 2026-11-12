(() => {
    'use strict';

    const NOKKEL = 'offentligScroll';
    const panel = document.querySelector('.oppfolging-panel');

    // Setter tilbake posisjonen etter at siden er lastet på nytt.
    try {
        const lagret = JSON.parse(sessionStorage.getItem(NOKKEL) || 'null');
        if (lagret) {
            sessionStorage.removeItem(NOKKEL);
            window.scrollTo(0, lagret.side);
            if (panel) panel.scrollTop = lagret.panel;
        }
    } catch { /* ignorer */ }

    // Husker posisjonen når et skjema i panelet sendes.
    document.addEventListener('submit', event => {
        if (!event.target.closest('.oppfolging-panel')) return;

        sessionStorage.setItem(NOKKEL, JSON.stringify({
            side: window.scrollY,
            panel: panel ? panel.scrollTop : 0
        }));
    });
})();