# CrapSharp — suivi d'avancement

> Fichier destiné à la **review par une autre IA ou un humain**. Mis à jour à chaque étape.
> Objectif, conventions de mesure et feuille de route : [PLAN.md](PLAN.md). Reviews : [REVIEWS.md](REVIEWS.md).
> Dernière mise à jour : 2026-11-01 — étapes 6 et 7 réalisées, **en attente de relecture** ; l'étape 7 n'est pas commitée.
> Un commit initial (`3d8d55f`) existe ; les travaux suivants restent dans l'arbre de travail.

## Résumé de l'état

| # | Étape | État |
|---|---|---|
| 1-4 | Outil `crapsharp` : complexité, couverture, score, CLI complète | fait |
| 5 | Corrections issues des reviews 1 (Codex) et 2 (Claude) | fait |
| 6 | Fixtures réelles et tests de bout en bout | **fait, à relire** (manques listés plus bas) |
| 7 | Oracle de complexité indépendant et comparaison avec ReportGenerator | **fait, à relire** |
| 8 | Skill complet (discipline ub-mode, `crapsharp`, Stryker.NET) + README | **rédigé, à relire** |
| 9 | Packaging `dotnet tool` | **fait en local**, à relire (pas de publication NuGet, pas de licence) |
| 10 | Microsoft.Testing.Platform | **fait, à relire** |
| 11 | Duplication | **jscpd adopté** (provisoire), à relire |
| 12 | Livraison autonome et essais du skill dans plusieurs agents | **en cours** : CI faite |
| 13 | Adaptateur Spec Kit | à terme |
| 14 | Description publique et découvrabilité | à faire en dernier |

`dotnet test` : **162 tests rapides réussis, 20 ignorés** (net10.0, vérification rapportée). Les tests de bout en bout
ont été lancés séparément sur le projet `tests/CrapSharp.Tests/CrapSharp.Tests.csproj` avec `CRAPSHARP_E2E=1` :
**26 réussis, 0 échec, 0 ignoré** (49 s, vérifié le 2026-11-01).
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
| `mtp` | MSTest sur MTP avec seulement `coverlet.collector` | projet non lancé, code 1, message nommant les deux extensions utiles (étape 10) |
| `mtp-codecoverage`, `mtp-coverlet` | MTP avec chacune des deux extensions | `Sign` 100 %, CRAP 2, code 0 (étape 10) |
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
dépôt git ; correspondance partielle de chemins avec de vrais rapports ; projet sans aucun membre ; Linux et macOS
(seul Windows 11 a servi) ;
les fixtures ne sont construites qu'avec coverlet.collector 6.0.0, xunit 2.5.3 et MSTest 4.0.2. Les tests de bout en
bout dépendent du réseau (NuGet) et de la présence des runtimes .NET 9 et 10.

## Ce qui a changé à l'étape 7

Aucun changement de l'outil : des mesures et un test.

- **Test ajouté** : `ComplexityOracleTests` (de bout en bout, 5 fixtures). Il compile chaque fixture avec l'analyseur Microsoft
  CA1502 réglé à un seuil de 1 et compare sa complexité, méthode par méthode, à celle de `ComplexityCalculator`. Résultat :
  identique partout (rouge d'abord tant que la configuration de l'analyseur était incomplète, puis vert).
- **ReportGenerator 5.5.12** essayé à la main sur les rapports Cobertura des fixtures : CC différente (IL de coverlet, `Ten` 18
  au lieu de 10), couverture identique sauf fonctions locales, code de sortie toujours 0 (pas de gate). Détail dans PLAN.md §6.
- **Proposition à valider** : poursuivre le calcul propre à CrapSharp (PLAN.md §7).

**Non vérifié** : seules les constructions présentes dans les 5 fixtures sont recoupées avec CA1502 (pas les `switch`, `when`,
`??`, `?.`, motifs `and`/`or` de §4.2) ; ReportGenerator n'a été lancé qu'à la main, une fois ; la CC de CA1502 dépend du
SDK 10.0.401.

## Ce qui a changé à l'étape 10

| Changement | Fichiers |
|---|---|
| Détection MTP / VSTest par projet de tests et choix de la commande de couverture (`dotnet run --project … -- --coverage` ou `--coverlet`) | `Coverage/TestRunner.cs`, `Coverage/CoverageProvider.cs` |
| Projet MTP sans extension de couverture : non lancé, avertissement, erreur si aucun rapport | `Coverage/CoverageProvider.cs` |
| Message « aucun rapport » et message d'échec de tests généralisés (« tests failed for … ») | `Coverage/CoverageProvider.cs` |
| 3 fixtures (`mtp`, `mtp-codecoverage`, `mtp-coverlet`) et 3 tests de bout en bout, 3 tests unitaires | `tests/fixtures`, `EndToEndTests.cs`, `CoverageProviderTests.cs` |

