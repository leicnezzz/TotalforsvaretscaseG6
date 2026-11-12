(() => {
    'use strict';
    let retryKartverketAt = 0;

    async function requestJson(url, signal, timeout) {
        const controller = new AbortController();
        const abort = () => controller.abort();
        signal.addEventListener('abort', abort, { once: true });
        if (signal.aborted) controller.abort();
        const timer = setTimeout(abort, timeout);
        try {
            const response = await fetch(url, {
                signal: controller.signal, credentials: 'omit', referrerPolicy: 'no-referrer'
            });
            if (!response.ok) throw new Error('Adressetjenesten er utilgjengelig.');
            return await response.json();
        } finally {
            clearTimeout(timer);
            signal.removeEventListener('abort', abort);
        }
    } // Forespørsel til Kartverkets API for henting av adresseforslag. 

    async function suggestions(query, signal) {
        if (Date.now() >= retryKartverketAt) {
            try {
                const url = new URL('https://api.kartverket.no/adresser/v1/sok');
                url.search = new URLSearchParams({ sok: query, treffPerSide: '5', utkoordsys: '4258', fuzzy: 'true' });
                const result = await requestJson(url, signal, 3000);
                if (result.adresser?.length) return result.adresser.map(address => ({
                    label: `${address.adressetekst}, ${address.postnummer} ${address.poststed}`,
                    latitude: Number(address.representasjonspunkt?.lat),
                    longitude: Number(address.representasjonspunkt?.lon)
                }));
            } catch (error) {
                if (signal.aborted) throw error;
                retryKartverketAt = Date.now() + 300000;
            }
        } // Hvis Kartverkets API ikke er tilgjengelig, hentes adresseforslag fra Photon. 
        const url = new URL('https://photon.komoot.io/api/');
        url.search = new URLSearchParams({ q: query, limit: '5', countrycode: 'NO', lat: '58.1467', lon: '7.9956' });
        const result = await requestJson(url, signal, 8000);
        return (result.features ?? [])
            .filter(feature => feature.properties?.countrycode?.toUpperCase() === 'NO')
            .map(feature => {
                const address = feature.properties;
                const street = [address.street || address.name, address.housenumber].filter(Boolean).join(' ');
                const city = address.city || address.town || address.village || address.locality;
                return {
                    label: [street, [address.postcode, city].filter(Boolean).join(' ')].filter(Boolean).join(', '),
                    latitude: Number(feature.geometry?.coordinates?.[1]),
                    longitude: Number(feature.geometry?.coordinates?.[0])
                };
            });
    } // henter adresseforslag, og returnerer liste med forslag: label, latitude og longitude.

    function attach(container, onChange = () => {}) {
        const address = container.querySelector('[name="Address"]');
        const latitude = container.querySelector('[name="Latitude"]');
        const longitude = container.querySelector('[name="Longitude"]');
        const results = container.querySelector('[data-address-results]');
        const status = container.querySelector('[data-address-status]');
        const mapElement = container.querySelector('[data-address-map]');
        let timer;
        let request;
        let map;
        let marker;

        function valid() {
            return Boolean(address.value.trim() && latitude.value && longitude.value &&
                Number.isFinite(Number(latitude.value)) && Math.abs(Number(latitude.value)) <= 90 &&
                Number.isFinite(Number(longitude.value)) && Math.abs(Number(longitude.value)) <= 180);
        } // Sjekker om adressefelt og koordinater er gyldige. 

        function refresh() {
            if (!mapElement?.getClientRects().length) return;
            if (!window.L) {
                status.textContent = 'Kartet kunne ikke lastes. Adresseforslag kan fortsatt brukes.';
                return;
            }
            if (!map) {
                map = L.map(mapElement).setView([58.1467, 7.9956], 12);
                L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19, attribution: '&copy; OpenStreetMap contributors'
                }).addTo(map);
                map.on('click', event => selectMapPoint(map.wrapLatLng(event.latlng)));
            }
            map.invalidateSize();
            if (valid()) {
                const point = [Number(latitude.value), Number(longitude.value)];
                const moved = !marker || !marker.getLatLng().equals(point);
                if (!marker) marker = L.circleMarker(point, {
                    radius: 8, color: '#fff', weight: 2, fillColor: '#2e6840', fillOpacity: 1
                }).addTo(map);
                else marker.setLatLng(point);
                const label = document.createElement('span');
                label.textContent = address.value;
                marker.bindPopup(label);
                if (moved) map.setView(point, 16);
            } else if (marker) {
                marker.remove();
                marker = null;
            }
        } // Oppdaterer kartet med valgt adresse og koordinater.

        function reset() {
            clearTimeout(timer);
            request?.abort();
            request = null;
            results.replaceChildren();
            results.hidden = true;
            address.setAttribute('aria-expanded', 'false');
            status.textContent = '';
            refresh();
        }

        function selectPosition(label, lat, lon, message) {
            address.value = label;
            latitude.value = lat.toFixed(6);
            longitude.value = lon.toFixed(6);
            address.setCustomValidity('');
            reset();
            status.textContent = message;
            onChange();
        }

        async function selectMapPoint(point) {
            reset();
            latitude.value = '';
            longitude.value = '';
            address.setCustomValidity('Vent mens plasseringen velges.');
            refresh();
            status.textContent = 'Ser etter adresse nær kartpunktet …';

            const current = new AbortController();
            request = current;
            const url = new URL('https://api.kartverket.no/adresser/v1/punktsok');
            url.search = new URLSearchParams({
                lat: point.lat.toFixed(6),
                lon: point.lng.toFixed(6),
                radius: '50',
                treffPerSide: '1'
            });

            let nearest;
            try {
                const result = await requestJson(url, current.signal, 3000);
                nearest = result.adresser?.[0];
            } catch {
                // Kartpunktet kan brukes selv om adresseoppslaget ikke svarer.
            }

            if (request !== current) return;

            const coordinates = `${point.lat.toFixed(6)}, ${point.lng.toFixed(6)}`;
            const label = nearest?.adressetekst
                ? [nearest.adressetekst, [nearest.postnummer, nearest.poststed].filter(Boolean).join(' ')]
                    .filter(Boolean).join(', ')
                : 'Kartplassering: ' + coordinates;
            const message = nearest?.adressetekst
                ? 'Kartpunkt valgt. Nærmeste adresse: ' + label
                : 'Kartpunkt valgt: ' + coordinates;
            selectPosition(label, point.lat, point.lng, message);
        }

        async function search() {
            const query = address.value.trim();
            if (query.length < 3) return;
            request?.abort();
            const current = new AbortController();
            request = current;
            status.textContent = 'Søker etter adresse …';
            try {
                const addresses = await suggestions(query, current.signal);
                if (request !== current || address.value.trim() !== query) return;
                results.replaceChildren();
                const seen = new Set();
                for (const match of addresses) {
                    if (!Number.isFinite(match.latitude) || !Number.isFinite(match.longitude) ||
                        Math.abs(match.latitude) > 90 || Math.abs(match.longitude) > 180 || seen.has(match.label)) continue;
                    seen.add(match.label);
                    const item = document.createElement('li');
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.textContent = match.label;
                    button.addEventListener('click', () => {
                        selectPosition(match.label, match.latitude, match.longitude, `Valgt adresse: ${match.label}`);
                        address.focus({ preventScroll: true });
                    });
                    item.append(button);
                    results.append(item);
                }
                results.hidden = !results.children.length;
                address.setAttribute('aria-expanded', String(!results.hidden));
                status.textContent = results.hidden
                    ? 'Ingen treff. Prøv gatenavn, husnummer og poststed.'
                    : 'Velg adressen din fra forslagene.';
            } catch {
                if (request === current && !current.signal.aborted) {
                    status.textContent = 'Adresseforslag er utilgjengelige. Prøv igjen om litt.';
                }
            }
        } // Søker etter adresseforslag og vises i en liste under adressefeltet. 
        // Om bruker velger et forslag, fylles adressefelt og koordinater i skjemaet. 

        address.addEventListener('input', () => {
            reset();
            latitude.value = '';
            longitude.value = '';
            address.setCustomValidity('Velg en adresse fra forslagene eller klikk i kartet.');
            refresh();
            status.textContent = address.value.trim().length < 3 ? 'Skriv minst tre tegn.' : 'Henter adresseforslag …';
            if (address.value.trim().length >= 3) timer = setTimeout(search, 600);
            onChange();
        });
        address.addEventListener('keydown', event => {
            if (event.key === 'Enter') {
                event.preventDefault();
                clearTimeout(timer);
                search();
            }
            if (event.key === 'ArrowDown' && !results.hidden) {
                event.preventDefault();
                results.querySelector('button')?.focus();
            }
        });
        results.addEventListener('keydown', event => {
            const buttons = [...results.querySelectorAll('button')];
            const index = buttons.indexOf(document.activeElement);
            if (event.key === 'Escape') {
                reset();
                address.focus();
            } else if (['ArrowUp', 'ArrowDown'].includes(event.key) && index >= 0) {
                event.preventDefault();
                buttons[(index + (event.key === 'ArrowDown' ? 1 : -1) + buttons.length) % buttons.length].focus();
            }
        });
        return { valid, refresh, reset, selectMapPoint };
    }

    window.ParatusResourceAddress = { attach };
})();
