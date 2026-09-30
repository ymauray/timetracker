# TimeTracker

[![CI](https://github.com/ymauray/timetracker/actions/workflows/ci.yml/badge.svg)](https://github.com/ymauray/timetracker/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/ymauray/timetracker)](https://github.com/ymauray/timetracker/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Petit outil en ligne de commande qui lit un pointage d'heures de travail au format Markdown et génère un bilan hebdomadaire et mensuel (Markdown + PDF), avec report de solde d'un mois sur l'autre.

Site : https://ymauray.github.io/timetracker/

## Fonctionnalités

- Pointage quotidien en Markdown (arrivée, pause déjeuner, départ) — un seul fichier texte, éditable partout.
- Règle de pause minimale configurable (ex : une pause de 15 min est quand même décomptée comme 30 min, et une journée sans pause perd aussi 30 min).
- Jours d'absence (Congés, Maladie, Férié, RTT, Divers) neutres sur le solde d'heures.
- Bilan hebdomadaire et bilan mensuel avec report du solde d'un mois sur l'autre et alerte de tolérance (± une plage configurable, 10 h par défaut).
- Jour ouvré oublié dans le pointage → signalé et compté en déficit.
- Génère un rapport Markdown et un rapport PDF mis en forme.
- Binaire natif autonome (self-contained, un seul fichier) : aucune installation de runtime .NET nécessaire.

## Installation

Téléchargez le binaire correspondant à votre système depuis la [dernière release](https://github.com/ymauray/timetracker/releases/latest) :

| OS | Fichier |
|---|---|
| Windows x64 | `TimeTracker-win-x64.exe` |
| Linux x64 | `TimeTracker-linux-x64` |
| macOS Intel | `TimeTracker-osx-x64` |
| macOS Apple Silicon | `TimeTracker-osx-arm64` |

Sur Linux/macOS, rendez le fichier exécutable : `chmod +x TimeTracker-*`.

Aucune dépendance : le binaire est autonome (runtime .NET et police embarqués).

## Utilisation

```
TimeTracker [-r releve.md] [-b bilan.md] [--no-pdf] [-h]
```

| Option | Description | Défaut |
|---|---|---|
| `-r`, `--releve <chemin>` | Fichier de pointage à lire | `releve.md` |
| `-b`, `--bilan <chemin>` | Fichier bilan Markdown à générer (le PDF est créé à côté, même nom, extension `.pdf`) | `bilan.md` |
| `--no-pdf` | Ne génère pas le PDF | — |
| `-h`, `--help` | Affiche l'aide | — |

### Format de `releve.md`

Un en-tête optionnel (*front-matter*) définit les paramètres de calcul, suivi d'un tableau Markdown :

```
---
duree_journee: 8h12
pause_minimum: 0h30
seuil_pause: 5h
tolerance: 10h
---
|Date      |Arrivée|Début pause|Fin pause|Départ|Absence|
|----------|-------|-----------|---------|------|-------|
|28.09.2026|8h50   |12h00      |12h20    |16h50 |       |
|29.09.2026|       |           |         |      |Congés |
```

- Dates au format `j.m.aaaa` (jour et mois à 1 ou 2 chiffres, ex : `1.10.2026`, `21.02.2027`, `28.09.2026`), heures au format `Hh` ou `HhMM` (ex : `8h`, `8h30`).
- Colonne `Absence` : vide (jour travaillé) ou un code parmi `Conges`/`CP`, `Maladie`, `Ferie`/`Férié`, `RTT`, `Divers`.
- Sans front-matter, les valeurs par défaut s'appliquent (`duree_journee: 8h12`, `pause_minimum: 0h30`, `seuil_pause: 5h`, `tolerance: 10h`). Une clé absente prend sa valeur par défaut.
- `pause_minimum` est décomptée de tout jour travaillé : une pause plus courte, ou aucune pause saisie, compte pour cette durée.
- `seuil_pause` : une journée dont la présence (départ − arrivée) est inférieure à ce seuil ne perd que sa pause réelle, sans minimum. À 5h pile, la pause minimum s'applique.
- `tolerance` : écart maximal du solde mensuel cumulé, en plus ou en moins, avant l'alerte « hors tolérance ».
- Une ligne sans date (colonne `Date` vide) est ignorée : pratique pour aérer visuellement le tableau entre deux semaines.
- Un jour ouvré absent du fichier est compté comme un déficit de la journée entière.

Toute ligne invalide (date, heure, code absence, cohérence horaire...) est rapportée avec son numéro de ligne ; aucun bilan n'est généré tant que le fichier contient des erreurs.

## Compilation depuis les sources

Prérequis : [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build
dotnet test
dotnet publish src/TimeTracker -c Release -r <RID> --self-contained -p:PublishSingleFile=true
```

`<RID>` : `win-x64`, `linux-x64`, `osx-x64` ou `osx-arm64`.

> Un binaire compilé ainsi répond `0.0.0.0` à `--version` : la vraie version n'est injectée que par la [release CI](#intégration-continue-et-releases) via `-p:Version=X.Y.Z`. Pour tester un numéro de version précis en local, ajoutez `-p:Version=X.Y.Z` à la commande ci-dessus.

## Intégration continue et releases

- **CI** (`.github/workflows/ci.yml`) : build + tests unitaires (xUnit) à chaque push et pull request sur `main`.
- **Release** (`.github/workflows/release.yml`) : déclenchée par un tag `vX.Y.Z` ; compile et publie les binaires autonomes des quatre plateformes en pièces jointes de la [release GitHub](https://github.com/ymauray/timetracker/releases) correspondante.

## Licence

[MIT](LICENSE).

La police [Roboto](https://fonts.google.com/specimen/Roboto) (Google, [SIL Open Font License 1.1](https://openfontlicense.org/)) est embarquée dans le binaire pour la génération des PDF.
