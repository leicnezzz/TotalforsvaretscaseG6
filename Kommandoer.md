Når dere skal sette opp databasen, bruk disse kommandoene for ditt OS:

FOR WINDOWS BRUKERE:
docker compose \
-f docker-compose.yml \
-f docker-compose.override.windows.yml up

FOR MAC BRUKERE:
docker compose \
-f docker-compose.yml \
-f docker-compose.override.mac.yml up
