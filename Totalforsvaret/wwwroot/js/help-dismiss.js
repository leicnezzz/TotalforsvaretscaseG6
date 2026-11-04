document.addEventListener('click', event => {
    const openHelp = document.querySelectorAll(
        'details.public-help[open], details.card-help[open], details.registration-help[open]'
    );

    for (const help of openHelp) {
        // La nettleseren lukke hjelpen selv ved klikk på spørsmålstegnet.
        if (!help.querySelector('summary')?.contains(event.target)) {
            help.open = false;
        }
    }
}, { capture: true });
