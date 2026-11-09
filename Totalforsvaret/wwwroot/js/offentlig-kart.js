(() => {
    'use strict';

    const melding = document.getElementById('kartmelding');
    if (!melding) return;

    if (!window.L) {
        melding.textContent =
            'Kartet kunne ikke lastes. Kontroller internettilkoblingen.';
        return;
    }

    const data = JSON.parse(
        document.getElementById('offentligKartData').textContent
    );

    const kart = L.map('offentligKart')
        .setView([58.1467, 7.9956], 12);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution:
            '&copy; <a href="https://www.openstreetmap.org/copyright">' +
            'OpenStreetMap</a> contributors'
    }).addTo(kart);

    function gyldig(lat, lon) {
        return Number.isFinite(lat) &&
            Number.isFinite(lon) &&
            Math.abs(lat) <= 90 &&
            Math.abs(lon) <= 180;
    }

    function popup(element, felter) {
        const innhold = document.createElement('div');
        const tittel = document.createElement('strong');

        tittel.textContent = element.navn;
        innhold.append(tittel);

        for (const [etikett, felt] of felter) {
            const linje = document.createElement('div');

            linje.textContent =
                etikett + ': ' + (element[felt] || 'Ikke oppgitt');

            innhold.append(linje);
        }

        return innhold;
    }

    // Henter fargene fra systemets palett.
    const palett = getComputedStyle(
        document.querySelector('.offentlig-dashboard')
    );

    const farge = navn => palett.getPropertyValue(navn).trim();

    const typer = [
        {
            key: 'ressurser',
            navn: 'Tilgjengelige ressurser',
            farge: farge('--tilgjengelig'),
            felter: [
                ['Kategori', 'kategori'],
                ['Tilbyder', 'tilbyder'],
                ['Kontakt', 'kontaktpunkt'],
                ['Tilgjengelig fra', 'tilgjengeligFra']
            ]
        },
        {
            key: 'behov',
            navn: 'Aktive behov',
            farge: farge('--planlagt'),
            felter: [
                ['Type', 'type'],
                ['Prioritet', 'prioritet'],
                ['Status', 'status'],
                ['Beskrivelse', 'beskrivelse']
            ]
        },
        {
            key: 'kriser',
            navn: 'Aktive kriser',
            farge: farge('--akutt'),
            felter: [
                ['Type', 'type'],
                ['Alvorlighetsgrad', 'alvorlighetsgrad'],
                ['Status', 'status'],
                ['Beskrivelse', 'beskrivelse']
            ]
        }
    ];

    function ikon(type) {
        return L.divIcon({
            className: 'kart-markorikon',
            html:
                '<span class="kart-markor kart-markor-' +
                type +
                '" aria-hidden="true"></span>',
            iconSize: [24, 24],
            iconAnchor: [12, 12],
            popupAnchor: [0, -12]
        });
    }

    let ugyldige = 0;

    // Oppretter markørene for de tre typene.
    for (const type of typer) {
        type.lag = L.featureGroup().addTo(kart);
        type.markorer = [];

        for (const element of data[type.key] ?? []) {
            if (!gyldig(element.latitude, element.longitude)) {
                ugyldige++;
                continue;
            }

            const punkt = [element.latitude, element.longitude];

            const markor = type.key !== 'ressurser'
                ? L.marker(punkt, {
                    icon: ikon(type.key),
                    title: element.navn || type.navn
                })
                : L.circleMarker(punkt, {
                    radius: 9,
                    color: '#000000',
                    weight: 3,
                    fillColor: element.egen ? farge('--offentlig') : type.farge,
                    fillOpacity: 1
                });

            markor.oversiktId = element.id;
            markor.bindPopup(popup(element, type.felter));
            type.markorer.push(markor);
        }
    }

    // Søkepanelet starter sammenlagt.
    const panel = L.DomUtil.create('div', 'offentlig-kartfilter');

    panel.innerHTML = [
        '<form class="kart-stedsok" role="search" aria-label="Søk etter sted på kartet">',
        '<label for="kartSted" class="kart-skjult">Sted eller adresse</label>',
        '<div class="kart-sokrad">',
        '<input id="kartSted" type="search" placeholder="Søk etter sted" autocomplete="off" minlength="2" required />',
        '<button type="submit" hidden tabindex="-1" aria-hidden="true">Søk</button>',
        '<button type="button" class="kart-filterknapp" aria-label="Åpne kartfiltre" aria-expanded="false" aria-controls="kartDetaljer">▾</button>',
        '</div>',
        '</form>',
        '<div id="kartDetaljer" hidden>',
        '<p class="kart-sokstatus" role="status" aria-live="polite"></p>',
        '<ul class="kart-stedsforslag" aria-label="Stedsforslag" hidden></ul>',
        '<div class="kart-radiusrad">',
        '<label for="kartRadius">Radius</label>',
        '<select id="kartRadius">',
        '<option value="1000">1 km</option>',
        '<option value="5000" selected>5 km</option>',
        '<option value="10000">10 km</option>',
        '</select>',
        '<button class="kart-nullstill" type="button">Nullstill søk</button>',
        '</div>',
        '<p class="kart-sentrum">Alle steder vises. Velg et sted for å bruke radius.</p>',
        '<fieldset class="kart-typer"><legend>Vis på kartet</legend></fieldset>',
        '<p class="kart-treff" role="status" aria-live="polite"></p>',
        '<div class="kart-kilde">Stedssøk: <a href="https://photon.komoot.io/" target="_blank" rel="noopener noreferrer">Photon</a> · OpenStreetMap</div>',
        '</div>'
    ].join('');

    L.DomEvent.disableClickPropagation(panel);
    L.DomEvent.disableScrollPropagation(panel);

    const kontroll = L.control({ position: 'topright' });
    kontroll.onAdd = () => panel;
    kontroll.addTo(kart);

    const form = panel.querySelector('form');
    const sok = panel.querySelector('input[type="search"]');
    const sokstatus = panel.querySelector('.kart-sokstatus');
    const resultater = panel.querySelector('.kart-stedsforslag');
    const radius = panel.querySelector('select');
    const sentrumtekst = panel.querySelector('.kart-sentrum');
    const trefftekst = panel.querySelector('.kart-treff');
    const detaljer = panel.querySelector('#kartDetaljer');
    const filterknapp = panel.querySelector('.kart-filterknapp');

    function visKartfiltre(apent) {
        detaljer.hidden = !apent;

        filterknapp.textContent = apent ? '▴' : '▾';
        filterknapp.setAttribute('aria-expanded', String(apent));
        filterknapp.setAttribute(
            'aria-label',
            apent ? 'Lukk kartfiltre' : 'Åpne kartfiltre'
        );

        sok.setAttribute('aria-expanded', String(apent));
    }

    sok.setAttribute('aria-controls', 'kartDetaljer');

    sok.addEventListener('focus', () => visKartfiltre(true));
    form.addEventListener('submit', () => visKartfiltre(true));

    filterknapp.addEventListener('click', () => {
        visKartfiltre(detaljer.hidden);
    });

    visKartfiltre(false);

    // Fullskjerm inkluderer kart, søk, filtre og zoomknapper.
    const kartElement = document.getElementById('offentligKart');

    const fullKnapp = L.DomUtil.create(
        'button',
        'kart-fullskjermknapp'
    );

    fullKnapp.type = 'button';
    fullKnapp.setAttribute('aria-controls', 'offentligKart');

    const fullKontroll = L.control({ position: 'bottomright' });
    fullKontroll.onAdd = () => fullKnapp;
    fullKontroll.addTo(kart);

    L.DomEvent.disableClickPropagation(fullKnapp);

    function oppdaterFullskjerm() {
        const aktiv =
            document.fullscreenElement === kartElement ||
            kartElement.classList.contains('kart-fullskjerm');

        fullKnapp.textContent = aktiv ? '⤡' : '⤢';
        fullKnapp.title = aktiv
            ? 'Avslutt fullskjerm'
            : 'Vis kart i fullskjerm';

        fullKnapp.setAttribute('aria-label', fullKnapp.title);
        fullKnapp.setAttribute('aria-pressed', String(aktiv));

        requestAnimationFrame(() => {
            kart.invalidateSize({ pan: false });
        });
    }

    function sideFullskjerm(aktiv) {
        kartElement.classList.toggle('kart-fullskjerm', aktiv);

        document.body.classList.toggle(
            'kart-fullskjerm-apen',
            aktiv
        );

        oppdaterFullskjerm();
        fullKnapp.focus();
    }

    fullKnapp.addEventListener('click', async () => {
        if (kartElement.classList.contains('kart-fullskjerm')) {
            sideFullskjerm(false);
            return;
        }

        if (document.fullscreenElement === kartElement) {
            try {
                await document.exitFullscreen();
            } catch {
                melding.textContent =
                    'Bruk Esc for å avslutte fullskjerm.';
            }

            return;
        }

        try {
            if (!kartElement.requestFullscreen) {
                throw new Error('Bruk sidevisning');
            }

            await kartElement.requestFullscreen();
        } catch {
            sideFullskjerm(true);
        }
    });

    document.addEventListener('fullscreenchange', () => {
        oppdaterFullskjerm();
        fullKnapp.focus();
    });

    document.addEventListener('keydown', event => {
        if (
            event.key === 'Escape' &&
            kartElement.classList.contains('kart-fullskjerm')
        ) {
            sideFullskjerm(false);
        }
    });

    window.addEventListener('resize', oppdaterFullskjerm);
    oppdaterFullskjerm();

    let sentrum = null;
    let sirkel = null;
    let sentrumsmarkor = null;
    let foresporsel = null;
    let versjon = 0;

    function tilpassAlle() {
        const markorer = typer.flatMap(
            type => type.lag.getLayers()
        );

        if (markorer.length) {
            kart.fitBounds(
                L.featureGroup(markorer).getBounds(),
                {
                    padding: [40, 40],
                    maxZoom: 14
                }
            );
        }
    }

    function filtrer(tilpass = false) {
        const avstand = Number(radius.value);
        const antall = [];

        for (const type of typer) {
            type.lag.clearLayers();

            if (type.boks.checked) {
                for (const markor of type.markorer) {
                    const innenforRadius =
                        !sentrum ||
                        sentrum.distanceTo(
                            markor.getLatLng()
                        ) <= avstand;

                    if (innenforRadius) {
                        type.lag.addLayer(markor);
                    }
                }
            }

            antall.push(
                type.lag.getLayers().length +
                ' ' +
                type.navn.toLowerCase()
            );
        }

        if (sentrum) {
            if (sirkel) {
                sirkel.setLatLng(sentrum).setRadius(avstand);
            } else {
                sirkel = L.circle(sentrum, {
                    radius: avstand,
                    color: farge('--offentlig'),
                    weight: 2,
                    fillOpacity: 0.06,
                    interactive: false
                }).addTo(kart);
            }

            if (sentrumsmarkor) {
                sentrumsmarkor.setLatLng(sentrum);
            } else {
                sentrumsmarkor = L.circleMarker(sentrum, {
                    radius: 4,
                    color: '#FFFFFF',
                    weight: 2,
                    fillColor: farge('--offentlig'),
                    fillOpacity: 1,
                    interactive: false
                }).addTo(kart);
            }

            if (tilpass) {
                kart.fitBounds(sirkel.getBounds(), {
                    padding: [30, 30]
                });
            }
        } else if (tilpass) {
            tilpassAlle();
        }

        melding.textContent =
            antall.join(', ') +
            ' vist' +
            (sentrum
                ? ' innenfor ' + (avstand / 1000) + ' km'
                : '') +
            '. Grønn sirkel: ressurs. Gul firkant: behov. ' +
            'Rød trekant: krise. Klikk for detaljer.';

        if (!typer.some(type => type.boks.checked)) {
            melding.textContent =
                'Ingen typer er valgt. ' +
                'Merk av det du vil vise på kartet.';
        }

        if (ugyldige) {
            melding.textContent +=
                ' ' + ugyldige +
                ' oppføringer mangler gyldig posisjon.';
        }

        trefftekst.textContent = antall.join(' · ');
    }

    // Lager avkrysningsboksene.
    const felt = panel.querySelector('fieldset');

    for (const type of typer) {
        const label = document.createElement('label');
        const boks = document.createElement('input');

        boks.type = 'checkbox';
        boks.checked = true;
        boks.addEventListener('change', () => filtrer());

        type.boks = boks;

        const symbol = document.createElement('span');

        symbol.className =
            'kart-symbol kart-symbol-' + type.key;

        symbol.style.setProperty('--symbolfarge', type.farge);
        symbol.setAttribute('aria-hidden', 'true');

        label.append(
            boks,
            symbol,
            document.createTextNode(type.navn)
        );

        felt.append(label);
    }

    radius.addEventListener('change', () => filtrer(true));

    function avbryt() {
        versjon++;
        foresporsel?.abort();

        resultater.replaceChildren();
        resultater.hidden = true;
        sokstatus.textContent = '';

        form.querySelector('button').disabled = false;
    }

    sok.addEventListener('input', avbryt);

    sok.addEventListener('keydown', event => {
        if (event.key === 'Escape') avbryt();
    });

    panel.querySelector('.kart-nullstill')
        .addEventListener('click', () => {
            avbryt();

            sok.value = '';
            sentrum = null;

            sirkel?.remove();
            sentrumsmarkor?.remove();

            sirkel = null;
            sentrumsmarkor = null;

            sentrumtekst.textContent =
                'Alle steder vises. Velg et sted for å bruke radius.';

            filtrer(true);
            sok.focus();
        });

    // Søker etter norske steder og adresser.
    form.addEventListener('submit', async event => {
        event.preventDefault();
        avbryt();

        const query = sok.value.trim();

        if (query.length < 2) {
            sokstatus.textContent = 'Skriv minst to tegn.';
            return;
        }

        const minVersjon = versjon;
        const controller = new AbortController();

        foresporsel = controller;

        const timeout = setTimeout(
            () => controller.abort(),
            10000
        );

        form.querySelector('button').disabled = true;
        sokstatus.textContent = 'Søker etter sted …';

        try {
            const url = new URL(
                'https://photon.komoot.io/api/'
            );

            url.search = new URLSearchParams({
                q: query,
                limit: '6',
                countrycode: 'NO',
                lat: '58.1467',
                lon: '7.9956'
            });

            const response = await fetch(url, {
                signal: controller.signal,
                credentials: 'omit',
                referrerPolicy: 'no-referrer'
            });

            if (!response.ok) {
                throw new Error('Stedssøk utilgjengelig');
            }

            const json = await response.json();

            if (minVersjon !== versjon) return;

            const treff = (json.features ?? []).filter(feature => {
                const punkt = feature.geometry?.coordinates;

                return feature.properties?.countrycode
                        ?.toUpperCase() === 'NO' &&
                    punkt &&
                    gyldig(punkt[1], punkt[0]);
            });

            for (const feature of treff) {
                const egenskaper = feature.properties;

                const etikett = [...new Set([
                    egenskaper.name,
                    [
                        egenskaper.street,
                        egenskaper.housenumber
                    ].filter(Boolean).join(' '),
                    egenskaper.city ||
                    egenskaper.town ||
                    egenskaper.village ||
                    egenskaper.locality,
                    egenskaper.county
                ].filter(Boolean))].join(', ') || query;

                const li = document.createElement('li');
                const knapp = document.createElement('button');

                knapp.type = 'button';
                knapp.textContent = etikett;

                knapp.addEventListener('click', () => {
                    const [lon, lat] =
                        feature.geometry.coordinates;

                    sentrum = L.latLng(lat, lon);
                    sok.value = etikett;

                    sentrumtekst.textContent =
                        'Radius fra: ' + etikett;

                    avbryt();
                    filtrer(true);
                    sok.focus();
                });

                li.append(knapp);
                resultater.append(li);
            }

            resultater.hidden = !treff.length;

            sokstatus.textContent = treff.length
                ? 'Velg et sted i listen.'
                : 'Ingen steder funnet. Prøv et annet ' +
                'stedsnavn eller en adresse.';
        } catch (error) {
            if (minVersjon === versjon) {
                sokstatus.textContent =
                    'Stedssøket kunne ikke fullføres. Prøv igjen. ' +
                    'Kart og filtre kan fortsatt brukes.';
            }
        } finally {
            clearTimeout(timeout);

            if (minVersjon === versjon) {
                form.querySelector('button').disabled = false;
                foresporsel = null;
            }
        }
    });

    // Åpner en valgt oppføring fra kortenes lister.
    document.addEventListener('oversikt:vis-pa-kart', event => {
        const valgt = event.detail;
        const type = typer.find(t => t.key === valgt.type);

        const markor = type?.markorer.find(
            m => m.oversiktId === valgt.id
        );

        if (!markor) return;

        // Slår på typen dersom den er skjult i kartfilteret.
        type.boks.checked = true;

        // Fjerner radius hvis den skjuler den valgte oppføringen.
        if (
            sentrum &&
            sentrum.distanceTo(markor.getLatLng()) > Number(radius.value)
        ) {
            sentrum = null;

            sirkel?.remove();
            sentrumsmarkor?.remove();

            sirkel = null;
            sentrumsmarkor = null;

            sentrumtekst.textContent =
                'Radius fjernet for å vise valgt oppføring.';
        }

        filtrer();

        kart.setView(
            markor.getLatLng(),
            Math.max(kart.getZoom(), 15)
        );

        markor.openPopup();
    });
    
    filtrer(true);
})();