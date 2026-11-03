---
title: Minimal API
description: Déclaration des endpoints avec les Minimal APIs d'ASP.NET Core et réponses typées avec TypedResults.
tags: [dotnet, aspnet-core, minimal-api]
source: ambio-api
created: 2026-11-03
updated: 2026-11-03
---

# Minimal API

Les Minimal APIs permettent de déclarer des endpoints HTTP directement sur `WebApplication`, sans contrôleur.

## Déclaration des endpoints

Chaque verbe HTTP a sa méthode : `MapGet`, `MapPost`, `MapPut`, `MapDelete`. Elle prend une route et un délégué (une lambda) qui traite la requête.

```csharp
app.MapGet(
    "/job-applications",
    async (AmbioDbContext ambioDbContext) => await ambioDbContext.JobApplications
        .Select(ja => ja.ToResponse())
        .ToListAsync());
```

Quand la lambda renvoie directement un objet, il est sérialisé en JSON avec un statut `200 OK`.

## Contraintes de route

Une contrainte limite les valeurs acceptées par un paramètre de route :

```csharp
app.MapGet("/job-applications/{id:guid}", ...);
```

Si la valeur n'est pas un GUID, la route ne correspond pas et l'API renvoie `404 Not Found` sans exécuter l'endpoint.

## Liaison des paramètres

Les paramètres de la lambda sont remplis automatiquement selon leur type :

| Paramètre                                                  | Source                                             |
|------------------------------------------------------------|----------------------------------------------------|
| `Guid id`                                                  | la route (même nom que `{id}`)                     |
| `JobApplicationRequest jobApplicationRequest`              | le corps de la requête, désérialisé depuis le JSON |
| `AmbioDbContext ambioDbContext`, `IValidator<T> validator` | l'injection de dépendances                         |

```csharp
app.MapPut(
    "/job-applications/{id:guid}",
    async (Guid id, JobApplicationRequest jobApplicationRequest, AmbioDbContext ambioDbContext) => { ... });
```

> Si le corps JSON ne peut pas être désérialisé (mauvais type, enum envoyé en texte…), l'endpoint n'est pas exécuté et l'API renvoie `400 Bad Request`.

## TypedResults

`TypedResults` est une fabrique de réponses HTTP typées : `Ok`, `Created`, `NoContent`, `NotFound`, `ValidationProblem`…

Quand un endpoint peut renvoyer plusieurs réponses, on déclare le type de retour avec `Results<T1, T2, ...>` :

```csharp
app.MapGet(
    "/job-applications/{id:guid}",
    async Task<Results<NotFound, Ok<JobApplicationResponse>>> (Guid id, AmbioDbContext ambioDbContext) =>
    {
        var jobApplication = await ambioDbContext.JobApplications.FindAsync(id);

        return jobApplication is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(jobApplication.ToResponse());
    });
```

Ce que ça apporte :

- **Vérification à la compilation** : renvoyer un type absent de `Results<...>` provoque une erreur de compilation.
- **Documentation OpenAPI automatique** : les réponses possibles (`404`, `200` avec le schéma `JobApplicationResponse`) sont déduites du type de retour, sans attribut supplémentaire.
- **Testabilité** : le résultat est un objet typé qu'on peut inspecter dans un test unitaire.

`TypedResults` est préféré à `Results` (sans « Typed »), qui renvoie un `IResult` générique et ne fournit pas ces métadonnées.

### Réponses utilisées dans le projet

| Méthode                                  | Statut | Usage                                                 |
|------------------------------------------|--------|-------------------------------------------------------|
| `TypedResults.Ok(value)`                 | 200    | lecture ou mise à jour réussie                        |
| `TypedResults.Created(uri, value)`       | 201    | création, avec l'en-tête `Location` vers la ressource |
| `TypedResults.NoContent()`               | 204    | suppression réussie                                   |
| `TypedResults.NotFound()`                | 404    | ressource inexistante                                 |
| `TypedResults.ValidationProblem(errors)` | 400    | erreurs de validation au format ProblemDetails        |

```csharp
return TypedResults.Created($"/job-applications/{jobApplication.Id}", jobApplication.ToResponse());
```

## Documentation

- [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Réponses dans les Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses)
