(() => {
    'use strict';

    const form = document.querySelector('[data-need-wizard]');
    if (!form) return;

    const steps = [...form.querySelectorAll('[data-need-step]')];
    const next = form.querySelector('[data-need-next]');
    const back = form.querySelector('[data-need-back]');
    const cancel = form.querySelector('[data-need-cancel]');
    const submit = form.querySelector('[data-need-submit]');
    const location = form.querySelector('[data-need-location]');
    const markerColor = location?.dataset.markerColor ?? '#163c4c';
    const review = form.querySelector('[data-need-review]');
    const reviewMapElement = form.querySelector('[data-need-review-map]');
    const reviewStep = steps.length - 1;
    let reviewMap;
    let reviewMarker;
    const fields = [...steps[0].querySelectorAll('input:not([type=hidden]), select, textarea')];
    let currentStep = 0;
    const fieldError = document.createElement('div');
    steps[0].querySelector('h2').after(fieldError);
    let selectedAddress = '';
    const area = location?.hasAttribute('data-fill-area') ? form.elements.Area : null;
    let autoArea = '';

    const address = location ? window.ParatusResourceAddress.attach(steps[1], label => {
        selectedAddress = label;
        if (area && label && (!area.value.trim() || area.value === autoArea)) {
            autoArea = label.slice(0, area.maxLength);
            area.value = autoArea;
        }
        location.textContent = locationText();
        location.classList.remove('text-danger');
    }, { allowMapSelection: true, lookupMapAddress: true, markerColor }) : null;

    const crisisLocationsElement = form.querySelector('[data-crisis-locations]');
    if (address && crisisLocationsElement) {
        const locations = new Map(JSON.parse(crisisLocationsElement.textContent)
            .map(point => [String(point.Id), point]));
        const crisis = form.elements.CrisisId;
        const useCrisisLocation = form.querySelector('[data-use-crisis-location]');
        const selectedLocation = () => locations.get(crisis.value);

        function applyCrisisLocation() {
            const point = selectedLocation();
            useCrisisLocation.hidden = !point;
            if (point) {
                address.lookupAddress({ lat: point.Latitude, lng: point.Longitude });
            } else {
                form.elements.Latitude.value = '';
                form.elements.Longitude.value = '';
                selectedAddress = '';
                address.reset();
                location.textContent = locationText();
            }
        }

        useCrisisLocation.hidden = !selectedLocation();
        useCrisisLocation.addEventListener('click', applyCrisisLocation);
        crisis.addEventListener('change', applyCrisisLocation);
        if (!address.valid() && selectedLocation()) applyCrisisLocation();
    }
    function locationText() {
        if (!address.valid()) return 'Ingen plassering valgt.';
        const latitude = form.elements.Latitude.value;
        const longitude = form.elements.Longitude.value;
        const coordinates = latitude + ', ' + longitude;
        return selectedAddress || 'Kartpunkt: ' + coordinates;
    }

    function refreshReviewMap() {
        reviewMapElement.hidden = !window.L || !address.valid();
        if (reviewMapElement.hidden) {
            return;
        }

        const point = ['Latitude', 'Longitude'].map(name =>
            Number(form.elements[name].value.replace(',', '.')));
        if (!reviewMap) {
            reviewMap = L.map(reviewMapElement, {
                dragging: false, scrollWheelZoom: false, doubleClickZoom: false,
                touchZoom: false, boxZoom: false, keyboard: false, zoomControl: false
            });
            L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19, attribution: '&copy; OpenStreetMap contributors'
            }).addTo(reviewMap);
        }
        reviewMap.invalidateSize({ pan: false });
        reviewMap.setView(point, 16, { animate: false });
        if (reviewMarker) reviewMarker.setLatLng(point);
        else reviewMarker = L.circleMarker(point, {
            radius: 8, color: '#fff', weight: 2, fillColor: markerColor, fillOpacity: 1
        }).addTo(reviewMap);
    }

    function showStep(index, focus = true) {
        const changed = currentStep !== index;
        currentStep = index;
        steps.forEach((step, position) => {
            step.hidden = position !== index;
            if (changed) step.classList.toggle('is-entering', position === index && focus);
        });
        cancel.hidden = index !== 0;
        back.hidden = index === 0;
        next.hidden = index === steps.length - 1;
        submit.hidden = !next.hidden;

        if (address && index === 1) location.textContent = locationText();
        requestAnimationFrame(() => {
            if (currentStep !== index) return;
            if (address && index === 1) address.refresh();
            if (reviewMapElement && index === reviewStep) refreshReviewMap();
        });

        if (focus) {
            const heading = steps[index].querySelector('h2');
            heading.focus({ preventScroll: true });
            const bounds = heading.getBoundingClientRect();
            if (bounds.top < 0 || bounds.bottom > window.innerHeight) {
                heading.scrollIntoView({ block: 'nearest', behavior: 'instant' });
            }
        }
    }


    function invalidField() {
        return fields.find(field => (field !== area && !field.value.trim()) || !field.checkValidity());
    }

    function showFieldError(field) {
        showStep(0, false);
        const label = field.labels?.[0]?.textContent.trim() || field.name;
        const error = field.value.trim()
            ? field.validationMessage
            : field.dataset.valRequired || 'Fyll ut eller velg en verdi.';
        const message = document.createElement('p');
        message.className = 'text-danger';
        message.setAttribute('role', 'alert');
        message.textContent = label + ': ' + error;
        fieldError.replaceChildren(message);
        field.focus();
    }

    function checkLocation() {
        if (!address) return true;
        if (address.pending()) {
            location.textContent = 'Finner nærmeste adresse …';
            return false;
        }
        if (address.valid() && (!area || area.value.trim())) return true;
        showStep(1);
        location.textContent = address.valid() && area
            ? 'Ingen adresse ble funnet. Gå tilbake og fyll ut området manuelt.'
            : 'Velg en adresse fra forslagene eller klikk på kartet.';
        location.classList.add('text-danger');
        return false;
    }

    function addReviewItem(label, value) {
        const term = document.createElement('dt');
        const detail = document.createElement('dd');
        term.textContent = label;
        detail.textContent = value;
        review.append(term, detail);
    }

    function advance() {
        if (currentStep === 0) {
            const invalid = invalidField();
            if (invalid) {
                showFieldError(invalid);
                return;
            }
            fieldError.replaceChildren();
            if (address) {
                showStep(1);
                return;
            }
        }
        if (!checkLocation()) return;
        review.replaceChildren();
        fields.forEach(field => {
            const value = field.type === 'datetime-local' && field.value
                ? new Intl.DateTimeFormat('nb-NO', { dateStyle: 'short', timeStyle: 'short', hour12: false })
                    .format(new Date(field.value))
                : field.selectedOptions?.[0]?.textContent || field.value;
            addReviewItem((field.labels?.[0]?.textContent || field.name).trim(), value.trim());
        });
        if (address) addReviewItem('Plassering', locationText());
        showStep(reviewStep);
    }

    fields.forEach(field => {
        field.addEventListener('input', () => fieldError.replaceChildren());
        field.addEventListener('change', () => fieldError.replaceChildren());
    });

    next.addEventListener('click', advance);
    back.addEventListener('click', () => showStep(currentStep - 1));
    form.addEventListener('invalid', event => {
        if (currentStep !== 0 && fields.includes(event.target)) {
            showFieldError(event.target);
        }
    }, true);


    form.addEventListener('submit', event => {
        if (currentStep !== reviewStep) {
            event.preventDefault();
            advance();
            return;
        }

        const invalid = invalidField();
        if (invalid) {
            event.preventDefault();
            showFieldError(invalid);
        } else if (!checkLocation()) {
            event.preventDefault();
        }
    });

    const locationError = address && steps[1].querySelector('.field-validation-error');
    const detailsError = steps[0].querySelector('.field-validation-error');
    showStep(locationError && !detailsError ? 1 : 0, false);
})();
