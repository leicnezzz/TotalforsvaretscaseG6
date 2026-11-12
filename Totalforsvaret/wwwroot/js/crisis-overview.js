(() => {
    'use strict';

    const overview = document.querySelector('[data-crisis-map-url]');
    if (!overview) return;

    function openMap(cards) {
        const url = new URL(overview.dataset.crisisMapUrl, window.location.href);
        const ids = new Set(new URLSearchParams(window.location.search).getAll('hendelser'));
        for (const card of cards) ids.add(card.dataset.crisisId);
        for (const id of ids) url.searchParams.append('hendelser', id);
        url.hash = 'offentligKart';
        window.location.assign(url);
    }

    for (const group of document.querySelectorAll('[data-crisis-group]')) {
        const multiple = group.querySelector('[data-crisis-multiple]');

        const cards = [...group.querySelectorAll('[data-crisis-card]')];
        const open = group.querySelector('[data-crisis-open]');
        const marked = new Set();

        if (multiple) multiple.parentElement.hidden = false;
        if (open) open.parentElement.hidden = false;
        multiple?.addEventListener('change', () => {
            marked.clear();
            open.disabled = true;
            for (const card of cards) {
                card.classList.remove('is-marked');
                const button = card.querySelector('[data-crisis-select]');
                if (multiple.checked) button.setAttribute('aria-pressed', 'false');
                else button.removeAttribute('aria-pressed');
            }
        });

        for (const card of cards) {
            const button = card.querySelector('[data-crisis-select]');
            button.addEventListener('click', () => {
                if (!multiple?.checked) {
                    openMap([card]);
                    return;
                }

                if (marked.has(card)) marked.delete(card);
                else marked.add(card);
                card.classList.toggle('is-marked', marked.has(card));
                button.setAttribute('aria-pressed', String(marked.has(card)));
                open.disabled = marked.size === 0;
            });
        }

        open?.addEventListener('click', () => {
            if (marked.size) openMap(marked);
        });
    }
})();
