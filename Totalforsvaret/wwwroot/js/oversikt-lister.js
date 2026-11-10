(() => {
    'use strict';

    const dialog = document.getElementById('oversiktDialog');
    const dataElement = document.getElementById('oversiktListeData');

    if (!dialog || !dataElement) return;

    const data = JSON.parse(dataElement.textContent);
    const tittel = document.getElementById('oversiktListeTittel');
    const typevalg = document.getElementById('oversiktType');
    const sortering = document.getElementById('oversiktSortering');
    const liste = document.getElementById('oversiktListe');
    const antall = document.getElementById('oversiktAntall');

    let visning = 'aktive';

    // Samler eksisterende transportnavn under samme filter.
    function behovstype(type) {
        const verdi = (type || '').trim().toLowerCase();

        if ([
            'transport',
            'kjøretøy',
            'transport (kjøretøy)'
        ].includes(verdi)) {
            return 'transport';
        }

        return verdi === 'utstyr' ? 'utstyr' : 'annet';
    }

    function prioritet(verdi) {
        return ({
            Kritisk: 4,
            Høy: 3,
            Middels: 2,
            Lav: 1
        })[verdi] || 0;
    }

    function gyldigPosisjon(item) {
        return Number.isFinite(item.latitude) &&
            Number.isFinite(item.longitude) &&
            Math.abs(item.latitude) <= 90 &&
            Math.abs(item.longitude) <= 180;
    }

    // Registrerte opplysninger vises som tekst.
    function element(tag, tekst, klasse) {
        const node = document.createElement(tag);

        if (tekst != null) node.textContent = tekst;
        if (klasse) node.className = klasse;

        return node;
    }

    function valg(select, alternativer) {
        select.replaceChildren();

        for (const [verdi, tekst] of alternativer) {
            const option = element('option', tekst);
            option.value = verdi;
            select.append(option);
        }
    }

    function utvalg() {
        const erRessurser = visning === 'ressurser';

        let treff = erRessurser
            ? [...data.ressurser]
            : data.behov.filter(behov => visning === 'loste'
                ? behov.status === 'Løst'
                : ['Åpen', 'Under behandling'].includes(behov.status));

        if (typevalg.value !== 'alle') {
            treff = treff.filter(item => erRessurser
                ? String(item.kategoriId) === typevalg.value
                : behovstype(item.type) === typevalg.value);
        }

        treff.sort((a, b) => {
            if (sortering.value === 'prioritet') {
                return prioritet(b.prioritet) - prioritet(a.prioritet) ||
                    b.id - a.id;
            }

            if (sortering.value === 'navn') {
                return (a.navn || '').localeCompare(b.navn || '', 'nb') ||
                    b.id - a.id;
            }

            return b.id - a.id;
        });

        return treff;
    }

    function tegnListe() {
        const erRessurser = visning === 'ressurser';
        const treff = utvalg();

        antall.textContent = treff.length + ' treff';
        liste.replaceChildren();

        if (!treff.length) {
            liste.append(element(
                'p',
                'Ingen oppføringer passer til valget. ' +
                'Velg «Alle» for å vise alle typer.',
                'oversikt-tom'
            ));
            return;
        }

        const tabell = element('table', null, 'oversikt-tabell');

        tabell.append(element(
            'caption',
            tittel.textContent,
            'visually-hidden'
        ));

        const thead = element('thead');
        const hoderad = element('tr');

        const kolonner = erRessurser
            ? [
                'Navn',
                'Type',
                'Tilbyder',
                'Kontakt',
                'Status',
                'Handlinger'
            ]
            : [
                'Navn',
                'Type',
                'Prioritet',
                'Status',
                'Tilknyttet krise',
                'Handlinger'
            ];

        for (const navn of kolonner) {
            const th = element('th', navn);
            th.scope = 'col';
            hoderad.append(th);
        }

        thead.append(hoderad);
        tabell.append(thead);

        const tbody = element('tbody');

        for (const item of treff) {
            const rad = element('tr');

            const type = erRessurser
                ? (
                    item.kategoriId === 1
                        ? 'Kjøretøy'
                        : item.kategoriId === 2
                            ? 'Utstyr'
                            : 'Annen kategori'
                )
                : behovstype(item.type) === 'transport'
                    ? 'Transport (kjøretøy)'
                    : item.type || 'Ikke oppgitt';

            const verdier = erRessurser
                ? [
                    item.navn || 'Uten navn',
                    type,
                    item.tilbyder || 'Ikke oppgitt',
                    item.kontaktpunkt || 'Ikke oppgitt',
                    'Tilgjengelig'
                ]
                : [
                    item.navn || 'Uten navn',
                    type,
                    item.prioritet || 'Ikke oppgitt',
                    item.status,
                    item.krise || 'Ikke oppgitt'
                ];

            for (const verdi of verdier) {
                rad.append(element('td', verdi));
            }

            const celle = element('td');
            const handlinger = element(
                'div',
                null,
                'oversikt-radhandlinger'
            );

            if (visning !== 'loste') {
                const kartknapp = element(
                    'button',
                    'Vis på kart',
                    'btn btn-sm btn-outline-secondary'
                );

                kartknapp.type = 'button';
                kartknapp.disabled = !gyldigPosisjon(item);

                if (kartknapp.disabled) {
                    kartknapp.title =
                        'Oppføringen mangler gyldige kartkoordinater.';
                }

                kartknapp.addEventListener('click', () => {
                    dialog.close();

                    document.dispatchEvent(
                        new CustomEvent('oversikt:vis-pa-kart', {
                            detail: {
                                type: erRessurser ? 'ressurser' : 'behov',
                                id: item.id
                            }
                        })
                    );

                    document.getElementById('offentligKart')
                        .scrollIntoView({
                            behavior: 'smooth',
                            block: 'center'
                        });
                });

                handlinger.append(kartknapp);
            }

            if (!erRessurser) {
                const lenke = element(
                    'a',
                    'Endre behov',
                    'btn btn-sm btn-outline-primary'
                );

                const url = new URL(
                    dialog.dataset.endreBehov,
                    window.location.origin
                );

                url.searchParams.set('id', String(item.id));
                lenke.href = url.href;

                handlinger.append(lenke);
            }

            celle.append(handlinger);
            rad.append(celle);
            tbody.append(rad);
        }

        tabell.append(tbody);
        liste.append(tabell);
    }
    function apne(type) {
        visning = type;
        const erRessurser = type === 'ressurser';

        tittel.textContent = erRessurser
            ? 'Tilgjengelige ressurser'
            : type === 'loste'
                ? 'Løste behov'
                : 'Aktive behov';

        valg(typevalg, erRessurser
            ? [
                ['alle', 'Alle'],
                ['1', 'Kjøretøy'],
                ['2', 'Utstyr']
            ]
            : [
                ['alle', 'Alle'],
                ['transport', 'Transport (kjøretøy)'],
                ['utstyr', 'Utstyr']
            ]);

        valg(sortering, erRessurser
            ? [
                ['nyeste', 'Nyeste først'],
                ['navn', 'Navn A–Å']
            ]
            : [
                ['prioritet', 'Høyest prioritet først'],
                ['nyeste', 'Nyeste først'],
                ['navn', 'Navn A–Å']
            ]);

        tegnListe();
        dialog.showModal();
    }

    document.querySelectorAll('[data-oversikt-liste]')
        .forEach(kort => {
            kort.addEventListener('click', () => {
                apne(kort.dataset.oversiktListe);
            });
        });

    typevalg.addEventListener('change', tegnListe);
    sortering.addEventListener('change', tegnListe);

    document.getElementById('oversiktLukk')
        .addEventListener('click', () => dialog.close());
})();