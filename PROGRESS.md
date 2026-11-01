# CrapSharp — suivi d'avancement

> Fichier destiné à la **review par une autre IA ou un humain**. Mis à jour à chaque étape.
> Objectif, conventions de mesure et feuille de route : [PLAN.md](PLAN.md). Reviews : [REVIEWS.md](REVIEWS.md).
> Dernière mise à jour : 2026-11-01 — étape 6 réalisée, **en attente de relecture** ; rien de l'étape 6 n'est commité.
> Un commit initial (`3d8d55f`) existe ; les travaux suivants restent dans l'arbre de travail.

## Résumé de l'état

| # | Étape | État |
|---|---|---|
| 1-4 | Outil `crapsharp` : complexité, couverture, score, CLI complète | fait |
| 5 | Corrections issues des reviews 1 (Codex) et 2 (Claude) | fait |
| 6 | Fixtures réelles et tests de bout en bout | **fait, à relire** (manques listés plus bas) |
| 7 | Comparaison avec ReportGenerator | à faire |
| 8 | Skill complet (discipline ub-mode, `crapsharp`, Stryker.NET) + README | à faire |
| 9 | Packaging `dotnet tool` | à faire |
| 10-11 | Microsoft.Testing.Platform, duplication | à faire |
| 12 | Livraison autonome et essais du skill dans plusieurs agents | à faire |
| 13 | Adaptateur Spec Kit | à terme |

`dotnet test` : **155 tests, 0 échec, 14 ignorés** (net10.0, ≈ 0,2 s). Les 14 ignorés sont les tests de bout en bout
(`CRAPSHARP_E2E=1` pour les lancer, ≈ 40 s : `CRAPSHARP_E2E=1 dotnet test --filter Category=EndToEnd`, 14 réussis).
L'outil publié, lancé sur le dépôt (`crapsharp . --threshold 10`), exécute lui-même les tests avec couverture, se
note, et sort avec le code 0, **sans avertissement** : pire CRAP = 9,06 (`MemberNaming.MemberName`, CC 9, 90 %).
`App.Run(Options)` (CRAP 10) a été découpé en `Run`, `Invalid`, `Score` : il n'apparaît plus parmi les pires.

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

## Ce qui a changé à l'étape 6

**Corrections demandées par la revue 3**, chacune d'abord par un test rouge :