L'auto-contrôle de l'outil sort toujours 0, sans avertissement. `--coverage-command` fonctionnait déjà avec MTP (essayé à la main).

## Complément de l'étape 6 (après l'étape 10)

Les manques listés à l'étape 6 sont comblés, sauf ce qui est noté plus bas.

| Changement | Fichiers |
|---|---|
| Avertissement « source file(s) changed after the coverage report was written » avec `--coverage` / `--use-existing-coverage` | `App.cs` (`WarnIfStale`) |
| Aucun membre mesurable : code 1 avant tout test ; avec `--filter` / `--changed` : note et code 0 | `App.cs` (`NoMembers`) |
| 3 fixtures : `unusedproject`, `nomembers`, `constructs` ; 4 tests de bout en bout (`CompletenessTests`) ; 4 tests unitaires | `tests/fixtures`, `CompletenessTests.cs`, `AppTests.cs` |
| Recoupement CA1502 étendu aux constructions rares : **5 écarts**, épinglés dans le test | `ComplexityOracleTests.cs`, PLAN.md §4.2 |
| Nettoyage des dépôts git temporaires (fichiers en lecture seule) | `ToolHarness.cs` |

**Décidé** : conserver la convention de CrapSharp face aux cinq écarts avec CA1502 (`default` d'un `switch`, `??=`,
`catch`, bras de `switch` expression, motif `or`) et compter chaque étiquette `case`, même si plusieurs partagent un
corps. La lecture du calcul Microsoft confirme les omissions de CA1502 ; la publication NIST nuance le comptage des
étiquettes et l'usage du seuil sur une grande décision multiple (PLAN.md §4.2, §7 et §8). Le code de calcul n'a pas été
changé. **À valider aussi** : l'avertissement « périmé » est informatif
(code inchangé) ; « aucun membre » est une erreur sur un arbre complet et une simple note sur une sélection étroite.

**Toujours non vérifié** : Linux et macOS ; les constructions de §4.2 absentes de `constructs` (par exemple `and` / `not`,
`goto`, `lock`) ; une correspondance partielle de chemins dans un seul projet (seul un projet entier absent est testé).

## Ce qui a été fait à l'étape 8

