using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;
using Totalforsvaret.AppHost;

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

// Kun en visningsressurs for den eksisterende databasen.
builder.AddResource(
    new DatabaseDisplayResource(
        "totalforsvaret-db",
        mariadb.Resource))
    .WithInitialState(new CustomResourceSnapshot
    {
        ResourceType = "Database",
        Properties = []
    });

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

                if (string.IsNullOrWhiteSpace(resolved))
                {
                    return HealthCheckResult.Unhealthy(
                        "Tilkoblingsstrengen til MariaDB mangler.");
                }
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
// Finner hjemmemappen automatisk på både Windows og Mac.

var certificatePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".aspnet",
    "https",
    "totalforsvaret.pfx");

if (!File.Exists(certificatePath))
{
    throw new InvalidOperationException(
        "HTTPS-sertifikatet mangler. Følg oppstartsveiledningen.");
}

var certificatePassword = builder.AddParameter(
    "https-password",
    secret: true);

builder
    .AddDockerfile(
        "totalforsvaret",
        contextPath: "..",
        dockerfilePath: "Totalforsvaret/Dockerfile")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ASPNETCORE_HTTPS_PORTS", "8081")
    .WithEnvironment(
        "ASPNETCORE_Kestrel__Certificates__Default__Path",
        "/https/totalforsvaret.pfx")
    .WithEnvironment(
        "ASPNETCORE_Kestrel__Certificates__Default__Password",
        certificatePassword)
    .WithBindMount(
        certificatePath,
        "/https/totalforsvaret.pfx",
        isReadOnly: true)
    .WithHttpsEndpoint(targetPort: 8081, name: "https")
    .WithEnvironment(
        "ConnectionStrings__DefaultConnection",
        connectionString)
    .WaitFor(mariadb);

builder.Build().Run();

