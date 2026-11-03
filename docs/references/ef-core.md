---
title: Entity Framework Core
description: Configuration d'EF Core avec PostgreSQL, mapping des entités, intercepteurs et migrations.
tags: [dotnet, ef-core, postgresql]
source: ambio-api
created: 2026-10-30
updated: 2026-11-03
---

# Entity Framework Core

## Installation

| Package                                    | Rôle                                                         |
|--------------------------------------------|--------------------------------------------------------------|
| `Microsoft.EntityFrameworkCore.Design`     | outillage des migrations, référencé en `PrivateAssets="all"` |
| `Microsoft.EntityFrameworkCore.Relational` | fonctionnalités communes aux bases relationnelles            |
| `Npgsql.EntityFrameworkCore.PostgreSQL`    | accès à PostgreSQL                                           |
| `EFCore.NamingConventions`                 | conventions de nommage globales (snake_case)                 |

## Configuration

Le DbContext est enregistré dans l'injection de dépendances via la méthode `AddDbContext` de `IServiceCollection`. Il y a deux façons de le configurer :

- surcharger la méthode `OnConfiguring` du DbContext ;
- passer les options depuis l'injection de dépendances. Le DbContext doit alors déclarer un constructeur qui prend un `DbContextOptions<TContext>`, où `TContext` est le type du DbContext.

Le projet utilise la seconde méthode :

```csharp
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TimestampInterceptor>();
builder.Services.AddDbContext<AmbioDbContext>((provider, options) =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        options.UseSnakeCaseNamingConvention();
        options.AddInterceptors(provider.GetRequiredService<TimestampInterceptor>());
    },
    contextLifetime: ServiceLifetime.Scoped,
    optionsLifetime: ServiceLifetime.Singleton);
```

```csharp
public sealed class AmbioDbContext : DbContext
{
    public AmbioDbContext(DbContextOptions<AmbioDbContext> options) : base(options)
    {
    }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
}
```

Deux autres paramètres peuvent être passés à `AddDbContext` :

- `contextLifetime` : durée de vie du DbContext (`Scoped` par défaut, soit une instance par requête HTTP).
- `optionsLifetime` : durée de vie des options. Si la configuration ne change jamais pendant la vie de l'application, les options peuvent être déclarées en singleton.

> Quand les options sont en singleton, le `provider` reçu dans la lambda est le conteneur racine. Les services qu'on y résout (ici l'intercepteur) doivent donc être eux aussi des singletons.

`UseSnakeCaseNamingConvention()` convertit tous les noms en snake_case : la table `JobApplications` devient `job_applications` et la propriété `AppliedOn` devient `applied_on`.

## Mapping des entités

La configuration de chaque entité est isolée dans une classe qui implémente `IEntityTypeConfiguration<T>` :

```csharp
public class JobApplicationMapping : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.Property(ja => ja.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(ja => ja.Title)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(ja => ja.Status)
            .HasConversion<string>();
    }
}
```

