---
title: Audit de sécurité et technique — socle de l'API
description: Audit du socle de l'API Ambio (CRUD des candidatures) avant le développement du front.
tags: [audit, securite, aspnet-core]
source: ambio-api
created: 2026-11-03
updated: 2026-11-03
---

# Audit de sécurité et technique — socle de l'API

## Contexte

- **Périmètre** : branche `feat/job-applications`, commit `a2f25e5`. Il couvre l'API `/job-applications`, la configuration et le `docker-compose.yaml`.
- **Usages prévus** :
  - **Local** : outil personnel, non exposé sur le web, avec une base distincte de celle de dev.
  - **Démo** : instance publique servant de projet vitrine.
- **Choix assumé** : pas d'authentification.

Chaque constat a une gravité pour chacun des deux usages : `Haute`, `Moyenne`, `Faible` ou `Info`.

Les constats marqués « vérifié » ont été reproduits sur l'API lancée en local.

## Sécurité

| #   | Constat                                                  | Local   | Démo    |
|-----|----------------------------------------------------------|---------|---------|
| S1  | URL non validée (XSS stocké)                             | Faible  | Haute   |
| S2  | PostgreSQL exposé sur toutes les interfaces réseau       | Moyenne | Info    |
| S3  | Pas d'authentification                                   | Info    | Moyenne |
| S4  | Pas de rate limiting                                     | Info    | Moyenne |
| S5  | Longueurs maximales non validées (erreur 500)            | Faible  | Moyenne |
| S6  | Pas de gestion globale des erreurs                       | Faible  | Faible  |
| S7  | `GET /job-applications` non paginé                       | Faible  | Moyenne |
| S8  | `AllowedHosts: "*"`                                      | Info    | Faible  |
| S9  | HTTPS derrière un proxy (HSTS, forwarded headers)        | Info    | Faible  |
| S10 | CORS non configuré                                       | Info    | Info    |
| S11 | Gestion des secrets                                      | Info    | Moyenne |
| S12 | Dépendances et image Docker                              | Info    | Info    |

### S1 — URL non validée (XSS stocké) — vérifié

**Constat** : le validateur vérifie seulement que `Url` n'est pas vide. `POST` avec `"url": "javascript:alert(1)"` renvoie `201 Created`.

**Risque** : si le front affiche l'URL dans un lien `<a href>`, un clic exécute le script. Sur la démo publique, n'importe qui peut enregistrer ce type de lien et il sera vu par les visiteurs.

**Recommandation** : n'accepter que les URL absolues en `http` ou `https`.

```csharp
RuleFor(ja => ja.Url)
    .NotEmpty()
    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
```

### S2 — PostgreSQL exposé sur toutes les interfaces réseau

**Constat** : `ports: - ${POSTGRES_PORT:-5432}:5432` publie le port sur `0.0.0.0`, avec les identifiants par défaut `ambio` / `ambio`.

