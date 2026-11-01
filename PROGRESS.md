# CrapSharp — suivi d'avancement

> Fichier destiné à la **review par une autre IA ou un humain**. Mis à jour à chaque étape.
> Objectif, conventions de mesure et feuille de route : [PLAN.md](PLAN.md). Reviews : [REVIEWS.md](REVIEWS.md).
> Dernière mise à jour : 2026-11-01 — étapes 1 à 12 faites (la 12 en partie), licence MIT, CI Linux et Windows verte ; premier essai sur un
> projet réel (Tower Attack, un jeu Godot) : voir « Retours du premier projet réel ». Les commits sont sur `main`.

## Résumé de l'état

| # | Étape | État |
|---|---|---|
| 1-4 | Outil `crapsharp` : complexité, couverture, score, CLI complète | fait |
| 5 | Corrections issues des reviews 1 (Codex) et 2 (Claude) | fait |
| 6 | Fixtures réelles et tests de bout en bout | **fait, à relire** (manques listés plus bas) |
| 7 | Oracle de complexité indépendant et comparaison avec ReportGenerator | **fait, à relire** |
| 8 | Skill complet (discipline ub-mode, `crapsharp`, Stryker.NET) + README | **rédigé, à relire** |
| 9 | Packaging `dotnet tool` | **fait en local** ; métadonnées, README et workflow NuGet préparés, publication non faite |
| 10 | Microsoft.Testing.Platform | **fait, à relire** |
| 11 | Duplication | **jscpd adopté** (provisoire), à relire |
| 12 | Livraison autonome et essais du skill dans Claude Code, Codex et GitHub Copilot (VS Code) | **en cours** : CI faite ; trois hôtes et pilote externe à essayer |
| 13 | Adaptateur Spec Kit | à terme |
| 14 | Description publique et découvrabilité | à faire en dernier |

`dotnet test --configuration Release` : **208 tests rapides réussis, 23 ignorés** (net10.0, vérifié le 2026-11-01). Les tests de bout en bout
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

**Licence** : MIT (fichier `LICENSE`, `PackageLicenseExpression=MIT`, section du README), à la demande de l'utilisateur d'une licence qui impose de
mentionner le projet et son auteur (la ligne de copyright « Nicolas Friederich - CrapSharp » doit être conservée dans les copies). Titulaire : Nicolas Friederich,
nom donné par l'utilisateur. Apache-2.0 (fichier `NOTICE`) est l'alternative plus contraignante
sur la mention ; non retenue. Pas d'avis juridique : choix technique à faire relire.

**À décider** : l'identifiant `CrapSharp` et la version `0.1.0-preview.1` (propositions) ; la licence (indispensable avant toute
publication NuGet ou GitHub publique) ; la publication éventuelle sur NuGet.

