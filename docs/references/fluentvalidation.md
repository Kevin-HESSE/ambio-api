---
title: FluentValidation
description: Validation des requêtes avec FluentValidation dans une Minimal API.
tags: [dotnet, aspnet-core, validation, fluentvalidation]
source: ambio-api
created: 2026-11-03
updated: 2026-11-03
---

# FluentValidation

FluentValidation permet de décrire les règles de validation d'un objet dans une classe dédiée, avec une syntaxe fluide.

## Installation

| Package                                          | Rôle                                                           |
|--------------------------------------------------|----------------------------------------------------------------|
| `FluentValidation`                               | définition des règles de validation                            |
| `FluentValidation.DependencyInjectionExtensions` | enregistrement des validateurs dans l'injection de dépendances |

> Le package `FluentValidation.AspNetCore`, qui faisait la validation automatique, est déprécié. La validation est donc appelée explicitement dans l'endpoint.

## Créer un validateur

Un validateur hérite de `AbstractValidator<T>` et déclare ses règles dans le constructeur :

```csharp
public class JobApplicationRequestValidator : AbstractValidator<JobApplicationRequest>
{
    public JobApplicationRequestValidator()
    {
        RuleFor(ja => ja.Title).NotEmpty();
        RuleFor(ja => ja.Company).NotEmpty();
        RuleFor(ja => ja.Url).NotEmpty();
        RuleFor(ja => ja.Status).IsInEnum();
    }
}
```

| Règle              | Effet                                                                            |
|--------------------|----------------------------------------------------------------------------------|
| `NotEmpty()`       | refuse `null`, une chaîne vide ou composée uniquement d'espaces                  |
| `IsInEnum()`       | refuse une valeur numérique qui ne correspond à aucun membre de l'enum (ex. `9`) |
| `MaximumLength(n)` | refuse une chaîne de plus de `n` caractères                                      |

## Enregistrement

Tous les validateurs de l'assembly sont enregistrés en une ligne :

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

Par défaut, ils sont enregistrés en `Scoped` sous l'interface `IValidator<T>`.

## Utilisation dans un endpoint

Le validateur est injecté en paramètre de l'endpoint, puis appelé avant tout traitement :

```csharp
app.MapPost(
    "/job-applications",
    async Task<Results<ValidationProblem, Created<JobApplicationResponse>>> (
        JobApplicationRequest jobApplicationRequest,
        AmbioDbContext ambioDbContext,
        IValidator<JobApplicationRequest> validator) =>
    {
        var validationResult = await validator.ValidateAsync(jobApplicationRequest);

        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        // ...
    });
```

`ToDictionary()` convertit les erreurs au format attendu par `ValidationProblem` (nom de la propriété → liste de messages). La réponse est une `400 Bad Request` au format ProblemDetails :

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["'Title' ne doit pas être vide."],
    "Status": ["'Status' a une plage de valeurs qui n'inclut pas '9'."]
  }
}
```

> Les messages sont traduits selon la culture du serveur (ici en français). Les clés reprennent le nom de la propriété C# (`Title`), pas le nom JSON (`title`).

## Documentation

- [FluentValidation](https://docs.fluentvalidation.net/)
- [Validateurs intégrés](https://docs.fluentvalidation.net/en/latest/built-in-validators.html)
