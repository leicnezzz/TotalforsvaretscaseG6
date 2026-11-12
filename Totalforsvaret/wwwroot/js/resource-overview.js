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

    const mapElement = document.getElementById('resource-overview-map');
    const mapStatus = document.getElementById('resource-overview-map-status');
    const defaultCenter = [58.1467, 7.9956];
    const map = mapElement && window.L
        ? L.map(mapElement).setView(defaultCenter, 12)
        : null;
    const markers = map ? L.featureGroup().addTo(map) : null;
    let pickingCard = null;
    let previewCard = null;
    let previewMarker = null;

    if (map) {
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(map);
    } else if (mapStatus) {
        mapStatus.textContent = 'Kartet kunne ikke lastes.';
    }

    function savedPosition(form) {
        const values = [form.elements.Latitude.value, form.elements.Longitude.value];
        if (values.some(value => !value.trim())) return null;

        const [lat, lon] = values.map(Number);
        return Number.isFinite(lat) && Number.isFinite(lon) &&
            Math.abs(lat) <= 90 && Math.abs(lon) <= 180 && (lat !== 0 || lon !== 0)
            ? [lat, lon]
            : null;
    }

    function setPickingCard(card) {
        editors.get(pickingCard)?.pickButton.setAttribute('aria-pressed', 'false');
        pickingCard = card;
        mapElement.classList.toggle('is-picking', Boolean(card));
        if (card) {
            editors.get(card).pickButton.setAttribute('aria-pressed', 'true');
            mapStatus.textContent = 'Velg ny plassering for ' + card.querySelector('strong').textContent + ' ved å klikke på kartet.';
        } else {
            mapStatus.textContent = markers.getLayers().length ? '' : 'Ingen ressurser med kartplassering å vise.';
        }
    }

    function clearPreview(card) {
        if (!previewMarker || previewCard !== card) return;
        previewMarker.remove();
        previewMarker = null;
        previewCard = null;
    }

    function showPreview(card, form) {
        clearPreview(card);
        const position = savedPosition(form);
        if (!map || !card.open || card.hidden || !position) return;

        const editor = editors.get(card);
        if (position.every((value, index) => value === editor.position?.[index])) return;

        clearPreview(previewCard);
        previewCard = card;
        previewMarker = L.circleMarker(position, {
            radius: 10, color: '#153a49', weight: 3, fillColor: '#fff', fillOpacity: 1,
            interactive: false
        }).addTo(map);
        const label = document.createElement('span');
        label.textContent = 'Ny plassering for ' + card.querySelector('strong').textContent;
        previewMarker.bindTooltip(label, { permanent: true, direction: 'top' });
        if (!map.getBounds().pad(-0.15).contains(position)) map.panTo(position);
    }

    if (map) {
        map.on('click', event => {
            if (!pickingCard) return;
            const card = pickingCard;
            const editor = editors.get(card);
            setPickingCard(null);
            if (!card.open || card.hidden || editor.busy) return;
            clearPreview(card);
            editor.location.selectMapPoint(map.wrapLatLng(event.latlng));
            editor.pickButton.focus();
        });
        document.addEventListener('keydown', event => {
            if (event.key !== 'Escape' || !pickingCard) return;
            const { pickButton } = editors.get(pickingCard);
            setPickingCard(null);
            pickButton.focus();
        });
    }

    function updateMap(cards) {
        if (!markers) return;

        markers.clearLayers();
        const groups = new Map();
        for (const card of cards) {
            if (card.hidden) continue;
            const position = editors.get(card)?.position;
            if (!position) continue;

            const key = position.join(',');
            if (!groups.has(key)) groups.set(key, { position, cards: [] });
            groups.get(key).cards.push(card);
        }

        for (const { position, cards: colocated } of groups.values()) {
            const popup = document.createElement(colocated.length === 1 ? 'span' : 'ul');
            if (colocated.length === 1) {
                popup.textContent = colocated[0].querySelector('strong').textContent;
            } else {
                popup.className = 'overview-map-popup';
                for (const card of colocated) {
                    const item = document.createElement('li');
                    item.textContent = card.querySelector('strong').textContent;
                    popup.append(item);
                }
            }

            L.circleMarker(position, {
                radius: colocated.length === 1 ? 8 : 12,
                color: '#fff',
                weight: 2,
                fillColor: '#2e6840',
                fillOpacity: 1
            }).bindPopup(popup).addTo(markers);
        }

        if (pickingCard) return;

        mapStatus.textContent = groups.size || previewCard ? '' : 'Ingen ressurser med kartplassering å vise.';
        if (previewCard) return;

        if (groups.size) {
            map.fitBounds(markers.getBounds(), { padding: [30, 30], maxZoom: 15 });
        } else {
            map.setView(defaultCenter, 12);
        }
    }

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
        if (pickingCard?.hidden) setPickingCard(null);
        if (previewCard?.hidden) clearPreview(previewCard);
        updateMap(cards);
    } // Filtrerer ressursene etter søk og kategori, oppdaterer antall treff.

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
        const pickButton = form.querySelector('[data-pick-resource-position]');
        const location = window.ParatusResourceAddress.attach(form.querySelector('[data-resource-address]'), () => {
            localStatus.textContent = 'Du har ulagrede endringer.';
            if (pickingCard === card) setPickingCard(null);
            showPreview(card, form);
        });
        const editor = { saved: snapshot(form), position: savedPosition(form), location, pickButton, busy: false };
        editors.set(card, editor);
        if (map) pickButton.hidden = false;
        pickButton.addEventListener('click', () => {
            if (editor.busy) return;
            if (pickingCard === card) {
                setPickingCard(null);
                return;
            }

            setPickingCard(card);
            const position = savedPosition(form);
            if (position && !map.getBounds().contains(position)) map.panTo(position);
            mapElement.closest('.overview-map-panel').scrollIntoView({ block: 'nearest' });
        });
        card.addEventListener('toggle', () => {
            if (card.open) {
                showPreview(card, form);
                return;
            }
            if (pickingCard === card) setPickingCard(null);
            clearPreview(card);
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
            form.elements.Address.setCustomValidity(location.valid() ? '' : 'Velg en adresse fra forslagene eller klikk i kartet.');
            if (!form.reportValidity()) return;
            if (pickingCard === card) setPickingCard(null);
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
                editor.position = savedPosition(form);
                clearPreview(card);
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
    } // Validerer feltene og sender endinger til lagring eller demovisning.
// Oppdaterer ressurskortet når handlingen lykkes.

    list.addEventListener('click', event => {
        const card = event.target.closest('[data-resource-id]');
        if (!card) return;
        const editor = editors.get(card);
        if (!editor || editor.busy) return;
        const form = card.querySelector('form');
        if (event.target.closest('[data-resource-cancel]')) {
            if (pickingCard === card) setPickingCard(null);
            clearPreview(card);
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
        if (!event.target.closest('[data-resource-delete], [data-demo-delete]')) return;
        const name = card.querySelector('strong').textContent;
        pendingDelete = card;
        document.getElementById('delete-resource-name').textContent = name;
        deleteDialog.showModal();
    }); // Håndterer klikk på knapper for å avbryte redigering eller slette ressurser.

    deleteDialog.querySelector('[data-delete-confirm]').addEventListener('click', () => {
        const card = pendingDelete;
        if (!card) return;
        const editor = editors.get(card);
        const form = card.querySelector('form');
        const name = card.querySelector('strong').textContent;
        deleteDialog.close();
        if (pickingCard === card) setPickingCard(null);
        editor.busy = true;
        form.inert = true;
        window.ParatusResources.submitPreview(form, 'resource:delete', {
            ResourceId: form.elements.ResourceId.value
        }, (result = { demo: true }) => {
            editor.location.reset();
            clearPreview(card);
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
})(); // Viser en oversikt over ressurser med søk og filter, redigering og sletting. 