- `skill/dotnet-craft/SKILL.md` : discipline en trois temps (faire fonctionner, faire bien, pratique), gate CRAP (commande, codes de
  sortie, que faire d'un score élevé, ce que le score ne prouve pas), mutations avec Stryker.NET, pratique (journal, petits
  commits). Texte original, rédigé en anglais (décision de l'utilisateur : fichiers publics en anglais ; PLAN.md, PROGRESS.md et REVIEWS.md restent en français).
- `README.md` : présentation, utilisation, options, codes de sortie, limites, développement, licence.
- Vérifié à la main : `dotnet new tool-manifest` puis `dotnet tool install dotnet-stryker` (Stryker.NET 5.0.0), puis
  `dotnet stryker --project App.csproj --break-at N` depuis le dossier du projet de tests de la fixture `basic` : 57 mutants,
  36 tués, 9 survivants, score 65,45 % ; code 0 avec `--break-at 60`, code 2 avec `--break-at 100`. Formule de couverture
  nécessaire recalculée (CC 4 → 27,9 %, CC 8 → 68,5 %).

**À valider** : (nom retenu : `dotnet-craft`) ; le seuil de départ `--break-at 80`, simple proposition ; la politique « mutations avant de proposer
le changement, pas dans la boucle TDD ».

**Non vérifié** : l'installation et l'invocation du skill dans Claude Code, Codex ou ChatGPT (étape 12) ; `crapsharp` en
`dotnet tool` (étape 9, le skill indique donc `dotnet publish` puis `dotnet <dll>`) ; Stryker sur un projet de tests
Microsoft.Testing.Platform ; la section « duplication » du skill est volontairement une consigne manuelle (étape 11) ; il n'y a
toujours pas de licence.

## Ce qui a été fait à l'étape 9

- `CrapSharp.csproj` : `PackAsTool`, `ToolCommandName=crapsharp`, `PackageId=CrapSharp`, version `0.1.0-preview.1`, README dans le paquet.
- Test de bout en bout `PackagingTests` : `dotnet pack` avec une version unique, manifeste d'outils vierge, `dotnet tool install
  --add-source`, `dotnet crapsharp --help` (code 0), puis le gate strict sur la fixture `basic` (code 2, `Ten` = 10 exactement).
- Essayé à la main : installation avec `--tool-path` (commande `crapsharp` directe) ; le paquet contient bien `DotnetToolSettings.xml`
  et le README.
- Les commandes du skill et du README utilisent maintenant `dotnet crapsharp` après `dotnet pack` et `dotnet tool install`.

**À décider** : l'identifiant `CrapSharp` et la version `0.1.0-preview.1` (propositions) ; la licence (indispensable avant toute
publication NuGet ou GitHub publique) ; la publication éventuelle sur NuGet.

**Non vérifié** : installation depuis NuGet (le paquet n'y est pas) ; Linux et macOS ; l'installation globale (`-g`) n'a pas été essayée,
seulement `--tool-path` et le manifeste local.

## Ce qui a été fait à l'étape 11 (duplication)

- Fixture `tests/fixtures/duplication` (clone exact, clone renommé, DTO semblables, code unique) ; jscpd 5.4.2 mesuré : clone exact
  trouvé, clone renommé **non trouvé**, DTO signalés seulement avec `--min-tokens 30`.
- Décision de l'utilisateur : jscpd d'abord. Section « Duplication check with jscpd » ajoutée au skill, paragraphe au README.
- Vérifié à la main : la commande épinglée `npx --yes jscpd@5.4.2 …` sort 1 sur la fixture, 0 sur `src` du dépôt (moins d'une seconde) ;
  sur `src` + tests, 2 petits clones dans `CoverageProviderTests` (non corrigés, hors gate).

**Non vérifié** : un dépôt .NET réel plus grand (temps, faux positifs) ; Linux et macOS ; aucun test automatisé n'exécute jscpd
(il dépend de Node et du réseau), la mesure est manuelle ; la brique Roslyn pour les clones renommés n'est pas écrite.

## Ce qui a été fait à l'étape 12 (CI)

- `.github/workflows/ci.yml` : sur `ubuntu-latest` et `windows-latest` (.NET 9 et 10, Node 22) : tests rapides, auto-contrôle CRAP
  (`dotnet publish` puis `dotnet out/CrapSharp.dll . --threshold 10`), jscpd sur `src`, tests de bout en bout (`CRAPSHARP_E2E=1`).
- Premier passage, vérifié par l'API publique de GitHub (run sur la branche `ci-workflow`) : **les deux systèmes sont verts**, y
  compris les tests de bout en bout sur Linux (donc : multi-cible, MTP, Stryker n'est pas concerné, paquet `dotnet tool`, `git`).
  Durées : tests rapides 14 s (Linux) / 22 s (Windows), auto-contrôle 7 s / 5 s, jscpd 2 s / 9 s.

**Durée des tests de bout en bout en CI** : 920 s au premier passage, alors que chaque test dure de moins d'une seconde à 23 s
(durées lues dans les annotations du run, limitées aux 10 plus longs par système). Cause établie par mesure : les serveurs de build
(nœuds MSBuild, serveur du compilateur) restent vivants après les builds lancés par les tests. Avec `MSBUILDDISABLENODEREUSE=1`,
`UseSharedCompilation=false` et `DOTNET_CLI_USE_MSBUILD_SERVER=0` définis pour tout le workflow, l'étape passe à **145 s** sur
Linux comme sur Windows. Le mécanisme exact (qui attend quoi) n'est pas établi.

**Pilote sur une copie propre** (le dépôt lui-même, cloné depuis GitHub dans un dossier temporaire, commit `1a541a3`) :
- manifeste `dotnet new tool-manifest`, puis `crapsharp 0.1.0-preview.1` (paquet local) et `dotnet-stryker 5.0.0` épinglés dans `dotnet-tools.json` ;
- `dotnet crapsharp . --threshold 10` : code 0, 4 s, aucun avertissement ;
- `dotnet stryker --project CrapSharp.csproj --break-at 50` depuis `tests/CrapSharp.Tests` : 612 mutants, 375 tués, 65 survivants,
  10 sans couverture, **score 83,33 %**, 55 s, code 0. Les 65 survivants n'ont pas été examinés : ils sont une piste d'amélioration des tests.

**Reste de l'étape 12** : essai du skill dans Claude Code et Codex / ChatGPT (de ton côté) ; un pilote sur un projet qui n'est pas le
nôtre ; matrice « testé / documenté / non pris en charge » .

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
- Projets de tests sur Microsoft.Testing.Platform : pris en charge avec `Microsoft.Testing.Extensions.CodeCoverage` ou
  `coverlet.MTP` (lancés par `dotnet run`, voir PLAN.md §6, étape 10). Essayés seulement avec MSTest ; xunit.v3, TUnit et
  NUnit non essayés ; des versions incompatibles entre extension et plateforme font planter le lancement.
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

Voir la feuille de route (PLAN.md §6). Étapes 6, 7, 8 (rédigée) et 10 faites, à relire ; étape 9 faite en local ; prochaines étapes : **11** (duplication) puis **12** (essai de livraison dans plusieurs agents).

## Comment vérifier

```bash
dotnet test                                                     # 162 réussis, 20 ignorés
dotnet publish src/CrapSharp -o ./out
dotnet ./out/CrapSharp.dll . --threshold 10                     # lance les tests avec couverture, doit sortir 0
```

Tests de bout en bout (lents, réseau NuGet) :

```bash
CRAPSHARP_E2E=1 dotnet test --filter Category=EndToEnd          # 26 cas attendus
```

Environnement : Windows 11, SDK .NET 10.0.401, runtime 10.0.12.
