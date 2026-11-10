(() => {
    'use strict';

    document.querySelectorAll('[data-utskrift]').forEach(knapp => {
        knapp.addEventListener('click', () => {
            document.body.dataset.utskrift = knapp.dataset.utskrift;
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