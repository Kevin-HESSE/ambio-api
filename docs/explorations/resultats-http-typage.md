---
title: Résultats HTTP — Match ou builder fluide
description: Pistes à expérimenter dans un dépôt de test pour convertir un résultat métier en réponse HTTP avec les Minimal APIs.
tags: [aspnet-core, minimal-api, openapi, roslyn, benchmark]
source: exploration
created: 2026-11-19
updated: 2026-11-19
---

# Résultats HTTP — Match ou builder fluide

> Note d'exploration, détachée du projet. Rien ici n'a été implémenté ni mesuré : ce sont des hypothèses issues d'une
> discussion, à vérifier dans un dépôt de test.

## Point de départ

Un service renvoie un résultat métier : un succès, ou un échec avec une nature (`NotFound`, `ValidationFailed`,
`Conflict`...). Le handler Minimal API doit le convertir en réponse HTTP.

La question : peut-on écrire cette conversion de façon lisible et réutilisable, sans perdre le typage fort de
`Results<T1, ..., Tn>` ?

## Les deux concepts

### `Match`

Une méthode qui prend un délégué par cas et renvoie un `Results<...>`.

```csharp
private static async Task<Results<Ok<Response>, ValidationProblem, Conflict, NotFound>> UpdateHandler(...)
{
    return result.Match(
        onSuccess: value => TypedResults.Ok(value),
        onInvalid: errors => TypedResults.ValidationProblem(errors),
        onConflict: _ => TypedResults.Conflict(),
        onError: _ => TypedResults.NotFound());
}
```

| Avantages supposés                                                | Inconvénients supposés                                      |
|-------------------------------------------------------------------|-------------------------------------------------------------|
| Typage fort, vérifié à la compilation                             | Une surcharge de `Match` par nombre de cas                  |
| OpenAPI déduit les réponses du type `Results<...>`                | Chaque surcharge fige un mapping nature → paramètre         |
| Tous les cas sont visibles dans la signature du handler           | Lecture moins naturelle quand les cas se multiplient        |

### Builder fluide

Une chaîne d'étapes : chacune traite un cas, la dernière fournit la réponse par défaut.

```csharp
private static async Task<IResult> UpdateHandler(...)
{
    return result.ConvertToHttpResult()
        .IsUpdated()
        .OrHaveInvalidData()
        .OrConflict()
        .OrFinallyNotFound();
}
```

| Avantages supposés                                  | Inconvénients supposés                                                   |
|-----------------------------------------------------|--------------------------------------------------------------------------|
| Lecture proche d'une phrase                         | Retour en `IResult` : plus de typage fort                                |
| Étapes réutilisables d'un endpoint à l'autre        | OpenAPI ne connaîtrait plus les réponses possibles                       |
| Ajouter un cas ne demande pas de nouvelle surcharge | Une étape oubliée ne provoque aucune erreur (le cas tombe dans le défaut) |

## Variantes à essayer

### `params` dans `Match`

```csharp
public IResult Match(
    Func<T, IResult> onSuccess,
    Func<Result<T>, IResult> onDefault,
    params ReadOnlySpan<(ResultKind Kind, Func<Result<T>, IResult> Factory)> cases)
```

Hypothèse : `params` impose un seul type d'élément, et C# n'a pas de génériques variadiques. Le retour serait donc
forcément `IResult`, comme pour le builder. À confirmer, et à juger sur la lisibilité des tuples.

### Builder fluide typé

Chaque étape renverrait un nouveau type avec un paramètre générique de plus. La dernière étape construirait le
`Results<...>` grâce à ses conversions implicites.

```csharp
public sealed class HttpMatch<T, T1>(Result<T> source, T1? r1)
    where T1 : class, IResult
{
    public HttpMatch<T, T1, T2> Or<T2>(ResultKind kind, Func<Result<T>, T2> factory)
        where T2 : class, IResult
        => new(source, r1, r1 is null && source.Kind == kind ? factory(source) : null);

    public Results<T1, T2> OrFinally<T2>(Func<Result<T>, T2> fallback)
        where T2 : class, IResult
    {
        if (r1 is not null) return r1;
        return fallback(source);
    }
}

// HttpMatch<T, T1, T2>, HttpMatch<T, T1, T2, T3>... on the same model
```

```csharp
public static HttpMatch<T, T1, ValidationProblem> OrHaveInvalidData<T, T1>(this HttpMatch<T, T1> m)
    where T1 : class, IResult
    => m.Or(ResultKind.ValidationFailed, r => TypedResults.ValidationProblem(r.ToDictionary()));
```

Questions à vérifier :

