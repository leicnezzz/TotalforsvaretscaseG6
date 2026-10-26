# Aktørområder

- `Offentlig/Controllers`, `Offentlig/Models` og `Offentlig/Views`: behovsregistrering og videre funksjoner for offentlig aktør.
- `Privat/Controllers`, `Privat/Models` og `Privat/Views`: ressursregistrering og videre funksjoner for privat aktør.
- `../Models`: delte domene- og databasemodeller, inkludert Resource, Category og Registration.
- `../Handlers`: delte tjenester, inkludert ResourceHandler.
- `../Controllers/HomeController.cs` og `../Views/Home`: felles startsider, kart og personvern.
- `../Views/Shared` og `../wwwroot`: delt layout og statiske filer.

Bruk `[Area("Offentlig")]` eller `[Area("Privat")]` på nye kontrollere og eksplisitt `asp-area` på lenker mellom områder. Områdene har egne ViewImports og ViewStart og bruker den felles layouten.

## Adresser

- `/Privat/Resource/Skjema`: registrer ressurs (GET/POST).
- `/Offentlig/Need/Create`: eksisterende behovsside.
- `/Home/Skjema` og `/Need/Create` støttes fortsatt via kompatibilitetsruter.

Oppdelingen organiserer kode; den legger ikke til tilgangskontroll. Ressurslagring og validering er uendret. Ingen databasemigrering er nødvendig.