| Point de la review 3 | État avant | Traitement |
|---|---|---|
| Rapports multi-cibles perdus (`NewestPerRun`) | non traité ; **reproduit** sur la fixture `multitarget` : 2 rapports, 1 lu, `Twice` à 6,00 au lieu de 2,00 | `LatestRuns` (rapports d'un même run, fenêtre de 10 min) et `All` pour le dossier vidé par l'outil |
| Seuil NaN / Infinity / négatif / 0 | accepté | refusé : « finite number above 0 » |
| Valeur manquante suivie d'une option (`--threshold --changed`) | l'option était avalée comme valeur | « expects a value » |
| Plusieurs sources de couverture | priorité silencieuse | erreur « Choose one coverage source » |
| Sélection vide | aucun traitement | voir PLAN.md §4.4 (`--filter` sans correspondance : 1 ; `--changed` sans changement : 0 ; aucun fichier de production : 1) |
| Score brut dans le JSON | seulement tronqué | `crapRaw` |
| Périmètre et provenance du JSON | absents | `scope` (`complete`, `changed`, `filter`, `files`) et `coverage` (`source`, `reports`) |
| Projet de tests sans rapport parmi plusieurs | silencieux | avertissement qui le nomme |

**Les deux problèmes du dernier commit** : `App.Run(Options)` (CC 10) et l'avertissement à tort sur `ExitCodes.cs`
(les fichiers sans membre mesurable ne comptent plus dans la correspondance de couverture).

**Fixtures et tests de bout en bout** : voir PLAN.md §6. 12 fixtures, 14 tests, outil publié par `dotnet publish -c Release`.
Valeurs attendues (CC, lignes, CRAP brut, code de sortie) calculées à la main avant la première exécution de chaque
fixture, sauf `basic`, `onecollectorless`, `nocollector` et `mtp` dont les sorties ont été constatées en même temps.

| Fixture | Ce qui est vérifié | Résultat constaté |
|---|---|---|
| `multitarget` | union des rapports net9.0 + net10.0 | 2 rapports lus ; `Abs` et `Twice` à 2,00 |
| `twotestprojects` | un rapport par projet de tests | 2 rapports ; `Alpha.Pick`, `Beta.Pick` à 2,00 |
| `excluded` | membre, propriété et type `[ExcludeFromCodeCoverage]` | absents du Cobertura **et** du tableau |
| `closures` | fonction locale, lambda, `async`, itérateur | `Score` : CC 5 conforme ; couverture **76,4 % au lieu de 70,6 % calculé** (voir ci-dessous) ; `LoadAsync` 83,3 %, `Evens` 100 % conformes |
| `sameline` | deux membres sur une ligne | `Second`, jamais appelé, apparaît à 100 % : limite connue, épinglée |
| `failingtest` | test en échec | code 3 malgré un score sous le seuil, rapport affiché, `Sign` 85,7 % / 3,0262 |
| `deterministic` | `ContinuousIntegrationBuild` + racine source | coverlet remet les chemins réels : succès |
| `deterministic-paths` | idem avec `DeterministicReport=true` | rapport en `/_/src/...` : code 1, message, pas de JSON |
| `mtp` | MSTest sur Microsoft.Testing.Platform, SDK 10 | `dotnet test` refuse le projet : code 1 et message mentionnant MTP |
| `basic` | score égal au seuil, `<top-level statements>`, `--use-existing-coverage`, `--filter`, `--changed` hors git, entrées invalides | `Ten` = 10,00 exactement → code 2 ; top-level CC 2, 0 % ; le reste conforme |
| `onecollectorless` | un projet de tests sans collecteur | avertissement nommant `Beta.Tests`, `Beta.Pick` à 6,00 |
| `nocollector` | aucun projet avec collecteur | code 1, pas de JSON |

**Écart avec le calcul à la main (un seul)** : sur `closures.Score`, j'attendais 12 lignes touchées sur 17 (le corps du lambda,
lignes 14 à 17, ne s'exécute pas). Coverlet rapporte aussi la ligne 18 (`};`, fin de l'instruction qui crée le délégué)
comme touchée : 13/17. L'outil lit le rapport tel quel ; ce n'est pas un écart avec une convention de §4. Mon
explication de la ligne 18 est une déduction à partir du rapport (lignes par méthode), pas une vérification dans coverlet.

**À valider (choix pris sans décision préalable)** : sources de couverture exclusives (le doc disait « priorité ») ;
sélection vide (`--changed` sans changement → 0, `--filter` sans correspondance → 1) ; fenêtre de 10 minutes de
`--use-existing-coverage` ; avertissement seul pour un projet de tests sans rapport ; un rapport partiel écrase
toujours `.metrics/crap.json` (il est maintenant marqué `scope.complete = false`).

**Non vérifié / non fait** (voir aussi PLAN.md §6) : rapport réellement périmé de bout en bout ; `--changed` dans un vrai
dépôt git ; correspondance partielle de chemins avec de vrais rapports ; projet sans aucun membre ; MTP avec adhésion
au nouveau `dotnet test` (`global.json`) et les deux collecteurs MTP (étape 10) ; Linux et macOS (seul Windows 11 a servi) ;
les fixtures ne sont construites qu'avec coverlet.collector 6.0.0, xunit 2.5.3 et MSTest 4.0.2. Les tests de bout en
bout dépendent du réseau (NuGet) et de la présence des runtimes .NET 9 et 10.

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

