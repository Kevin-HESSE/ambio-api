# Endpoints

<!-- TOC -->
* [Endpoints](#endpoints)
  * [Candidatures](#candidatures)
    * [Règle générale](#règle-générale)
    * [Route `GET /job-applications`](#route-get-job-applications)
      * [Filtres](#filtres)
      * [Réponse](#réponse)
    * [Route `GET /job-applications/{id}`](#route-get-job-applicationsid)
      * [Réponse](#réponse-1)
    * [Route `POST /job-applications`](#route-post-job-applications)
      * [Requête](#requête)
      * [Réponse](#réponse-2)
    * [Route `POST /job-applications/spontaneous`](#route-post-job-applicationsspontaneous)
      * [Requête](#requête-1)
      * [Réponse](#réponse-3)
    * [Route `PUT /job-applications/{id}`](#route-put-job-applicationsid)
      * [Requête](#requête-2)
      * [Réponse](#réponse-4)
    * [Route `POST /job-applications/{id}/status`](#route-post-job-applicationsidstatus)
      * [Requête](#requête-3)
      * [Réponse](#réponse-5)
    * [Route `POST /job-applications/{id}/archive`](#route-post-job-applicationsidarchive)
      * [Requête](#requête-4)
      * [Réponse](#réponse-6)
    * [Route `POST /job-applications/{id}/reopen`](#route-post-job-applicationsidreopen)
      * [Requête](#requête-5)
      * [Réponse](#réponse-7)
  * [Entreprises](#entreprises)
    * [Route `GET /companies`](#route-get-companies)
      * [Filtres](#filtres-1)
      * [Réponse](#réponse-8)
<!-- TOC -->

## Candidatures

| Lien                                            | URL                              | Verbe | Description                                                |
|-------------------------------------------------|----------------------------------|-------|------------------------------------------------------------|
| [Voir](#route-get-job-applications)             | `/job-applications`              | GET   | Récupérer toutes les candidatures non archivées par défaut |
| [Voir](#route-get-job-applicationsid)           | `/job-applications/{id}`         | GET   | Récupérer une candidature spécifique                       |
| [Voir](#route-post-job-applications)            | `/job-applications`              | POST  | Créer une nouvelle candidature depuis une offre            |
| [Voir](#route-post-job-applicationsspontaneous) | `/job-applications/spontaneous`  | POST  | Créer une nouvelle candidature spontanée                   |
| [Voir](#route-put-job-applicationsid)           | `/job-applications/{id}`         | PUT   | Mettre à jour une candidature spécifique                   |
| [Voir](#route-post-job-applicationsidstatus)    | `/job-applications/{id}/status`  | POST  | Changer le statut d'une candidature                        |
| [Voir](#route-post-job-applicationsidarchive)   | `/job-applications/{id}/archive` | POST  | Archiver une candidature spécifique                        |
| [Voir](#route-post-job-applicationsidreopen)    | `/job-applications/{id}/reopen`  | POST  | Rouvrir une candidature spécifique déjà archivée           |

### Règle générale

- Les candidatures archivées sont en lecture seule. Toute tentative de modification renvoie un code `409`.

### Route `GET /job-applications`

La route renvoie une liste paginée de candidatures. Par défaut, seules les candidatures non archivées sont retournées.

#### Filtres

Les filtres suivants sont disponibles. Ils peuvent se cumuler :

| Paramètre      | Description                                      | Valeur                     | Règle                 | Défaut | Exemple                                   |
|----------------|:-------------------------------------------------|----------------------------|:----------------------|:-------|-------------------------------------------|
| `status`       | Filtre par statut, plusieurs valeurs possibles   | [Voir](./description.md)   | Aucune                | Aucune | `/job-applications?status=sent,interview` |
| `search`       | Filtre par titre par une recherche partielle     | Chaîne de caractères       | Insensible à la casse | Aucune | `/job-applications?search=php`            |
| `company`      | Filtre par entreprise par une recherche stricte  | Chaîne de caractères       | Insensible à la casse | Aucune | `/job-applications?company=acme`          |
| `archived`     | Filtre sur l'archivage                           | `none`, `only`, `included` | Aucune                | `none` | `/job-applications?archived=only`         |
| `page`         | Détermine le lot d'éléments renvoyés             | Nombre                     | Strictement positif   | 1      | `/job-applications?page=2`                |
| `itemsPerPage` | Détermine le nombre d'éléments renvoyés par page | Nombre                     | Min: 1, Max: 100      | 10     | `/job-applications?itemsPerPage=50`       |
| `order[date]`  | Trie les candidatures par date de création       | `asc`, `desc`              | Aucune                | `desc` | `/job-applications?order[date]=asc`       |
| `order[title]` | Trie les candidatures par titre                  | `asc`, `desc`              | Aucune                | Aucune | `/job-applications?order[title]=asc`      |

Si une valeur incorrecte est fournie dans les filtres, un message d'erreur est renvoyé.

> L'ordre par défaut affiche les candidatures créées de la plus récente à la plus ancienne. Lorsque plusieurs tris sont
> demandés, la priorité est la suivante (du plus prioritaire au moins prioritaire) :
> - date
> - titre

> Définitions des valeurs possibles pour le filtre `archived` :
> - `none` : aucune candidature archivée n'est renvoyée
> - `only` : seules les candidatures archivées sont renvoyées
> - `included` : les candidatures archivées et non archivées sont renvoyées

> Si la valeur du paramètre `page` est supérieure à la dernière page, la dernière page est utilisée.

#### Réponse

Les status codes possibles sont :

| Status code | Raison                             |
|-------------|------------------------------------|
| `200`       | Candidatures renvoyées avec succès |
| `400`       | Filtre incorrect                   |

Le contenu de la réponse renvoyée, en cas de succès, est le suivant :

```json
{
    "data": [
        {
            "id": "string",
            "title": "string",
            "company": "string",
            "url": "string|null",
            "currentStatus": "string",
            "appliedOn": "string",
            "archivedAt": "string|null"
        }
    ],
    "meta": {
        "itemsPerPage": "number",
        "currentPage": "number",
        "totalItems": "number",
        "first": "string",
        "last": "string",
        "previous": "string|null",
        "next": "string|null"
    }
}
```

Les propriétés `first` et `last` sont des URLs vers la première et la dernière page. Quand il n'y a qu'une seule page,
`first` et `last` pointent vers la page courante. `previous` est `null` si la page précédente n'existe pas. `next` est
`null` si la page suivante n'existe pas.

Toutes les URLs sont absolues et conservent les filtres actifs.

### Route `GET /job-applications/{id}`

La route renvoie le détail d'une candidature avec l'historique de ses statuts. Une candidature archivée est aussi
renvoyée.

#### Réponse

Les status codes possibles sont :

| Status code | Raison                           |
|-------------|----------------------------------|
| `200`       | Candidature renvoyée avec succès |
| `404`       | Candidature non trouvée          |

Le contenu de la réponse renvoyée, en cas de succès, est le suivant :

```json
{
    "id": "string",
    "title": "string",
    "company": "string",
    "kind": "string",
    "url": "string|null",
    "appliedOn": "string",
    "currentStatus": "string",
    "history": [
        {
            "status": "string",
            "date": "string"
        }
    ],
    "archivedAt": "string|null"
}
```

> La propriété `history` contient les enregistrements de la table `status_history`. La sous-propriété `status` contient
> la valeur de `status_to`, et la sous-propriété `date` contient la date à laquelle le statut a été modifié.
> L'historique est ordonné du plus ancien au plus récent selon leur date de création (created_at).

### Route `POST /job-applications`

La route permet de créer une nouvelle candidature depuis une offre.

#### Requête

```json
{
    "title": "string",
    "company": "string",
    "url": "string",
    "appliedOn": "string"
}
```

| Propriété   | Valeur                         | Obligatoire | Description                       |
|-------------|--------------------------------|-------------|-----------------------------------|
| `title`     | chaîne de caractères           | Oui         | Le titre de la candidature        |
| `company`   | chaîne de caractères           | Oui         | L'entreprise de la candidature    |
| `url`       | URL valide                     | Oui         | L'URL de l'offre                  |
| `appliedOn` | une date du jour ou antérieure | Oui         | La date d'envoi de la candidature |

#### Réponse

Les status codes possibles sont :

| Status code | Raison                                           |
|-------------|--------------------------------------------------|
| `201`       | Candidature créée avec succès                    |
| `400`       | Erreur de validation dans le corps de la requête |

Le contenu de la réponse renvoyée, en cas de succès, est identique à celui de la route `GET /job-applications/{id}`.

### Route `POST /job-applications/spontaneous`

La route permet de créer une nouvelle candidature sans offre précise.

La propriété `url` n'est pas nécessaire, car une candidature spontanée ne dépend d'aucune offre.

#### Requête

```json
{
    "title": "string",
    "company": "string",
    "appliedOn": "string"
}
```

| Propriété   | Valeur                         | Obligatoire | Description                       |
|-------------|--------------------------------|-------------|-----------------------------------|
| `title`     | chaîne de caractères           | Oui         | Le titre de la candidature        |
| `company`   | chaîne de caractères           | Oui         | L'entreprise de la candidature    |
| `appliedOn` | une date du jour ou antérieure | Oui         | La date d'envoi de la candidature |

#### Réponse

Les status codes possibles sont :

| Status code | Raison                                           |
|-------------|--------------------------------------------------|
| `201`       | Candidature créée avec succès                    |
| `400`       | Erreur de validation dans le corps de la requête |

Le contenu de la réponse renvoyée, en cas de succès, est identique à celui de la route `GET /job-applications/{id}`.

### Route `PUT /job-applications/{id}`

La route permet de modifier certaines propriétés d'une candidature.

#### Requête

```json
{
    "title": "string",
    "company": "string",
    "url": "string|null"
}
```

| Propriété | Valeur               | Obligatoire                        | Description                    |
|-----------|----------------------|------------------------------------|--------------------------------|
| `title`   | chaîne de caractères | Oui                                | Le titre de la candidature     |
| `company` | chaîne de caractères | Oui                                | L'entreprise de la candidature |
| `url`     | URL valide ou `null` | Oui / Non si candidature spontanée | L'URL de l'offre               |

> Si `url` n'est pas `null` pour une candidature spontanée, une erreur de validation est renvoyée.
> Si `url` est `null` pour une candidature liée à une offre, une erreur de validation est renvoyée.

#### Réponse

Les status codes possibles sont :

| Status code | Raison                                           |
|-------------|--------------------------------------------------|
| `200`       | Candidature modifiée avec succès                 |
| `400`       | Erreur de validation dans le corps de la requête |
| `404`       | Candidature non trouvée                          |
| `409`       | Candidature archivée                             |

Le contenu de la réponse renvoyée, en cas de succès, est identique à celui de la route `GET /job-applications/{id}`.

### Route `POST /job-applications/{id}/status`

La route permet de modifier le statut d'une candidature. Chaque changement de statut ajoute une nouvelle entrée dans
l'historique.

#### Requête

```json
{
    "status": "string",
    "date": "string|null"
}
```

| Propriété | Valeur                                                   | Obligatoire | Description                                                              |
|-----------|----------------------------------------------------------|-------------|--------------------------------------------------------------------------|
| `status`  | `interview`, `offer`, `taken`, `rejected` ou `withdrawn` | Oui         | Le nouveau statut de la candidature                                      |
| `date`    | une date du jour ou antérieure, ou `null`                | Non         | La date du changement de statut. Si `null`, la date du jour est utilisée |

Une validation vérifie que le statut souhaité est applicable et suit l'ordre. Un statut identique au statut actuel est
interdit. Un retour en arrière est impossible.

> La date du nouveau statut ne doit pas être antérieure à celle du statut précédent.

#### Réponse

Les status codes possibles sont :

| Status code | Raison                                                         |
|-------------|----------------------------------------------------------------|
| `204`       | Statut mis à jour avec succès                                  |
| `400`       | Statut invalide ou date invalide                               |
| `404`       | Candidature non trouvée                                        |
| `409`       | Transition interdite par l'état actuel ou candidature archivée |

### Route `POST /job-applications/{id}/archive`

La route permet d'archiver une candidature.

#### Requête

Aucun corps n'est nécessaire.

#### Réponse

Les status codes possibles sont :

| Status code | Raison                           |
|-------------|----------------------------------|
| `204`       | Candidature archivée avec succès |
| `404`       | Candidature non trouvée          |
| `409`       | Candidature déjà archivée        |

### Route `POST /job-applications/{id}/reopen`

La route permet de rouvrir une candidature archivée.

#### Requête

Aucun corps n'est nécessaire.

#### Réponse

Les status codes possibles sont :

| Status code | Raison                           |
|-------------|----------------------------------|
| `204`       | Candidature rouverte avec succès |
| `404`       | Candidature non trouvée          |
| `409`       | Candidature déjà ouverte         |

## Entreprises

> Aucune table n'est prévue actuellement. La liste des entreprises est basée sur la colonne `company` de la table
> `job_applications`.

| Lien                         | URL          | Verbe | Description                        |
|------------------------------|--------------|-------|------------------------------------|
| [Voir](#route-get-companies) | `/companies` | GET   | Récupérer la liste des entreprises |

### Route `GET /companies`

La route renvoie la liste des entreprises, dans l'ordre alphabétique. Seuls les noms uniques sont renvoyés, y compris
ceux des candidatures archivées.

#### Filtres

| Paramètre | Description                                 | Valeur               | Règle                 | Défaut | Exemple              |
|-----------|:--------------------------------------------|----------------------|:----------------------|:-------|----------------------|
| `name`    | Filtre par nom avec une recherche partielle | Chaîne de caractères | Insensible à la casse | Aucune | `/companies?name=ac` |

#### Réponse

Les status codes possibles sont :

| Status code | Raison                            |
|-------------|-----------------------------------|
| `200`       | Entreprises renvoyées avec succès |

Le contenu de la réponse renvoyée, en cas de succès, est le suivant :

```json
[
    {
        "name": "string"
    }
]
```
