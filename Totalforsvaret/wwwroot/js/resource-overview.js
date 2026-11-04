(() => {
    'use strict';
    const list = document.getElementById('resource-list');
    const status = document.getElementById('overview-status');
    if (!list || !status) return;

    const search = document.getElementById('resource-search');
    const filter = document.getElementById('resource-filter');
    const editors = new Map();
    const deleteDialog = document.getElementById('resource-delete-confirm');
    let pendingDelete;

    deleteDialog.querySelector('[data-delete-cancel]').addEventListener('click', () => deleteDialog.close());
    deleteDialog.addEventListener('close', () => { pendingDelete = null; });

    function filterCards() {
        const query = search.value.trim().toLocaleLowerCase('nb');
        const cards = [...list.querySelectorAll('[data-resource-id]')];
        for (const card of cards) {
            card.hidden = !card.querySelector('strong').textContent.toLocaleLowerCase('nb').includes(query) ||
                Boolean(filter.value && card.dataset.resourceKind !== filter.value);
        }
        const count = cards.filter(card => !card.hidden).length;
        document.getElementById('resource-count').textContent = count + (count === 1 ? ' ressurs' : ' ressurser');
        const empty = document.getElementById('resource-empty');
        empty.hidden = count > 0;
        empty.textContent = cards.length
            ? 'Ingen ressurser passer søket. Prøv et annet navn eller en annen kategori.'
            : 'Du har ingen ressurser å vise. Velg «Registreringen» for å legge til en ressurs.';
    }

    function snapshot(form) {
        return [...form.elements].filter(element => element.name).map(element => ({
            element, value: element.value, checked: element.checked
        }));
    }

    function updateSummary(card, resource) {
        card.querySelector('strong').textContent = resource.Object;
        const info = resource.Vehicle
            ? 'Kjøretøy · Førerkort kl. ' + (resource.Vehicle.RequiredLicense || 'Ikke oppgitt')
            : 'Utstyr · ' + resource.Equipment.EquipmentType + ' · ' + resource.Equipment.Quantity + ' stk.';
        card.querySelector('[data-resource-info]').textContent = info;
        const dot = document.createElement('i');
        dot.className = resource.Available ? 'available' : 'unavailable';
        dot.setAttribute('aria-hidden', 'true');
        card.querySelector('.resource-availability').replaceChildren(
            document.createTextNode(resource.Available ? 'Tilgjengelig ' : 'Ikke tilgjengelig '), dot
        );
    }

    for (const card of list.querySelectorAll('[data-resource-id]')) {
        const form = card.querySelector('form');
        const localStatus = form.querySelector('.resource-edit-status');
        const location = window.ParatusResourceAddress.attach(form.querySelector('[data-resource-address]'), () => {
            localStatus.textContent = 'Du har ulagrede endringer.';
        });
        const editor = { saved: snapshot(form), location, busy: false };
        editors.set(card, editor);
        card.addEventListener('toggle', () => {
            if (card.open) requestAnimationFrame(location.refresh);
        });
        form.addEventListener('input', () => { localStatus.textContent = 'Du har ulagrede endringer.'; });
        form.addEventListener('change', () => {
            const group = form.querySelector('[data-registration-number]');
            if (group) {
                const visible = form.elements.HasRegistrationNumber.value === 'true';
                group.hidden = !visible;
                group.querySelector('input').disabled = !visible;
            }
        });
        form.addEventListener('submit', event => {
            event.preventDefault();
            if (editor.busy) return;
            const phone = form.elements.Kontaktpunkt;
            for (const name of ['Object', 'Condition']) {
                form.elements[name].setCustomValidity(form.elements[name].value.trim() ? '' : 'Fyll ut dette feltet.');
            }
            phone.setCustomValidity(window.ParatusResources.validPhone(phone.value)
                ? '' : 'Skriv et telefonnummer med 8–15 sifre.');
            form.elements.Address.setCustomValidity(location.valid() ? '' : 'Velg en adresse fra forslagene.');
            if (!form.reportValidity()) return;
            const resource = window.ParatusResources.readResource(form);
            resource.ResourceId = form.elements.ResourceId.value;
            editor.busy = true;
            form.inert = true;
            form.setAttribute('aria-busy', 'true');
            localStatus.textContent = 'Lagrer …';
            window.ParatusResources.submitPreview(form, 'resource:update', resource, (result = { demo: true }) => {
                editor.busy = false;
                form.inert = false;
                form.removeAttribute('aria-busy');
                editor.saved = snapshot(form);
                updateSummary(card, resource);
                localStatus.textContent = result.demo ? 'Demovisningen er oppdatert. Endringene gjelder til siden lastes på nytt.' : 'Endringene er lagret.';
                status.textContent = resource.Object + ' er oppdatert' + (result.demo ? ' i demovisningen.' : '.');
                filterCards();
            }, text => {
                editor.busy = false;
                form.inert = false;
                form.removeAttribute('aria-busy');
                localStatus.textContent = text || 'Endringene kunne ikke lagres. Prøv igjen.';
            });
        });
    }

    list.addEventListener('click', event => {
        const card = event.target.closest('[data-resource-id]');
        if (!card) return;
        const editor = editors.get(card);
        if (!editor || editor.busy) return;
        const form = card.querySelector('form');
        if (event.target.closest('[data-resource-cancel]')) {
            for (const saved of editor.saved) {
                saved.element.value = saved.value;
                if (typeof saved.checked === 'boolean') saved.element.checked = saved.checked;
                if (saved.element.setCustomValidity) saved.element.setCustomValidity('');
            }
            form.dispatchEvent(new Event('change'));
            editor.location.reset();
            form.querySelector('.resource-edit-status').textContent = '';
            card.open = false;
            card.querySelector('summary').focus();
            return;
        }
        if (!event.target.closest('[data-demo-delete]')) return;
        const name = card.querySelector('strong').textContent;
        pendingDelete = card;
        document.getElementById('delete-resource-name').textContent = name;
        deleteDialog.showModal();
    });

    deleteDialog.querySelector('[data-delete-confirm]').addEventListener('click', () => {
        const card = pendingDelete;
        if (!card) return;
        const editor = editors.get(card);
        const form = card.querySelector('form');
        const name = card.querySelector('strong').textContent;
        deleteDialog.close();
        editor.busy = true;
        form.inert = true;
        window.ParatusResources.submitPreview(form, 'resource:delete', {
            ResourceId: form.elements.ResourceId.value
        }, (result = { demo: true }) => {
            editor.location.reset();
            card.remove();
            editors.delete(card);
            status.textContent = name + (result.demo ? ' er fjernet fra demovisningen.' : ' er slettet.');
            filterCards();
            const next = [...list.querySelectorAll('[data-resource-id]')].find(element => !element.hidden);
            if (next) next.querySelector('summary').focus();
            else document.querySelector('.overview-intro a').focus();
        }, text => {
            editor.busy = false;
            form.inert = false;
            form.querySelector('.resource-edit-status').textContent = text || 'Ressursen kunne ikke slettes. Prøv igjen.';
        });
    });
    search.addEventListener('input', filterCards);
    filter.addEventListener('change', filterCards);
    filterCards();
})();
