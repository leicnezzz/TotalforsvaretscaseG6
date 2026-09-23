(() => {
    const container = document.getElementById('resourceMap');
    if (!container) return;

    const latitudeInput = document.getElementById('latInput');
    const longitudeInput = document.getElementById('lngInput');
    const editable = Boolean(latitudeInput);
    const latitude = editable ? latitudeInput.value : container.dataset.latitude;
    const longitude = editable ? longitudeInput.value : container.dataset.longitude;
    const lat = Number(latitude);
    const lng = Number(longitude);
    const hasPosition = latitude?.trim() && longitude?.trim()
        && Number.isFinite(lat) && Number.isFinite(lng)
        && Math.abs(lat) <= 90 && Math.abs(lng) <= 180;

    const map = L.map(container).setView(hasPosition ? [lat, lng] : [58.1467, 7.9956], hasPosition ? 15 : 13);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    let marker;
    function showPosition(lat, lng) {
        if (marker) marker.setLatLng([lat, lng]);
        else marker = L.marker([lat, lng]).addTo(map);

        const popup = document.createElement('span');
        const name = document.getElementById('resourceName')?.textContent;
        popup.textContent = `${name || 'Valgt posisjon'}: ${lat}, ${lng}`;
        marker.bindPopup(popup).openPopup();
        if (editable) document.getElementById('posTekst').textContent = `${lat}, ${lng}`;
    }

    if (hasPosition) showPosition(lat, lng);
    if (editable) {
        map.on('click', event => {
            latitudeInput.value = event.latlng.lat.toFixed(5);
            longitudeInput.value = event.latlng.lng.toFixed(5);
            showPosition(Number(latitudeInput.value), Number(longitudeInput.value));
        });
    }
})();
