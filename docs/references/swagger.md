---
title: OpenAPI et Swagger UI
description: Génération du document OpenAPI avec ASP.NET Core et affichage avec Swagger UI.
tags: [dotnet, aspnet-core, openapi, swagger]
source: ambio-api
created: 2026-10-31
updated: 2026-11-03
---

# OpenAPI et Swagger UI

Depuis .NET 9, ASP.NET Core génère lui-même le document OpenAPI. Swashbuckle n'est plus utilisé que pour l'interface graphique.

## Installation

| Package                            | Rôle                           |
|------------------------------------|--------------------------------|
| `Microsoft.AspNetCore.OpenApi`     | génération du document OpenAPI |
| `Swashbuckle.AspNetCore.SwaggerUI` | interface graphique            |

## Document OpenAPI

Enregistrement du service :

```csharp
builder.Services.AddOpenApi();
```

Exposition du document :

```csharp
app.MapOpenApi();
```

Le document est servi sur `/openapi/v1.json`.

Les types de retour des endpoints (`Results<NotFound, Ok<T>>`, voir [Minimal API](minimal-api.md)) sont lus automatiquement pour décrire les réponses possibles dans le document.

## UI

Pour activer l'interface, il faut ajouter la dépendance `Swashbuckle.AspNetCore.SwaggerUI` et la brancher sur le document généré :

```csharp
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");
});
```

L'interface est disponible sur `/swagger`.

## Environnement

Dans le projet, le document et l'interface ne sont exposés qu'en Development :

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}
```

## Documentation

- [OpenAPI dans ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview)
