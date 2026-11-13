(() => {
    'use strict';

    const hendelser = new URLSearchParams(window.location.search).getAll('hendelser');
    if (!hendelser.length) return;

    for (const element of document.querySelectorAll('a[href], form[method="post"][action]')) {
        const attribute = element.matches('a') ? 'href' : 'action';
        const destination = element.getAttribute(attribute);
        if (!destination || destination.startsWith('#')) continue;

        const url = new URL(destination, window.location.href);
        const offentligSide =
            /(?:^|\/)(?:OffentligAktor|Offentlig|Crisis|Need|Behov|Ressurs)(?:\/|$)/i.test(url.pathname) ||
            /\/Home\/ResourceRegistration\/?$/i.test(url.pathname);
        if (url.origin !== window.location.origin || !offentligSide ||
            url.searchParams.has('hendelser')) continue;

        for (const id of hendelser) url.searchParams.append('hendelser', id);
        element.setAttribute(attribute, url.pathname + url.search + url.hash);
    }
})();
