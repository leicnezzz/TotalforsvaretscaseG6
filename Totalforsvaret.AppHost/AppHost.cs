using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;

var builder = DistributedApplication.CreateBuilder(args);

// Passordet leses fra AppHost sine User Secrets.
var password = builder.AddParameter(
    "mariadb-password",
    secret: true);

// Aspire starter MariaDB i Docker.
var mariadb = builder
    .AddContainer("mariadb", "mariadb", "11.4")
    .WithEnvironment("MARIADB_ROOT_PASSWORD", password)
    .WithEnvironment("MARIADB_DATABASE", "totalforsvaret")
    .WithEndpoint(
         port: 3308,
         targetPort: 3306,
         scheme: "tcp",
         name: "tcp")
    .WithVolume(
        "totalforsvaretscaseg6_mariadbdata",
        "/var/lib/mysql");

var endpoint = mariadb.GetEndpoint("tcp");

// Aspire lager tilkoblingen til containerens publiserte adresse.
var connectionString = ReferenceExpression.Create(
    $"Server={endpoint.Property(EndpointProperty.Host)};Port={endpoint.Property(EndpointProperty.Port)};Database=totalforsvaret;User=root;Password={password};");

builder.Services.AddHealthChecks()
    .AddAsyncCheck(
        "mariadb-ready",
        async cancellationToken =>
        {
            try
            {
                var resolved = await connectionString.GetValueAsync(
                    new ValueProviderContext(),
                    cancellationToken);

                var settings = new MySqlConnectionStringBuilder(resolved)
                {
                    ConnectionTimeout = 3,
                    DefaultCommandTimeout = 3
                };

                await using var connection =
                    new MySqlConnection(settings.ConnectionString);

                await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";

                await command.ExecuteScalarAsync(cancellationToken);

                return HealthCheckResult.Healthy(
                    "MariaDB svarer.");
            }
            catch (Exception)
            {
                return HealthCheckResult.Unhealthy(
                    "MariaDB er ikke tilgjengelig.");
            }
        });

mariadb.WithHealthCheck("mariadb-ready");

// Webappen får tilkoblingen gjennom en miljøvariabel.
builder
    .AddProject<Projects.Totalforsvaret>(
        "totalforsvaret",
        launchProfileName: "https")
    .WithEnvironment(
        "ConnectionStrings__DefaultConnection",
        connectionString)
    .WaitFor(mariadb);

builder.Build().Run();