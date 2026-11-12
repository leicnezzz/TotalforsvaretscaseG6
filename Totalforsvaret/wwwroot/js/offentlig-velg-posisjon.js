const kart = L.map("velgPosisjonKart").setView([58.1467, 7.9956], 14);

L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution: "&copy; OpenStreetMap"
}).addTo(kart);

let markor = null;

kart.on("click", function (e) {
    if (markor) {
        markor.setLatLng(e.latlng);
    } else {
        markor = L.marker(e.latlng).addTo(kart);
    }

    document.getElementById("Latitude").value = e.latlng.lat.toFixed(6);
    document.getElementById("Longitude").value = e.latlng.lng.toFixed(6);
    document.getElementById("posisjonFeil").hidden = true;

    fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&accept-language=nb&lat=${e.latlng.lat}&lon=${e.latlng.lng}`)
        .then(svar => svar.json())
        .then(data => {
            const a = data.address || {};
            const gate = [a.road, a.house_number].filter(Boolean).join(" ");
            const sted = [a.postcode, a.city || a.town || a.village].filter(Boolean).join(" ");
            const adresse = [gate, sted].filter(Boolean).join(", ");

            if (adresse) {
                document.getElementById("Address").value = adresse;
            }
        })
        .catch(() => { });
});

document.getElementById("velgPosisjonKart").closest("form").addEventListener("submit", function (e) {
    if (!document.getElementById("Latitude").value) {
        e.preventDefault();
        document.getElementById("posisjonFeil").hidden = false;
    }
});