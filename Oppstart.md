## Oppstart – innlevering 2

### Forutsetninger
- .NET 10 SDK
- Docker Desktop med Linux-containere, startet før applikasjonen
- Visual Studio eller Rider med støtte for prosjektets .NET-versjon

### Arkitektur
Aspire AppHost starter:
- MariaDB 11.4 i en Docker-container.
- ASP.NET Core-webappen i en Docker-container med HTTPS.

MariaDB bruker et navngitt Docker-volum som bevarer data
når containeren gjenskapes.

### Lokal konfigurasjon
Hver utvikler oppretter og stoler på sitt eget
HTTPS-utviklingssertifikat.

Sertifikatet eksporteres til:
~/.aspnet/https/totalforsvaret.pfx

AppHost trenger følgende lokale User Secrets:
- Parameters:mariadb-password genereres og lagres automatisk hvis verdien mangler.
- Parameters:https-password

Sertifikatpassordet må samsvare med det eksporterte sertifikatet.
Databasepassordet må samsvare med den lokale databasen.
Passord og sertifikatfiler skal ikke legges i Git.

### Kjøring
1. Start Docker Desktop.
2. Visual Studio: Velg Totalforsvaret.AppHost og HTTPS-profilen. I Rider velger du en Aspire Host-kjørekonfigurasjon for samme prosjekt og profil.
3. Åpne webappens HTTPS-lenke i Aspire-dashboardet.

AppHost er det felles oppstartsoppsettet for Windows og Mac.

### Databasetabeller
Tabellene opprettes og oppdateres automatisk ved oppstart i Development-miljøet, før testbrukerne opprettes.

### Verifisering
- Windows/Visual Studio: begge containere starter, og innlogging fungerer.
- Mac/Rider: ikke verifisert ennå.