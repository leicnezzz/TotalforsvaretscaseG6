(() => {
    'use strict';
    const form = document.getElementById('resource-wizard');
    if (!form) return;

    const field = name => form.elements.namedItem(name);
    const step = name => form.querySelector('[data-step="' + name + '"]');
    const category = field('CategoryId');
    const kind = () => category.selectedOptions[0]?.dataset.resourceKind;
    const compact = form.dataset.compact === 'true';
    let authenticated = compact || form.dataset.authenticated === 'true';
    const compactBack = form.querySelector('[data-compact-back]');
    const message = document.getElementById('wizard-message');
    const submit = document.getElementById('vehicle-submit');
    const draftKey = 'paratus.resource-draft.v2';
    const fields = ['Object', 'Condition', 'RequiredLicense', 'RegistrationNumber', 'EquipmentType',
        'Quantity', 'AdditionalInformation', 'Kontaktpunkt', 'Available', 'TilgjengeligFra',
        'Address', 'Latitude', 'Longitude'];
    let stage = 0;
    let accountMode = 'login';
    let accountReady = authenticated;
    let manualVehicleEdit = false;
    let completed = false;
    let busy = false;
    let lastCategory = category.value;
    const address = window.ParatusResourceAddress.attach(step('location'), () => {
        stage = Math.min(stage, 3);
        field('Confirmed').checked = false;
        render();
        saveDraft();
    });
// Gjenopprettet et lagret utkast som viser melding hvis det ikke kan åpnes.
    function setBusy(value) {
        busy = value;
        form.inert = value;
        form.setAttribute('aria-busy', String(value));
    }

    function showMessage(text) {
        message.textContent = text;
        message.hidden = !text;
    }

    function showStep(name, visible) {
        const element = step(name);
        element.hidden = !visible;
        element.disabled = !visible;
    }

    function clearVehicleCheck() {
        form.querySelectorAll('[name="VehicleInfoCorrect"]').forEach(radio => { radio.checked = false; });
    }

    function refreshValidity() {
        for (const name of ['Object', 'Condition']) {
            field(name).setCustomValidity(field(name).value.trim() ? '' : 'Fyll ut dette feltet.');
        }
        const phone = field('Kontaktpunkt');
        phone.setCustomValidity(window.ParatusResources.validPhone(phone.value)
            ? '' : 'Skriv et telefonnummer med 8–15 sifre, eventuelt med landskode.');
        const error = document.getElementById('vehicle-contact-error');
        error.hidden = !phone.value || phone.validity.valid;
        error.textContent = phone.validationMessage;
        field('Address').setCustomValidity(address.valid() ? '' : 'Velg en adresse fra forslagene.');
        const password = field('PreviewRegisterPassword');
        password.setCustomValidity(password.value && (!/\p{L}/u.test(password.value) || !/\d/.test(password.value))
            ? 'Passordet må ha bokstaver og tall.' : '');
        field('PreviewConfirmPassword').setCustomValidity(
            field('PreviewConfirmPassword').value !== password.value ? 'Passordene må være like.' : '');
    }

    function validate(container) {
        refreshValidity();
        for (const element of container.querySelectorAll('input, select, textarea')) {
            if (element.willValidate && !element.reportValidity()) return false;
        }
        return true;
    }
    // Validerer feltene i skjemaet og viser feilmeldinger for manglende eller ugyldige verdier. 
    function renderAccount() {
        document.getElementById('wizard-account-choice').hidden = accountReady;
        document.getElementById('wizard-account-complete').hidden = !accountReady;
        for (const button of form.querySelectorAll('[data-account-mode]')) {
            button.setAttribute('aria-pressed', String(button.dataset.accountMode === accountMode));
        }
        for (const panel of form.querySelectorAll('[data-account-panel]')) {
            panel.hidden = accountReady || panel.dataset.accountPanel !== accountMode;
            panel.disabled = panel.hidden || step('account').hidden;
        }
    }

    function buildSummary() {
        const data = window.ParatusResources.readResource(form);
        const rows = [
            ['Ressurstype', category.selectedOptions[0]?.textContent],
            ['Navn / modell', data.Object],
            ['Tilstand', data.Condition]
        ];
        if (data.Vehicle) {
            rows.push(['Registreringsnummer', data.Vehicle.RegistrationNumber || 'Uten registreringsnummer'],
                ['Førerkortklasse', data.Vehicle.RequiredLicense || 'Ikke oppgitt']);
        }
        if (data.Equipment) rows.push(['Utstyrstype', data.Equipment.EquipmentType], ['Antall', String(data.Equipment.Quantity)]);
        if (compact) {
            rows.push(['Kontaktperson', [data.ContactFirstName, data.ContactLastName].join(' ')],
                ['E-post', data.ContactEmail]);
        }
        rows.push(['Tilleggsinformasjon', data.AdditionalInformation || 'Ingen'],
            ['Telefon', data.Kontaktpunkt], ['Tilgjengelig', data.Available ? 'Ja' : 'Ikke akkurat nå'],
            ['Tilgjengelig fra', data.TilgjengeligFra?.replace('T', ' ') || (data.Available ? 'Fra registreringstidspunktet' : 'Ikke oppgitt')],
            ['Adresse', data.Address]);
        const summary = document.getElementById('vehicle-summary');
        summary.replaceChildren();
        for (const [label, value] of rows) {
            const term = document.createElement('dt');
            const description = document.createElement('dd');
            term.textContent = label;
            description.textContent = value;
            summary.append(term, description);
        }
    }
// Oppsummerer ressursinformasjon og vises i sammendrag før innsending. 
    function render() {
        const vehicle = kind() === 'vehicle';
        const equipment = kind() === 'equipment';
        const supported = (vehicle || equipment) && !completed;
        form.querySelector('.wizard-step').hidden = completed;
        const numbered = field('HasRegistrationNumber').value === 'true';
        showStep('registration-choice', supported && vehicle);
        showStep('registration-number', supported && vehicle && numbered);
        showStep('resource-details', supported && (equipment || Boolean(field('HasRegistrationNumber').value)));
        showStep('additional', supported && (compact || stage >= 1));
        showStep('availability', supported && stage >= 2);
        showStep('location', supported && stage >= 3);
        if (compact) {
            form.dataset.phase = completed ? 'done' : stage >= 4 ? 'review' : stage === 3 ? 'map' : stage === 2 ? 'contact' : 'details';
            compactBack.hidden = completed || stage < 2;
        }
        showStep('account', supported && stage >= 4 && !authenticated);
        showStep('confirmation', supported && stage >= 4 && accountReady);
        const unsupported = document.getElementById('wizard-category-note');
        unsupported.hidden = !category.value || supported || completed;
        for (const [selector, visible] of [['[data-vehicle-fields]', vehicle], ['[data-equipment-fields]', equipment]]) {
            const group = form.querySelector(selector);
            group.hidden = !visible;
            group.querySelectorAll('input, select').forEach(element => { element.disabled = !visible; });
        }
        const demo = vehicle && numbered && field('RegistrationNumber').value.replace(/[ -]/g, '').toUpperCase() === 'AB12345';
        field('Object').readOnly = demo && !manualVehicleEdit;
        field('RequiredLicense').readOnly = demo && !manualVehicleEdit;
        document.getElementById('vehicle-lookup-status').textContent = demo
            ? 'Eksempeldata er fylt inn. Kontroller opplysningene og oppgi tilstanden.'
            : 'Kjøretøyoppslag kobles til senere. Fyll inn manuelt, eller prøv AB12345 for demodata.';
        const check = document.getElementById('vehicle-information-check');
        check.hidden = !vehicle || !numbered || step('resource-details').hidden;
        check.querySelectorAll('input').forEach(radio => {
            radio.disabled = check.hidden;
            radio.required = !check.hidden;
        });
        document.getElementById('resource-details-title').textContent = equipment ? 'Utstyrsinformasjon' : 'Kjøretøyinformasjon';
        document.getElementById('resource-details-description').textContent = equipment
            ? 'Oppgi navn, utstyrstype, antall og tilstand. Beskriv gjerne kapasitet eller nødvendig opplæring i neste steg.'
            : 'Oppgi modell, tilstand og eventuell førerkortklasse. Kontroller opplysningene før du går videre.';
        document.getElementById('resource-name-label').textContent = equipment ? 'Navn / modell *' : 'Modell / serie *';
        field('Object').placeholder = equipment ? 'Eks. DJI Mavic 4 eller Honda aggregat' : 'Eks. Toyota Hiace';
        document.getElementById('vehicle-additional-count').textContent = field('AdditionalInformation').value.length;
        document.getElementById('wizard-progress').textContent = completed ? 'Registreringen er fullført.'
            : !supported ? 'Velg en ressurstype for å begynne.'
            : compact ? (stage >= 4 ? 'Kontroller og bekreft' : stage === 3 ? 'Velg plassering' : stage === 2 ? 'Kontakt og tilgjengelighet' : 'Fyll ut ressursopplysningene')
            : ['1 av 5 · Ressursinformasjon', '2 av 5 · Ytterligere informasjon', '3 av 5 · Kontakt og tilgjengelighet',
                '4 av 5 · Plassering', '5 av 5 · Kontroller og bekreft'][stage];
        form.querySelectorAll('.wizard-step:not(.wizard-step-confirm)').forEach(element => {
            element.classList.remove('wizard-step-white', 'wizard-step-soft', 'wizard-step-blue');
        });
        const colors = ['wizard-step-white', 'wizard-step-soft', 'wizard-step-blue'];
        [...form.querySelectorAll('.wizard-step:not(.wizard-step-confirm)')].filter(element => !element.hidden)
            .forEach((element, index) => element.classList.add(colors[index % colors.length]));
        renderAccount();
        refreshValidity();
        submit.disabled = busy || completed || stage < 4 || !accountReady || !field('Confirmed').checked;
        if (stage >= 4) buildSummary();
        if (!step('location').hidden && (!compact || stage === 3)) requestAnimationFrame(address.refresh);
    }
    
    function moveTo(name) {
        render();
        saveDraft();
        const heading = step(name).querySelector('h3');
        heading.tabIndex = -1;
        heading.focus({ preventScroll: true });
        if (compact) {
            if (form.getBoundingClientRect().top < 0) {
                form.scrollIntoView({ block: 'start', behavior: 'instant' });
            }
            return;
        }

        step(name).scrollIntoView({
            block: 'start', behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth'
        });
    }

    function saveDraft() {
        if (completed) return;
        const values = {};
        for (const name of fields) values[name] = field(name).value;
        try {
            sessionStorage.setItem(draftKey, JSON.stringify({
                version: 2, updated: Date.now(), category: category.value, kind: kind(), stage,
                hasNumber: field('HasRegistrationNumber').value,
                vehicleInfoCorrect: field('VehicleInfoCorrect').value, manualVehicleEdit, values
            }));
        } catch {
            showMessage('Utkastet kan ikke bevares i denne fanen. Hold siden åpen mens du fyller ut.');
        }
    }

    function restoreDraft() {
        try {
            const current = sessionStorage.getItem(draftKey);
            const legacy = current ? null : sessionStorage.getItem('paratus.vehicle-draft.v1');
            const raw = current || legacy;
            if (!raw || raw.length > 20000) return;
            const draft = JSON.parse(raw);
            if (![1, 2].includes(draft.version) || !Number.isFinite(draft.updated) ||
                Date.now() - draft.updated > 86400000 || !draft.values) return;
            const values = draft.values;
            if (draft.version === 1) {
                values.Object = values.Model;
                values.Kontaktpunkt = values.Contact;
                values.TilgjengeligFra = values.AvailableFrom;
            }
            for (const name of fields) {
                if (typeof values[name] === 'string') field(name).value = values[name];
            }
            const savedKind = draft.kind || (draft.version === 1
                ? ({ '2': 'vehicle', '3': 'equipment' })[draft.category]
                : (values.EquipmentType ? 'equipment' : 'vehicle'));
            const restoredCategory = [...category.options].find(option => option.dataset.resourceKind === savedKind);
            if (restoredCategory) category.value = restoredCategory.value;
            for (const radio of form.querySelectorAll('[name="HasRegistrationNumber"]')) radio.checked = radio.value === draft.hasNumber;
            for (const radio of form.querySelectorAll('[name="VehicleInfoCorrect"]')) radio.checked = radio.value === draft.vehicleInfoCorrect;
            manualVehicleEdit = draft.manualVehicleEdit === true;
            // Tidligere felt gjenopprettes, men valideres på nytt før neste steg.
            stage = 0;
            lastCategory = category.value;
            showMessage('Utkastet ditt er gjenopprettet. Kontroller opplysningene og fortsett.');
        } catch {
            showMessage('Utkastet kunne ikke åpnes. Du kan starte en ny registrering.');
        }
    } // Gjenoppretter utkast fra forrige registrering hvis det finnes, og viser feilmeling hvis utkastet er ugyldig. 

    form.addEventListener('input', event => {
        if (event.target.name.startsWith('Preview')) {
            refreshValidity();
            return;
        }
        const name = event.target.name;
        field('Confirmed').checked = name === 'Confirmed' && field('Confirmed').checked;
        if (['Object', 'Condition', 'RequiredLicense', 'EquipmentType', 'Quantity', 'RegistrationNumber', 'VehicleInfoCorrect', 'HasRegistrationNumber'].includes(name)) stage = 0;
        if (['Kontaktpunkt', 'Available', 'TilgjengeligFra'].includes(name)) stage = Math.min(stage, 2);
        if (name === 'RegistrationNumber') {
            const demo = field('RegistrationNumber').value.replace(/[ -]/g, '').toUpperCase() === 'AB12345';
            const previousDemo = field('Object').value === 'Toyota Hiace (demodata)';
            if (demo) {
                field('Object').value = 'Toyota Hiace (demodata)';
                field('RequiredLicense').value = 'B';
            } else if (previousDemo) {
                field('Object').value = '';
                field('RequiredLicense').value = '';
            }
            manualVehicleEdit = false;
            clearVehicleCheck();
        }
        if (['Object', 'RequiredLicense'].includes(name)) clearVehicleCheck();
        if (name === 'VehicleInfoCorrect' && event.target.value === 'false') manualVehicleEdit = true;
        render();
        saveDraft();
    });

    category.addEventListener('change', () => {
        if (lastCategory !== category.value) {
            // Behold felles kontakt/adresse, men unngå data fra feil kategori.
            for (const name of ['Object', 'Condition', 'RequiredLicense', 'RegistrationNumber', 'EquipmentType', 'AdditionalInformation']) field(name).value = '';
            field('Quantity').value = '1';
            form.querySelectorAll('[name="HasRegistrationNumber"]').forEach(radio => { radio.checked = false; });
            clearVehicleCheck();
            manualVehicleEdit = false;
            stage = 0;
            lastCategory = category.value;
        }
        render();
        saveDraft();
    }); // Oppdaterer skjemaet når brukeren velger en ny kategori, og nullstiller feltene som ikke gjelder for ny kategori. 

    field('HasRegistrationNumber').forEach(radio => radio.addEventListener('change', () => {
        if (radio.checked && radio.value === 'false') {
            if (field('Object').value === 'Toyota Hiace (demodata)' && !manualVehicleEdit) {
                field('Object').value = '';
                field('RequiredLicense').value = '';
            }
            field('RegistrationNumber').value = '';
            manualVehicleEdit = true;
            clearVehicleCheck();
        }
        render();
        saveDraft();
    }));

    function validateDetails() {
        if (kind() === 'vehicle') {
            if (!validate(step('registration-choice'))) return false;
            if (field('HasRegistrationNumber').value === 'true' && !validate(step('registration-number'))) return false;
        }
        if (!validate(step('resource-details'))) return false;
        if (kind() === 'vehicle' && field('HasRegistrationNumber').value === 'true' && field('VehicleInfoCorrect').value !== 'true') {
            showMessage('Rett opplysningene og velg «Ja, informasjonen stemmer» før du fortsetter.');
            return false;
        }
        showMessage('');
        return true;
    }

    form.querySelector('[data-continue="details"]').addEventListener('click', () => {
        if (!validateDetails()) return;
        stage = 1;
        moveTo('additional');
    });
    document.getElementById('wizard-additional-next').addEventListener('click', () => {
        if (compact && !validateDetails()) return;
        if (!validate(step('additional'))) return;
        stage = 2;
        moveTo('availability');
    });
    document.getElementById('wizard-contact-next').addEventListener('click', () => {
        if (!validate(step('availability'))) return;
        stage = 3;
        moveTo('location');
    });
    form.querySelector('[data-continue="location"]').addEventListener('click', () => {
        if (!validate(step('location')) || !address.valid()) return;
        stage = 4;
        moveTo(accountReady ? 'confirmation' : 'account');
    });

    for (const button of form.querySelectorAll('[data-account-mode]')) {
        button.addEventListener('click', () => {
            accountMode = button.dataset.accountMode;
            form.querySelectorAll('.wizard-auth-panel input[type="password"]').forEach(element => { element.value = ''; });
            render();
        });
    }
    for (const button of form.querySelectorAll('[data-account-continue]')) {
        button.addEventListener('click', () => {
            const panel = form.querySelector('[data-account-panel="' + accountMode + '"]');
            if (!validate(panel) || busy) return;
            setBusy(true);
            button.disabled = true;
            const credentials = {};
            panel.querySelectorAll('input').forEach(element => { credentials[element.name.replace('Preview', '')] = element.value; });
            // Egen integrasjonshendelse. Passord inngår aldri i ressursdata eller utkast.
            const complete = (result = { demo: true }) => {
                setBusy(false);
                button.disabled = false;
                accountReady = true;
                if (!result.demo) authenticated = true;
                for (const key of Object.keys(credentials)) delete credentials[key];
                form.querySelectorAll('.wizard-auth-panel input[type="password"]').forEach(element => { element.value = ''; });
                document.getElementById('wizard-account-complete-text').textContent = result.demo
                    ? 'Kontosteget er gjennomgått som demo. Kontroller ressursen nedenfor.'
                    : 'Kontoen er klar. Kontroller ressursen nedenfor.';
                moveTo('confirmation');
            };
            const fail = text => {
                setBusy(false);
                button.disabled = false;
                for (const key of Object.keys(credentials)) delete credentials[key];
                showMessage(text || 'Kunne ikke fullføre kontosteget. Prøv igjen.');
            };
            const event = new CustomEvent('resource:account', {
                bubbles: true, cancelable: true, detail: { mode: accountMode, credentials, complete, fail }
            });
            if (form.dispatchEvent(event)) fail('Kontotjenesten kunne ikke lastes. Last siden på nytt.');
        });
    }
    compactBack?.addEventListener('click', () => {
        field('Confirmed').checked = false;
        if (stage === 4) {
            stage = 3;
            moveTo('location');
        } else if (stage === 3) {
            stage = 2;
            moveTo('availability');
        } else {
            stage = 0;
            render();
            saveDraft();
            category.focus({ preventScroll: true });
            if (form.getBoundingClientRect().top < 0) {
                form.scrollIntoView({ block: 'start', behavior: 'instant' });
            }
        }
    });

    document.getElementById('wizard-account-edit').addEventListener('click', () => {
        accountReady = false;
        field('Confirmed').checked = false;
        moveTo('account');
    });

    form.addEventListener('keydown', event => {
        const panel = event.target.closest('[data-account-panel]');
        if (event.key === 'Enter' && panel) {
            event.preventDefault();
            panel.querySelector('[data-account-continue]').click();
        }
    });

    form.addEventListener('submit', event => {
        event.preventDefault();
        // Enter i et vanlig felt skal ikke hoppe over de trinnvise knappene.
        if (busy || completed || stage !== 4 || !accountReady || !field('Confirmed').checked) return;
        if (!validate(form) || !address.valid()) return;
        setBusy(true);
        render();
        const resource = window.ParatusResources.readResource(form);
        window.ParatusResources.submitPreview(form, 'resource:create', resource, (result = { demo: true }) => {
            setBusy(false);
            completed = true;
            try {
                sessionStorage.removeItem(draftKey);
                sessionStorage.removeItem('paratus.vehicle-draft.v1');
            } catch { /* Registreringen kan avsluttes selv om nettleserlagring er sperret. */ }
            render();
            document.getElementById('wizard-success-message').textContent = result.demo
                ? 'Forhåndsvisningen er fullført. Ressursen er ikke lagret. Backend kobles til senere.'
                : 'Ressursen er registrert.';
            const success = document.getElementById('wizard-success');
            success.querySelector('h3').textContent = 'Ressursen er registrert';
            success.hidden = false;
            success.focus();
        }, text => {
            setBusy(false);
            showMessage(text || 'Registreringen kunne ikke fullføres. Opplysningene dine er beholdt.');
            render();
        });
    });
// Sender ressursdata til serveren for registrering, og gir melding om handlingen lykkes eller feiles. 
    function reset() {
        if (busy) return;
        form.reset();
        stage = 0;
        completed = false;
        accountReady = authenticated;
        accountMode = 'login';
        manualVehicleEdit = false;
        lastCategory = '';
        for (const name of fields) field(name).setCustomValidity('');
        address.reset();
        document.getElementById('wizard-success').hidden = true;
        showMessage('');
        try {
            sessionStorage.removeItem(draftKey);
            sessionStorage.removeItem('paratus.vehicle-draft.v1');
        } catch { /* Brukeren kan fortsatt starte på nytt. */ }
        render();
        category.focus();
    }
    document.getElementById('wizard-new').addEventListener('click', reset);
    document.getElementById('wizard-clear').addEventListener('click', () => {
        if (window.confirm('Tømme utkastet og begynne på nytt?')) reset();
    });
    restoreDraft();
    render();
})();
