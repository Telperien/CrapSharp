# CrapSharp — suivi d'avancement

> Fichier destiné à la **review par une autre IA ou un humain**. Mis à jour à chaque étape.
> Objectif, conventions de mesure et feuille de route : [PLAN.md](PLAN.md). Reviews : [REVIEWS.md](REVIEWS.md).
> Dernière mise à jour : 2026-11-01 — étapes 1 à 5 terminées, règle de correspondance partielle ajustée,
> **rien n'est commité** (demande de l'utilisateur).

## Résumé de l'état

| # | Étape | État |
|---|---|---|
| 1-4 | Outil `crapsharp` : complexité, couverture, score, CLI complète | fait |
| 5 | Corrections issues des reviews 1 (Codex) et 2 (Claude) | fait |
| 6 | Fixtures réelles et tests de bout en bout | **prochaine étape** |
| 7 | Comparaison avec ReportGenerator | à faire |
| 8 | Skill complet (discipline ub-mode, `crapsharp`, Stryker.NET) + README | à faire |
| 9 | Packaging `dotnet tool` | à faire |
| 10-11 | Microsoft.Testing.Platform, duplication | à faire |

`dotnet test` : **127 tests, 0 échec** (net10.0). L'outil publié, lancé sur le dépôt (`crapsharp . --threshold 10`),
exécute lui-même les tests avec couverture, se note, et sort avec le code 0 : pire CRAP = 9,06
(`MemberNaming.MemberName`, CC 9, 90 %).

## Ce qui a changé à l'étape 5

