# **Gruppenummer og studentnavn:**
Gruppe 6

Emma Moland Leiknes - emmaml@uia.no,
Kenny Minh Bui - kennymb@uia.no,
Lucas Fjeld - lucasf@uia.no,
Maya Noor Asad - mayana@uia.no,
Rebekka Boije - rebekkabo@uia.no,
Marcia Kristine Olsen - marciako@uia.no,

Kurskode og navn: IS-202-1 26H Programmeringsprosjekt

## Systemarkitektur

Paratus bruker ASP.NET Core MVC. Kontrollerne håndterer forespørsler, Razor-visningene viser skjema og oversikter, og tjenestene håndterer blant annet ressursmatching og tildeling. Repository-laget i `DataAccess` bruker EF Core til å lese og lagre data i MariaDB. Leaflet viser kartet, og Bootstrap brukes i grensesnittet.

ASP.NET Core Identity håndterer kontoer og innlogging. Rollepolicyer skiller private og offentlige aktører, og serveren sjekker eierskap ved handlinger på egne ressurser. POST-skjema beskyttes med antiforgery-token. EF Core og parameterisert SQL håndterer databaseverdier, og Razor og kartets tekstfelter viser brukerinnhold som tekst.

## Oppstart med Docker og Aspire

Installer .NET 10 SDK, Docker Desktop og Visual Studio eller Rider med støtte for .NET 10 og Aspire.

1. Klon repoet og åpne `Totalforsvaret.slnx`.
2. Start Docker Desktop med Linux-containere.
3. Gjennomfør [HTTPS-engangsoppsettet i Oppstart.md](Oppstart.md#2-klargjør-https-én-gang-per-maskin).
4. Start `Totalforsvaret.AppHost` med profilen `https`. I Rider brukes en Aspire Host-kjørekonfigurasjon.
5. Vent til MariaDB og webappen har startet. Åpne HTTPS-lenken for `totalforsvaret` i Aspire-dashboardet.

Databasepassord, tilkobling og opprettelse av tabeller håndteres automatisk. Docker-volumet bevarer data mellom stopp og oppstart.

**Offentlig demokonto**

- E-post `offentlig@example.com`
- Passord `Passordet12345`

Velg innlogging for offentlig aktør. Privat konto opprettes gjennom «Opprett bruker».

Se [Oppstart.md](Oppstart.md) for alle kommandoer, Rider-oppsett, lagring og feilsøking.

## Bruk av applikasjonen

1. Opprett en privat konto. Registrer en tilgjengelig ressurs, velg plassering på kartet og lagre.
2. Logg inn med offentlig demokonto. Opprett et behov med en type som matcher ressursen, for eksempel transport for et kjøretøy.
3. Finn treffet i den offentlige oversikten og velg **Vurder ressurs** for å sende forespørselen.
4. Logg inn som den private ressurseieren. Åpne forespørselen i **Innboks** og velg **Godta**. Sjekk at statusen blir **Tildelt**.
5. Når ressursen er levert tilbake, åpner ressurseieren den samme samtalen i **Innboks** og velger **Frigi ressurs**. Tildelingen vises deretter som **Frigitt** i historikken.

Ressurseieren bekrefter at ressursen er tilbake. En offentlig aktør kan følge statusen i den offentlige innboksen. Offentlig eide ressurser frigis også av eieren, fra offentlig innboks.

Den offentlige innboksen er en felles arbeidsflate for beredskapskoordinering. Offentlige aktører kan lese og svare i alle tildelingstråder. Private aktører kan bare lese og svare i tråder knyttet til egne ressurser. Godkjenning, avslag og frigivelse krever eierskap til ressursen.

## Gjennomførte tester

- **XSS-sjekk i ressursoversikten.** Vi registrerte en ressurs med navnet `<b>Karttest</b>`. Oversikten viste HTML-taggene som vanlig tekst.
- **Lagring av spesialtegn.** Vi registrerte en ressurs med navnet `O'Brien transport` og bekreftet i ressursoversikten at apostrofen ble bevart. Sjekken bekrefter at lagringen håndterer apostrofer.
- **Automatiserte tester.** Kjør `dotnet test Totalforsvaret.Tests/Totalforsvaret.Tests.csproj` for å teste blant annet validering, innlogging, roller og tildelingsregler. GitHub Actions kjører også testprosjektet. Siste lokale kjøring ga 127 beståtte tester. Tilgangstesten sjekker at offentlige aktører deler trådene, og at private aktører bare får tilgang til egne tråder. Tjenestetestene sjekker blant annet at eieren kan frigi en tildelt ressurs, og at andre brukere avvises.
- **MariaDB 11.4.** Vi sjekket statusendringer fra separate databasekontekster i en isolert Docker-container. Et sent avslag overskrev ingen godkjenning. Da et behov ble avsluttet, ble ventende forespørsler avbrutt, og ressursen kunne forespørres på nytt.
- **Docker-oppstart.** Vi bygde webappens image, startet webapp og MariaDB og kjørte migrasjonene mot en tom database. Begge containere startet, og innlogging fungerte i en tidligere Windows-kjøring.

## Oppstartflyt fra tom database 
Gruppen startet appen og MariaDB via Aspir og testet tildelingsflyten over HTTPS. I nettleseren bekreftet vi at den gamle tildelingen var frigitt, og at en ny forespøsel for samme ressurs sto som "Venter på svar."

En samlet gjennomkjøring i nettleseren med begge rollene, fra registrering til frigivelse, er foreløpig ikke dokumentert. Brukervennlighetstesting og oppstart på en ren maskin med Mac/Rider er foreløpig ikke verifisert.

## **Teknologistack** ##
- .Net / ASP.NET core MVC som web rammeverk
- Entity Framework Core for databasetilgang
- Leaflet.js for interaktiv kartfunksjonalitet
- .Net Aspire styrer flyten mellom applikasjonens tjenester i utvikling og kjøremiljø

## **Bruk av KI** ##
Vi brukte KI i planleggingsfasen for å få en bedre forståelse av hvordan vi skulle gjennomføre prosjektet slik at det møtte alle kravene. Det ble ikke brukt som en erstatning av eget arbeid, men heller som et verktøy for å oppnå forståelse i hvordan man utfører de ulike arbeidskravene. Vi fikk også forslag til HTML, CSS og JavaScript koder. Under utviklingen av prosjektet jobbet vi i lag med KI for å få svar på hvordan koden fungerte og eventuelle feilmeldinger. Vi hadde flere tilfeller der koden ikke ville kjøre, om det var lokalt etter en oppdatering fra main, eller når vi jobbet med sammenslåing av grener. Videre har gruppen brukt KI til å debugge, eventuell konflikter mellom Rider og Visual Studio Code. Vi brukte Codex for å analysere problemene og komme med løsningsforslag, og ChatGPT for å finne feil i HTTPS-oppsetter i Docker. Det ble også brukt Codex som hjelpemiddel for å beskrive i detalj i README hvordan man kjører systemet med docker.

## **Eksempel på KI prompt** ##
"Jeg får ikke programmet til å kjøre lokalt etter jeg pullet og fetchet fra main. Hvordan skal jeg fikse dette?"