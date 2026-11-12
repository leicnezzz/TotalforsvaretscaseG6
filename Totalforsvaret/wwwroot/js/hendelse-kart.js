(() => {
    'use strict';

    const kartElement = document.getElementById('hendelseKart');
    if (!kartElement) return;

    const skjema = kartElement.closest('form');
    const latitude = skjema.querySelector('[name="Latitude"]');
    const longitude = skjema.querySelector('[name="Longitude"]');
    const status = document.getElementById('hendelseKartStatus');

    if (!window.L) {
        status.textContent = 'Kartet kunne ikke lastes. Last siden på nytt.';
        return;
    }

    function lesKoordinat(felt) {
        const tekst = felt.value.trim().replace(',', '.');
        return tekst === '' ? NaN : Number(tekst);
    }

    const startLat = lesKoordinat(latitude);
    const startLon = lesKoordinat(longitude);

    const harPlassering =
        Number.isFinite(startLat) &&
        Number.isFinite(startLon) &&
        startLat >= -90 && startLat <= 90 &&
        startLon >= -180 && startLon <= 180;

    const kart = L.map(kartElement).setView(
        harPlassering ? [startLat, startLon] : [58.1467, 7.9956],
        harPlassering ? 14 : 10
    );

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(kart);

    const farge = getComputedStyle(kartElement)
        .getPropertyValue('--akutt').trim() || '#DA2727';

    const svg = document.createElementNS(
        'http://www.w3.org/2000/svg',
        'svg'
    );

    svg.setAttribute('width', '28');
    svg.setAttribute('height', '28');
    svg.setAttribute('viewBox', '0 0 28 28');

    const trekant = document.createElementNS(
        'http://www.w3.org/2000/svg',
        'polygon'
    );

    trekant.setAttribute('points', '14,3 25,25 3,25');
    trekant.setAttribute('fill', farge);
    trekant.setAttribute('stroke', 'black');
    trekant.setAttribute('stroke-width', '3');
    trekant.setAttribute('stroke-linejoin', 'round');
    svg.append(trekant);

    const ikon = L.divIcon({
        className: 'hendelse-kartikon',
        html: svg.outerHTML,
        iconSize: [28, 28],
        iconAnchor: [14, 25]
    });

    let markor;

    function velgPlassering(punkt) {
        latitude.value = punkt.lat.toFixed(6);
        longitude.value = punkt.lng.toFixed(6);

        if (markor) {
            markor.setLatLng(punkt);
        } else {
            markor = L.marker(punkt, {
                icon: ikon,
                draggable: true,
                title: 'Hendelsens plassering'
            }).addTo(kart);

            markor.on('dragend', () => {
                velgPlassering(markor.getLatLng());
            });
        }

        status.textContent =
            'Valgt plassering: ' +
            latitude.value + ', ' + longitude.value;
    }

    kart.on('click', event => {
        velgPlassering(event.latlng);
    });

    if (harPlassering) {
        velgPlassering(L.latLng(startLat, startLon));
    }

    // Oppdater størrelsen når skjemasteget blir synlig igjen.
    const storrelse = new ResizeObserver(() => {
        if (kartElement.clientWidth && kartElement.clientHeight) {
            kart.invalidateSize({ pan: false });
        }
    });

    storrelse.observe(kartElement);
})();