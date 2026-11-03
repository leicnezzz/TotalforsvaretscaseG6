(() => {
    'use strict';
    // Kun lokal demo. Ingen nettverkskall eller lagring; fjernes ved backend-integrasjon.
    const list = document.getElementById('resource-list');
    const status = document.getElementById('overview-status');
    if (!list || !status) return;

    list.addEventListener('submit', event => {
        const form = event.target.closest('[data-demo-edit]');
        if (!form) return;
        event.preventDefault();
        const card = form.closest('[data-resource-id]');
        const available = form.elements.Available.value === 'true';
        const label = card.querySelector('.resource-availability');
        const dot = document.createElement('i');
        dot.className = available ? 'available' : 'unavailable';
        dot.setAttribute('aria-hidden', 'true');
        label.replaceChildren(document.createTextNode(available ? 'Tilgjengelig ' : 'Ikke tilgjengelig '), dot);
        status.textContent = 'Demovisningen er oppdatert. Ingen endringer er lagret i databasen.';
    });

    list.addEventListener('change', event => {
        if (event.target.name !== 'Placement') return;
        const form = event.target.closest('form');
        const other = form.querySelector('.resource-other-address');
        other.hidden = event.target.value !== 'other';
        other.querySelector('input').disabled = other.hidden;
    });

    list.addEventListener('click', event => {
        const button = event.target.closest('[data-demo-delete]');
        if (!button) return;
        const card = button.closest('[data-resource-id]');
        const name = card.querySelector('strong').textContent;
        if (!window.confirm(`Fjerne ${name} fra demovisningen? Dette påvirker ikke databasen.`)) return;
        card.remove();
        const next = list.querySelector('summary');
        document.getElementById('resource-empty').hidden = Boolean(next);
        status.textContent = `${name} er fjernet fra demovisningen. Last siden på nytt for å få den tilbake.`;
        if (next) next.focus();
        else document.querySelector('.overview-intro a').focus();
    });
})();
