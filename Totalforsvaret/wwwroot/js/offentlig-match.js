(() => {
    'use strict';

    const page = document.getElementById('match');
    const data = JSON.parse(document.getElementById('oversiktListeData').textContent);
    const mapElement = page.querySelector('[data-match-map]');
    const mapStatus = page.querySelector('[data-match-map-status]');
    const summary = page.querySelector('[data-match-summary]');
    const pairFields = page.querySelector('[data-match-pairs]');
    const confirm = page.querySelector('[data-match-confirm]');
    const start = [58.1467, 7.9956];
    const groups = {};
    let map;

    function coordinates(item) {
        const { latitude, longitude } = item;
        return Number.isFinite(latitude) && Number.isFinite(longitude) &&
            Math.abs(latitude) <= 90 && Math.abs(longitude) <= 180
            ? [latitude, longitude] : null;
    }

    function fit(points, animate = true) {
        if (!map || !points.length) return;
        const bounds = L.latLngBounds(points);
        if (bounds.getNorthEast().equals(bounds.getSouthWest())) {
            map.setView(points[0], 16, { animate });
        } else {
            map.fitBounds(bounds, { padding: [30, 30], maxZoom: 16, animate });
        }
    }

    function selectedItems() {
        return Object.values(groups).flatMap(group =>
            group.items.filter(item => group.selected.has(item.id)));
    }

    function focusSelection() {
        const selected = selectedItems();
        const points = selected.map(coordinates).filter(Boolean);
        fit(points, !window.matchMedia('(prefers-reduced-motion: reduce)').matches);
        if (!map) return;
        mapStatus.textContent = selected.length > points.length
            ? 'Noen valgte oppføringer mangler kartplassering.' : '';
    }

    function updateSummary() {
        const needs = [...groups.behov.selected];
        const resources = [...groups.ressurser.selected];
        const canMatch = resources.length > 0 && (needs.length === 1 || needs.length === resources.length);
        confirm.disabled = !canMatch;
        pairFields.replaceChildren();

        if (!needs.length || !resources.length) {
            summary.textContent = 'Velg minst ett behov og én ressurs.';
            return;
        }
        if (!canMatch) {
            summary.textContent = 'Velg ett behov for alle ressursene, eller like mange behov som ressurser.';
            return;
        }

        for (const [name, ids] of [['needId', needs], ['resourceId', resources]]) {
            for (const id of ids) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = name;
                input.value = id;
                pairFields.append(input);
            }
        }
        const pairs = resources.map((id, index) => {
            const resource = groups.ressurser.items.find(item => item.id === id);
            const need = groups.behov.items.find(item => item.id === (needs.length === 1 ? needs[0] : needs[index]));
            return resource.navn + ' → ' + need.navn;
        });
        summary.textContent = pairs.join(' · ');
    }

    function choose(group, item) {
        if (group.selected.has(item.id)) group.selected.delete(item.id);
        else {
            if (!group.multiple.checked) group.selected.clear();
            group.selected.add(item.id);
        }

        group.results.querySelectorAll('[data-match-item]').forEach(button => {
            button.setAttribute('aria-pressed', String(group.selected.has(Number(button.dataset.matchItem))));
        });
        group.markers.forEach((marker, id) => marker.setStyle({
            radius: group.selected.has(id) ? 9 : 6,
            weight: group.selected.has(id) ? 3 : 2
        }));
        updateSummary();
        focusSelection();
    }

    function render(group) {
        const query = group.search.value.trim().toLocaleLowerCase('nb');
        const matches = group.items.filter(item =>
            [item.navn, item.type, item.krise, item.adresse, item.tilbyder, item.status, item.prioritet, group.label(item)]
                .some(value => String(value ?? '').toLocaleLowerCase('nb').includes(query)));
        group.results.replaceChildren();
        if (!matches.length) {
            const empty = document.createElement('li');
            empty.className = 'match-hint';
            empty.textContent = query ? 'Ingen treff på søket.' : 'Ingen oppføringer å vise.';
            group.results.append(empty);
        }

        matches.forEach(item => {
            const row = document.createElement('li');
            row.className = 'match-result';
            const button = document.createElement('button');
            button.type = 'button';
            button.dataset.matchItem = item.id;
            button.setAttribute('aria-pressed', String(group.selected.has(item.id)));

            const icon = document.createElement('span');
            icon.className = 'match-card-icon';
            icon.setAttribute('aria-hidden', 'true');
            if (group.key === 'behov') {
                icon.append(page.querySelector('#match-need-icon').content.cloneNode(true));
            }

            const content = document.createElement('span');
            content.className = 'match-card-content';
            const title = document.createElement('strong');
            title.textContent = item.navn;
            const detail = document.createElement('span');
            detail.className = 'match-card-detail';
            detail.textContent = (group.key === 'behov' ? item.krise : item.adresse) || group.label(item);
            const status = document.createElement('span');
            status.className = 'match-card-status';
            status.textContent = group.key === 'ressurser' ? 'Tilgjengelig'
                : item.prioritet === 'Kritisk' ? 'Kritisk behov' : item.status;
            status.classList.toggle('match-critical', item.prioritet === 'Kritisk');

            content.append(title, detail, status);
            button.append(icon, content);
            button.addEventListener('click', () => choose(group, item));
            row.append(button);
            group.results.append(row);
        });
    }

    for (const key of ['behov', 'ressurser']) {
        const isNeed = key === 'behov';
        const group = {
            key,
            items: isNeed ? data.behov.filter(item => item.status !== 'Løst') : data.ressurser,
            label: item => isNeed ? [item.type, item.prioritet, item.krise].filter(Boolean).join(' · ')
                : [({Vehicles: 'Kjøretøy', Equipment: 'Utstyr'})[item.kategori] || item.kategori, 'Tilgjengelig'].filter(Boolean).join(' · '),
            search: page.querySelector('[data-match-search="' + key + '"]'),
            multiple: page.querySelector('[data-match-multiple="' + key + '"]'),
            results: page.querySelector('[data-match-results="' + key + '"]'),
            selected: new Set(),
            markers: new Map()
        };
        groups[key] = group;
        group.search.addEventListener('input', () => render(group));
        page.querySelector('[data-match-clear="' + key + '"]').addEventListener('click', () => {
            group.search.value = '';
            render(group);
            group.search.focus();
        });
        group.multiple.addEventListener('change', () => {
            if (group.multiple.checked || group.selected.size <= 1) return;
            const latest = [...group.selected].pop();
            group.selected.clear();
            choose(group, group.items.find(item => item.id === latest));
        });
        render(group);
    }

    function initMap() {
        if (!window.L) {
            mapStatus.textContent = 'Kartet kunne ikke lastes. Du kan fortsatt søke i listene.';
            return;
        }
        map = L.map(mapElement, { scrollWheelZoom: false }).setView(start, 12);
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19, attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        const points = [];
        Object.values(groups).forEach(group => group.items.forEach(item => {
            const point = coordinates(item);
            if (!point) return;
            points.push(point);
            const marker = L.circleMarker(point, {
                radius: 6, weight: 2, color: '#fff',
                fillColor: group.key === 'behov' ? '#163c4c' : '#2e6840', fillOpacity: 1
            }).addTo(map);
            const label = document.createElement('span');
            label.textContent = item.navn;
            marker.bindTooltip(label);
            marker.on('click', () => choose(group, item));
            group.markers.set(item.id, marker);
        }));
        fit(points, false);
    }

    page.querySelector('[data-match-form]').addEventListener('submit', event => {
        if (confirm.disabled) event.preventDefault();
        else confirm.disabled = true;
    });
    initMap();
})();
