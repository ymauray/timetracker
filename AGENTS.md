# AGENTS.md

Instructions pour les assistants IA travaillant sur ce dépôt.

## Le projet

TimeTracker est un outil CLI .NET 10 qui lit un pointage d'heures de travail (`releve.md`, tableau Markdown + front-matter de config) et génère un bilan hebdomadaire/mensuel en Markdown et PDF. Binaire self-contained, single-file, sans dépendance externe (police PDF embarquée).

## Structure

```
src/TimeTracker/        Application (top-level Program.cs)
tests/TimeTracker.Tests/ Tests xUnit
docs/                    Site GitHub Pages statique (pas de build step)
.github/workflows/       CI (build+test) et release (publication multi-plateforme sur tag vX.Y.Z)
```

## Commandes

```
dotnet build
dotnet test
dotnet publish src/TimeTracker -c Release -r <win-x64|linux-x64|osx-x64|osx-arm64> --self-contained -p:PublishSingleFile=true
```

## Règles métier (ne pas les redécouvrir en lisant le code seul, elles sont réparties entre `TimeCalculator.cs` et `ConfigParser.cs`)

- Journée de référence et pause minimum sont **configurables** via le front-matter de `releve.md` (`duree_journee`, `pause_minimum`, `seuil_pause`, `tolerance`), avec défauts 8h12 / 0h30 / 5h / 10h si absents.
- Pause décomptée d'un jour travaillé = `max(pause réelle, pause_minimum)` — jamais moins que le minimum, **y compris quand aucune pause n'est saisie** (règlement : une journée sans pause de midi perd la pause minimum).
- Exception : une journée dont la présence (départ − arrivée) est **strictement inférieure** à `seuil_pause` ne décompte que sa pause réelle (zéro si aucune).
- Un jour marqué avec un code d'absence (`Conges`/`CP`, `Maladie`, `Ferie`, `RTT`, `Divers`) est neutre : réalisé = théorique, aucun impact sur le solde. `CP` reste un alias accepté pour `Conges` (saisie historique).
- `Demi` exige des horaires : réalisé = temps travaillé (règles de pause comprises) + théorique ÷ 2, arrondi à la minute inférieure. Sert au demi-congé, à la demi-RTT, à l'après-midi férié.
- `Maladie` accepte des horaires (malaise en cours de journée) : réalisé = temps travaillé, complété jusqu'au théorique s'il en manque. Les autres codes refusent les horaires.
- Une ligne du tableau sans date (colonne `Date` vide) est silencieusement ignorée par `ReleveParser` — sert à aérer visuellement `releve.md`. Ne pas la traiter comme une erreur.
- Les dates acceptent 1 ou 2 chiffres pour le jour et le mois (`d.M.yyyy`), pas seulement `dd.MM.yyyy`.
- Un jour ouvré absent du fichier (ni pointage ni code d'absence) compte comme un déficit total de la journée — ce n'est **pas** ignoré.
- Le solde mensuel se cumule d'un mois sur l'autre (pas de remise à zéro) ; la tolérance (`tolerance`, 10h par défaut) s'évalue sur ce cumul, pas sur l'écart du mois seul.
- Toutes les chaînes de format passées à des fonctions Excel/QuestPDF/`TEXT()`-like doivent éviter les codes de format anglais localisés (ex. `dddd`) — préférer un mapping explicite (`CHOOSE`/tableau de libellés) quand la sortie doit être indépendante de la langue du logiciel qui l'ouvre. Historique : ce bug s'est produit une fois sur le prototype Excel du projet.

## Fixtures partagées

`tests/TimeTracker.Tests/Fixtures/` contient des paires `releve.md` + `attendu.json` calculées à la main, vérifiées par `FixturesTests.cs`. L'app iOS (dépôt séparé `timetracker.ios`) en garde une copie et doit produire les mêmes résultats : toute évolution d'une règle métier passe par une fixture, et la copie iOS doit être mise à jour. Le format est décrit dans `Fixtures/LISEZMOI.md`.

## Style

`.editorconfig` à la racine fait foi. Pas de commentaires inutiles, noms de méthodes/variables en français (cohérence avec le domaine et le fichier `releve.md` que l'utilisateur édite directement).

## CI

- `ci.yml` : build + test sur push/PR vers `main`.
- `release.yml` : sur tag `vX.Y.Z`, publie 4 binaires (win-x64, linux-x64, osx-x64, osx-arm64) en assets de la release GitHub. La version est injectée via `-p:Version=` depuis le tag, ne pas la coder en dur ailleurs.

## Versioning

Le `.csproj` a `<Version>0.0.0.0</Version>` en dur comme valeur par défaut. C'est volontaire : un `dotnet publish` local sans `-p:Version=X.Y.Z` produira donc un binaire qui répond `0.0.0.0` à `--version` — ce n'est pas un bug, c'est ce qui permet de distinguer un build de dev d'un binaire officiel issu de `release.yml`. Ne pas "corriger" cette valeur par défaut ; documenter/expliquer plutôt que modifier si quelqu'un le signale.
