# Oppstart

Veiledningen gjelder utviklingskjøring gjennom `Totalforsvaret.AppHost` i Visual Studio eller Rider.

## 1. Installer og klargjør

- Installer .NET 10 SDK og en Visual Studio- eller Rider-versjon som støtter .NET 10 og Aspire.
- Installer og start Docker Desktop. Bruk Linux-containere.
- Klon repoet og åpne `Totalforsvaret.slnx`.
- Kjør kommandoene nedenfor fra repoets rotmappe, der `Totalforsvaret.slnx` ligger.

## 2. Klargjør HTTPS én gang per maskin

Velg ditt eget lokale sertifikatpassord. Erstatt `<sertifikatpassord>` med samme verdi i begge kommandoene som bruker passordet. Behold anførselstegnene.

Opprett og godkjenn utviklingssertifikatet. Godta forespørselen fra operativsystemet:

```text
dotnet dev-certs https --trust
```

Opprett mappen for sertifikatet.

Windows, i PowerShell:

```powershell
New-Item -ItemType Directory -Force -Path "$HOME/.aspnet/https"
```

Mac, i terminalen:

```bash
mkdir -p "$HOME/.aspnet/https"
```

Eksporter sertifikatet. Denne kommandoen fungerer i PowerShell og i terminalen på Mac:

```text
dotnet dev-certs https --export-path "$HOME/.aspnet/https/totalforsvaret.pfx" --password "<sertifikatpassord>"
```

Lagre det samme passordet lokalt for AppHost:

```text
dotnet user-secrets set "Parameters:https-password" "<sertifikatpassord>" --project Totalforsvaret.AppHost
```

Sertifikatet og passordet gjenbrukes ved senere oppstarter. Passord og sertifikatfiler skal holdes utenfor Git.

## 3. Start applikasjonen

1. Kontroller at Docker Desktop kjører.
2. I Visual Studio velger du `Totalforsvaret.AppHost` som oppstartsprosjekt og `https` som profil.
3. I Rider velger du en Aspire Host-kjørekonfigurasjon med `Totalforsvaret.AppHost` og launch-profilen `https`.
4. Trykk Start/Run. Første oppstart kan ta tid mens Docker-imagene lastes ned og webappen bygges.
5. Aspire-dashboardet åpnes. Vent til MariaDB svarer og webappen har startet.
6. Åpne HTTPS-lenken for ressursen `totalforsvaret` i dashboardet.

AppHost starter MariaDB 11.4 og webappen i hver sin Docker-container. Databasepassordet genereres automatisk og lagres i AppHost sine lokale User Secrets. Tilkoblingsstrengen sendes automatisk til webappen. Tabellene opprettes eller oppdateres før testbrukeren opprettes.

## 4. Logg inn

Offentlig demokonto:
E-post: offentlig@example.com
Passord: Passordet12345

Velg innlogging for offentlig aktør. Kontoen opprettes i Development-miljøet dersom den mangler. En eksisterende konto beholder sitt passord.

For privat aktør velger du «Opprett bruker» og registrerer din egen konto.

## 5. Stopp og start igjen

Stopp AppHost i IDE-en. Neste gang starter du samme AppHost med profilen `https`.

Dataene ligger i Docker-volumet `totalforsvaretscaseg6_mariadbdata` og beholdes mellom oppstarter. Behold volumet og AppHost sine User Secrets. Hvis et eksisterende databasevolum bruker et eldre passord, må `Parameters:mariadb-password` fortsatt samsvare med dette.

## Feilsøking

- **HTTPS-sertifikatet mangler** – kontroller at filen ligger på `~/.aspnet/https/totalforsvaret.pfx`, og fullfør steg 2.
- **Sertifikatet kan ikke åpnes** – passordet i `Parameters:https-password` må være identisk med eksportpassordet.
- **MariaDB blir ikke klar** – kontroller Docker-loggen og om det eksisterende volumet og passordet hører sammen.
- **Porten er opptatt** – stopp en tidligere AppHost-kjøring. MariaDB bruker verts port 3308, og launch-profilen angir dashboardets porter.

## Kjør enhetstestene

Fra repoets rotmappe:

```text
dotnet test Totalforsvaret.Tests/Totalforsvaret.Tests.csproj
```

## Verifisering

Begge containere og innlogging er verifisert i en Windows- og Ryder versjon.