- Les conversions implicites de `Results<>` passent-elles bien avec des paramètres génériques ?
- Combien de classes faut-il réellement (une par arité, `Results<>` allant de 2 à 6) ? Et combien de raccourcis
  nommés, puisque chacun doit exister pour chaque arité ?
- L'ordre de la chaîne fixe l'ordre des types dans `Results<>` : est-ce gênant à l'usage ?
- Une étape oubliée fait-elle bien échouer la compilation contre la signature du handler ?

### Builder `IResult` + `.Produces(...)`

```csharp
app.MapPut("/{id:guid}", UpdateHandler)
    .Produces<Response>()
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status409Conflict)
    .Produces(StatusCodes.Status404NotFound);
```

Hypothèse : OpenAPI redevient complet, mais rien ne garantit que la déclaration correspond au code du handler. D'où
les deux solutions de vérification ci-dessous.

## Vérifier la cohérence entre le code et la déclaration

### Analyseur Roslyn

Idée : au build, l'analyseur lit chaque appel `Map*` et la chaîne fluide du handler, puis compare les codes trouvés aux
`.Produces(...)` déclarés. En cas d'écart, il lève un warning ou une erreur. Un correctif automatique pourrait ajouter
les `Produces` manquants.

Pour ne pas coder les étapes en dur, chaque étape du builder porterait un attribut lu par l'analyseur :

```csharp
[ProducesStatus(StatusCodes.Status409Conflict)]
public static HttpResultFor<Result<T>> OrConflict<T>(...)
```

| Package                          | Rôle                    |
|----------------------------------|-------------------------|
| `Microsoft.CodeAnalysis.CSharp`  | écriture de l'analyseur |
| `Microsoft.CodeAnalysis.Testing` | tests de l'analyseur    |

- Projet `netstandard2.0`, référencé par l'API avec `OutputItemType="Analyzer"` et `ReferenceOutputAssembly="false"`.
- Cas simple à viser d'abord : un handler en méthode, avec un seul `return` et une chaîne directe.
- Cas difficiles à explorer ensuite : lambdas, `return` multiples, chaîne construite dans une autre méthode.

**Variante source generator** : générer les `Produces` au lieu de les vérifier. Un générateur ne peut pas modifier un
appel existant. Il faudrait des *interceptors*, avec un risque de conflit possible avec le `RequestDelegateGenerator`
d'ASP.NET Core. À tester si l'analyseur fonctionne.

### Vérification dans les tests d'intégration

Idée : un middleware ajouté par la `WebApplicationFactory` (via un `IStartupFilter`, après le routage) qui, après chaque
requête :

1. récupère l'endpoint appelé (`context.GetEndpoint()`) ;
2. lit ses métadonnées `IProducesResponseTypeMetadata` (alimentées par `.Produces(...)` ou par `Results<>`) ;
3. fait échouer le test si le `StatusCode` de la réponse n'est pas déclaré.

Hypothèses :

- Ça fonctionnerait avec `IResult` comme avec `Results<>`, quelle que soit la forme du handler.
- Seuls les cas couverts par un test seraient vérifiés.
- Quelques dizaines de lignes suffiraient. À mesurer.

## Benchmarking

Hypothèse : l'écart entre `Match` et le builder fluide serait négligeable.

| | `Match` | Builder fluide |
|---|---|---|
| Allocations | résultat HTTP + enveloppe `Results<...>` | résultat HTTP + instance du builder |
| Appels de délégués | 1 à 2 | 1 à 2 par étape |
| Lambdas | sans capture, a priori mises en cache | sans capture, a priori mises en cache |

L'ordre de grandeur supposé est la nanoseconde. Une vraie requête coûte des millisecondes (JSON, validation, base de
données).

| Package                 | Rôle                   |
|-------------------------|------------------------|
| `BenchmarkDotNet`       | mesure des variantes   |

Protocole à mettre en place :

- Construire le résultat métier à l'avance, sans base de données.
- Mesurer chaque variante (`Match`, builder `IResult`, builder typé, `params`) sur chaque cas : succès, validation,
  conflit, défaut.
- Activer `[MemoryDiagnoser]` pour comparer les allocations.
- Optionnel : un builder en `struct`, pour voir si le gain d'allocation est visible et si les copies posent problème.
- Optionnel : un test de charge HTTP de bout en bout, pour voir si l'écart reste mesurable dans une vraie requête.

## Plan pour un dépôt de test

1. Une API Minimal minimale, avec un service factice qui renvoie chaque nature de résultat.
2. Les variantes : `Match`, builder `IResult` + `.Produces(...)`, builder typé, `Match` avec `params`.
3. Comparer le document OpenAPI produit par chaque variante.
4. Les tests d'intégration avec le middleware de vérification.
5. L'analyseur Roslyn et ses tests.
6. Les benchmarks.
