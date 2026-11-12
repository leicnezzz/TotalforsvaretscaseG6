(() => {
    'use strict';

    const melding = document.getElementById('kartmelding');
    if (!melding) return;

    const kartElement = document.getElementById('offentligKart');
    const bekreftelse = kartElement.querySelector('.kart-bekreftelse');
    const bekreftelsestekst = bekreftelse.querySelector('.kart-bekreftelsestekst');
    bekreftelse.querySelector('.btn-close').addEventListener('click', () => {
        bekreftelse.hidden = true;
        bekreftelsestekst.textContent = '';
        kartElement.focus({ preventScroll: true });
    });

    function visBekreftelse(tekst) {
        bekreftelse.hidden = false;
        bekreftelsestekst.textContent = tekst;
    }

    if (!window.L) {
        melding.textContent =
            'Kartet kunne ikke lastes. Kontroller internettilkoblingen.';
        return;
    }

    const data = JSON.parse(
        document.getElementById('offentligKartData').textContent
    );

    const forespurteHendelser = new Set(new URLSearchParams(window.location.search).getAll('hendelser'));
    const hendelsesvisning = forespurteHendelser.size > 0;
    const valgteHendelser = (data.kriser ?? []).filter(krise => forespurteHendelser.has(String(krise.id)));
    const synligeHendelser = new Set(valgteHendelser.map(krise => krise.id));
    const hendelserUtenPunkt = new Set(valgteHendelser
        .filter(krise => !gyldig(krise.latitude, krise.longitude))
        .map(krise => krise.id));

    function oppdaterKarthoyde() {
        const topp = kartElement.getBoundingClientRect().top + window.scrollY;
        kartElement.style.setProperty('--kart-topp', Math.ceil(topp) + 'px');
    }

    oppdaterKarthoyde();

    const standardvisning = { sentrum: [58.1467, 7.9956], zoom: 13 };
    const kart = L.map(kartElement, { zoomControl: false, maxZoom: 19 })
        .setView(standardvisning.sentrum, standardvisning.zoom);

    L.DomEvent.disableClickPropagation(bekreftelse);

    L.control.zoom({ position: 'bottomleft' }).addTo(kart);

    // Bruk kartfliser mens det sammenhengende kartbildet lastes.
    kart.createPane('terrengbilde').style.zIndex = 225;
    kart.createPane('dybdelag').style.zIndex = 240;

    const terreng = L.tileLayer(
        'https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png',
        { attribution: '&copy; Kartverket', maxNativeZoom: 18 }
    );
    const dybde = L.tileLayer.wms('https://wms.geonorge.no/skwms1/wms.dybdedata2', {
        layers: 'Dybdekontur,Dybdepunkt',
        pane: 'dybdelag',
        format: 'image/png',
        transparent: true,
        version: '1.3.0',
        attribution: 'Dybdedata: &copy; Kartverket'
    });
    const satellitt = L.tileLayer(
        'https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
        { attribution: 'Esri, Vantor, Earthstar Geographics og GIS User Community' }
    );
    const bakgrunnskart = {
        vanlig: L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }),
        terreng: L.layerGroup([terreng, dybde]),
        satellitt
    };
    let aktivtKart = bakgrunnskart.vanlig.addTo(kart);

    let terrengbilde = null;
    let lasterTerrengbilde = null;

    function oppdaterTerrengbilde() {
        if (aktivtKart !== bakgrunnskart.terreng) return;

        const storrelse = kart.getSize();
        if (!storrelse.x || !storrelse.y) return;

        const marg = L.point(96, 96);
        const utsnitt = storrelse.add(marg.multiplyBy(2));
        const skala = Math.min(1, 2048 / Math.max(utsnitt.x, utsnitt.y));
        const crs = kart.options.crs;
        const sorvest = kart.containerPointToLatLng(L.point(-marg.x, storrelse.y + marg.y));
        const nordost = kart.containerPointToLatLng(L.point(storrelse.x + marg.x, -marg.y));
        const nedre = crs.project(sorvest);
        const ovre = crs.project(nordost);
        const parametre = new URLSearchParams({
            service: 'WMS',
            request: 'GetMap',
            version: '1.3.0',
            layers: 'topo',
            styles: '',
            crs: crs.code,
            bbox: [nedre.x, nedre.y, ovre.x, ovre.y].join(','),
            width: Math.round(utsnitt.x * skala),
            height: Math.round(utsnitt.y * skala),
            format: 'image/png'
        });

        lasterTerrengbilde?.remove();
        const bilde = L.imageOverlay(
            'https://wms.geonorge.no/skwms1/wms.topo?' + parametre,
            L.latLngBounds(sorvest, nordost),
            { pane: 'terrengbilde', opacity: 0 }
        );
        lasterTerrengbilde = bilde;
        bilde.once('load', () => {
            if (lasterTerrengbilde !== bilde) return;
            terrengbilde?.remove();
            terrengbilde = bilde.setOpacity(1);
            lasterTerrengbilde = null;
        });
        bilde.once('error', () => {
            if (lasterTerrengbilde !== bilde) return;
            bilde.remove();
            lasterTerrengbilde = null;
        });
        bilde.addTo(kart);
    }

    kart.on('moveend', oppdaterTerrengbilde);
    oppdaterTerrengbilde();

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
            navn: hendelsesvisning ? 'Valgte hendelser' : 'Aktive kriser',
            farge: farge('--akutt'),
            felter: [
                ['Type', 'type'],
                ['Alvorlighetsgrad', 'alvorlighetsgrad'],
                ['Status', 'status'],
                ['Beskrivelse', 'beskrivelse']
            ]
        },
        {
            key: 'veihindringer',
            navn: 'Veihindringer',
            farge: '#a24512'
        },
        {
            key: 'soner',
            navn: 'Soner',
            farge: '#6e42a1'
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

    // Bruker de samme markørlagene for kartdata og midlertidige markeringer.
    for (const type of typer) {
        type.lag = L.featureGroup().addTo(kart);
        type.markorer = [];

        for (const element of data[type.key] ?? []) {
            if (!gyldig(element.latitude, element.longitude)) continue;

            const punkt = [element.latitude, element.longitude];

            const markor = type.key !== 'ressurser'
                ? L.marker(punkt, {
                    icon: ikon(type.key),
                    bubblingMouseEvents: true,
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
            markor.hendelseId = type.key === 'kriser' ? element.id : element.kriseId;
            markor.bindPopup(popup(element, type.felter));
            type.markorer.push(markor);
        }
    }

    // Stedssøk og filter deler den eksisterende filtreringen.
    const panel = L.DomUtil.create('div', 'offentlig-kartfilter');

    panel.innerHTML = [
        '<form class="kart-stedsok" role="search" aria-label="Søk etter sted på kartet">',
        '<label for="kartSted" class="kart-skjult">Sted eller adresse</label>',
        '<div class="kart-sokrad">',
        '<input id="kartSted" type="search" placeholder="Søk etter sted" autocomplete="off" minlength="2" required />',
        '<button type="submit">Søk</button>',
        '</div>',
        '<p class="kart-sokstatus" role="status"></p>',
        '<ul class="kart-stedsforslag" aria-label="Stedsforslag" hidden></ul>',
        '</form>',
        '<div class="kart-filterhandlinger">',
        '<button type="button" class="kart-filterknapp" aria-expanded="false" aria-controls="kartSoneliste">Markerte soner</button>',
        '<button type="button" class="kart-filterknapp" aria-expanded="false" aria-controls="kartDetaljer">Filter</button>',
        '</div>',
        '<section id="kartSoneliste" class="kart-sidepanel" aria-labelledby="kartSonelisteTittel" hidden>',
        '<h2 id="kartSonelisteTittel">Markerte soner</h2>',
        '<p class="kart-soneliste-tom">Ingen soner er markert.</p>',
        '<ul class="kart-sonerader"></ul>',
        '</section>',
        '<div id="kartDetaljer" class="kart-sidepanel" hidden>',
        '<fieldset class="kart-typer kart-bakgrunn"><legend>Karttype</legend>',
        '<label><input type="radio" name="karttype" value="vanlig" checked> Standard</label>',
        '<label><input type="radio" name="karttype" value="terreng"> Terreng (høyde og dybde)</label>',
        '<label><input type="radio" name="karttype" value="satellitt"> Satellitt</label>',
        '</fieldset>',
        '<div class="kart-radiusrad">',
        '<label for="kartRadius">Radius</label>',
        '<select id="kartRadius">',
        '<option value="0" selected>Ingen radius</option>',
        '<option value="1000">1 km</option>',
        '<option value="5000">5 km</option>',
        '<option value="10000">10 km</option>',
        '</select>',
        '<button class="kart-nullstill" type="button">Nullstill søk</button>',
        '</div>',
        '<p class="kart-sentrum">Alle steder vises. Velg et sted for å bruke radius.</p>',
        '<fieldset class="kart-typer"><legend>Vis på kartet</legend></fieldset>',
        '<div class="kart-kilde">Stedssøk: <a href="https://photon.komoot.io/" target="_blank" rel="noopener noreferrer">Photon</a> · OpenStreetMap</div>',
        '</div>'
    ].join('');

    L.DomEvent.disableClickPropagation(panel);
    L.DomEvent.disableScrollPropagation(panel);

    kartElement.append(panel);

    const form = panel.querySelector('form');
    const sokToolbar = document.getElementById('kartSokToolbar');
    sokToolbar?.append(form);
    const sok = form.querySelector('input[type="search"]');
    const sokstatus = form.querySelector('.kart-sokstatus');
    const resultater = form.querySelector('.kart-stedsforslag');
    const radius = panel.querySelector('select');
    const sentrumtekst = panel.querySelector('.kart-sentrum');
    const detaljer = panel.querySelector('#kartDetaljer');
    const filterknapp = panel.querySelector('[aria-controls="kartDetaljer"]');
    const soneliste = panel.querySelector('#kartSoneliste');
    const sonelisteknapp = panel.querySelector('[aria-controls="kartSoneliste"]');

    function visSoneliste(apent) {
        soneliste.hidden = !apent;
        sonelisteknapp.setAttribute('aria-expanded', String(apent));
    }

    sonelisteknapp.addEventListener('click', () => {
        const apent = soneliste.hidden;
        if (apent) {
            visKartfiltre(false);
            if (mobilvisning.matches) visHandling(null);
            oppdaterSoneliste();
        }
        visSoneliste(apent);
    });

    function visKartfiltre(apent) {
        detaljer.hidden = !apent;
        filterknapp.setAttribute('aria-expanded', String(apent));
    }

    filterknapp.addEventListener('click', () => {
        if (detaljer.hidden) {
            visSoneliste(false);
            if (mobilvisning.matches) visHandling(null);
        }
        visKartfiltre(detaljer.hidden);
    });

    for (const valg of panel.querySelectorAll('input[name="karttype"]')) {
        valg.addEventListener('change', () => {
            terrengbilde?.remove();
            lasterTerrengbilde?.remove();
            terrengbilde = null;
            lasterTerrengbilde = null;
            kart.removeLayer(aktivtKart);
            aktivtKart = bakgrunnskart[valg.value].addTo(kart);
            oppdaterSonefarger();
            oppdaterTerrengbilde();
        });
    }

    // Fullskjerm inkluderer kart, søk, filtre og zoomknapper.
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

        const sokPlass = aktiv ? panel : sokToolbar;
        if (form.parentElement !== sokPlass) sokPlass.append(form);

        fullKnapp.textContent = aktiv ? '⤡' : '⤢';
        fullKnapp.title = aktiv
            ? 'Avslutt fullskjerm'
            : 'Vis kart i fullskjerm';

        fullKnapp.setAttribute('aria-label', fullKnapp.title);
        fullKnapp.setAttribute('aria-pressed', String(aktiv));

        requestAnimationFrame(() => {
            if (!aktiv) oppdaterKarthoyde();
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
            kartElement.classList.contains('kart-fullskjerm') &&
            !event.defaultPrevented
        ) {
            sideFullskjerm(false);
        }
    });

    window.addEventListener('resize', oppdaterFullskjerm);
    const hoydeObserver = new ResizeObserver(oppdaterFullskjerm);
    for (const element of document.querySelectorAll('.offentlig-site-header, .kart-oversiktsrad')) {
        hoydeObserver.observe(element);
    }
    oppdaterFullskjerm();

    let sentrum = null;
    let sirkel = null;
    let sentrumsmarkor = null;
    let foresporsel = null;
    let versjon = 0;

    function fjernSokesentrum() {
        sentrum = null;
        sirkel?.remove();
        sentrumsmarkor?.remove();
        sirkel = null;
        sentrumsmarkor = null;
    }

    const samlelag = L.featureGroup().addTo(kart);
    const soneetiketter = L.layerGroup().addTo(kart);
    const samlemarkorer = new Map();

    function grupperEtterPlass(markorer) {
        const avstand = 26;
        const ruter = new Map();
        const grupper = [];

        for (const oppforing of markorer) {
            const punkt = kart.project(oppforing.markor.getLatLng());
            const x = Math.floor(punkt.x / avstand);
            const y = Math.floor(punkt.y / avstand);
            let funnet;

            for (let kolonne = x - 1; kolonne <= x + 1 && !funnet; kolonne++) {
                for (let rad = y - 1; rad <= y + 1 && !funnet; rad++) {
                    const naboer = ruter.get(`${kolonne},${rad}`) ?? [];
                    funnet = naboer.find(gruppe => gruppe.punkt.distanceTo(punkt) <= avstand);
                }
            }

            if (funnet) {
                funnet.oppforinger.push(oppforing);
            } else {
                const gruppe = { punkt, oppforinger: [oppforing] };
                const nokkel = `${x},${y}`;
                if (!ruter.has(nokkel)) ruter.set(nokkel, []);
                ruter.get(nokkel).push(gruppe);
                grupper.push(gruppe.oppforinger);
            }
        }

        return grupper;
    }

    function grupperMarkorer(grupper) {
        for (const gruppe of grupper) {
            if (gruppe.length === 1) {
                const [{ markor, type }] = gruppe;
                type.lag.addLayer(markor);
                continue;
            }

            const innhold = document.createElement('div');
            innhold.className = 'kart-samleinnhold';
            const tittel = document.createElement('strong');
            tittel.textContent = `${gruppe.length} oppføringer på dette stedet`;
            innhold.append(tittel);

            const merke = L.marker(gruppe[0].markor.getLatLng(), {
                icon: L.divIcon({
                    className: 'kart-samlemarkor',
                    html: String(gruppe.length),
                    iconSize: [30, 30],
                    iconAnchor: [15, 15],
                    popupAnchor: [0, -15]
                }),
                bubblingMouseEvents: true,
                title: tittel.textContent
            }).bindPopup(innhold, { maxHeight: 230 }).addTo(samlelag);

            for (const { markor, type } of gruppe) {
                const detalj = document.createElement('details');
                const etikett = document.createElement('summary');
                const kort = markor.getPopup().getContent();
                etikett.textContent = type.navn + ' · ' + kort.firstElementChild.textContent;
                detalj.append(etikett, kort);
                innhold.append(detalj);
                samlemarkorer.set(markor, { merke, detalj });
            }
        }
    }

    function apneMarkor(markor) {
        const samlet = samlemarkorer.get(markor);
        if (samlet) {
            samlet.detalj.open = true;
            samlet.merke.openPopup();
        } else {
            markor.openPopup();
        }
    }

    function prioriterteSoner() {
        return soneType.lag.getLayers().sort((a, b) =>
            risikoPrioritet[a.sonedata.risiko] - risikoPrioritet[b.sonedata.risiko]);
    }

    function soneSnitt(punkter, y) {
        const kryss = [];
        for (let i = 0, j = punkter.length - 1; i < punkter.length; j = i++) {
            const a = punkter[i];
            const b = punkter[j];
            if ((a.y > y) !== (b.y > y)) {
                kryss.push(a.x + (y - a.y) * (b.x - a.x) / (b.y - a.y));
            }
        }

        kryss.sort((a, b) => a - b);
        const intervaller = [];
        for (let i = 0; i < kryss.length; i += 2) intervaller.push([kryss[i], kryss[i + 1]]);
        return intervaller;
    }

    function synligeSonesnitt(sone, dekkende, y) {
        let intervaller = soneSnitt(sone, y);
        for (const polygon of dekkende) {
            for (const [fra, til] of soneSnitt(polygon, y)) {
                intervaller = intervaller.flatMap(([venstre, hoyre]) => {
                    if (til <= venstre || fra >= hoyre) return [[venstre, hoyre]];

                    const synlige = [];
                    if (fra > venstre) synlige.push([venstre, fra]);
                    if (til < hoyre) synlige.push([til, hoyre]);
                    return synlige;
                });
            }
        }
        return intervaller;
    }

    function etikettFaarPlass(sone, dekkende, x, y, bredde, hoyde) {
        const venstre = x - bredde / 2;
        const hoyre = x + bredde / 2;
        const topp = y - hoyde / 2;
        const bunn = y + hoyde / 2;
        const hjorner = [
            { x: venstre, y: topp }, { x: hoyre, y: topp },
            { x: hoyre, y: bunn }, { x: venstre, y: bunn }
        ];
        if (!synligeSonesnitt(sone, dekkende, y).some(([fra, til]) =>
            fra < venstre && til > hoyre)) return false;

        // En kant eller et helt polygon inne i boksen betyr at boksen krysser en sonegrense.
        return [sone, ...dekkende].every(polygon => polygon.every((a, i) => {
            if (a.x >= venstre && a.x <= hoyre && a.y >= topp && a.y <= bunn) return false;
            const b = polygon[(i + 1) % polygon.length];
            return !hjorner.some((c, j) => kanterBerorer(a, b, c, hjorner[(j + 1) % hjorner.length]));
        }));
    }

    function oppdaterSoneetiketter() {
        soneetiketter.clearLayers();
        if (kart.getZoom() < 14) return;

        const soner = prioriterteSoner();
        const flater = soner.map(sone => sone.getLatLngs()[0].map(punkt => kart.project(punkt)));
        for (const [indeks, sone] of soner.entries()) {
            const etikett = sone.kartetikett;
            const flate = flater[indeks];
            const dekkende = flater.slice(indeks + 1);
            const hoyder = flate.map(punkt => punkt.y);
            const topp = Math.min(...hoyder);
            const bunn = Math.max(...hoyder);
            const midt = (topp + bunn) / 2;
            const kandidater = [];

            // Finn plass i de synlige delene, også når en sone er delt i flere biter.
            for (let i = 1; i < 20; i++) {
                const y = topp + (bunn - topp) * i / 20;
                for (const [fra, til] of synligeSonesnitt(flate, dekkende, y)) {
                    const bredde = til - fra;
                    if (bredde >= 64) kandidater.push({ x: (fra + til) / 2, y, bredde });
                }
            }
            kandidater.sort((a, b) => Math.abs(a.y - midt) - Math.abs(b.y - midt) || b.bredde - a.bredde);

            let boks;
            let plassert = false;
            for (const { x, y, bredde } of kandidater) {
                etikett.style.maxWidth = Math.min(bredde - 8, 180) + 'px';
                const posisjon = kart.unproject(L.point(x, y));
                if (!boks) {
                    boks = L.tooltip({ direction: 'center', className: 'kart-soneetikett', opacity: 0 })
                        .setLatLng(posisjon).setContent(etikett).addTo(soneetiketter);
                }

                if (etikett.offsetWidth && etikettFaarPlass(flate, dekkende, x, y,
                    etikett.offsetWidth + 6, etikett.offsetHeight + 6)) {
                    boks.setLatLng(posisjon).setOpacity(1);
                    plassert = true;
                    break;
                }
            }

            if (boks && !plassert) soneetiketter.removeLayer(boks);
        }
    }

    function filtrer(tilpass = false) {
        const avstand = sentrum ? Number(radius.value) : 0;
        if (!hendelsesvisning) melding.textContent = '';
        const punktmarkorer = [];
        samlelag.clearLayers();
        samlemarkorer.clear();

        for (const type of typer) {
            type.lag.clearLayers();

            if (type.boks.checked) {
                for (const markor of type.markorer) {
                    if (hendelsesvisning) {
                        if (!synligeHendelser.has(markor.hendelseId)) continue;
                        if (!['kriser', 'behov', 'soner', 'veihindringer'].includes(type.key)) continue;
                    }

                    const punkt = markor.getLatLng?.();
                    const posisjon = punkt ?? markor.getBounds().getCenter();
                    if (avstand && sentrum.distanceTo(posisjon) > avstand) continue;

                    if (punkt) {
                        punktmarkorer.push({ markor, type });
                    } else {
                        type.lag.addLayer(markor);
                    }
                }
            }
        }

        grupperMarkorer(grupperEtterPlass(punktmarkorer));
        if (hendelsesvisning) {
            const utenMarkor = valgteHendelser.filter(krise =>
                synligeHendelser.has(krise.id) && hendelserUtenPunkt.has(krise.id) &&
                !punktmarkorer.some(({ markor }) => markor.hendelseId === krise.id) &&
                !typer.find(t => t.key === 'soner').lag.getLayers().some(m => m.hendelseId === krise.id));
            const informasjon = [synligeHendelser.size + ' av ' + valgteHendelser.length + ' valgte hendelser er slått på.'];
            if (utenMarkor.length) informasjon.push('Ingen kartplassering for ' + utenMarkor.map(krise => krise.navn).join(', ') + '.');
            if (punktmarkorer.some(({ type }) => type.key === 'behov')) {
                informasjon.push('Tilknyttede behov vises for hendelser uten egen kartplassering.');
            }
            melding.textContent = informasjon.join(' ');

            if (tilpass) {
                const bounds = L.latLngBounds(punktmarkorer.map(({ markor }) => markor.getLatLng()));
                for (const zone of typer.find(t => t.key === 'soner').lag.getLayers()) bounds.extend(zone.getBounds());
                if (bounds.isValid()) kart.fitBounds(bounds, { maxZoom: 16, padding: [32, 32], animate: false });
            }
        }
        oppdaterSoneetiketter();
        planleggSonemasker();

        if (avstand) {
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
        } else {
            sirkel?.remove();
            sirkel = null;
        }

        if (sentrum) {
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

            if (tilpass && sirkel) {
                kart.fitBounds(sirkel.getBounds(), {
                    padding: [30, 30]
                });
            }
        }
    }

    kart.on('zoomend', () => filtrer());

    // Lager avkrysningsboksene.
    const felt = panel.querySelector('.kart-typer:not(.kart-bakgrunn)');

    if (hendelsesvisning) {
        const hendelsesfilter = document.createElement('fieldset');
        hendelsesfilter.className = 'kart-typer';
        const tittel = document.createElement('legend');
        tittel.textContent = 'Vis valgte hendelser';
        hendelsesfilter.append(tittel);

        for (const hendelse of valgteHendelser) {
            const label = document.createElement('label');
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.checked = true;
            checkbox.addEventListener('change', () => {
                if (checkbox.checked) synligeHendelser.add(hendelse.id);
                else synligeHendelser.delete(hendelse.id);
                filtrer(true);
                oppdaterSoneliste();
            });
            label.append(checkbox, document.createTextNode(' ' + hendelse.navn));
            hendelsesfilter.append(label);
        }

        const nullstill = document.createElement('a');
        nullstill.href = window.location.pathname;
        nullstill.textContent = 'Vis hele kartet';
        hendelsesfilter.append(nullstill);
        felt.before(hendelsesfilter);
        felt.hidden = true;
    }

    for (const type of typer) {
        const label = document.createElement('label');
        const boks = document.createElement('input');

        boks.type = 'checkbox';
        boks.checked = !hendelsesvisning || type.key !== 'ressurser';
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
            radius.value = '0';
            fjernSokesentrum();

            sentrumtekst.textContent =
                'Alle steder vises. Velg et sted for å bruke radius.';

            filtrer();
            kart.setView(standardvisning.sentrum, standardvisning.zoom);
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
                    radius.value = '0';

                    sentrumtekst.textContent =
                        'Valgt sted: ' + etikett + '. Radius er valgfritt.';

                    avbryt();
                    filtrer();
                    kart.setView(sentrum, feature.properties.housenumber ? 17 : 15);
                    if (markeringSkjema?.dataset.kartmarkering === 'behov' && !markeringSkjema.inert) {
                        velgBehovplassering(sentrum, etikett);
                    }
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

    const handlinger = kartElement.querySelector('.kart-handlinger');
    const handlingsknapper = [...handlinger.querySelectorAll('.kart-handlingsrad button')];
    const arbeidValg = document.getElementById('kartArbeidshendelse');
    const arbeidsstatus = document.getElementById('kartArbeidsstatus');
    const hendelserForm = document.getElementById('kartHendelserForm');
    const hendelserFelt = document.getElementById('kartHendelserValg');
    const csrf = document.querySelector('#kartAntiforgery input').value;
    let arbeidId = valgteHendelser.length === 1 ? valgteHendelser[0].id : null;
    let lagrer = false;
    let skjemaEndret = false;

    arbeidValg.add(new Option(valgteHendelser.length ? 'Velg hendelse' : 'Åpne en hendelse først', ''));
    for (const hendelse of valgteHendelser) arbeidValg.add(new Option(hendelse.navn, hendelse.id));
    arbeidValg.value = arbeidId ?? '';
    arbeidValg.disabled = !valgteHendelser.length;
    for (const hendelse of data.kriser ?? []) {
        const label = document.createElement('label');
        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.name = 'hendelser';
        checkbox.value = hendelse.id;
        checkbox.checked = forespurteHendelser.has(String(hendelse.id));
        label.append(checkbox, document.createTextNode(hendelse.navn + ' · ' + hendelse.status));
        hendelserFelt.append(label);
    }
    function oppdaterArbeid() {
        for (const knapp of handlingsknapper) knapp.disabled = !arbeidId || lagrer;
        arbeidsstatus.textContent = lagrer ? 'Lagrer …' : !valgteHendelser.length
            ? 'Åpne minst én hendelse for å gjøre endringer.'
            : !arbeidId ? 'Velg hvilken hendelse endringene gjelder.' : '';
    }
    oppdaterArbeid();
    for (const link of document.querySelectorAll('a[href*="/crisis/overview"]')) {
        const url = new URL(link.href);
        for (const id of forespurteHendelser) url.searchParams.append('hendelser', id);
        link.href = url;
    }
    handlinger.addEventListener('input', event => {
        if (event.target.closest('form')) skjemaEndret = true;
    });
    window.addEventListener('beforeunload', event => {
        if (lagrer || skjemaEndret) { event.preventDefault(); event.returnValue = ''; }
    });
    async function lagreForBytte() {
        if (lagrer) return false;
        if (!skjemaEndret && !sonehjorner.length && !plassering) return true;
        if (markeringSkjema && ['soner', 'veihindringer'].includes(markeringSkjema.dataset.kartmarkering))
            return await lagreMarkering(markeringSkjema);
        arbeidsstatus.textContent = 'Fullfør eller avbryt skjemaet før du bytter hendelse.';
        return false;
    }
    arbeidValg.addEventListener('change', async () => {
        const neste = Number(arbeidValg.value) || null;
        arbeidValg.value = arbeidId ?? '';
        if (!await lagreForBytte()) return;
        visHandling(null);
        arbeidId = neste;
        arbeidValg.value = arbeidId ?? '';
        oppdaterArbeid();
        oppdaterSoneliste();
        filtrerMatch();
        for (const type of typer) for (const marker of type.markorer) {
            if (marker.sletteknapp) marker.sletteknapp.disabled = marker.hendelseId !== arbeidId;
        }
    });
    hendelserForm.addEventListener('submit', async event => {
        event.preventDefault();
        if (!await lagreForBytte()) return;
        const url = new URL(hendelserForm.action);
        for (const id of new FormData(hendelserForm).getAll('hendelser')) url.searchParams.append('hendelser', id);
        url.hash = 'offentligKart';
        window.location.assign(url);
    });


    L.DomEvent.disableClickPropagation(handlinger);
    L.DomEvent.disableScrollPropagation(handlinger);

    const mobilvisning = window.matchMedia('(max-width: 940px)');
    let aktivKnapp = null;
    let markeringSkjema = null;
    let plassering = null;
    const sonehjorner = [];
    const forhandsvisning = L.layerGroup().addTo(kart);
    const soneType = typer.find(type => type.key === 'soner');

    const sonerenderer = L.svg();
    const sonerader = soneliste.querySelector('.kart-sonerader');
    const tomSoneliste = soneliste.querySelector('.kart-soneliste-tom');
    const risikoNavn = { lav: 'Lav risiko', moderat: 'Moderat risiko', hoy: 'Høy risiko' };
    const risikoKant = { lav: null, moderat: '2 4', hoy: '8 5' };
    const risikoPrioritet = { lav: 0, moderat: 1, hoy: 2 };
    const svgNavnerom = 'http://www.w3.org/2000/svg';
    let sonemasker;
    let sonemaskeRamme;

    const satellittSonefarger = { lav: '#48f4bf', moderat: '#ffdc3d', hoy: '#ff4168' };

    function sonestil(risiko) {
        const erSatellitt = aktivtKart === satellitt;
        return {
            color: erSatellitt ? satellittSonefarger[risiko] : farge('--sone-' + risiko),
            dashArray: risikoKant[risiko],
            weight: erSatellitt ? 5 : 4,
            fillOpacity: erSatellitt ? 0.62 : 0.34
        };
    }

    function oppdaterSonefarger() {
        for (const sone of soneType.markorer) {
            const stil = sonestil(sone.sonedata.risiko);
            sone.setStyle(stil);
            sone.kartetikett.style.setProperty('--sonefarge', stil.color);
        }

        if (markeringSkjema?.dataset.kartmarkering === 'soner') visSoneutkast();
    }

    function oppdaterSoneliste() {
        sonerader.replaceChildren();
        tomSoneliste.hidden = soneType.markorer.length > 0;

        for (const [indeks, sone] of soneType.markorer.entries()) {
            if (hendelsesvisning && !synligeHendelser.has(sone.hendelseId)) continue;
            const { navn, risiko } = sone.sonedata;
            const rad = document.createElement('li');
            rad.className = 'kart-sonerad kart-sonevalg-' + risiko;

            const prove = document.createElement('span');
            prove.className = 'kart-soneproeve';
            prove.setAttribute('aria-hidden', 'true');
            const tekst = document.createElement('span');
            const tittel = document.createElement('strong');
            tittel.textContent = navn;
            const niva = document.createElement('small');
            niva.textContent = risikoNavn[risiko];
            tekst.append(tittel, niva);

            const slett = document.createElement('button');
            slett.type = 'button';
            slett.className = 'btn btn-sm btn-outline-danger';
            slett.textContent = 'Slett';
            slett.setAttribute('aria-label', 'Slett sone ' + navn);
            slett.disabled = sone.hendelseId !== arbeidId;
            slett.addEventListener('click', () => slettMarkering(sone, soneType, slett));

            rad.append(prove, tekst, slett);
            sonerader.append(rad);
        }
    }

    // Høyere risiko dekker lavere risiko. Ved lik risiko ligger den sist tegnede øverst.
    function planleggSonemasker() {
        if (sonemaskeRamme !== undefined) return;
        sonemaskeRamme = requestAnimationFrame(() => {
            sonemaskeRamme = undefined;
            const soner = prioriterteSoner();
            for (const sone of soner) sone.bringToFront();
            const flater = soner.map(sone => sone.getElement()).filter(Boolean);
            sonemasker?.remove();
            for (const flate of flater) flate.removeAttribute('mask');
            if (flater.length < 2) return;

            const svg = flater[0].ownerSVGElement;
            const { x, y, width, height } = svg.viewBox.baseVal;
            sonemasker = document.createElementNS(svgNavnerom, 'defs');
            for (let indeks = flater.length - 2; indeks >= 0; indeks--) {
                const maske = document.createElementNS(svgNavnerom, 'mask');
                const id = 'offentlig-sonemaske-' + indeks;
                maske.id = id;
                maske.setAttribute('maskUnits', 'userSpaceOnUse');

                const bakgrunn = document.createElementNS(svgNavnerom, 'rect');
                for (const [attributt, verdi] of Object.entries({ x, y, width, height })) {
                    maske.setAttribute(attributt, verdi);
                    bakgrunn.setAttribute(attributt, verdi);
                }
                bakgrunn.setAttribute('fill', 'white');
                maske.append(bakgrunn);

                for (const nyere of flater.slice(indeks + 1)) {
                    const utsnitt = document.createElementNS(svgNavnerom, 'path');
                    utsnitt.setAttribute('d', nyere.getAttribute('d'));
                    utsnitt.setAttribute('fill', 'black');
                    maske.append(utsnitt);
                }

                sonemasker.append(maske);
                flater[indeks].setAttribute('mask', 'url(#' + id + ')');
            }
            svg.prepend(sonemasker);
        });
    }

    sonerenderer.on('update', planleggSonemasker);

    const hindringPanel = document.getElementById('kartVeihindring');
    const hindringHint = document.getElementById('kartVelgVeihindring');
    const behovPanel = document.getElementById('kartNyttBehov');
    const behovInnhold = behovPanel.querySelector('.kart-behovskjema');
    const behovStatus = behovPanel.querySelector('.kart-behovstatus');
    let henterBehov = false;

    function visPlassering(punkt) {
        forhandsvisning.clearLayers();
        forhandsvisning.addLayer(L.circleMarker(punkt, {
            radius: 6,
            color: '#163c4c',
            fillColor: '#fff',
            fillOpacity: 1,
            interactive: false
        }));
        markeringSkjema.querySelector('.kart-posisjon').textContent =
            `Valgt posisjon: ${punkt.lat.toFixed(5)}, ${punkt.lng.toFixed(5)}`;
        markeringSkjema.querySelector('[type="submit"]').disabled = false;
    }

    function velgBehovplassering(punkt, etikett) {
        const { Latitude, Longitude } = markeringSkjema.elements;
        Latitude.value = punkt.lat.toFixed(6);
        Longitude.value = punkt.lng.toFixed(6);
        visPlassering(punkt);
        if (etikett) markeringSkjema.querySelector('.kart-posisjon').textContent = etikett;
    }

    function aktiverBehovplassering(skjema) {
        markeringSkjema = skjema;
        kartElement.classList.add('kart-plassering-aktiv');

        const { Latitude, Longitude } = skjema.elements;
        if (Latitude.value && Longitude.value) {
            const lat = Number(Latitude.value.replace(',', '.'));
            const lon = Number(Longitude.value.replace(',', '.'));
            if (gyldig(lat, lon)) visPlassering(L.latLng(lat, lon));
        }
    }

    function oppdaterPrioritetsvalg(blokk) {
        const felt = blokk.querySelector('select[name$=".Priority"]');
        if (!felt.value) felt.value = 'Middels';
        for (const knapp of blokk.querySelectorAll('[data-prioritet]')) {
            knapp.setAttribute('aria-pressed', String(knapp.dataset.prioritet === felt.value));
        }
    }

    function visPrioritetsvalg(skjema) {
        for (const blokk of skjema.querySelectorAll('#behov-liste fieldset')) {
            const felt = blokk.querySelector('select[name$=".Priority"]');
            const valg = document.createElement('div');
            valg.className = 'kart-piller';
            valg.setAttribute('role', 'group');
            valg.setAttribute('aria-label', 'Prioritet');
            for (const alternativ of Array.from(felt.options).filter(alternativ => alternativ.value)) {
                const knapp = document.createElement('button');
                knapp.type = 'button';
                knapp.dataset.prioritet = alternativ.value;
                knapp.textContent = alternativ.value === 'Middels' ? 'Normal' : alternativ.textContent;
                valg.append(knapp);
            }

            felt.hidden = true;
            felt.after(valg);
            oppdaterPrioritetsvalg(blokk);
        }

        skjema.addEventListener('click', event => {
            const knapp = event.target.closest('[data-prioritet]');
            if (!knapp) return;
            const blokk = knapp.closest('fieldset');
            blokk.querySelector('select[name$=".Priority"]').value = knapp.dataset.prioritet;
            oppdaterPrioritetsvalg(blokk);
        });
    }

    function visBehovskjema(skjema) {
        skjema.dataset.kartmarkering = 'behov';
        if (skjema.elements.CrisisId) {
            skjema.elements.CrisisId.value = arbeidId ?? '';
            skjema.elements.CrisisId.disabled = true;
        }
        const posisjonsfelt = skjema.querySelector('[data-behovplassering]');
        posisjonsfelt.querySelector('#behovKart').remove();
        posisjonsfelt.querySelector('h2').textContent = 'Sted eller område';
        posisjonsfelt.querySelector('p').remove();
        posisjonsfelt.className = 'kart-behovplassering';
        const sted = posisjonsfelt.querySelector('#behovKartmelding');
        sted.classList.add('kart-posisjon');
        sted.textContent = 'Søk etter adresse eller marker i kartet';

        for (const blokk of skjema.querySelectorAll('#behov-liste fieldset')) {
            const beskrivelse = blokk.querySelector('textarea').closest('.mb-3');
            const prioritet = blokk.querySelector('select[name$=".Priority"]').closest('.row');
            prioritet.before(beskrivelse);
            blokk.querySelector('select[name$=".Status"]').closest('.mb-3').hidden = true;
        }

        skjema.querySelector('[type="submit"]').parentElement.className = 'kart-panelknapper';
        skjema.querySelector('[type="submit"]').textContent = 'Registrer behov';

        const avbryt = skjema.querySelector('[data-avbryt-behov]');
        const avbrytKnapp = document.createElement('button');
        avbrytKnapp.type = 'button';
        avbrytKnapp.className = avbryt.className;
        avbrytKnapp.textContent = avbryt.textContent.trim();
        avbrytKnapp.addEventListener('click', lukkHandling);
        avbryt.replaceWith(avbrytKnapp);

        skjema.querySelector('[type="submit"]').disabled = true;
        visPrioritetsvalg(skjema);
        window.kobleBehovsliste(skjema, oppdaterPrioritetsvalg);
        behovInnhold.replaceChildren(skjema);
        if (!behovPanel.hidden) aktiverBehovplassering(skjema);
    }

    async function hentBehovskjema() {
        if (henterBehov) return;
        henterBehov = true;
        behovStatus.textContent = 'Henter skjema …';

        try {
            const svar = await fetch(behovPanel.dataset.skjemaUrl);
            if (!svar.ok) throw new Error('Skjema utilgjengelig');

            const side = new DOMParser().parseFromString(await svar.text(), 'text/html');
            const skjema = side.getElementById('flereBehovSkjema');
            if (!skjema) throw new Error('Skjema mangler');

            visBehovskjema(skjema);
            behovStatus.textContent = '';
        } catch {
            behovStatus.textContent = 'Skjemaet kunne ikke hentes. Lukk og åpne panelet for å prøve igjen.';
        } finally {
            henterBehov = false;
        }
    }

    behovPanel.addEventListener('submit', async event => {
        event.preventDefault();
        const skjema = event.target;
        if (skjema.inert || !skjema.elements.Latitude.value || !skjema.elements.Longitude.value) return;

        if (!arbeidId || lagrer) return;
        const data = new FormData(skjema);
        data.set('CrisisId', String(arbeidId));
        lagrer = true;
        oppdaterArbeid();
        skjema.inert = true;
        behovStatus.textContent = 'Registrerer behov …';

        try {
            const svar = await fetch(skjema.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: data
            });
            if (!svar.ok) throw new Error('Registrering utilgjengelig');

            if (svar.headers.get('Content-Type')?.includes('application/json')) {
                const { redirectUrl } = await svar.json();
                if (!redirectUrl) throw new Error('Oversikten kunne ikke åpnes');

                lagrer = false;
                skjemaEndret = false;
                const url = new URL(redirectUrl, window.location.origin);
                for (const id of forespurteHendelser) url.searchParams.append('hendelser', id);
                url.hash = 'offentligKart';
                window.location.assign(url);
                return;
            }

            const side = new DOMParser().parseFromString(await svar.text(), 'text/html');
            const oppdatertSkjema = side.getElementById('flereBehovSkjema');
            if (!oppdatertSkjema) throw new Error('Uventet svar');

            visBehovskjema(oppdatertSkjema);
            behovStatus.textContent = 'Se meldingene i skjemaet og prøv igjen.';
            behovPanel.scrollTop = 0;
        } catch {
            skjema.inert = false;
            behovStatus.textContent = 'Kunne ikke bekrefte registreringen. Sjekk oversikten før du prøver igjen.';
        } finally {
            lagrer = false;
            oppdaterArbeid();
        }
    });

    function kryssprodukt(a, b, c) {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    function kanterBerorer(a, b, c, d) {
        const retning = (p, q, r) => {
            const kryss = kryssprodukt(p, q, r);
            return Math.abs(kryss) < 0.01 ? 0 : Math.sign(kryss);
        };
        const paKant = (p, q, r) =>
            r.x >= Math.min(p.x, q.x) && r.x <= Math.max(p.x, q.x) &&
            r.y >= Math.min(p.y, q.y) && r.y <= Math.max(p.y, q.y);

        const abc = retning(a, b, c);
        const abd = retning(a, b, d);
        const cda = retning(c, d, a);
        const cdb = retning(c, d, b);

        return (abc * abd < 0 && cda * cdb < 0) ||
            (abc === 0 && paKant(a, b, c)) ||
            (abd === 0 && paKant(a, b, d)) ||
            (cda === 0 && paKant(c, d, a)) ||
            (cdb === 0 && paKant(c, d, b));
    }

    function gyldigSone(hjorner) {
        const antall = hjorner.length;
        if (antall < 3) return false;

        const punkter = hjorner.map(punkt => kart.options.crs.project(punkt));
        let dobbeltAreal = 0;

        for (let i = 0; i < antall; i++) {
            const a = punkter[i];
            const b = punkter[(i + 1) % antall];
            const c = punkter[(i + 2) % antall];
            dobbeltAreal += kryssprodukt(punkter[0], a, b);

            // Nabokanter kan dele et hjørne, men ikke gå tilbake langs samme kant.
            const bakover = (b.x - a.x) * (c.x - b.x) + (b.y - a.y) * (c.y - b.y) < 0;
            if (bakover && Math.abs(kryssprodukt(a, b, c)) < 0.01) return false;

            for (let j = i + 2; j < antall; j++) {
                if (i === 0 && j === antall - 1) continue;
                if (kanterBerorer(a, b, punkter[j], punkter[(j + 1) % antall])) return false;
            }
        }

        return Math.abs(dobbeltAreal) >= 0.01;
    }

    function lagSoneetikett(navn, fargekode) {
        const etikett = document.createElement('div');
        etikett.style.setProperty('--sonefarge', fargekode);
        etikett.textContent = navn;
        return etikett;
    }

    function visSoneutkast() {
        const antall = sonehjorner.length;
        const gyldig = gyldigSone(sonehjorner);
        const risiko = markeringSkjema.elements.risiko.value;
        const stil = { ...sonestil(risiko), interactive: false };
        forhandsvisning.clearLayers();

        if (gyldig) {
            forhandsvisning.addLayer(L.polygon(sonehjorner, stil));
        } else if (antall > 1) {
            forhandsvisning.addLayer(L.polyline(sonehjorner, stil));
        }

        for (const hjorne of sonehjorner) {
            forhandsvisning.addLayer(L.circleMarker(hjorne, {
                ...stil, radius: 5, weight: 2, fillColor: '#fff', fillOpacity: 1
            }));
        }

        let status = 'Klikk minst tre hjørner langs kanten av sonen.';
        if (antall >= 3 && !gyldig) {
            status = 'Hjørnene må danne et område uten kryssende kanter. Angre siste hjørne og prøv igjen.';
        } else if (gyldig) {
            status = `${antall} hjørner valgt. Fortsett å tegne eller velg Godta sone.`;
        } else if (antall) {
            status = `${antall} av minst 3 hjørner valgt. Fortsett langs kanten.`;
        }

        markeringSkjema.querySelector('.kart-posisjon').textContent = status;
        markeringSkjema.querySelector('[type="submit"]').disabled = !gyldig;
        markeringSkjema.querySelector('[data-angre-hjorne]').disabled = antall === 0;
    }

    function nullstillPlassering() {
        plassering = null;
        sonehjorner.length = 0;
        forhandsvisning.clearLayers();

        if (!markeringSkjema) return;
        if (markeringSkjema.dataset.kartmarkering === 'soner') {
            visSoneutkast();
        } else {
            markeringSkjema.querySelector('.kart-posisjon').textContent =
                'Klikk i kartet for å velge plassering.';
            markeringSkjema.querySelector('[type="submit"]').disabled = true;
        }
    }

    function visHandling(knapp) {
        if (knapp && (!arbeidId || lagrer)) return;
        nullstillPlassering();
        if (knapp && mobilvisning.matches) {
            visKartfiltre(false);
            visSoneliste(false);
        }
        aktivKnapp = knapp;
        markeringSkjema = null;
        hindringHint.textContent = '';

        for (const handling of handlingsknapper) {
            const seksjon = document.getElementById(handling.getAttribute('aria-controls'));
            const apen = handling === knapp;
            const venterPaPlassering = apen && seksjon === hindringPanel;
            seksjon.hidden = !apen || venterPaPlassering;
            handling.setAttribute('aria-expanded', String(!seksjon.hidden));
            handling.toggleAttribute('data-venter-plassering', venterPaPlassering);

            if (apen) {
                markeringSkjema = seksjon.querySelector('[data-kartmarkering]');
                if (venterPaPlassering) {
                    hindringHint.textContent = 'Klikk i kartet der veihindringen ligger.';
                }
                if (seksjon === behovPanel) {
                    if (markeringSkjema) aktiverBehovplassering(markeringSkjema);
                    else hentBehovskjema();
                }
            }
        }

        kartElement.classList.toggle('kart-plassering-aktiv', Boolean(markeringSkjema));
    }

    for (const knapp of handlingsknapper) {
        knapp.addEventListener('click', async () => {
            if (!await lagreForBytte()) return;
            visHandling(aktivKnapp === knapp ? null : knapp);
        });
    }

    function lukkHandling() {
        if (lagrer) return;
        skjemaEndret = false;
        markeringSkjema?.reset();
        aktivKnapp?.focus();
        visHandling(null);
    }

    for (const knapp of handlinger.querySelectorAll('[data-kartavbryt]')) {
        knapp.addEventListener('click', lukkHandling);
    }

    const matchPanel = document.getElementById('kartMatch');
    const matchSok = document.getElementById('kartMatchSok');
    const matchStatus = matchPanel.querySelector('.kart-matchstatus');
    const matchTreff = [...matchPanel.querySelectorAll('.kart-matchtreff')];

    function filtrerMatch() {
        if (!matchStatus) return;
        const tekst = matchSok.value.trim().toLocaleLowerCase('nb-NO');
        const type = matchPanel.querySelector('input[name="matchtype"]:checked').value;

        for (const treff of matchTreff) {
            treff.hidden = !arbeidId || Number(treff.dataset.crisisId) !== arbeidId ||
                (type && treff.dataset.matchType !== type) ||
                !treff.dataset.matchSok.toLocaleLowerCase('nb-NO').includes(tekst);
        }

        matchStatus.hidden = matchTreff.some(treff => !treff.hidden);
    }

    matchPanel.addEventListener('submit', async event => {
        const form = event.target;
        event.preventDefault();
        const row = form.closest('.kart-matchtreff');
        if (!arbeidId || Number(row?.dataset.crisisId) !== arbeidId || lagrer) return;
        lagrer = true;
        oppdaterArbeid();
        form.inert = true;
        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form) });
            if (!response.ok || new URL(response.url).pathname.toLowerCase().includes('/account/'))
                throw new Error('Handlingen kunne ikke lagres. Kontroller innloggingen og prøv igjen.');
            lagrer = false;
            skjemaEndret = false;
            window.location.reload();
        } catch (error) {
            lagrer = false;
            form.inert = false;
            oppdaterArbeid();
            arbeidsstatus.textContent = error.message;
        }
    });
    matchSok.addEventListener('input', filtrerMatch);
    matchPanel.querySelector('.kart-valgfelt').addEventListener('change', filtrerMatch);

    kartElement.addEventListener('keydown', event => {
        if (event.key === 'Escape' && aktivKnapp) {
            event.preventDefault();
            lukkHandling();
        }
    });

    kart.on('click', event => {
        if (!markeringSkjema) return;

        if (markeringSkjema.dataset.kartmarkering === 'soner') {
            const hjorne = event.latlng;
            if (sonehjorner.some(punkt => punkt.equals(hjorne))) return;

            sonehjorner.push(hjorne);
            visSoneutkast();
            return;
        }

        plassering = kart.wrapLatLng(event.latlng);
        if (markeringSkjema.dataset.kartmarkering === 'behov') {
            if (markeringSkjema.inert) return;
            kart.closePopup();
            velgBehovplassering(plassering);
            return;
        }

        if (markeringSkjema.dataset.kartmarkering === 'veihindringer') {
            kart.closePopup();
            if (hindringPanel.hidden) {
                hindringHint.textContent = '';
                hindringPanel.hidden = false;
                aktivKnapp.removeAttribute('data-venter-plassering');
                aktivKnapp.setAttribute('aria-expanded', 'true');
                markeringSkjema.elements.navn.focus({ preventScroll: true });
            }
        }

        visPlassering(plassering);
    });

    handlinger.querySelector('.kart-sonevalg').addEventListener('change', visSoneutkast);

    handlinger.querySelector('[data-angre-hjorne]').addEventListener('click', () => {
        sonehjorner.pop();
        visSoneutkast();
    });


    async function kartApi(crisisId, suffix, method, body) {
        const response = await fetch('/api/crises/' + crisisId + '/map-annotations' + suffix, {
            method, headers: { 'Content-Type': 'application/json', 'Accept': 'application/json',
                'RequestVerificationToken': csrf },
            body: body ? JSON.stringify(body) : undefined
        });
        if (!response.ok) throw new Error('Lagringen mislyktes (' + response.status + '). Prøv igjen.');
        return response.status === 204 ? null : await response.json();
    }

    async function slettMarkering(markor, type, knapp) {
        if (!arbeidId || markor.hendelseId !== arbeidId || lagrer) return;
        lagrer = true;
        knapp.disabled = true;
        oppdaterArbeid();
        try {
            await kartApi(markor.hendelseId, '/' + markor.databaseId, 'DELETE');
            type.markorer.splice(type.markorer.indexOf(markor), 1);
            filtrer();
            oppdaterSoneliste();
            visBekreftelse('Markeringen er slettet fra hendelsen.');
        } catch (error) {
            arbeidsstatus.textContent = error.message;
            knapp.disabled = false;
        } finally {
            lagrer = false;
            for (const button of handlingsknapper) button.disabled = !arbeidId;
        }
    }

    function leggTilMarkering(item) {
        const type = typer.find(t => t.key === item.kind);
        if (!type || !Array.isArray(item.coordinates) || !item.coordinates.length) return;
        const erSone = item.kind === 'soner';
        const stil = erSone ? sonestil(item.risk) : null;
        const markor = erSone
            ? L.polygon(item.coordinates, { ...stil, renderer: sonerenderer, interactive: false })
            : L.marker(item.coordinates[0], { icon: ikon(type.key), title: item.name, bubblingMouseEvents: true });
        markor.databaseId = item.id;
        markor.hendelseId = item.crisisId;
        if (erSone) {
            markor.kartetikett = lagSoneetikett(item.name, stil.color);
            markor.sonedata = { navn: item.name, risiko: item.risk };
        } else {
            const innhold = popup({ navn: item.name, merknad: item.description, pavirkning: item.impact },
                [['Beskrivelse', 'merknad'], ['Påvirkning', 'pavirkning']]);
            const fjern = document.createElement('button');
            fjern.type = 'button';
            fjern.className = 'btn btn-sm btn-outline-secondary mt-2';
            fjern.textContent = 'Fjern markering';
            fjern.disabled = arbeidId !== item.crisisId;
            fjern.addEventListener('click', () => slettMarkering(markor, type, fjern));
            markor.sletteknapp = fjern;
            innhold.append(fjern);
            markor.bindPopup(innhold);
        }
        type.markorer.push(markor);
        return markor;
    }

    async function lagreMarkering(skjema) {
        if (!arbeidId || lagrer || skjema !== markeringSkjema) return false;
        const erSone = skjema.dataset.kartmarkering === 'soner';
        if ((erSone ? !gyldigSone(sonehjorner) : !plassering) || !skjema.reportValidity()) {
            arbeidsstatus.textContent = 'Fullfør navn og plassering, eller avbryt, før du bytter hendelse.';
            return false;
        }
        const item = {
            crisisId: arbeidId, kind: skjema.dataset.kartmarkering,
            name: skjema.elements.navn.value.trim(),
            description: skjema.elements.beskrivelse?.value.trim() ?? '',
            impact: skjema.elements.pavirkning?.value ?? '',
            risk: skjema.elements.risiko?.value ?? '',
            coordinates: (erSone ? sonehjorner : [plassering]).map(p => [p.lat, p.lng])
        };
        lagrer = true;
        skjema.inert = true;
        oppdaterArbeid();
        try {
            const result = await kartApi(item.crisisId, '', 'POST', item);
            item.id = result.id;
            leggTilMarkering(item);
            skjemaEndret = false;
            skjema.reset();
            lagrer = false;
            aktivKnapp?.focus();
            visHandling(null);
            oppdaterSoneliste();
            filtrer();
            oppdaterArbeid();
            visBekreftelse('Lagret på ' + valgteHendelser.find(h => h.id === item.crisisId).navn + '.');
            return true;
        } catch (error) {
            arbeidsstatus.textContent = error.message;
            return false;
        } finally {
            lagrer = false;
            skjema.inert = false;
            for (const button of handlingsknapper) button.disabled = !arbeidId;
        }
    }
    for (const skjema of handlinger.querySelectorAll('[data-kartmarkering]')) {
        skjema.addEventListener('submit', async event => {
            event.preventDefault();
            await lagreMarkering(skjema);
        });
    }
    for (const item of data.markeringer ?? []) leggTilMarkering(item);
    oppdaterSoneliste();
    filtrerMatch();

    // Åpner en valgt oppføring fra kortenes lister.
    document.addEventListener('oversikt:vis-pa-kart', event => {
        const valgt = event.detail;
        if (valgt.type === 'kriser' && !forespurteHendelser.has(String(valgt.id))) {
            lagreForBytte().then(ok => {
                if (!ok) return;
                const url = new URL(window.location.href);
                url.searchParams.append('hendelser', valgt.id);
                window.location.assign(url);
            });
            return;
        }
        const type = typer.find(t => t.key === valgt.type);

        const markor = type?.markorer.find(
            m => m.oversiktId === valgt.id
        );

        if (!markor) return;
        if (hendelsesvisning && (!synligeHendelser.has(markor.hendelseId) || !['kriser', 'behov'].includes(type.key))) return;

        // Slår på typen dersom den er skjult i kartfilteret.
        type.boks.checked = true;

        // Fjerner radius hvis den skjuler den valgte oppføringen.
        if (
            sentrum && Number(radius.value) &&
            sentrum.distanceTo(markor.getLatLng()) > Number(radius.value)
        ) {
            radius.value = '0';

            sentrumtekst.textContent =
                'Radius fjernet for å vise valgt oppføring.';
        }

        filtrer();

        kart.setView(
            markor.getLatLng(),
            Math.max(kart.getZoom(), 15),
            { animate: false }
        );

        apneMarkor(markor);
    });
    
    filtrer(hendelsesvisning);
})();