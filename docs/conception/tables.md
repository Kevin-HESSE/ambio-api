# Tables

Les types enum sont des énumérations définies dans l'application. Elles seront converties en chaînes de caractères
automatiquement par EF Core.

## Candidatures

La table `job_applications` représente une candidature.

| Champ         | Type         | Spécificité | Description                                            |
|---------------|--------------|-------------|--------------------------------------------------------|
| `id`          | UUID         | PRIMARY KEY | Identifiant unique de la candidature                   |
| `title`       | varchar(255) | NOT NULL    | Titre de la candidature                                |
| `company`     | varchar(255) | NOT NULL    | Entreprise de la candidature                           |
| `kind`        | varchar(255) | NOT NULL    | Colonne discriminante indiquant le type de candidature |
| `status`      | enum         | NOT NULL    | Statut actuel de la candidature                        |
| `applied_on`  | date         | NOT NULL    | Date d'envoi de la candidature                         |
| `created_at`  | timestamptz  | DEFAULT NOW | Date de création de la candidature                     |
| `updated_at`  | timestamptz  | DEFAULT NOW | Date de mise à jour de la candidature                  |
| `archived_at` | timestamptz  | NULLABLE    | Date d'archivage de la candidature                     |

> La colonne `status` reflète la dernière entrée de l'historique.

> La colonne `kind` ne peut avoir qu'une des valeurs suivantes : `offer` ou `spontaneous`.

### Champs supplémentaires pour une offre

La candidature est créée à partir d'une offre d'emploi.

| Champ | Type | Spécificité | Description    |
|-------|------|-------------|----------------|
| `url` | text | NULLABLE    | URL de l'offre |

> La colonne `url` est obligatoire dans le cadre d'une offre (kind = `offer`) mais doit rester `NULLABLE` en raison de
> la [stratégie](#stratégie).

### Champs supplémentaires pour une candidature spontanée

La candidature est créée spontanément. Elle n'a aucun champ spécifique.

### Stratégie

La stratégie suivra le TPH (Table Per Hierarchy). Les deux classes supplémentaires, pour répondre aux besoins,
hériteront d'une classe abstraite.

| Nom                      | Abstraite | Hérite de        | Discriminant  |
|--------------------------|-----------|------------------|:--------------|
| `JobApplication`         | Oui       | Aucun            | Aucun         |
| `OfferApplication`       | Non       | `JobApplication` | `offer`       |
| `SpontaneousApplication` | Non       | `JobApplication` | `spontaneous` |

## Historique d'une candidature

La table `status_history` représente l'historique des statuts d'une candidature. Elle trace les changements de statut
d'une candidature à partir de son envoi. L'historique ne peut pas être modifié par le client.

| Champ                | Type        | Spécificité                                    | Description                                        |
|----------------------|-------------|------------------------------------------------|----------------------------------------------------|
| `id`                 | UUID        | PRIMARY KEY                                    | Identifiant unique de l'entrée                     |
| `job_application_id` | UUID        | NOT NULL, FK référençant `job_applications.id` | Identifiant de la candidature                      |
| `status_from`        | enum        | NOT NULL                                       | Statut précédent de la candidature                 |
| `status_to`          | enum        | NOT NULL                                       | Nouveau statut de la candidature                   |
| `changed_at`         | date        | NOT NULL                                       | Date du changement de statut, donnée par le client |
| `created_at`         | timestamptz | DEFAULT NOW                                    | Date de création de l'entrée, côté serveur         |

Lors de la création d'une candidature, une entrée est créée dans cette table avec le statut actuel de la candidature
dans la colonne `status_to`, et la valeur `created` dans la colonne `status_from`. Un historique commence donc toujours
par la combinaison suivante :

- `created` → `sent` : la colonne `changed_at` prend la valeur de `applied_on` de la candidature.

> La valeur `created` est propre à la création de la candidature et n'est utilisée qu'à cet endroit. Elle permet de
> tracer la création de la candidature de façon claire et explicite. Ainsi, la colonne `status_from` n'est jamais nulle.
> Les autres valeurs possibles sont identiques à celles de la colonne `status_to`.
