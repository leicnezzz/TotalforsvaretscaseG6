(() => {
    'use strict';

    document.querySelectorAll('[data-utskrift]').forEach(knapp => {
        knapp.addEventListener('click', () => {
            const utskrift = knapp.dataset.utskrift;
if (utskrift === 'treff' || utskrift === 'oversiktsliste') {
    const erTreff = utskrift === 'treff';
    const tabell = document.querySelector(erTreff
        ? '#oversiktDialog .treff-tabellramme table'
        : '#oversiktListe table');

    if (!tabell) return;

    const tittel = erTreff
        ? 'Foreslåtte treff'
        : document.getElementById('oversiktListeTittel').textContent.trim();

    const kopi = tabell.cloneNode(true);
    kopi.removeAttribute('class');
    kopi.querySelectorAll('.oversikt-mobilfelt, caption.visually-hidden')
    .forEach(element => element.remove());

    kopi.querySelectorAll('tbody tr[hidden]').forEach(rad => rad.remove());
    kopi.querySelectorAll('thead th:last-child, tbody td:last-child')
        .forEach(celle => celle.remove());

    const ramme = document.createElement('iframe');
    ramme.title = 'Utskrift av foreslåtte treff';
    ramme.style.cssText = 'position:fixed;width:1px;height:1px;left:-10000px;border:0';

    ramme.addEventListener('load', () => {
        const utskriftsvindu = ramme.contentWindow;
        utskriftsvindu.addEventListener('afterprint', () => ramme.remove(), { once: true });
        utskriftsvindu.focus();
        utskriftsvindu.print();
    }, { once: true });

    ramme.srcdoc = `<!doctype html>
        <html lang="nb">
        <head>
            <meta charset="utf-8">
            <title>Foreslåtte treff</title>
            <style>
                body { font: 12pt Arial, sans-serif; }
                table { width: 100%; border-collapse: collapse; }
                th, td { padding: 8px; border: 1px solid #aaa; text-align: left; }
                small { display: block; }
                tr { break-inside: avoid; }
            </style>
        </head>
        <body>
            <h1>Foreslåtte treff</h1>
            ${kopi.outerHTML}
        </body>
        </html>`;

    document.body.append(ramme);
    return;
}

document.body.dataset.utskrift = utskrift;
window.print();
        });
    });

    window.addEventListener('afterprint', () => {
        delete document.body.dataset.utskrift;
    });
    function tilGeoJson() {
        const data = JSON.parse(
            document.getElementById('offentligKartData').textContent
        );

        const punkter = (liste, type) => (liste ?? [])
            .filter(e => Number.isFinite(e.latitude) && Number.isFinite(e.longitude))
            .map(e => ({
                type: 'Feature',
                geometry: {
                    type: 'Point',
                    coordinates: [e.longitude, e.latitude]
                },
                properties: { objekttype: type, ...e }
            }));

        return {
            type: 'FeatureCollection',
            features: [
                ...punkter(data.ressurser, 'ressurs'),
                ...punkter(data.behov, 'behov')
            ]
        };
    }

    document.querySelectorAll('[data-eksport="geojson"]').forEach(knapp => {
        knapp.addEventListener('click', () => {
            const json = JSON.stringify(tilGeoJson(), null, 2);
            const blob = new Blob([json], { type: 'application/geo+json' });
            const lenke = document.createElement('a');

            lenke.href = URL.createObjectURL(blob);
            lenke.download = 'ressurser-og-behov.geojson';
            lenke.click();
            URL.revokeObjectURL(lenke.href);
        });
    });
})();