| Changement | Fichiers |
|---|---|
| Seuil strict : un score **≥ N** échoue ; message « threshold not met: k method(s) score N or more » | `Scoring/ThresholdCheck.cs`, `App.cs` |
| Affichage tronqué (tableau : CRAP à 2 décimales, couverture en % entier ; JSON : 2 et 1 décimales) | `Scoring/Truncation.cs`, `ReportFormatter.cs`, `JsonReportWriter.cs` |
| Cible `net10.0` + `RollForward=Major` (outil et tests) | les deux `.csproj` |
| Membres `[ExcludeFromCodeCoverage]` / `[ExcludeFromCoverage]` (membre, propriété, type englobant) non notés | `Complexity/CoverageExclusion.cs`, `ComplexityCalculator.cs` |
| Instructions de niveau supérieur : une entrée `<top-level statements>` par fichier | `Complexity/ComplexityCalculator.cs` |
| Échec de `dotnet test` / de la commande de couverture : rapport affiché, puis **code 3** | `Coverage/CoverageProvider.cs` (`RunFailed`), `App.cs`, `ExitCodes.cs` |
| Avertissement si aucun fichier analysé n'apparaît dans la couverture | `Coverage/CoverageMap.cs` (`Contains`), `App.cs` |
| Message « aucun rapport » qui mentionne Microsoft.Testing.Platform | `Coverage/CoverageProvider.cs` |
| Options strictes (option inconnue, valeur manquante, second chemin → code 1) ; `--help` / `-h` | `Options.cs` (`OptionsException`, `Usage`), `App.Run(string[])` |
| `Program.cs` réduit à une ligne (le traitement des erreurs d'options est testé dans `App`) | `Program.cs` |

24 tests ajoutés à l'étape 5 (102 → 126), et les tests existants du seuil, de l'arrondi et des options adaptés.

## Décision après la troisième review

Une source de production absente d'un rapport valide garde une couverture de 0 %. Une correspondance partielle
produit un avertissement qui nomme les projets concernés ; elle ne bloque pas l'analyse. Si aucun fichier analysé ne
correspond, l'outil s'arrête avec le code 1, sans produire de rapport. Le changement concerne `App.cs`,
`Projects/ProjectDiscovery.cs` et `AppTests.cs` ; la suite compte maintenant 127 tests.

## Usage

```bash
crapsharp [chemin] [options]       # chemin = dossier, .sln, .csproj ou fichier .cs (défaut : .)
crapsharp --help
```

| Option | Effet |
|---|---|
| *(aucune)* | lance `dotnet test --collect:"XPlat Code Coverage"` sur chaque projet de tests, puis note tout |
| `--coverage <xml>` (répétable) | utilise ces rapports Cobertura, ne lance rien |
| `--use-existing-coverage` | réutilise les rapports déjà présents (le plus récent par run, sous la racine) |
| `--coverage-command "<cmd>"` | lance cette commande (via un `.cmd` temporaire ou `sh -c`) et utilise les `*cobertura*.xml` qu'elle a écrits |
| `--no-coverage` | complexité seule (CRAP et couverture affichés `-`) |
| `--changed` | ne garde que les `.cs` modifiés / ajoutés / non suivis selon `git status` |
| `--filter <fragment>` | ne garde que les fichiers dont le chemin contient le fragment (`/` ou `\`) |
| `--threshold <N>` | code 2 sauf si **toutes** les méthodes sont strictement sous N |
| `-h`, `--help` | aide |

Priorité si plusieurs sources de couverture : `--no-coverage` > `--coverage` > `--coverage-command` >
`--use-existing-coverage` > défaut. Codes de sortie : 0 ok, 1 erreur, 2 seuil non respecté, 3 tests ou commande de
couverture en échec (priorité 1 > 3 > 2 ; voir PLAN.md §4.5).
Sortie : tableau texte (stdout, pire d'abord) + `<racine>/.metrics/crap.json` quand il y a une couverture.

## Structure du code

Voir PLAN.md §5. Points utiles pour relire :
- `App` est tout le programme sauf le point d'entrée ; il reçoit un `IProcessRunner` pour être testé sans processus.
- `TestSupport.cs` : `FakeProcessRunner` (aucun vrai processus) et `TempWorkspace` (mini-solution sur disque).

## Limites connues

- Rapprochement par plages de lignes : approximation (voir PLAN.md §4.4), pas encore mesurée sur de vrais rapports.
- Exclusion `[ExcludeFromCodeCoverage]` : écrite d'après le comportement documenté de coverlet, **non vérifiée** sur un
  vrai rapport.
- Projets de tests sur Microsoft.Testing.Platform : `--collect` n'y fonctionne probablement pas ; seul le message
  d'erreur a été adapté, sans essai réel.
- `--changed` : chemins git entre guillemets (caractères non ASCII) mal décodés ; le JSON ne contient alors que les
  fichiers filtrés (il écrase le rapport complet précédent). Même chose avec `--filter`.
- `--use-existing-coverage` prend tout fichier `*cobertura*.xml` sous la racine (hors bin/obj/.git/node_modules).
- Les `.sln` ne sont pas ouverts : on cherche les `.csproj` sous le dossier.
- Détection d'un projet de tests par mots-clés dans le `.csproj` (pas d'évaluation MSBuild, donc pas de
  `Directory.Build.props`).
- Une correspondance partielle est signalée par projet ; les méthodes absentes du rapport comptent à 0 %.
  Aucun fichier apparié donne le code 1. Le cas d'un projet de tests sans rapport parmi plusieurs reste à vérifier.
- Non mesuré : lambdas dans les initialiseurs de champs, branches `#if` inactives.
- Opérateurs nommés `operator +` et non `op_Addition`.

## Vérifications faites à la main (2026-11-01, non automatisées)

Avec l'outil publié (`dotnet publish src/CrapSharp -o <dossier>`), sur ce dépôt :
- `--help` : aide, code 0 ; `--bogus` : « Unknown option », code 1 ;
- défaut avec `--threshold 10` : lance les tests, tableau, JSON, code 0 ; l'entrée `<top-level statements>` de
  `Program.cs` apparaît (CC 1, 0 %, CRAP 2).

Vérifications plus anciennes (étape 4, avant les changements de l'étape 5) : `--use-existing-coverage`, `--filter`,
`--no-coverage`, `--changed`, `--coverage-command` avec guillemets internes.

## Reste à faire

Voir la feuille de route (PLAN.md §6). Prochaine étape : **6, fixtures réelles et tests de bout en bout**.

## Comment vérifier

```bash
dotnet test                                                     # 127 tests attendus
dotnet publish src/CrapSharp -o ./out
dotnet ./out/CrapSharp.dll . --threshold 10                     # lance les tests avec couverture, doit sortir 0
```

Environnement : Windows 11, SDK .NET 10.0.401, runtime 10.0.12.
