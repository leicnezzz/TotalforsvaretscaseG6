# **Oppgave 1** #
**Gruppenummer og studentnavn:**
Gruppe 6

Emma Moland Leiknes - emmaml@uia.no
Kenny Minh Bui - kennymb@uia.no
Lucas Fjeld - lucasf@uia.no
Maya Noor Asad - mayana@uia.no
Rebekka Boije, rebekkabo@uia.no
Marcia Kristine Olsen - marciako@uia.no

Kurskode og navn: IS-202-1 26H Programmerings prosjekt
Dato: 25.09.2026

## ** Systemarkitektur ** ##
Gruppen har brukt en monolittisk arkitektur, der alt er samlet og kjøres som et. Dette er for å gjøre det lettere å overvåke, utvikle, teste og skaper en god oversikt for å jobbe parallelt. Likevel er gruppen klar over sårbarheten, ved at hvis en ting ikke fungerer så går hele systemet ned. Webapplikasjonen er basert på ASP.net Core MVC (Model, view, controller), noe som gir en klar separasjon. Views er det som vises på nettsiden, brukergrensesnittet, og her har gruppen blant annet et interaktiv kart som var en obligatorisk del av oppgaven. Controller håndterer HTTP-forespørsler, styrer flyten i applikasjonen og kaller på forretningslogikken. For eksempel, Homecontroller med actions for Index, Privacy og Map. Gruppen har valgt en midlertidlig løsning for datatilgang med resourcehandler for å senere koble til en database. Den planlagte databasen bruker Entity Framework Core og er migrasjons basert. 

## ** Teknologistack ** ##
.Net / ASP.NET core MVC som web rammeverk
Entity Framework Core for databasetilgang
Leaflet.js for interaktiv kartfunksjonalitet
.Net Aspire styrer flyten mellom applikasjonens tjenester i utvikling og kjøremiljø

## ** Infrastruktur og drift ** ##
Applikasjon bruker containers med Docker, som er delt i 2 containers. En er for selv webapplikasjonen og den andre for databasen som bruker MySQL

## **Testing scenarier og resultater ** ##
Gruppen valgte å teste validering av navn, kartplassering og tilgjengelighet. Testene bekrefter at registreringer uten navn og plassering blir avvist, og at "Nei" godtas som et gyldig valg for tilgjengelighet.
En ressurs kan dermed registreres selv om den ikke er tilgjengelig, noe som gir grunnlag for videre arbeid med spesifikke tilgjengelighetstider. Alle tre unit-testene bestod.

## **Bruk av KI ** ##
Siden programmerings prosjektet bruker flere språk som er nye for gruppen (javascript, html, css) har vi brukt KI til å generere koden for disse spesifikke språkene og bedt om å forklare. Videre har gruppen brukt KI til å debugge, eventuell konflikter mellom Rider og Visual Studio Code. I tillegg har KI hjulpet med å anvende oppgaven riktig. Via prompt engineering ved å mate KI med informasjon om oppgaven, forelesnings presentasjoner og informasjon om applikasjonen som har blitt bygd så langt. 
