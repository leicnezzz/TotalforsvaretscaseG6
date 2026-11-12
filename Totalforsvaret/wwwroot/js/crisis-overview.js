(() => {
    'use strict';

    for (const group of document.querySelectorAll('[data-crisis-group]')) {
        const multiple = group.querySelector('[data-crisis-multiple]');

        const cards = [...group.querySelectorAll('[data-crisis-card]')];
        const open = group.querySelector('[data-crisis-open]');
        const marked = new Set();

        function showDetails(card, visible) {
            card.querySelector('.crisis-details').hidden = !visible;
            card.querySelector('[data-crisis-select]').setAttribute('aria-expanded', String(visible));
        }

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
                    showDetails(card, card.querySelector('.crisis-details').hidden);
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
            for (const card of cards) {
                if (marked.has(card)) showDetails(card, true);
            }
        });
    }
})();
