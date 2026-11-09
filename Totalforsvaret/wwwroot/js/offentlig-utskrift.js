(() => {
    'use strict';

    document.querySelectorAll('[data-utskrift]').forEach(knapp => {
        knapp.addEventListener('click', () => {
            document.body.dataset.utskrift = knapp.dataset.utskrift;
            window.print();
        });
    });

    window.addEventListener('afterprint', () => {
        delete document.body.dataset.utskrift;
    });
})();