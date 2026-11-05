(async () => {
    const element = document.getElementById("private-resource-map");
    const status = document.getElementById("map-status");

    if (!element || !status) return;

    if (typeof L === "undefined") {
        status.textContent = "Kartet kunne ikke lastes.";
        return;
    }
    // laster inn kartet og viser registrerte ressurser for brukeren.
    const map = L.map(element, {
        scrollWheelZoom: false
    }).setView([58.1467, 7.9956], 12);

    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    try {
        const response = await fetch(element.dataset.pointsUrl, {
            credentials: "same-origin",
            headers: {
                Accept: "application/json"
            }
        });

        if (!response.ok ||
            !response.headers.get("content-type")?.includes("application/json")) {
            throw new Error("Kartdata kunne ikke hentes.");
        }

        const points = await response.json();

        if (!Array.isArray(points)) {
            throw new Error("Ugyldige kartdata.");
        }

        const markers = L.featureGroup();

        for (const point of points) {
            const latitude = Number(point.latitude);
            const longitude = Number(point.longitude);

            if (!Number.isFinite(latitude) ||
                !Number.isFinite(longitude) ||
                Math.abs(latitude) > 90 ||
                Math.abs(longitude) > 180) {
                continue;
            }

            // textContent hindrer at ressursnavn tolkes som HTML.
            const popup = document.createElement("span");
            popup.textContent = point.navn || "Registrert ressurs";

            L.circleMarker([latitude, longitude], {
                radius: 8,
                color: "#ffffff",
                weight: 2,
                fillColor: "#426647",
                fillOpacity: 1
            })
                .bindPopup(popup)
                .addTo(markers);
        }

        markers.addTo(map);

        if (markers.getLayers().length === 0) {
            status.textContent = "Du har ingen registrerte ressurser på kartet ennå.";
            return;
        }

        map.fitBounds(markers.getBounds(), {
            padding: [35, 35],
            maxZoom: 15
        });

        status.textContent = "";
        status.hidden = true;
    } catch {
        status.textContent =
            "Kunne ikke hente ressursene. Prøv å laste siden på nytt.";
    }
})();
// Viser kart med registrerte ressurser for brukeren.