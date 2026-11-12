window.kobleBehovsliste = function (skjema, oppdaterFelt) {
    const behovListe = skjema.querySelector('#behov-liste');
    const leggTilKnapp = skjema.querySelector('#legg-til-behov');
    function oppdaterValidering() {
        const jquery = window.jQuery;
        if (!jquery?.validator?.unobtrusive) return;

        jquery(skjema).removeData('validator').removeData('unobtrusiveValidation');
        jquery.validator.unobtrusive.parse(skjema);
    }

    function oppdaterFjernKnapper() {
        const bareEttBehov =
            behovListe.querySelectorAll('fieldset').length <= 1;

        behovListe.querySelectorAll('.fjern-behov').forEach(knapp => {
            knapp.hidden = bareEttBehov;
        });
    }

    oppdaterFjernKnapper();
    behovListe.addEventListener('click', e => {
        const knapp = e.target.closest('.fjern-behov');

        if (!knapp) return;

        const behov = behovListe.querySelectorAll('fieldset');

        // Behold alltid minst ett behov.
        if (behov.length <= 1) return;

        knapp.closest('fieldset').remove();
        oppdaterFjernKnapper();

        // Oppdater nummer og feltnavn etter sletting.
        behovListe.querySelectorAll('fieldset').forEach((blokk, indeks) => {
            blokk.querySelector('legend').textContent =
                `Behov ${indeks + 1}`;

            blokk.querySelectorAll('*').forEach(element => {
                ['name', 'id', 'for', 'data-valmsg-for'].forEach(attributt => {
                    const verdi = element.getAttribute(attributt);

                    if (verdi) {
                        element.setAttribute(
                            attributt,
                            verdi
                                .replace(/Needs\[\d+\]/g, `Needs[${indeks}]`)
                                .replace(/Needs_\d+__/g, `Needs_${indeks}__`)
                        );
                    }
                });
            });
        });

        oppdaterValidering();
    });

    leggTilKnapp.addEventListener('click', () => {
        const indeks = behovListe.querySelectorAll('fieldset').length;
        const nyttBehov = behovListe
            .querySelector('fieldset')
            .cloneNode(true);

        nyttBehov.querySelector('legend').textContent =
            `Behov ${indeks + 1}`;

        nyttBehov.querySelectorAll('*').forEach(element => {
            ['name', 'id', 'for', 'data-valmsg-for'].forEach(attributt => {
                const verdi = element.getAttribute(attributt);

                if (verdi) {
                    element.setAttribute(
                        attributt,
                        verdi
                            .replace(/Needs\[\d+\]/g, `Needs[${indeks}]`)
                            .replace(/Needs_\d+__/g, `Needs_${indeks}__`)
                    );
                }
            });
        });

        nyttBehov.querySelectorAll('input, textarea').forEach(felt => {
            felt.value = '';
            felt.classList.remove('input-validation-error');
        });

        nyttBehov.querySelectorAll('select').forEach(felt => {
            felt.selectedIndex = 0;
            felt.classList.remove('input-validation-error');
        });

        nyttBehov.querySelectorAll('[data-valmsg-for]').forEach(melding => {
            melding.textContent = '';
            melding.classList.remove('field-validation-error');
            melding.classList.add('field-validation-valid');
        });

        oppdaterFelt?.(nyttBehov);
        behovListe.appendChild(nyttBehov);
        oppdaterFjernKnapper();

        oppdaterValidering();

        nyttBehov.querySelector('input').focus();
    });
};