Une seule source de couverture à la fois : `--no-coverage`, `--coverage`, `--coverage-command` et
`--use-existing-coverage` sont exclusives (erreur sinon) ; sans l'une d'elles, c'est le défaut. Codes de sortie : 0 ok, 1 erreur, 2 seuil non respecté, 3 tests ou commande de
couverture en échec (priorité 1 > 3 > 2 ; voir PLAN.md §4.5).
Sortie : tableau texte (stdout, pire d'abord) + `<racine>/.metrics/crap.json` quand il y a une couverture.

## Structure du code

Voir PLAN.md §5. Points utiles pour relire :
- `App` est tout le programme sauf le point d'entrée ; il reçoit un `IProcessRunner` pour être testé sans processus.
- `TestSupport.cs` : `FakeProcessRunner` (aucun vrai processus) et `TempWorkspace` (mini-solution sur disque).

## Limites connues

- Rapprochement par plages de lignes : approximation (voir PLAN.md §4.4), **mesurée** sur `closures` et `sameline` : un
  membre jamais appelé qui partage sa ligne avec un autre apparaît couvert.
- Exclusion `[ExcludeFromCodeCoverage]` : **vérifiée** sur coverlet 6.0.0 (méthode, propriété, type).
- Projets de tests sur Microsoft.Testing.Platform : **constaté** avec le SDK 10, `dotnet test` refuse le projet (code 1,
  message). Les deux collecteurs MTP et l'adhésion par `global.json` n'ont pas été essayés (étape 10).
- `--changed` : chemins git entre guillemets (caractères non ASCII) mal décodés ; le JSON ne contient alors que les
  fichiers filtrés (il écrase le rapport complet précédent). Même chose avec `--filter`.
- `--use-existing-coverage` prend tout fichier `*cobertura*.xml` sous la racine (hors bin/obj/.git/node_modules).
- Les `.sln` ne sont pas ouverts : on cherche les `.csproj` sous le dossier.
- Détection d'un projet de tests par mots-clés dans le `.csproj` (pas d'évaluation MSBuild, donc pas de
  `Directory.Build.props`).
- Une correspondance partielle est signalée par projet ; les méthodes absentes du rapport comptent à 0 %.
  Aucun fichier apparié donne le code 1. Un projet de tests sans rapport parmi plusieurs est signalé par un avertissement.
- Non mesuré : lambdas dans les initialiseurs de champs, branches `#if` inactives.
- Opérateurs nommés `operator +` et non `op_Addition`.

## Vérifications faites à la main (2026-11-01, non automatisées)

Avec l'outil publié (`dotnet publish src/CrapSharp -o <dossier>`), sur ce dépôt :
- `--help` : aide, code 0 ; `--bogus` : « Unknown option », code 1 ;
- défaut avec `--threshold 10` : lance les tests, tableau, JSON, code 0 ; l'entrée `<top-level statements>` de
  `Program.cs` apparaît (CC 1, 0 %, CRAP 2) ;
- étape 6 : voir les tests de bout en bout (automatisés).

Vérifications plus anciennes (étape 4, avant les changements de l'étape 5) : `--use-existing-coverage`, `--filter`,
`--no-coverage`, `--changed`, `--coverage-command` avec guillemets internes.

## Reste à faire

Voir la feuille de route (PLAN.md §6). Étape 6 faite, à relire par Codex ; prochaine étape : **7** (comparaison avec ReportGenerator).

## Comment vérifier

```bash
dotnet test                                                     # 155 réussis, 14 ignorés
dotnet publish src/CrapSharp -o ./out
dotnet ./out/CrapSharp.dll . --threshold 10                     # lance les tests avec couverture, doit sortir 0
```

Tests de bout en bout (lents, réseau NuGet) :

```bash
CRAPSHARP_E2E=1 dotnet test --filter Category=EndToEnd          # 14 attendus
```

Environnement : Windows 11, SDK .NET 10.0.401, runtime 10.0.12.
