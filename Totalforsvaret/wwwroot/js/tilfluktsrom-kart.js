(() => {
    const form = document.getElementById('tilfluktsrom-sok');
    const input = document.getElementById('tilfluktsrom-adresse');
    const searchButton = form.querySelector('button[type="submit"]');
    const status = document.getElementById('tilfluktsrom-status');
    const addressResults = document.getElementById('tilfluktsrom-adressetreff');
    const shelterResults = document.getElementById('tilfluktsrom-resultater');
    const printButton = document.getElementById('tilfluktsrom-skriv-ut');
const printAddress = document.getElementById('tilfluktsrom-utskrift-adresse');

printButton.addEventListener('click', () => window.print());
    const markers = L.featureGroup().addTo(tilfluktsromKart);
    const numberFormat = new Intl.NumberFormat('nb-NO', {
        maximumFractionDigits: 1
    });

    let shelters;

    const shelterIcon = L.divIcon({
    className: 'shelter-map-icon',
    html: '<span aria-hidden="true">🛡&#xFE0E;</span>',
    iconSize: [32, 32],
    iconAnchor: [16, 16]
});

    async function loadShelters() {
        if (shelters) return shelters;

        const url = new URL('https://wfs.geonorge.no/skwms1/wfs.tilfluktsrom_offentlige');
        url.search = new URLSearchParams({
            service: 'WFS',
            version: '2.0.0',
            request: 'GetFeature',
            typeNames: 'app:Tilfluktsrom',
            srsName: 'urn:ogc:def:crs:EPSG::4258'
        });

        const response = await fetch(url);
        if (!response.ok) throw new Error('Tilfluktsrom kunne ikke lastes.');

        const xml = new DOMParser().parseFromString(await response.text(), 'application/xml');
        const text = (element, name) =>
            element.getElementsByTagNameNS('*', name)[0]?.textContent.trim();

        const rooms = [];
        for (const room of xml.getElementsByTagNameNS('*', 'Tilfluktsrom')) {
            const coordinates = text(room, 'pos');
            if (!coordinates) continue;

            const [lat, lon] = coordinates.split(/\s+/).map(Number);
            if (!Number.isFinite(lat) || !Number.isFinite(lon)) continue;

            const roomNumber = text(room, 'romnr');
            rooms.push({
                name: text(room, 'adresse') || `Tilfluktsrom ${roomNumber ?? ''}`.trim(),
                point: L.latLng(lat, lon)
            });
        }

        if (rooms.length === 0) throw new Error('Fant ingen tilfluktsrom i datasettet.');

        shelters = rooms;
        return shelters;
    }

    function formatDistance(meters) {
        return meters < 1000
            ? `${numberFormat.format(Math.round(meters))} m`
            : `${numberFormat.format(meters / 1000)} km`;
    }

    function addResult(list, label, onClick) {
        const item = document.createElement('li');
        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = label;
        button.addEventListener('click', onClick);
        item.append(button);
        list.append(item);
    }

    async function showNearest(address, label) {
        addressResults.replaceChildren();
        searchButton.disabled = true;
        status.textContent = 'Henter offentlige tilfluktsrom …';

        try {
            const position = address.representasjonspunkt;
            const origin = L.latLng(position.lat, position.lon);
            const rooms = await loadShelters();
            const nearest = rooms
                .map(room => ({ ...room, meters: origin.distanceTo(room.point) }))
                .sort((first, second) => first.meters - second.meters)
                .slice(0, 5);
markers.clearLayers();
shelterResults.replaceChildren();
            const addressMarker = L.circleMarker(origin, { radius: 8 }).addTo(markers);
            const addressPopup = document.createElement('p');
            addressPopup.textContent = `Din valgte adresse: ${label}`;
            addressMarker.bindPopup(addressPopup);

                       for (const room of nearest) {
                const distance = formatDistance(room.meters);
                const coordinates = `${room.point.lat.toFixed(5)}, ${room.point.lng.toFixed(5)}`;

                const popup = document.createElement('div');
                const heading = document.createElement('strong');
                heading.textContent = 'Tilfluktsrom';

                const details = document.createElement('p');
                details.textContent = `${room.name} · ${distance} i luftlinje · ${coordinates}`;
                popup.append(heading, details);

                const marker = L.marker(room.point, {
                    icon: shelterIcon,
                    title: 'Tilfluktsrom'
                }).addTo(markers)
                    .bindTooltip('Tilfluktsrom')
                    .bindPopup(popup);

                marker.getElement()?.setAttribute('aria-label', `Tilfluktsrom: ${room.name}`);

                const row = shelterResults.insertRow();
                const addressCell = row.insertCell();
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'shelter-table-link';
                button.textContent = room.name;

                button.addEventListener('click', () => {
                    tilfluktsromKart.panTo(room.point);
                    marker.openPopup();
                });

                addressCell.append(button);
                row.insertCell().textContent = distance;
                row.insertCell().textContent = coordinates;
            }

            tilfluktsromKart.fitBounds(markers.getBounds(), {
                padding: [24, 24],
                maxZoom: 16
            });
           status.textContent = `Viser de ${nearest.length} nærmeste offentlige tilfluktsrommene ved ${label}.`;
           printAddress.textContent = `Valgt adresse: ${label}`;
printButton.hidden = false;
        } catch {
            status.textContent = 'Kunne ikke hente tilfluktsrom. Prøv å søke på nytt.';
        } finally {
            searchButton.disabled = false;
        }
    }

   let suggestionTimer;
let addressRequest;

async function findAddresses() {
    clearTimeout(suggestionTimer);
    addressRequest?.abort();
    addressResults.replaceChildren();

    const [street, postcode] = input.value.split(',').map(part => part.trim());

    if (street.length < 3) {
        status.textContent = 'Skriv minst tre tegn for å få adresseforslag.';
        return;
    }

    const request = new AbortController();
    addressRequest = request;
    status.textContent = 'Søker etter adresser …';

    try {
        const url = new URL('https://api.kartverket.no/adresser/v1/sok');
        url.search = new URLSearchParams({
            sok: street,
            treffPerSide: '8',
            utkoordsys: '4258'
        });

        if (/^\d{4}$/.test(postcode ?? '')) {
            url.searchParams.set('postnummer', postcode);
        }

        const lastWord = street.split(/\s+/).at(-1);
const canSearchByPrefix = /^\p{L}{3,}$/u.test(lastWord);

if (canSearchByPrefix) {
    url.searchParams.set('sok', `${street}*`);
}

let response = await fetch(url, { signal: request.signal });

if (response.ok && canSearchByPrefix) {
    const prefixResults = await response.clone().json();

    if (!prefixResults.adresser?.length) {
        url.searchParams.set('sok', street);
        url.searchParams.set('fuzzy', 'true');
        response = await fetch(url, { signal: request.signal });
    }
}
        if (!response.ok) throw new Error('Adressesøket feilet.');

        const data = await response.json();
        if (request.signal.aborted) return;

        const addresses = (data.adresser ?? []).filter(address =>
            Number.isFinite(address.representasjonspunkt?.lat) &&
            Number.isFinite(address.representasjonspunkt?.lon)
        );

        status.textContent = addresses.length
            ? 'Velg riktig adresse i lista.'
            : 'Vi fant ingen adresse. Sjekk gatenavn og husnummer.';

        for (const address of addresses) {
            const label = `${address.adressetekst}, ${address.postnummer} ${address.poststed}`;
            addResult(addressResults, label, () => {
                input.value = `${address.adressetekst}, ${address.postnummer}`;
                showNearest(address, label);
            });
        }
    } catch {
        if (!request.signal.aborted) {
            status.textContent = 'Kunne ikke hente adresseforslag. Trykk Søk for å prøve igjen.';
        }
    }
}

input.addEventListener('input', () => {
    clearTimeout(suggestionTimer);
    addressRequest?.abort();
    addressResults.replaceChildren();
    suggestionTimer = setTimeout(findAddresses, 350);
});

form.addEventListener('submit', event => {
    event.preventDefault();
    findAddresses();
});

   input.disabled = false;
searchButton.disabled = false;
})();