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

    function attach(container, onChange = () => {}, { allowMapSelection = false, lookupMapAddress = false, markerColor = '#2e6840' } = {}) {
        const address = container.querySelector('[data-address-input], [name="Address"]');
        const latitude = container.querySelector('[name="Latitude"]');
        const longitude = container.querySelector('[name="Longitude"]');
        const results = container.querySelector('[data-address-results]');
        const status = container.querySelector('[data-address-status]');
        const mapElement = container.querySelector('[data-address-map]');
        let timer;
        let request;
        let map;
        let marker;
        let selectedAddress = address.value;
        let pointLookup;

        function valid() {
            return Boolean((allowMapSelection || address.value.trim()) && latitude.value && longitude.value &&
                Number.isFinite(Number(latitude.value.replace(',', '.'))) && Math.abs(Number(latitude.value.replace(',', '.'))) <= 90 &&
                Number.isFinite(Number(longitude.value.replace(',', '.'))) && Math.abs(Number(longitude.value.replace(',', '.'))) <= 180);
        } // Sjekker om adressefelt og koordinater er gyldige. 

        function selectMapPoint(point, label = '') {
            if (!point || !Number.isFinite(point.lat) || !Number.isFinite(point.lng) ||
                Math.abs(point.lat) > 90 || Math.abs(point.lng) > 180) return;
            latitude.value = point.lat.toFixed(6);
            longitude.value = point.lng.toFixed(6);
            selectedAddress = label;
            address.value = allowMapSelection ? '' : 'Valgt plassering på kartet';
            address.setCustomValidity('');
            reset(Boolean(label));
            status.textContent = label || 'Plassering valgt på kartet.';
            onChange(label);
        }

        function refresh(recenter = true) {
            if (!mapElement.getClientRects().length) return;
            if (!window.L) {
                status.textContent = 'Kartet kunne ikke lastes. Adresseforslag kan fortsatt brukes.';
                return;
            }
            if (!map) {
                map = L.map(mapElement).setView([58.1467, 7.9956], 12);
                L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19, attribution: '&copy; OpenStreetMap contributors'
                }).addTo(map);
                if (lookupMapAddress) {
                    map.on('click', ({ latlng }) => lookupAddress(latlng.wrap()));
                } else if (allowMapSelection) {
                    map.on('click', ({ latlng }) => selectMapPoint(latlng.wrap()));
                }
            }
            map.invalidateSize();
            if (valid()) {
                const point = [Number(latitude.value.replace(',', '.')), Number(longitude.value.replace(',', '.'))];
                const moved = !marker || !marker.getLatLng().equals(point);
                if (!marker) marker = L.circleMarker(point, {
                    radius: 8, color: '#fff', weight: 2, fillColor: markerColor, fillOpacity: 1
                }).addTo(map);
                else marker.setLatLng(point);
                const label = document.createElement('span');
                label.textContent = (allowMapSelection ? selectedAddress : address.value) || 'Valgt plassering';
                marker.bindPopup(label);
                if (moved && recenter) map.setView(point, 16);
            } else if (marker) {
                marker.remove();
                marker = null;
            }
        } // Oppdaterer kartet med valgt adresse og koordinater.

        function reset(recenter = true) {
            clearTimeout(timer);
            request?.abort();
            request = null;
            pointLookup = null;
            results.replaceChildren();
            results.hidden = true;
            address.setAttribute('aria-expanded', 'false');
            status.textContent = '';
            refresh(recenter);
        }

        async function lookupAddress(point) {
            selectMapPoint(point);
            if (!allowMapSelection) {
                address.value = '';
                onChange();
            }
            const current = new AbortController();
            request = current;
            pointLookup = current;
            const timeout = setTimeout(() => current.abort(), 7000);
            status.textContent = 'Finner nærmeste adresse …';
            try {
                const url = new URL('https://api.kartverket.no/adresser/v1/punktsok');
                url.search = new URLSearchParams({
                    lat: point.lat, lon: point.lng, radius: 150, treffPerSide: 1
                });
                const response = await fetch(url, { signal: current.signal });
                if (!response.ok) throw new Error('Adresseoppslag feilet');
                const result = (await response.json()).adresser?.[0];
                if (request !== current) return;
                if (!result?.adressetekst) {
                    status.textContent = 'Ingen adresse i nærheten. Søk etter en adresse eller velg et annet punkt.';
                    return;
                }

                latitude.value = point.lat.toFixed(6);
                longitude.value = point.lng.toFixed(6);
                address.value = [result.adressetekst, [result.postnummer, result.poststed]
                    .filter(Boolean).join(' ')].filter(Boolean).join(', ');
                selectedAddress = address.value;
                address.setCustomValidity('');
                status.textContent = 'Nærmeste adresse: ' + address.value;
                refresh(false);
                onChange(selectedAddress);
            } catch {
                if (request === current) status.textContent = 'Adresseoppslaget er utilgjengelig. Prøv adressesøk.';
            } finally {
                clearTimeout(timeout);
                if (request === current) request = null;
                if (pointLookup === current) pointLookup = null;
            }
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
                        address.value = match.label;
                        selectedAddress = match.label;
                        latitude.value = match.latitude.toFixed(6);
                        longitude.value = match.longitude.toFixed(6);
                        reset();
                        status.textContent = `Valgt adresse: ${match.label}`;
                        address.setCustomValidity('');
                        onChange(match.label);
                        refresh();
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
            if (!allowMapSelection) {
                latitude.value = '';
                longitude.value = '';
                selectedAddress = '';
                address.setCustomValidity('Velg en adresse fra forslagene.');
                refresh();
            }
            status.textContent = address.value.trim().length < 3 ? 'Skriv minst tre tegn.' : 'Henter adresseforslag …';
            if (address.value.trim().length >= 3) timer = setTimeout(search, 600);
            if (!allowMapSelection) onChange();
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
        return { valid, refresh, reset, lookupAddress, pending: () => Boolean(pointLookup) };
    }

    window.ParatusResourceAddress = { attach };
})();