**Non vérifié** : installation depuis NuGet (le paquet n'y est pas) ; Linux et macOS ; l'installation globale (`-g`) n'a pas été essayée,
seulement `--tool-path` et le manifeste local.

### Préparation de la publication NuGet (2026-11-01)

- Métadonnées de découverte ajoutées au `.csproj` : titre, description, tags, URL du projet et dépôt, notes de preview.
  Le SDK .NET 10 inclut SourceLink ; le `.nupkg` contient aussi le README et la licence MIT déclarée.
- README réorganisé avec une installation NuGet clairement annoncée comme future et une sortie de la fixture `basic` ;
  `docs/releasing.md` décrit la politique Trusted Publishing, la Release GitHub et la vérification après publication.
- `.github/workflows/publish-nuget.yml` publie uniquement à la publication explicite d'une GitHub Release : version du tag
  contrôlée, tests, pack, installation depuis le paquet local, puis clé temporaire OIDC et push NuGet.
- Vérifié localement : `dotnet pack` crée `CrapSharp.0.1.0-preview.1.nupkg` avec les métadonnées attendues ; une installation
  depuis ce paquet dans un manifeste neuf, avec `DOTNET_CLI_HOME` et le cache NuGet isolés, lance `--help` et un gate réel
  (`--filter MemberNaming`, pire score 9,06, sortie 0). Les 208 tests rapides passent.
- La relance ciblée du test de bout en bout `PackagingTests` n'a pas abouti dans cette session : la première restauration
  n'atteignait pas NuGet.org, puis `vstest` a expiré en attendant `testhost` avec le CLI isolé. Le smoke test manuel ci-dessus
  est passé ; le workflow GitHub et la publication réelle restent à exécuter.
- Reste extérieur au dépôt : choix définitif du nom et de la version, variable GitHub `NUGET_USER`, politique NuGet OIDC,
  publication d'une Release et installation depuis NuGet.org sur un environnement neuf.

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

**Guide et matrice** : `docs/using-the-skill.md` (en anglais) donne les dossiers d'installation et l'invocation pour Claude Code
(`.claude/skills/<nom>/SKILL.md`), Codex (`.agents/skills/<nom>/SKILL.md`) et GitHub Copilot en mode Agent dans VS Code
(`.github/skills/<nom>/SKILL.md`). Le tableau « testé / documenté / non pris en charge » y figure aussi. Aucun des trois hôtes
n'a été essayé : c'est écrit tel quel. Copilot CLI et les autres surfaces Copilot demandent une validation séparée.

**Reste de l'étape 12** : essai réel du skill dans Claude Code, Codex / ChatGPT et GitHub Copilot en mode Agent dans VS Code ;
un pilote sur un projet qui n'est pas le nôtre.

## Mutations sur CrapSharp lui-même (après l'étape 12)

Stryker.NET 5.0.0 lancé par l'utilisateur sur le dépôt : 440 mutants testés, 375 tués, 65 survivants (+ 10 sans couverture), score 83,33 %.
Les 75 mutants non tués ont été classés (rapport HTML lu par un script) :

| Famille | Exemples | Traitement |
|---|---|---|
| Sortie et messages | en-tête et colonnes du tableau, JSON indenté, messages d'erreur et d'avertissement, annonces « Running… » | tests qui fixent le texte exact |
| Ordre stable | fichiers, projets, rapports triés par ordre ordinal ; égalités départagées par nom | tests d'ordre |
| Bornes | rapport écrit pile au début, pile 10 min avant le plus récent | tests aux bornes |
| Processus | sortie standard et erreur capturées, exécutable introuvable (127), script `cmd` / `sh -c` | tests avec un vrai processus et des appels simulés |
| Résolution des rapports Cobertura | source vide, `hits` absent, classe sans fichier, dossier source qui contient le fichier | tests |
| Divers | décisions de plusieurs instructions de niveau supérieur, projet imbriqué, `.csproj` passé en argument, obsolescence jugée sur le plus récent rapport, jamais en mode lancement | tests |
| Code mort | `if (files.Count == 0) return true;` dans `CheckCoverage`, atteint plus jamais depuis « aucun membre » | **supprimé** |
| Équivalents ou inatteignables, laissés | `CoverageProvider` message « No coverage report given » (la liste n'est jamais vide), `ChangedFiles` bornes `>= 3` et `arrow >= 0`, `ProjectDiscovery` `Except` → `Concat` (les fichiers des tests sont exclus ensuite), `MemberNaming` valeurs de repli `"?"`, `CoverageReportFinder` repli `""` | non traités |

Tests ajoutés : 22 dans `OutputAndOrderTests.cs`, 6 dans `CoverageProviderTests`, 4 dans `AppTests` : 194 tests rapides.

**Résultat mesuré** (Stryker.NET 5.0.0, lancé deux fois après les tests) : de 375 tués / 65 survivants / 10 sans couverture
(**83,33 %**) à **435 tués / 9 survivants / 5 sans couverture (96,88 %)**, 60 s, code 0. Les 14 mutants restants ont tous été
examinés et laissés : mutants équivalents (`ChangedFiles` bornes sur des lignes de trois caractères ou une flèche en tête, `Except` → `Concat`
dans `ProjectDiscovery`, format `"0"` de `ReportFormatter` pour un nombre déjà entier, repli `""` de `CoverageReportFinder`), code inatteignable
(`"No coverage report given"`, replis `"?"` de `MemberNaming`) ou dépendant du système (`PathComparison` : le mutant « toujours insensible à la casse »
est identique sur Windows et ne se tue que sous Linux ; `sh -c` n'est exécuté que sous Linux, où la CI le teste).
Le score ne mesure que `src/CrapSharp` ; les tests de bout en bout ne sont pas joués par Stryker (ignorés sans `CRAPSHARP_E2E`).

## Retours du premier projet réel (Tower Attack)

Premier essai de l'outil sur un projet qui n'est pas CrapSharp : un jeu Godot (client Godot, serveur, simulation, présentation, outils).
Résultat de départ : 479 méthodes à 10 ou plus (code 2). Les avertissements nomment correctement les projets absents de la couverture.
Constats : la plupart des pires scores viennent du code Godot (nœuds, scènes), que le projet de tests n'exécute pas ; les vrais
candidats sont des méthodes de `Simulation` à 100 % de couverture mais de CC élevée (CC 72, 39, 36…) ; deux copies du client
(`Client` et `V2/Client`) produisent des méthodes en double.

Corrections qui en découlent :

| Changement | Détail |
|---|---|
| `--exclude <fragment>` (répétable) | écarte les fichiers dont le chemin contient le fragment ; combiné à `--filter` ; erreur (code 1) si plus rien ne reste ; le JSON marque le périmètre partiel et liste `exclude` |
| Chemin relatif dans le tableau | `src/CrapSharp/App.cs:34` au lieu de `App.cs:34`, avec des `/` quel que soit le système |
| Processus fils sans serveurs de build | `MSBUILDDISABLENODEREUSE=1`, `UseSharedCompilation=false`, `DOTNET_CLI_USE_MSBUILD_SERVER=0` fixés pour chaque processus lancé par l'outil (même cause que les 15 minutes de la CI) |

**`--changed-since <réf>`** (ajouté ensuite, pour les PR déjà commitées) : fichiers `.cs` changés depuis l'ancêtre commun de `<réf>` et de `HEAD`
(commités, modifiés ou non suivis). Erreur explicite si git ne connaît pas la référence (en CI : `fetch-depth: 0`). Exclusif avec `--changed`.
Le JSON porte `scope.changedSince`. Score de mutation mesuré ensuite : **97,01 %** (487 tués, 10 survivants équivalents ou inatteignables). 7 tests unitaires et 2 de bout en bout sur un vrai dépôt git ; le test de bout en bout a d'abord
échoué à cause d'une erreur de mon test (le second run écrasait le JSON du premier), pas de l'outil.

Codex a signalé que le contrôle CRAP complet « est resté au démarrage des tests » dans sa session. Je n'ai pas pu le reproduire ; le
dernier correctif ci-dessus en est la piste la plus probable, **non vérifiée dans son environnement**.

Résultats : 201 tests rapides (+ 27 cas de bout en bout), auto-contrôle de l'outil 0 sans avertissement, score de mutation 96,81 %
(455 tués, 9 survivants équivalents ou inatteignables comme avant).

**Non vérifié** : la nouvelle sortie sur Tower Attack lui-même (les commandes sont dans la réponse à l'utilisateur) ; l'essai du skill dans
Claude Code et Codex.

## Premier essai du skill dans Claude Code (projet d'essai `craft-test`)

Projet jetable créé à partir de la fixture `basic` (méthode `Decisions.Ten`, score 10,00 exactement, test existant trop faible), avec les
outils (CrapSharp 0.1.0-preview.3, Stryker 5.0.0) et le skill. Invocation explicite par `/dotnet-craft`. Constaté dans la réponse de Claude :
- il a **ajouté un test de bornes avant** de modifier le code (10 tests verts), puis remplacé les neuf `if` par `Math.Clamp(x, 0, 9)` ;
- `Ten` est passé à 1,00 ; les autres méthodes restent à 6,00 ; jscpd : aucun clone ; il n'a pas commité ;
- il a rapporté ce qu'il n'avait pas pu vérifier (le code de sortie de la première commande, masqué par `tail`) ;
- Stryker : 0 tué, 10 survivants, message « test coverage capture failed ». Reproduit et expliqué par moi : après la simplification, `Ten` n'a
  plus aucun opérateur à muter ; les 10 mutants restants sont dans `Untouched` et `Program.cs`, que les tests n'exécutent pas. C'est un vrai 0 %,
  pas une panne ; l'avertissement de Stryker est trompeur. Avec seulement le nouveau test (ancien code) : 40 tués sur 45, 72,73 %.

Changements du skill qui en découlent : lire le code de sortie soi-même (ne pas masquer par `tail`) et comment lire ce cas de Stryker.

## Premier essai du skill dans Codex (même projet `craft-test`)

Invocation explicite par `$dotnet-craft`, application de bureau. Constaté :
- même refactorisation (`Math.Clamp(x, 0, 9)`) avec un test paramétré de 14 cas écrit **avant** ; ce test passait dès sa première exécution
  (il fige le comportement existant, pas de phase rouge) et Codex l'a dit ;
- gate CRAP : `Ten` à 1,00, maximum du projet 6,00, **mais seulement hors du bac à sable** : dedans, la collecte de couverture était bloquée
  (`--filter Decisions.cs`, `--collect:"XPlat Code Coverage"`) et `--use-existing-coverage` ne trouvait aucun rapport ;
- Stryker : échec dans le bac à sable (accès refusé au gestionnaire de services Windows), puis hors bac : 1 mutant tué, 3 sans couverture
  (25 %, seuil 80 échoué) ; les variantes `--mutate "**/Decisions.cs{5..8}"` ne retiennent aucun mutant. Codex n'a **pas** présenté ce code
  de sortie 0 comme un succès du gate (l'échec est annoncé : « mutation incomplète ») ;
- jscpd : 0 clone sur `src` et `tests` ; pas de commit ; `git diff --check` propre.

À retenir : la différence avec Claude Code tient surtout au bac à sable de Codex, pas au skill. Le skill ne dit pas quoi faire quand la collecte
de couverture est bloquée ; à documenter (demander l'autorisation d'exécuter hors bac à sable, ne jamais déclarer le gate passé sans score réel).

**Non vérifié** : le déclenchement automatique du skill (essai 3 non fait), l'essai dans Codex, la suite après ce premier essai.

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
| `--exclude <fragment>` | écarte les fichiers dont le chemin contient le fragment (répétable) |
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

Dans l'ordre de priorité retenu (critères : risque de faux verdict, blocage d'un vrai usage, coût) :

1. **Finir le pilote sur Tower Attack** : essayer le skill dans Claude Code sur une petite tâche de `Simulation` (une méthode à CC 10 déjà
   couverte), noter scores, durées, faux positifs ; relancer l'outil avec `--exclude`. Puis le même essai dans Codex / ChatGPT.
2. **Publier le paquet sur NuGet** après choix de l'identifiant et de la version : configurer le compte NuGet, la politique
   Trusted Publishing et `NUGET_USER`, puis publier une GitHub Release. Le workflow, les métadonnées et le guide sont prêts ;
   l'installation locale depuis un manifeste vierge est vérifiée. L'installation et le restore depuis NuGet restent à tester.
3. **Trancher les limites révélées par le pilote** : un rapport partiel écrase le JSON complet (`--changed-since` est fait), jscpd ne voit pas les clones renommés, la découverte de fichiers ignore une partie de MSBuild.
4. **Adaptateur Spec Kit** (étape 13) puis **présentation publique finale** (étape 14), après le pilote.

Voir aussi PLAN.md §6 et §7.

## Comment vérifier

```bash
dotnet test                                                     # 208 réussis, 23 ignorés
dotnet publish src/CrapSharp -o ./out
dotnet ./out/CrapSharp.dll . --threshold 10                     # lance les tests avec couverture, doit sortir 0
```

Tests de bout en bout (lents, réseau NuGet) :

```bash
CRAPSHARP_E2E=1 dotnet test --filter Category=EndToEnd          # 29 cas attendus
```

Environnement : Windows 11, SDK .NET 10.0.401, runtime 10.0.12.
