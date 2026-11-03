---
title: Tests d'intégration
description: Tests d'intégration d'une API ASP.NET Core avec WebApplicationFactory, Testcontainers et Respawn.
tags: [dotnet, tests, xunit, testcontainers, respawn]
source: ambio-api
created: 2026-11-03
updated: 2026-11-03
---

# Tests d'intégration

Les tests d'intégration démarrent l'API en mémoire et l'appellent en HTTP. La base PostgreSQL est réelle et tourne dans un conteneur Docker créé pour les tests.

## Installation

| Package                                     | Rôle                                                       |
|---------------------------------------------|------------------------------------------------------------|
| `xunit.v3`, `xunit.v3.mtp-v2`               | framework de test, exécuté avec Microsoft Testing Platform |
| `Microsoft.AspNetCore.Mvc.Testing`          | `WebApplicationFactory` : héberge l'API en mémoire         |
| `Testcontainers.PostgreSql`                 | démarre un conteneur PostgreSQL pour les tests             |
| `Respawn`                                   | vide les tables entre chaque test                          |
| `Bogus`                                     | génère des données de test                                 |
| `Microsoft.Extensions.TimeProvider.Testing` | `FakeTimeProvider` : contrôle de l'heure                   |

> Docker doit être lancé pour exécuter les tests.

## Microsoft Testing Platform

Le projet de test utilise Microsoft Testing Platform (MTP) à la place de VSTest. Le `global.json` à la racine indique à `dotnet test` d'utiliser ce runner :

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Le projet de test est un exécutable :

```xml
<OutputType>Exe</OutputType>
<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
```

Commandes :

```shell
# Tous les tests
dotnet test

# Une classe de test
dotnet test --project tests/Ambio.Api.IntegrationTests --filter-class "*CreateTests"

# Une méthode de test
dotnet test --project tests/Ambio.Api.IntegrationTests --filter-method "*GetById_WhenJobApplicationExists_ShouldReturnOk"
```

## WebApplicationFactory

`WebApplicationFactory<T>` démarre l'API en mémoire et fournit un `HttpClient` pour l'appeler. `T` doit être un type de l'assembly de l'API. Le projet utilise une interface vide, `IApiMarker`, comme repère :

```csharp
namespace Ambio.Api;

public interface IApiMarker { }
```

La factory personnalise l'hôte dans `ConfigureWebHost` :

```csharp
public class AmbioApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Pas de logs pendant les tests
        builder.ConfigureLogging(logging => logging.ClearProviders());

        // Environnement dédié : ni appsettings.Development.json, ni endpoints réservés au Development
        builder.UseEnvironment("Testing");

        // La chaîne de connexion pointe vers le conteneur de test
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);

        // Remplacement de l'horloge
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(FakeTimeProvider);
        });
    }
}
```

`UseSetting` surcharge la configuration : l'API garde son propre enregistrement du DbContext (snake_case, intercepteur), seule la base change.

## Testcontainers

Le conteneur PostgreSQL est déclaré dans la factory, avec la même image qu'en développement :

```csharp
private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17")
    .WithDatabase("ambio")
    .WithUsername("ambio")
    .WithPassword("ambio")
    .WithCleanUp(true)
    .Build();
```

Au démarrage, la factory lance le conteneur puis applique les migrations. Le schéma de test est ainsi identique à celui de l'application :

```csharp
public async ValueTask InitializeAsync()
{
    await _dbContainer.StartAsync();

    using var scope = Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AmbioDbContext>();
    await dbContext.Database.MigrateAsync();

    // ... initialisation de Respawn
}
```

## Respawn

Respawn vide les tables entre les tests, sans recréer la base. C'est beaucoup plus rapide qu'un conteneur par test.

```csharp
_respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
{
    DbAdapter = DbAdapter.Postgres,
    SchemasToInclude = ["public"],
    TablesToIgnore = ["__EFMigrationsHistory"]
});
```

`__EFMigrationsHistory` est ignorée : elle contient la liste des migrations appliquées, qu'il ne faut pas effacer.

## Partage de la factory

Démarrer un conteneur est coûteux. Une **collection fixture** xUnit partage une seule factory (donc un seul conteneur) entre toutes les classes de test :

```csharp
[CollectionDefinition(AmbioApiFactory.BaseCollectionDefinition)]
public class IntegrationTestCollection : ICollectionFixture<AmbioApiFactory>
{
}
```

Les classes d'une même collection s'exécutent les unes après les autres. Les tests ne se marchent donc pas dessus dans la base.

## Classe de base

`BaseIntegrationTests` regroupe ce qui est commun à tous les tests :

- remise à zéro de la base avant chaque test (`InitializeAsync` → `ResetDatabaseAsync`) ;
- un `HttpClient` pour appeler l'API ;
- deux DbContext, chacun dans son propre scope :
  - `DbContext` pour préparer les données ;
  - `VerificationDbContext` pour vérifier le résultat. Comme ce second contexte n'a rien en cache, il lit ce qui est réellement en base.

```csharp
[Collection(AmbioApiFactory.BaseCollectionDefinition)]
public abstract class BaseIntegrationTests : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _testScope = _factory.Services.CreateAsyncScope();
        DbContext = _testScope.ServiceProvider.GetRequiredService<AmbioDbContext>();

        _verificationScope = _factory.Services.CreateAsyncScope();
        VerificationDbContext = _verificationScope.ServiceProvider.GetRequiredService<AmbioDbContext>();
    }
}
```

## Données de test avec Bogus

`Faker<T>` génère des entités avec des valeurs réalistes :

```csharp
protected readonly Faker<JobApplication> JobApplicationGenerator = new Faker<JobApplication>()
    .RuleFor(j => j.Title, f => f.Lorem.Word())
    .RuleFor(j => j.Company, f => f.Company.CompanyName())
    .RuleFor(j => j.Url, f => f.Internet.Url());

var one = JobApplicationGenerator.Generate();
var many = JobApplicationGenerator.Generate(10);
```

## Contrôle de l'heure

`FakeTimeProvider` remplace `TimeProvider` : l'heure est figée, et on l'avance à la demande. Cela permet de tester les dates de création et de mise à jour.

```csharp
var expectedCreatedAt = FakeTimeProvider.GetUtcNow().UtcDateTime;
FakeTimeProvider.Advance(TimeSpan.FromDays(1));
var expectedUpdatedAt = FakeTimeProvider.GetUtcNow().UtcDateTime;
```

## Exemple de test

```csharp
public class GetByIdTests : BaseJobApplicationTests
{
    public GetByIdTests(AmbioApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetById_WhenJobApplicationExists_ShouldReturnOk()
    {
        var jobApplication = await CreateJobApplicationAsync();

        var response = await Client.GetAsync($"{BaseUrl}/{jobApplication.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await ReadContentAsync<JobApplicationResponse>(response);
        Assert.Equal(jobApplication.Title, content.Title);
    }
}
```

`TestContext.Current.CancellationToken` (xUnit v3) annule les appels si le test est interrompu.

## Documentation

- [Tests d'intégration ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Respawn](https://github.com/jbogard/Respawn)
- [xUnit v3 et Microsoft Testing Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
