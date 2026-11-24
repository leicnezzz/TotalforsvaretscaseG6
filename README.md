# **Gruppenummer og studentnavn:**
Gruppe 6

Emma Moland Leiknes - emmaml@uia.no,
Kenny Minh Bui - kennymb@uia.no,
Lucas Fjeld - lucasf@uia.no,
Maya Noor Asad - mayana@uia.no,
Rebekka Boije - rebekkabo@uia.no,
Marcia Kristine Olsen - marciako@uia.no,

Kurskode og navn: IS-202-1 26H Programmeringsprosjekt

Dato: HUSK OG ENDRE MEG

## **Systemarkitektur, oppstart og testing** ##
Paratus er en ASP.NET Core MVC-applikasjon. Applikasjonen er delt inn i Controllere som håndterer forespørsler, Razor-visninger som viser grensesnittet, og Entity Framework Core til å lagre data i MariaDB. ASP.NET Core Identity håndterer alt av konto og innlogging. Det er brukertypen som styrer tilgangen til applikasjonen, så private aktører kan administrere egne ressurser, og offentlige aktører har skjemaet for kriser og behov. Systemet bruker Leaflet som kartfunksjon.

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

## **Bruk av applikasjonen** ##
Som privat aktør kan du opprette en konto og logge inn. Gå til ressursskjemaet for å registrere en ressurs. Fyll inn opplysningene, velg et punkt på kartet og lagre. I ressursoversikten kan du finne, endre eller slette egne ressurser.

Offentlige aktører har egne skjemaer og oversikter for kriser og behov via en egen innlogging.

## **Gjennomførte tester** ##
Gruppen valgte å teste validering av navn, kartplassering og tilgjengelighet. Testene bekrefter at registreringer uten navn og plassering blir avvist, og at "Nei" godtas som et gyldig valg for tilgjengelighet. En ressurs kan dermed registreres selv om den ikke er tilgjengelig, noe som gir grunnlag for videre arbeid med spesifikke tilgjengelighetstider.

- **Enhetstesting.** Vi kjørte `dotnet test Totalforsvaret.slnx`. 51 tester bestod, og 0 feilet. Disse testene dekker blant annet registrering, innlogging, brukertype og ressursvalidering. I denne kjøringen hadde testprosjektet for offentlig aktør ingen kjørbare tester.
- **Docker og database.** Docker-image ble bygget uten feil i Webbappen, og alle EF Core-migrasjonene ble kjørt mot en tom MariaDB 11.4-container. Oppstart.md viser også til at begge containere startet, pluss at innlogging fungerte i en tidligere Windows-kjøring.
- **Sikkerhetsrelatert enhetstesting.** Testene har også dekket brukertype og innloggingsfunksjoner som ber Identity om å telle mislykkede forsøk for kontolåsing.
- **Brukervennlighetstesting.** Vi har foreløpig ingen resultater fra brukervennlighetstesting.

## **Teknologistack** ##
- .Net / ASP.NET core MVC som web rammeverk
- Entity Framework Core for databasetilgang
- Leaflet.js for interaktiv kartfunksjonalitet
- .Net Aspire styrer flyten mellom applikasjonens tjenester i utvikling og kjøremiljø

## **Bruk av KI** ##
Vi brukte KI i planleggingsfasen for å få en bedre forståelse av hvordan vi skulle gjennomføre prosjektet slik at det møtte alle kravene. Det ble ikke brukt som en erstatning av eget arbeid, men heller som et verktøy for å oppnå forståelse i hvordan man utfører de ulike arbeidskravene. Vi fikk også forslag til HTML, CSS og JavaScript koder. Under utviklingen av prosjektet jobbet vi i lag med KI for å få svar på hvordan koden fungerte og eventuelle feilmeldinger. Vi hadde flere tilfeller der koden ikke ville kjøre, om det var lokalt etter en oppdatering fra main, eller når vi jobbet med sammenslåing av grener. Videre har gruppen brukt KI til å debugge, eventuell konflikter mellom Rider og Visual Studio Code. Vi brukte Codex for å analysere problemene og komme med løsningsforslag, og ChatGPT for å finne feil i HTTPS-oppsetter i Docker. Det ble også brukt Codex som hjelpemiddel for å beskrive i detalj i README hvordan man kjører systemet med docker.

## **Eksempel på KI prompt** ##
"Jeg får ikke programmet til å kjøre lokalt etter jeg pullet og fetchet fra main. Hvordan skal jeg fikse dette?"