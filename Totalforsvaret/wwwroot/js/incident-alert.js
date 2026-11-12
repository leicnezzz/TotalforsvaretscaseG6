(() => {
    for (const alert of document.querySelectorAll('[data-incident-key]')) {
        const storageKey = 'paratus:incident-dismissed:' + alert.dataset.incidentKey;

        try {
            alert.hidden = sessionStorage.getItem(storageKey) === '1';
        } catch {
            // Varslet kan fortsatt lukkes på siden uten tilgang til øktlagring.
        }

        alert.querySelector('[data-incident-dismiss]').addEventListener('click', () => {
            alert.hidden = true;

            try {
                sessionStorage.setItem(storageKey, '1');
            } catch {
                // Varslet er allerede lukket på siden.
            }
        });
    }
})();
