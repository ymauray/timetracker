# Contribuer

Merci de l'intérêt porté à TimeTracker ! C'est un petit projet personnel, mais les contributions externes sont bienvenues.

## Signaler un bug / proposer une fonctionnalité

Ouvrez une [issue](https://github.com/ymauray/timetracker/issues/new/choose) en utilisant le modèle correspondant.

## Proposer une modification de code

1. Forkez le dépôt et créez une branche depuis `main`.
2. Installez le [.NET 10 SDK](https://dotnet.microsoft.com/download).
3. Faites vos changements. Ajoutez ou mettez à jour les tests unitaires (`tests/TimeTracker.Tests`) pour tout changement de comportement.
4. Vérifiez que tout passe localement :
   ```
   dotnet build
   dotnet test
   ```
5. Ouvrez une pull request vers `main` en décrivant le changement et son motif. Le modèle de PR vous guide.

## Style de code

Le fichier `.editorconfig` à la racine définit les conventions de style C#. La plupart des éditeurs (Visual Studio, VS Code, JetBrains Rider) l'appliquent automatiquement.

## Portée du projet

TimeTracker reste volontairement simple : un fichier de pointage en Markdown, un bilan Markdown/PDF, pas de base de données ni d'interface graphique. Les propositions qui vont dans ce sens seront probablement refusées, mais n'hésitez pas à ouvrir une issue pour en discuter avant de coder quelque chose de conséquent.
