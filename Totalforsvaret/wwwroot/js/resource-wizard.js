(() => {
    "use strict";
    const form = document.getElementById("resource-wizard");
    if (!form) return;

    const category = document.getElementById("wizard-category");
    const message = document.getElementById("wizard-message");
    const submit = document.getElementById("vehicle-submit");
    const success = document.getElementById("wizard-success");
    const draftKey = "paratus.vehicle-draft.v1";
    const fields = ["RegistrationNumber", "Model", "Condition", "RequiredLicense",
        "AdditionalInformation", "Contact", "Available", "AvailableFrom", "Address", "Latitude", "Longitude"];
    const authenticated = form.dataset.authenticated === "true";
    // Kun en lokal demotilstand. Dette gir ingen ekte innlogging eller tilgang.
    let previewAccountComplete = authenticated;
    let accountMode = "login";
    let additionalComplete = false;
    let contactComplete = false;
    let completed = false;
    let map;
    let marker;
    let addressRequest;
    let addressTimer;
    let kartverketRetryAt = 0;
    let lastVehiclePlate = null;
    let manualVehicleEdit = false;
    const addressCache = new Map();
    const addressStatus = document.getElementById("vehicle-address-status");
    const addressResults = document.getElementById("vehicle-address-results");
    const vehicleLookupStatus = document.getElementById("vehicle-lookup-status");

    const input = name => form.elements.namedItem(name);
    const hasNumber = () => input("HasRegistrationNumber").value;
    const step = name => form.querySelector('[data-step="' + name + '"]');
    const showStep = (name, visible) => {
        const element = step(name);
        element.hidden = !visible;
        element.disabled = !visible;
    };
    function showMessage(text) {
        message.textContent = text;
        message.hidden = false;
    }
    function normalizePlate() {
        return input("RegistrationNumber").value.replace(/[ -]/g, "").toUpperCase();
    }
    function clearVehicleConfirmation() {
        for (const radio of form.querySelectorAll('[name="VehicleInfoCorrect"]')) radio.checked = false;
    }
    function previewVehicleInformation() {
        const plate = normalizePlate();
        if (lastVehiclePlate !== plate) {
            lastVehiclePlate = plate;
            if (plate === "AB12345") {
                // Kun frontend-eksempel. Ingen opplysninger er hentet om et virkelig kjøretøy.
                input("Model").value = "Toyota Hiace (demodata)";
                input("RequiredLicense").value = "B";
            }
        }
        const demo = plate === "AB12345";
        vehicleLookupStatus.textContent = demo
            ? "DEMO: Modell og førerkortklasse er fylt med eksempeldata, ikke reelle kjøretøyopplysninger. Tilstand må du oppgi selv."
            : "Automatisk kjøretøyoppslag kobles til senere. Fyll inn opplysningene manuelt, eller prøv AB12345 for demovisning.";
        input("Model").readOnly = demo && !manualVehicleEdit;
        input("RequiredLicense").readOnly = demo && !manualVehicleEdit;
    }
    function updateStepColors() {
        const colors = ["wizard-step-white", "wizard-step-soft", "wizard-step-blue"];
        const visible = Array.from(form.querySelectorAll(".wizard-step"))
            .filter(element => !element.hidden && !element.classList.contains("wizard-step-confirm"));
        visible.forEach((element, index) => {
            element.classList.remove(...colors);
            element.classList.add(colors[index % colors.length]);
        });
    }
    function updateAccountPanels() {
        document.getElementById("wizard-account-choice").hidden = previewAccountComplete;
        document.getElementById("wizard-account-complete").hidden = !previewAccountComplete;
        for (const button of form.querySelectorAll("[data-account-mode]")) {
            button.setAttribute("aria-pressed", String(button.dataset.accountMode === accountMode));
        }
        for (const panel of form.querySelectorAll("[data-account-panel]")) {
            const visible = !previewAccountComplete && panel.dataset.accountPanel === accountMode;
            panel.hidden = !visible;
            panel.disabled = !visible || step("account").hidden;
        }
        const password = input("PreviewRegisterPassword");
        const confirmation = input("PreviewConfirmPassword");
        password.setCustomValidity(password.value &&
            (!/\p{L}/u.test(password.value) || !/\d/.test(password.value))
            ? "Testpassordet må inneholde både bokstaver og tall." : "");
        confirmation.setCustomValidity(confirmation.value && confirmation.value !== password.value
            ? "Passordene må være like." : "");
    }
    function continueAccountPreview() {
        updateAccountPanels();
        const panel = form.querySelector('[data-account-panel="' + accountMode + '"]');
        for (const field of panel.querySelectorAll("input")) {
            if (!field.reportValidity()) return;
        }
        previewAccountComplete = true;
        document.getElementById("wizard-account-complete-text").textContent = accountMode === "login"
            ? "Innloggingssteget er gjennomgått (demo). Du kan nå forhåndsvise registreringen nedenfor."
            : "Kontosteget er gjennomgått (demo). Du kan nå forhåndsvise registreringen nedenfor.";
        // Passord lagres aldri i utkastet og fjernes straks demosteget er gjennomgått.
        for (const field of form.querySelectorAll('.wizard-auth-panel input[type="password"]')) field.value = "";
        input("Confirmed").checked = false;
        render();
        step("confirmation").querySelector("h3").setAttribute("tabindex", "-1");
        step("confirmation").querySelector("h3").focus();
    }
    function validCoordinates() {
        const lat = input("Latitude");
        const lon = input("Longitude");
        return input("Address").value.trim() !== "" && lat.value !== "" && lon.value !== "" &&
            Number.isFinite(Number(lat.value)) && Math.abs(Number(lat.value)) <= 90 &&
            Number.isFinite(Number(lon.value)) && Math.abs(Number(lon.value)) <= 180;
    }
    function buildSummary() {
        const summary = document.getElementById("vehicle-summary");
        summary.replaceChildren();
        const rows = [
            ["Kjøretøy", input("Model").value.trim()],
            ["Registreringsnummer", hasNumber() === "true" ? input("RegistrationNumber").value.toUpperCase() : "Uten registreringsnummer"],
            ["Tilstand", input("Condition").value.trim()],
            ["Førerkortklasse", input("RequiredLicense").value.trim() || "Ikke oppgitt"],
            ["Tilleggsinformasjon", input("AdditionalInformation").value.trim() || "Ingen"],
            ["Telefon", input("Contact").value.trim()],
            ["Tilgjengelig", input("Available").value === "true" ? "Ja" : input("Available").value === "false" ? "Ikke akkurat nå" : "Ikke valgt"],
            ["Tilgjengelig fra", input("AvailableFrom").value.replace("T", " ") || "Fra registreringstidspunktet"],
            ["Adresse", input("Address").value.trim()]
        ];
        for (const [label, value] of rows) {
            const term = document.createElement("dt");
            const description = document.createElement("dd");
            term.textContent = label;
            description.textContent = value;
            summary.append(term, description);
        }
    }
    function updateMap() {
        const visible = !step("location").hidden;
        document.getElementById("vehicle-location-map").hidden = !visible;
        if (!visible || typeof L === "undefined") return;
        if (!map) {
            map = L.map("vehicle-location-map", { scrollWheelZoom: false }).setView([58.1467, 7.9956], 12);
            L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
                maxZoom: 19,
                attribution: '&copy; OpenStreetMap contributors'
            }).addTo(map);
        }
        requestAnimationFrame(() => map.invalidateSize());
        if (validCoordinates()) {
            const point = [Number(input("Latitude").value), Number(input("Longitude").value)];
            const moved = !marker || marker.getLatLng().lat !== point[0] || marker.getLatLng().lng !== point[1];
            if (marker) marker.setLatLng(point);
            else marker = L.circleMarker(point, { radius: 8, color: "#fff", weight: 2, fillColor: "#2e6840", fillOpacity: 1 }).addTo(map);
            if (moved) map.setView(point, 16);
        } else if (marker) {
            marker.remove();
            marker = null;
            map.setView([58.1467, 7.9956], 12);
        }
    }
    function render() {
        const vehicle = category.value === "2" && !completed;
        const choice = hasNumber();
        document.getElementById("wizard-category-note").hidden = !category.value || vehicle || completed;
        input("RegistrationNumber").setCustomValidity("");
        if (choice === "true" && !/^[\p{L}\p{N} -]{2,12}$/u.test(input("RegistrationNumber").value.trim())) {
            input("RegistrationNumber").setCustomValidity("Skriv inn et registreringsnummer med 2–12 tegn.");
        }
        const details = vehicle && (choice === "false" ||
            (choice === "true" && input("RegistrationNumber").validity.valid));
        if (details && choice === "true") previewVehicleInformation();
        else {
            vehicleLookupStatus.textContent = "";
            input("Model").readOnly = false;
            input("RequiredLicense").readOnly = false;
        }
        const needsInformationCheck = Boolean(details && choice === "true" && input("Model").value.trim());
        document.getElementById("vehicle-information-check").hidden = !needsInformationCheck;
        for (const radio of form.querySelectorAll('[name="VehicleInfoCorrect"]')) {
            radio.required = needsInformationCheck;
            radio.disabled = !needsInformationCheck;
        }
        const informationAccepted = choice === "false" || input("VehicleInfoCorrect").value === "true";
        const filledDetails = details && informationAccepted && input("Model").value.trim() && input("Condition").value.trim();
        showStep("registration-choice", vehicle);
        showStep("registration-number", vehicle && choice === "true");
        showStep("vehicle-details", Boolean(details));
        showStep("additional", Boolean(filledDetails));
        showStep("availability", Boolean(filledDetails && additionalComplete));
        const phone = input("Contact").value.trim();
        const digits = phone.replace(/\D/g, "");
        const validPhone = /^\+?[\d\s()-]+$/.test(phone) && digits.length >= 8 && digits.length <= 15;
        input("Contact").setCustomValidity(validPhone ? "" : "Skriv inn et gyldig telefonnummer med 8–15 sifre, eventuelt med landskode.");
        const validContact = Boolean(validPhone && input("Available").value && input("AvailableFrom").validity.valid);
        const locationVisible = Boolean(filledDetails && additionalComplete && contactComplete && validContact);
        showStep("location", locationVisible);
        const ready = Boolean(locationVisible && validCoordinates());
        const locationComplete = ready;
        showStep("account", locationComplete && !authenticated);
        showStep("confirmation", locationComplete && previewAccountComplete);
        updateAccountPanels();
        input("Confirmed").required = true;
        const contactError = document.getElementById("vehicle-contact-error");
        contactError.hidden = validPhone || (!phone && !locationComplete);
        contactError.textContent = "Skriv inn et telefonnummer med 8–15 sifre, eventuelt med landskode.";
        const missing = [];
        if (!validPhone) missing.push("et gyldig telefonnummer");
        if (!input("Available").value) missing.push("tilgjengelighet");
        if (!input("AvailableFrom").validity.valid) missing.push("en gyldig dato eller la datoen stå tom");
        const prerequisiteNote = document.getElementById("wizard-prerequisite-note");
        prerequisiteNote.hidden = !missing.length;
        prerequisiteNote.textContent = missing.length ? "Gå tilbake og fyll ut " + missing.join(" og ") + "." : "";
        document.getElementById("vehicle-additional-count").textContent = String(input("AdditionalInformation").value.length);
        submit.disabled = !ready || !previewAccountComplete || !input("Confirmed").checked;
        if (locationComplete) buildSummary();
        updateStepColors();
        updateMap();
    }

    async function fetchAddressJson(url, signal, timeoutMs) {
        const controller = new AbortController();
        const abort = () => controller.abort();
        if (signal.aborted) controller.abort();
        else signal.addEventListener("abort", abort, { once: true });
        const timeout = setTimeout(abort, timeoutMs);
        try {
            const response = await fetch(url, {
                signal: controller.signal, credentials: "omit", referrerPolicy: "no-referrer"
            });
            if (!response.ok) throw new Error("Adressetjenesten svarte med " + response.status);
            return await response.json();
        } finally {
            clearTimeout(timeout);
            signal.removeEventListener("abort", abort);
        }
    }

    async function getAddressSuggestions(query, signal) {
        if (Date.now() >= kartverketRetryAt) {
            try {
                const url = new URL("https://api.kartverket.no/adresser/v1/sok");
                url.searchParams.set("sok", query);
                url.searchParams.set("treffPerSide", "5");
                url.searchParams.set("utkoordsys", "4258");
                url.searchParams.set("fuzzy", "true");
                const result = await fetchAddressJson(url, signal, 3000);
                const addresses = (Array.isArray(result.adresser) ? result.adresser : []).map(address => ({
                    latitude: Number(address.representasjonspunkt?.lat),
                    longitude: Number(address.representasjonspunkt?.lon),
                    label: [address.adressetekst, [address.postnummer, address.poststed].filter(Boolean).join(" ")].filter(Boolean).join(", ")
                }));
                if (addresses.length) return { provider: "Kartverket", addresses };
            } catch (error) {
                if (signal.aborted) throw error;
                // Ved driftsfeil bruker resten av søkene reserven direkte i fem minutter.
                kartverketRetryAt = Date.now() + 300000;
            }
        }

        const url = new URL("https://photon.komoot.io/api/");
        url.searchParams.set("q", query);
        url.searchParams.set("limit", "5");
        url.searchParams.set("countrycode", "NO");
        // Foretrekk nærområdet, uten å utelukke adresser andre steder i Norge.
        url.searchParams.set("lat", "58.1467");
        url.searchParams.set("lon", "7.9956");
        const result = await fetchAddressJson(url, signal, 8000);
        const addresses = (Array.isArray(result.features) ? result.features : [])
            .filter(feature => feature.properties?.countrycode?.toUpperCase() === "NO")
            .map(feature => {
                const address = feature.properties;
                const street = [address.street || address.name, address.housenumber].filter(Boolean).join(" ");
                const city = address.city || address.town || address.village || address.locality;
                return {
                    latitude: Number(feature.geometry?.coordinates?.[1]),
                    longitude: Number(feature.geometry?.coordinates?.[0]),
                    label: [street, [address.postcode, city].filter(Boolean).join(" ")].filter(Boolean).join(", ")
                };
            });
        return { provider: "Photon / OpenStreetMap", addresses };
    }

    async function searchAddress() {
        const query = input("Address").value.trim();
        if (query.length < 3) {
            addressStatus.textContent = "Skriv minst tre tegn for å få adresseforslag.";
            return;
        }
        addressRequest?.abort();
        const request = new AbortController();
        addressRequest = request;
        const timeout = setTimeout(() => request.abort(), 15000);
        addressResults.replaceChildren();
        addressResults.hidden = true;
        addressStatus.textContent = "Søker etter adresse …";
        try {
            let result = addressCache.get(query.toLowerCase());
            if (!result) {
                result = await getAddressSuggestions(query, request.signal);
                if (addressCache.size >= 20) addressCache.clear();
                addressCache.set(query.toLowerCase(), result);
            }
            if (addressRequest !== request || query !== input("Address").value.trim() || step("location").disabled) return;
            const addresses = result.addresses;
            const seen = new Set();
            let hits = 0;
            for (const address of addresses) {
                const { latitude, longitude, label } = address;
                if (!Number.isFinite(latitude) || !Number.isFinite(longitude) ||
                    Math.abs(latitude) > 90 || Math.abs(longitude) > 180 || !label || seen.has(label)) continue;
                seen.add(label);
                const item = document.createElement("li");
                const button = document.createElement("button");
                button.type = "button";
                button.textContent = label;
                button.addEventListener("click", () => {
                    clearTimeout(addressTimer);
                    addressRequest?.abort();
                    addressRequest = null;
                    input("Address").value = label;
                    input("Latitude").value = latitude.toFixed(6);
                    input("Longitude").value = longitude.toFixed(6);
                    input("Confirmed").checked = false;
                    addressResults.hidden = true;
                    addressStatus.textContent = "Valgt adresse: " + label;
                    saveDraft();
                    render();
                    if (map) {
                        map.setView([latitude, longitude], 16);
                        const popup = document.createElement("span");
                        popup.textContent = label;
                        marker?.bindPopup(popup).openPopup();
                    }
                    input("Address").focus();
                });
                item.append(button);
                addressResults.append(item);
                hits++;
            }
            addressResults.hidden = hits === 0;
            addressStatus.textContent = hits
                ? "Klikk på riktig adresse nedenfor. Forslag fra " + result.provider + "."
                : "Fant ingen adresse. Prøv gatenavn, husnummer og poststed, for eksempel «Elvegata 14B Kristiansand».";
        } catch (error) {
            if (addressRequest !== request) return;
            addressStatus.textContent = error.name === "AbortError"
                ? "Søket ble avbrutt eller tok for lang tid. Prøv igjen."
                : "Kunne ikke kontakte adressetjenestene. Kontroller nettforbindelsen og prøv igjen.";
        } finally {
            clearTimeout(timeout);
            if (addressRequest === request) {
                addressRequest = null;
            }
        }
    }

    input("Address").addEventListener("keydown", event => {
        if (event.key === "Enter") {
            event.preventDefault();
            clearTimeout(addressTimer);
            searchAddress();
        }
        if (event.key === "ArrowDown" && !addressResults.hidden) {
            event.preventDefault();
            addressResults.querySelector("button")?.focus();
        }
    });
    addressResults.addEventListener("keydown", event => {
        const buttons = Array.from(addressResults.querySelectorAll("button"));
        const index = buttons.indexOf(document.activeElement);
        if (event.key === "Escape") {
            addressResults.hidden = true;
            input("Address").focus();
        } else if ((event.key === "ArrowDown" || event.key === "ArrowUp") && index >= 0) {
            event.preventDefault();
            buttons[(index + (event.key === "ArrowDown" ? 1 : -1) + buttons.length) % buttons.length]?.focus();
        }
    });
    function saveDraft() {
        if (completed) return true;
        const values = {};
        for (const name of fields) values[name] = input(name).value;
        try {
            sessionStorage.setItem(draftKey, JSON.stringify({
                version: 1, updated: Date.now(), category: category.value,
                hasNumber: hasNumber(), vehicleInfoCorrect: input("VehicleInfoCorrect").value,
                manualVehicleEdit, additionalComplete, contactComplete, values
            }));
            return true;
        } catch {
            showMessage("Nettleseren kunne ikke bevare utkastet. Tillat lagring i denne fanen før du går videre til innlogging.");
            return false;
        }
    }
    function restoreDraft() {
        try {
            const raw = sessionStorage.getItem(draftKey);
            if (!raw) return;
            if (raw.length > 20000) { sessionStorage.removeItem(draftKey); return; }
            const draft = JSON.parse(raw);
            if (draft.version !== 1 || !Number.isFinite(draft.updated) ||
                Date.now() - draft.updated > 86400000 || !draft.values) {
                sessionStorage.removeItem(draftKey);
                return;
            }
            if (Array.from(category.options).some(option => option.value === draft.category)) category.value = draft.category;
            for (const name of fields) {
                if (typeof draft.values[name] === "string") input(name).value = draft.values[name];
            }
            for (const radio of form.querySelectorAll('[name="HasRegistrationNumber"]')) radio.checked = radio.value === draft.hasNumber;
            for (const radio of form.querySelectorAll('[name="VehicleInfoCorrect"]')) radio.checked = radio.value === draft.vehicleInfoCorrect;
            manualVehicleEdit = draft.manualVehicleEdit === true;
            additionalComplete = draft.additionalComplete === true;
            contactComplete = additionalComplete && draft.contactComplete === true;
            if (draft.hasNumber === "true" && input("Model").value.trim()) lastVehiclePlate = normalizePlate();
            // Bekreftelse må gis på nytt etter innlogging eller gjenåpning.
            input("Confirmed").checked = false;
        } catch {
            showMessage("Det gamle utkastet kunne ikke åpnes. Du kan fylle ut en ny registrering.");
        }
    }
    form.addEventListener("input", event => {
        if (event.target === category || ["HasRegistrationNumber", "RegistrationNumber", "Model",
            "Condition", "RequiredLicense", "VehicleInfoCorrect"].includes(event.target.name)) {
            additionalComplete = false;
            contactComplete = false;
        }
        if (["Contact", "Available", "AvailableFrom"].includes(event.target.name)) contactComplete = false;
        if (event.target === input("RegistrationNumber")) {
            lastVehiclePlate = null;
            manualVehicleEdit = false;
            input("Model").value = "";
            input("Condition").value = "";
            input("RequiredLicense").value = "";
            clearVehicleConfirmation();
        }
        if (event.target === input("Model") || event.target === input("RequiredLicense")) clearVehicleConfirmation();
        if (event.target.name === "VehicleInfoCorrect" && event.target.value === "false") manualVehicleEdit = true;
        if (event.target === input("Address")) {
            clearTimeout(addressTimer);
            addressRequest?.abort();
            addressRequest = null;
            input("Latitude").value = "";
            input("Longitude").value = "";
            addressResults.replaceChildren();
            addressResults.hidden = true;
            addressStatus.textContent = input("Address").value.trim().length >= 3
                ? "Henter adresseforslag …" : "Skriv minst tre tegn for å få adresseforslag.";
            if (input("Address").value.trim().length >= 3) addressTimer = setTimeout(searchAddress, 600);
        }
        if (event.target !== input("Confirmed")) input("Confirmed").checked = false;
        render();
        saveDraft();
    });
    form.addEventListener("change", event => {
        if (event.target.name === "HasRegistrationNumber" && hasNumber() === "false") {
            input("RegistrationNumber").value = "";
            if (lastVehiclePlate === "AB12345" && !manualVehicleEdit) {
                input("Model").value = "";
                input("RequiredLicense").value = "";
            }
            lastVehiclePlate = null;
            clearVehicleConfirmation();
        }
        render();
        saveDraft();
    });
    function moveToStep(name) {
        render();
        saveDraft();
        const heading = step(name).querySelector("h3");
        heading.setAttribute("tabindex", "-1");
        heading.focus({ preventScroll: true });
        step(name).scrollIntoView({
            behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth",
            block: "start"
        });
    }
    document.getElementById("wizard-additional-next").addEventListener("click", () => {
        if (!input("AdditionalInformation").reportValidity()) return;
        additionalComplete = true;
        moveToStep("availability");
    });
    document.getElementById("wizard-contact-next").addEventListener("click", () => {
        for (const field of step("availability").querySelectorAll("input, select")) {
            if (!field.reportValidity()) return;
        }
        contactComplete = true;
        moveToStep("location");
    });
    for (const button of form.querySelectorAll("[data-account-mode]")) {
        button.addEventListener("click", () => {
            accountMode = button.dataset.accountMode;
            for (const field of form.querySelectorAll('.wizard-auth-panel input[type="password"]')) field.value = "";
            render();
        });
    }
    for (const button of form.querySelectorAll("[data-account-continue]")) {
        button.addEventListener("click", continueAccountPreview);
    }
    for (const panel of form.querySelectorAll("[data-account-panel]")) {
        panel.addEventListener("keydown", event => {
            if (event.key === "Enter") {
                event.preventDefault();
                continueAccountPreview();
            }
        });
    }
    document.getElementById("wizard-account-edit").addEventListener("click", () => {
        previewAccountComplete = false;
        input("Confirmed").checked = false;
        render();
    });
    form.addEventListener("submit", event => {
        event.preventDefault();
        if (completed || !previewAccountComplete || !form.reportValidity()) return;
        // Ren frontend-prototype: ingen forespørsel sendes til applikasjonens backend.
        completed = true;
        message.hidden = true;
        for (const section of form.querySelectorAll(".wizard-step")) section.hidden = true;
        document.getElementById("wizard-success-message").textContent =
            "Slik vil bekreftelsen se ut. Ingen ressurs er sendt inn eller lagret.";
        success.hidden = false;
        success.focus();
    });
    document.getElementById("wizard-new").addEventListener("click", () => {
        completed = false;
        previewAccountComplete = authenticated;
        accountMode = "login";
        additionalComplete = false;
        contactComplete = false;
        form.reset();
        clearTimeout(addressTimer);
        lastVehiclePlate = null;
        manualVehicleEdit = false;
        addressRequest?.abort();
        addressRequest = null;
        addressResults.replaceChildren();
        addressResults.hidden = true;
        addressStatus.textContent = "";
        success.hidden = true;
        message.hidden = true;
        form.querySelector(".wizard-step").hidden = false;
        render();
        saveDraft();
        category.focus();
    });

    restoreDraft();
    render();
})();

