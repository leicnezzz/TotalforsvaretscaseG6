(() => {
    'use strict';

    const configuration = document.getElementById('resource-api');
    if (!configuration) return;

    const urls = configuration.dataset;

    async function post(url, body) {
        const tokenResponse = await fetch(urls.tokenUrl, {
            credentials: 'same-origin',
            cache: 'no-store',
            redirect: 'error'
        });

        if (!tokenResponse.ok) {
            throw new Error('Kunne ikke klargjøre forespørselen. Last siden på nytt.');
        } 

        const tokenData = await tokenResponse.json();

        const response = await fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            redirect: 'error',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'RequestVerificationToken': tokenData.token
            },
            body: JSON.stringify(body)
        });

        const result = await response.json().catch(() => ({}));

        if (!response.ok) {
            const validationErrors = Object.values(result.errors ?? {})
                .flat()
                .join(' ');

            throw new Error(
                (response.status === 401 ? 'Du må logge inn igjen før du fortsetter.' : '') ||
                (response.status === 403 ? 'Du har ikke tilgang til denne handlingen.' : '') ||
                result.message ||
                validationErrors ||
                'Forespørselen mislyktes. Kontroller at du har tilgang.'
            );
        }

        return result;
    } 

    function resourceUrl(baseUrl, resourceId) {
        const url = new URL(baseUrl, window.location.origin);
        url.searchParams.set('id', resourceId);
        return url;
    }

    for (const operation of ['create', 'update', 'delete']) {
        document.addEventListener('resource:' + operation, async event => {
            event.preventDefault();

            const { resource, complete, fail } = event.detail;

            try {
                let url = urls[operation + 'Url'];

                if (operation !== 'create') {
                    url = resourceUrl(url, resource.ResourceId);
                }

                const result = await post(url, resource);
                complete({ ...result, demo: false });
            } catch (error) {
                fail(
                    error instanceof TypeError
                        ? 'Kunne ikke kontakte serveren. Kontroller forbindelsen og at du fortsatt er innlogget.'
                        : error.message
                );
            }
        });
    }

    document.addEventListener('resource:account', async event => {
        event.preventDefault();

        const { mode, credentials, complete, fail } = event.detail;

        const input = mode === 'login'
            ? {
                Email: credentials.LoginEmail,
                Password: credentials.LoginPassword
            }
            : {
                FirstName: credentials.FirstName,
                LastName: credentials.LastName,
                Email: credentials.RegisterEmail,
                Password: credentials.RegisterPassword,
                ConfirmPassword: credentials.ConfirmPassword
            };

        try {
            const url = mode === 'login'
                ? urls.loginUrl
                : urls.registerUrl;

            await post(url, input);
            complete({ demo: false });
        } catch (error) {
            fail(
                error instanceof TypeError
                    ? 'Kunne ikke kontakte serveren. Prøv igjen.'
                    : error.message
            );
        }
    });
})();
// Sender opplysninger og ressursdata til serveren for ressurshåndtering, innlogging og registrering.
// Henter sikkerhetstoken før sending og gir melding om handlingen lyktes eller feiles. 