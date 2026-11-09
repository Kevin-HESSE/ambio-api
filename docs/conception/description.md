# Description d'Ambio

Ambio est une application web qui permet aux utilisateurs de gérer leurs candidatures aux offres d'emploi. Les offres
d'emploi se trouvent sur plusieurs plateformes telles que LinkedIn, Indeed.

Même après un retour par mail de l'entreprise, le suivi de la candidature sur le site de la plateforme n'est pas
toujours à jour. Ambio facilite ce processus en permettant aux utilisateurs de suivre leurs candidatures depuis une
seule interface.

> Le projet va évoluer petit à petit, en ajoutant de nouvelles fonctionnalités. Il sert également dans un but
> pédagogique, pour mettre en pratique les connaissances acquises.

## Fonctionnalités principales

- Voir la liste des candidatures et pouvoir la filtrer
- Créer une nouvelle candidature
- Voir le détail d'une candidature spécifique
- Mettre à jour une candidature spécifique
- Changer le statut d'une candidature spécifique
- Archiver une candidature spécifique
- Rouvrir une candidature spécifique déjà archivée

### Statuts d'une candidature et transitions

Une nouvelle candidature est considérée comme envoyée.

Une candidature envoyée peut être refusée, abandonnée ou passer à l'entretien.

Une candidature passée en entretien peut être refusée, abandonnée, ou le candidat peut recevoir une offre.

Une offre est l'avant-dernière étape. Elle peut être acceptée, refusée par l'entreprise ou abandonnée.

La désignation « refus » s'applique uniquement lorsque l'entreprise envoie une réponse négative.
La désignation « abandon » s'applique seulement lorsque le candidat arrête la candidature.

| Valeur      | Libellé    | Statut final | Suites possibles                     |
|-------------|------------|--------------|:-------------------------------------|
| `sent`      | Envoyée    | Non          | `interview`, `rejected`, `withdrawn` |
| `interview` | Entretien  | Non          | `offer`, `rejected`, `withdrawn`     |
| `offer`     | Offre      | Non          | `taken`, `rejected`, `withdrawn`     |
| `taken`     | Acceptée   | Oui          | Aucune                               |
| `rejected`  | Refusée    | Oui          | Aucune                               |
| `withdrawn` | Abandonnée | Oui          | Aucune                               |

### Archivage

Une candidature peut être archivée. Dans ce cas, elle ne peut plus être modifiée.

L'utilisateur peut rouvrir une candidature archivée.

## Stack technique

L'application va être développée avec :

- Frontend : Angular avec la bibliothèque d'UI PrimeNG
- Backend : API Minimal C# avec Entity Framework Core
- Base de données : PostgreSQL