Le DbContext charge toutes les configurations de l'assembly en une ligne :

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AmbioDbContext).Assembly);
}
```

Points à retenir :

- `HasConversion<string>()` stocke un enum sous forme de texte (`Applied`) plutôt que d'entier. La base reste lisible, mais renommer une valeur de l'enum demande une migration de données.
- `ValueGeneratedNever()` indique que la base ne génère pas l'Id. Il est généré côté code avec `Guid.CreateVersion7()`. Un UUID v7 contient un horodatage, il est donc triable dans l'ordre de création.

```csharp
public Guid Id { get; init; } = Guid.CreateVersion7();
```

## Intercepteurs

Un intercepteur permet d'exécuter du code autour des opérations d'EF Core. Le projet utilise un `SaveChangesInterceptor` pour remplir automatiquement `CreatedAt` et `UpdatedAt` sur les entités qui implémentent `ITimeStampable`.

```csharp
public sealed class TimestampInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;

    public TimestampInterceptor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetTimestamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<ITimeStampable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    break;
            }
        }
    }
}
```

- Il faut surcharger à la fois `SavingChanges` et `SavingChangesAsync` pour couvrir les deux appels.
- `TimeProvider` est injecté au lieu d'appeler `DateTime.UtcNow`. Dans les tests, il est remplacé par un `FakeTimeProvider` pour contrôler l'heure.
- Avec Npgsql, une colonne `timestamp with time zone` exige un `DateTime` de type UTC, d'où le `.UtcDateTime`.

## Logging

Il est possible de brancher `ILogger` sur le DbContext dans la configuration de `AddDbContext` :

```csharp
services.AddDbContext<MyDbContext>(options =>
{
    options.UseLoggerFactory(loggerFactory);
});
```

Certaines options utiles en développement ou en test peuvent être activées :

- `EnableSensitiveDataLogging` : affiche les valeurs des paramètres dans les logs.

> Attention, cette option doit être désactivée en production.

## Migrations

Dans ce projet, les migrations sont stockées dans `src/Ambio.Api/Data/Migrations`.

### Vérification

Pour vérifier si une migration est nécessaire :

```shell
dotnet ef migrations has-pending-model-changes --project src/Ambio.Api
```

### Création

Une fois le modèle défini, il faut générer une migration pour synchroniser la base de données avec le modèle :

```shell
dotnet ef migrations add <NomDeLaMigration> --project src/Ambio.Api --output-dir Data/Migrations
```

L'option `--output-dir` (ou `-o`) définit le dossier des migrations, relatif au projet ciblé par `--project`.

Pour supprimer la dernière migration, si elle n'a pas encore été appliquée :

```shell
dotnet ef migrations remove --project src/Ambio.Api
```

> Il faut toujours relire la migration générée. Par exemple, un renommage de propriété peut être interprété comme une suppression de colonne suivie d'un ajout, ce qui entraîne une perte de données.

### Application

```shell
dotnet ef database update --project src/Ambio.Api
```

### Production

Il existe deux méthodes pour exécuter les migrations en production :

- via un script SQL généré avec `dotnet ef migrations script > script.sql` ;
- via un bundle généré avec `dotnet ef migrations bundle`, qui produit un exécutable contenant les migrations.

Le bundle est la méthode recommandée, car il s'intègre bien avec les conteneurs Docker.

## Opérations

### Lecture

Pour récupérer une entité par son Id, on utilise `Find` (ou `FindAsync`) sur le `DbSet`. Si l'entité est déjà suivie par le contexte, elle est renvoyée depuis le cache. Sinon, une requête est envoyée à la base de données.

```csharp
var jobApplication = await _context.JobApplications.FindAsync(id);
```

Les autres méthodes :

```csharp
// Récupère la première entité correspondant au prédicat, ou null.
_context.Entity.FirstOrDefault(e => e.Id == id);

// Récupère la seule entité correspondant au prédicat, ou null.
// Lève une exception si plusieurs entités sont trouvées.
_context.Entity.SingleOrDefault(e => e.Id == id);
```

Pour une lecture seule, `AsNoTracking()` évite que le contexte suive les entités. C'est plus léger, et la lecture se fait toujours depuis la base. Les tests l'utilisent pour vérifier les données réellement persistées.

```csharp
_context.JobApplications.AsNoTracking().SingleAsync(x => x.Id == id);
```

### Mise à jour

Une entité récupérée par le contexte est suivie : il suffit de modifier ses propriétés puis d'appeler `SaveChanges`. EF Core détecte les changements et ne met à jour que les colonnes modifiées.

```csharp
var existingApplication = await _context.JobApplications.FindAsync(id);
existingApplication.Title = request.Title;
await _context.SaveChangesAsync();
```

### Suppression

Pour supprimer une entité, on utilise `Remove` sur le `DbSet`, puis `SaveChanges` pour appliquer la modification en base.

```csharp
_context.Entity.Remove(existingEntity);
await _context.SaveChangesAsync();
```

Il existe deux autres façons de faire :

```csharp
// Le type est déduit de l'entité passée en paramètre.
_context.Remove(existingEntity);

// Supprime une entité à partir de son Id, sans la récupérer au préalable.
// Lève une exception à SaveChanges si la ligne n'existe pas.
_context.Entity.Remove(new Entity { Id = id });
```

## Projection

Quand toutes les données d'une entité ne sont pas nécessaires, on peut sélectionner les propriétés avec LINQ :

```csharp
_context.Entity.Select(e => new { e.Id, e.Name });
```

La projection permet aussi de transposer les données dans un autre type. On ne récupère que les colonnes nécessaires, ce qui améliore les performances et la lisibilité.

> Si la projection appelle une méthode C#, comme `Select(ja => ja.ToResponse())` dans le projet, EF Core ne peut pas la traduire en SQL. Il charge l'entité complète, puis applique la méthode côté client. Cela fonctionne, mais on perd la sélection des colonnes.

## Documentation

- [Séparation des migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects?tabs=dotnet-core-cli)
- [Intercepteurs](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [Bundles de migration](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying#bundles)