**Risque** : la base est joignable depuis le réseau local (wifi public, réseau d'entreprise).

**Recommandation** : lier le port à la boucle locale.

```yaml
ports:
  - 127.0.0.1:${POSTGRES_PORT:-5432}:5432
```

### S3 — Pas d'authentification

**Constat** : tous les endpoints sont publics. C'est un choix assumé.

**Risque** : sur la démo, n'importe qui peut modifier ou supprimer toutes les données.

**Recommandation** : pour la démo, prévoir des données d'exemple réinitialisées régulièrement (seed + reset), et combiner avec S4.

### S4 — Pas de rate limiting

**Constat** : aucune limite de débit.

**Risque** : sur la démo, un script peut remplir la base ou saturer l'API.

**Recommandation** : utiliser le rate limiter intégré à ASP.NET Core (`AddRateLimiter` / `UseRateLimiter`), par exemple avec une fenêtre fixe par adresse IP.

### S5 — Longueurs maximales non validées — vérifié

**Constat** : la base limite `title` et `company` à 128 caractères et `url` à 255, mais le validateur ne vérifie pas ces longueurs. Un titre de 200 caractères provoque une `DbUpdateException`, donc une `500`. En Development, la réponse contient la stack trace et le détail SQL.

**Risque** : erreur non maîtrisée, fuite d'informations techniques si l'environnement est mal configuré.

**Recommandation** : ajouter `MaximumLength` dans le validateur, avec les mêmes valeurs que le mapping (idéalement des constantes partagées). Voir aussi T3 pour la longueur de l'URL.

### S6 — Pas de gestion globale des erreurs — vérifié

**Constat** : pas de `AddProblemDetails` ni de `UseExceptionHandler`. Un enum envoyé en texte (`"status": "Applied"`) renvoie une `400` sous forme de page d'exception en texte brut en Development. Les erreurs de validation, elles, sont au format ProblemDetails.

**Risque** : faible. En Production, ASP.NET Core ne renvoie pas la stack trace, mais le format des erreurs n'est pas homogène pour le front.

**Recommandation** :

```csharp
builder.Services.AddProblemDetails();
app.UseExceptionHandler();
app.UseStatusCodePages();
```

### S7 — `GET /job-applications` non paginé

**Constat** : la liste renvoie toutes les lignes.

**Risque** : temps de réponse et mémoire croissants. Sur la démo, combiné à S3 et S4, cela permet de dégrader le service.

**Recommandation** : pagination, prévue dans la prochaine boucle, avec une taille de page maximale imposée côté serveur.

### S8 — `AllowedHosts: "*"`

**Constat** : le filtrage par en-tête `Host` est désactivé.

**Recommandation** : pour la démo, renseigner le domaine réel dans la configuration de Production.

### S9 — HTTPS derrière un proxy

**Constat** : `UseHttpsRedirection` est présent, mais il n'y a ni `UseHsts` ni configuration des forwarded headers.

**Recommandation** : pour la démo, qui sera probablement derrière un reverse proxy chargé du TLS, ajouter `UseForwardedHeaders` (sinon la redirection HTTPS boucle ou se trompe de schéma) et `UseHsts` hors Development.

### S10 — CORS non configuré

**Constat** : pas de politique CORS. Le front ne pourra pas appeler l'API depuis un autre domaine ou un autre port.

**Recommandation** : déclarer une politique avec des origines explicites, lues depuis la configuration. Éviter `AllowAnyOrigin`.

### S11 — Gestion des secrets

**Constat** : la chaîne de connexion de dev, mot de passe compris, est dans `appsettings.Development.json`. Ce sont des identifiants locaux par défaut, c'est acceptable. Le fichier `.env` est bien ignoré par git.

**Recommandation** : pour l'instance locale et la démo, fournir la chaîne de connexion par variable d'environnement (`ConnectionStrings__DefaultConnection`), avec un mot de passe propre à chaque instance. Ne jamais la committer.

### S12 — Dépendances et image Docker — vérifié

**Constat** : `dotnet list package --vulnerable --include-transitive` ne remonte aucun package vulnérable (2026-11-03). L'image `postgres:17` est référencée par un tag mobile.

**Recommandation** : automatiser ce contrôle dans la CI et activer Dependabot (NuGet, Docker, GitHub Actions).

## Points positifs

- Les DTO sont séparés de l'entité : le client ne peut pas modifier l'Id ni les dates techniques.
- L'Id est généré côté serveur (UUID v7).
- Les paramètres de route sont typés (`{id:guid}`) et les requêtes passent par EF Core, sans SQL concaténé.
- OpenAPI et Swagger UI ne sont exposés qu'en Development.
- Les tests d'intégration tournent sur un vrai PostgreSQL, avec les migrations.

## Remarques techniques

### T1 — Contrat `status` asymétrique — vérifié

En requête, `status` est l'enum sous forme numérique (`1`). La valeur texte (`"Applied"`) est refusée, car aucun `JsonStringEnumConverter` n'est configuré. En réponse, `status` est un libellé d'affichage (`"Offer received"`). Le front ne peut donc pas renvoyer tel quel ce qu'il a reçu.

**Recommandation** : configurer `JsonStringEnumConverter` et renvoyer le nom de l'enum (`Offer`) dans les deux sens. Les libellés relèvent du front (traductions).

### T2 — Nommage `AppliedAt` / `AppliedOn`

Le DTO utilise `AppliedAt` et l'entité `AppliedOn`. C'est une date (`DateOnly`), donc `AppliedOn` est le plus juste. À aligner dans le contrat.

### T3 — Longueur de l'URL

255 caractères, c'est court pour une URL d'offre (les liens LinkedIn ou Indeed avec paramètres de suivi dépassent souvent cette limite). Passer à 2048 demande une migration.

### T4 — Erreurs de validation

Les messages FluentValidation sont traduits selon la culture du serveur (français sur ce poste). Les clés reprennent le nom C# (`Title`) au lieu du nom JSON (`title`). Pour un contrat stable côté front :

- fixer la culture (`ValidatorOptions.Global.LanguageManager.Culture`) ;
- configurer le nommage des propriétés en camelCase (`ValidatorOptions.Global.PropertyNameResolver`).

### T5 — Migrations avec conservation des données

L'instance locale contiendra de vraies données. Avant chaque migration :

- relire la migration générée (un renommage peut devenir une suppression suivie d'un ajout) ;
- sauvegarder la base (`pg_dump`) ;
- appliquer la migration avec un bundle (`dotnet ef migrations bundle`).
