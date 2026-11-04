(() => {
    'use strict';

    function validPhone(value) {
        const digits = value.replace(/\D/g, '');
        return /^\+?[\d\s()-]+$/.test(value.trim()) && digits.length >= 8 && digits.length <= 15;
    }

    // Felles felt tilsvarer Resource. Kategorispesifikke felt må senere bindes
    // til en inputmodell. BrukerId settes alltid av serveren ved integrasjon.
    function readResource(form) {
        const data = new FormData(form);
        const text = name => String(data.get(name) ?? '').trim();
        const category = Number(text('CategoryId'));
        const kind = form.dataset.resourceKind || form.querySelector('[name="CategoryId"]')?.selectedOptions?.[0]?.dataset.resourceKind;
        return {
            Object: text('Object'),
            CategoryId: category,
            Available: text('Available') === 'true',
            Kontaktpunkt: text('Kontaktpunkt'),
            TilgjengeligFra: text('TilgjengeligFra') || null,
            Latitude: Number(text('Latitude')),
            Longitude: Number(text('Longitude')),
            Address: text('Address'),
            AdditionalInformation: text('AdditionalInformation'),
            Condition: text('Condition'),
            Vehicle: kind === 'vehicle' ? {
                RegistrationNumber: text('HasRegistrationNumber') === 'true' ? text('RegistrationNumber').toUpperCase() : null,
                RequiredLicense: text('RequiredLicense')
            } : null,
            Equipment: kind === 'equipment' ? {
                EquipmentType: text('EquipmentType'),
                Quantity: Number(text('Quantity'))
            } : null
        };
    }

    // Integrasjonen kan avbryte hendelsen med preventDefault(), utføre sitt API-kall
    // og deretter kalle complete() eller fail(melding). Standard er en lokal demo.
    function submitPreview(target, eventName, resource, complete, fail) {
        let settled = false;
        const finish = callback => value => {
            if (settled) return;
            settled = true;
            callback(value);
        };
        const success = finish(complete);
        const event = new CustomEvent(eventName, {
            bubbles: true,
            cancelable: true,
            detail: { resource, complete: success, fail: finish(fail) }
        });
        if (target.dispatchEvent(event)) success({ demo: true });
    }

    window.ParatusResources = { validPhone, readResource, submitPreview };
})();
