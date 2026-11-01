# CrapSharp — plan du projet

> À relire par une autre IA / un humain avant de critiquer ou de continuer le travail.
> État détaillé et mode d'emploi : [PROGRESS.md](PROGRESS.md). Texte intégral des reviews : [REVIEWS.md](REVIEWS.md).
> Dernière mise à jour : 2026-11-01, étapes 6 et 7 réalisées (fixtures réelles, oracle indépendant de complexité, comparaison avec ReportGenerator) ; en attente de relecture.

## 1. Contexte

L'utilisateur veut un équivalent C# / .NET de [unclebob/ub-mode](https://github.com/unclebob/ub-mode).

| Dépôt amont | Nature | Licence | C# ? |
|---|---|---|---|
| [ub-mode](https://github.com/unclebob/ub-mode) | Un skill IA (`SKILL.md`), aucun code | **aucune** | sans objet |
| [crapper](https://github.com/unclebob/crapper) | Score CRAP par fonction (Python) | 0BSD | non |
| [dryer](https://github.com/unclebob/dryer) | Détection de duplication (Python) | 0BSD | non vérifié, probablement non |
| [mutator](https://github.com/unclebob/mutator) | Tests de mutation (Python) | 0BSD | non |
| [uml-viewer](https://github.com/unclebob/uml-viewer) | Diagramme UML qui lit l'EDN de crapper (Clojure) | — | sans objet |

**Ce que contient ub-mode** (`SKILL.md` lu le 2026-11-01) : surtout une **discipline de travail**, en trois temps.
- *Make it work* : plus petite modification possible, données d'abord, **TDD strict** (test qui échoue pour la bonne
  raison, puis plus petit changement), attaquer la prémisse, retirer avant d'ajouter, discipline d'architecture.
- *Make it right* : corriger les causes, unités vérifiables, tester le comportement et non l'implémentation, puis trois
  contrôles outillés : **crapper** (chaque fonction sous CRAP 10, « under CRAP 10 », donc strictement), **dryer**
  (éliminer la duplication de rang ≥ 0,8), **mutator** (couvrir le non couvert, tuer les survivants, supprimer le code
  mort). Refactoring : figer le comportement, changer la structure, prouver que rien n'a changé.
- *Practice* : journal des actions et de leurs raisons, économie du contexte, nettoyage des worktrees, petits commits
  ordonnés (pourquoi, quoi, preuve).

Le skill ne donne **aucune ligne de commande** pour ces outils.

**Skill existant `crap-score`** ([dotnet/skills](https://github.com/dotnet/skills), lu via skillselion, pas sur GitHub) :
consignes sans outil exécutable (l'IA compte elle-même les points de décision), seuils 5 / 15 / 30, sans code de retour.
Il ne remplace pas CrapSharp. Idée à reprendre dans notre skill : la couverture nécessaire pour passer sous un seuil
vaut `1 − ((seuil − CC) / CC²)^(1/3)` si CC < seuil (si CC ≥ seuil, il faut réduire CC par simplification ou extraction).

## 2. Objectif et périmètre

**Objectif final : un équivalent .NET d'ub-mode**, c'est-à-dire un skill de discipline adapté à .NET qui appelle des
contrôles déterministes. Il doit fonctionner sans Spec Kit, depuis la ligne de commande, la CI et plusieurs agents
capables d'accéder au dépôt et d'exécuter les outils. L'intégration Spec Kit est un usage prévu à terme.

| Phase | Contenu | État |
|---|---|---|
| A | Outil `crapsharp` (CRAP par méthode), fiable et validé | en cours (étapes 1 à 7 faites, 6 et 7 à relire) |
| B | Skill portable (discipline ub-mode + `crapsharp` + **Stryker.NET** pour les mutations), packaging | à faire |
| C | Contrôle de duplication : choix, intégration et validation | à faire |
| D | Distribution et essai de bout en bout : CLI/CI autonomes, skill utilisable par plusieurs agents | à faire |
| E | Adaptateur Spec Kit, sans dépendance du cœur à Spec Kit | à terme |

Pour les mutations, on utilise **Stryker.NET** tel quel, un outil existant et mature : pas de réécriture.

## 3. Livrables (un seul dépôt `CrapSharp`)

1. **Outil CLI `crapsharp`** (.NET 10, publié en `dotnet tool`).
2. **Skill** (`skill/<nom>/SKILL.md`, nom à confirmer, voir §7) : la discipline d'ub-mode reformulée pour .NET
   (texte original, car ub-mode n'a pas de licence), avec les commandes exactes de `crapsharp` et de Stryker.NET.
3. **Tests** : tests unitaires xUnit, plus des tests de bout en bout sur de vraies mini-solutions avec coverlet.
4. **Contrôle de duplication** utilisable depuis le skill et en CI, avec un verdict reproductible.
5. **Guide d'usage autonome** : installation, CLI, CI et skill pour les environnements d'agent vérifiés.
6. À terme : **adaptateur Spec Kit** et exemple exécutable sur un vrai projet .NET.

## 4. Conventions de mesure (spécification)

Ces règles sont ce que l'outil fait **aujourd'hui** ; chaque ligne a un test unitaire, sauf mention contraire.

### 4.1 Formule et seuil

- `CRAP = CC² × (1 − couverture)³ + CC`, couverture en fraction (0 à 1). Exemple : CC 3, couverture 75 % → 3,14.
- `--threshold N` : **échec si un score brut est ≥ N** (objectif « tout strictement sous N », comme ub-mode).
  Comparaison sur la valeur non arrondie.
- Affichage (tableau et JSON) **tronqué**, jamais arrondi vers le haut : 9,996 s'affiche 9,99 et 10,004 s'affiche
  10,00. Ce qu'on lit est donc cohérent avec le verdict, pour tout seuil d'au plus deux décimales.
- Conséquence à connaître : une méthode de CC ≥ N échoue même couverte à 100 % ; il faut réduire sa complexité
  (simplifier les décisions, supprimer du code inutile ou extraire une responsabilité), pas seulement ajouter des tests.

### 4.2 Complexité cyclomatique (analyse syntaxique Roslyn, sans modèle sémantique)

CC = 1 + nombre de points de décision dans le membre.

| Construction | Compte | Exemple → CC |
|---|---|---|
| `if`, `else if` | +1 chacun (`else` : 0) | `if (a) …; else if (b) …;` → 3 |
| `for`, `foreach`, `while`, `do` | +1 | → 2 |
| `catch` | +1 par clause (`finally` : 0) | deux `catch` → 3 |
| `case` d'un `switch` | +1 par étiquette, `default` : 0 | 3 `case` + `default` → 4 |
| bras de `switch` expression | +1, sauf le `_ =>` final sans `when` | `1 =>, 2 =>, _ =>` → 3 |
| `when` (garde) | +1 en plus du `case` ou du bras | `case int n when n > 0:` → 3 |
| `?:` | +1 | → 2 |
| `&&`, `\|\|` | +1 chacun | `a && b ? … : …` → 3 |
| `??`, `??=` | +1 chacun | `s ?? s ?? "a"` → 3 |
| `?.` | +1 chacun | `s?.Trim()?.Length ?? 0` → 4 |
| motifs `and` / `or` | +1 chacun | `is "a" or "b" ? … : …` → 3 |
| lambdas, fonctions locales | comptées **dans le membre englobant** | |
| `goto`, `break`, `return`, `throw`, `is` simple, `not` | 0 | |

Cette convention suit McCabe et crapper (chaque étiquette de `switch` compte, comme pour Java chez crapper). Elle peut
différer de Visual Studio ou de ReportGenerator. **Mesuré à l'étape 7** : elle coïncide avec l'analyseur Microsoft CA1502 (celui
de Visual Studio) sur toutes les méthodes des fixtures ; ReportGenerator affiche une autre complexité (voir §6).

### 4.3 Ce qui est mesuré

- **Membres** avec un corps : méthodes, constructeurs, destructeurs, opérateurs, conversions, accesseurs (`get`,
  `set`, `init`, `add`, `remove`), propriétés et indexeurs à expression. Membres abstraits, `extern` et propriétés auto :
  rien à mesurer.
- **Instructions de niveau supérieur** (`Program.cs` sans classe) : **une entrée** `<top-level statements>`, de la
  première à la dernière instruction (le compilateur en fait un seul `Main`).
- **Exclus** : membres portant `[ExcludeFromCodeCoverage]` ou `[ExcludeFromCoverage]`, sur le membre, sur sa
  propriété ou son événement, ou sur un type englobant (nom simple, suffixé `Attribute` ou qualifié). Coverlet les
  retire de son rapport par défaut : sans cette règle, ils seraient notés à 0 %. **Confirmé** sur la fixture `excluded`
  (coverlet 6.0.0) : un membre, une propriété et un type exclus sont absents du Cobertura comme du tableau.
- **Fichiers** : les `.cs` des dossiers de projets non-tests, hors `bin`, `obj`, `.git`, `node_modules`, et hors
  `*.g.cs`, `*.generated.cs`, `*.designer.cs`.
- **Non mesuré, connu** : lambdas dans les initialiseurs de champs ou de propriétés ; code dans les branches `#if`
  inactives (aucun symbole défini) ; fichiers liés hors du dossier du projet ; projets d'un `.sln` situés hors de la
  racine.

### 4.4 Couverture

- Source : rapports **Cobertura** de coverlet (`dotnet test --collect:"XPlat Code Coverage"`).
- Couverture d'un membre = lignes touchées / lignes rapportées dans `[début, fin]` du membre. C'est une
  **approximation** : elle inclut les lambdas et fonctions locales du membre ; deux membres sur une même ligne se
  partagent les mêmes lignes.
- Membre sans aucune ligne rapportée = 0 % (règle de crapper).
- Plusieurs rapports : fusion, on garde le maximum de hits par ligne. Un `dotnet test` de projet multi-cible écrit un
  rapport par framework : par défaut l'outil vide `.metrics/coverage` avant de lancer les tests et lit **tous** les
  rapports qui s'y trouvent. Avec `--use-existing-coverage`, il garde pour chaque dossier de résultats les rapports
  écrits dans les 10 minutes qui précèdent le plus récent (un même run) ; un run plus ancien est écarté.
- Un projet de tests qui réussit sans produire de rapport (pas de `coverlet.collector`, par exemple) : avertissement qui
  le nomme ; ce que seuls ses tests couvrent est noté 0 % (une couverture manquante ne fait que relever les scores).
  Si aucun projet de tests ne produit de rapport : erreur (code 1).
- Limites mesurées sur fixtures (étape 6) : une ligne de continuation d'une instruction qui crée un lambda peut être
  rapportée touchée alors que le corps du lambda n'a pas tourné (`};`, fixture `closures`) ; deux membres sur la même
  ligne partagent le même résultat, donc un membre jamais appelé peut sembler couvert (fixture `sameline`).
- Couverture des **branches** (`condition-coverage`) : ignorée (lignes uniquement).
- Si certains fichiers analysés sont absents du rapport : leurs membres restent à 0 % et un avertissement nomme les
  projets concernés. Un projet de production qu'aucun test n'exécute est un cas normal de cette règle.
- Si **aucun** fichier analysé n'apparaît dans les rapports : erreur (code 1, sans rapport produit). Vérifier notamment
  les chemins `/_/…` des builds déterministes. Coverlet 6.0.0 remet par défaut les chemins réels dans son rapport ;
  seul `DeterministicReport=true` y garde les chemins `/_/…` (fixtures `deterministic` et `deterministic-paths`).
- Sélection vide : aucun fichier de production trouvé → code 1 ; `--filter` qui ne correspond à rien → code 1 (faute
  de frappe probable) ; `--changed` sans aucun fichier modifié → code 0, rien n'est lancé et le JSON précédent est
  conservé. Un rapport `--changed` / `--filter` porte `scope.complete = false` dans le JSON.

### 4.5 Codes de sortie

| Code | Sens | Rapport produit ? |
|---|---|---|
| 0 | OK | oui |
| 1 | Erreur : options invalides, chemin absent, aucun rapport de couverture lisible, aucun fichier analysé apparié, `--changed` hors dépôt git | non |
| 2 | `--threshold N` : au moins une méthode à N ou plus | oui |
| 3 | `dotnet test` ou la commande de couverture a échoué : couverture peut-être incomplète | oui |

Priorité si plusieurs cas s'appliquent : 1, puis 3, puis 2. Options inconnues, valeur manquante (y compris une option
connue à la place de la valeur), second chemin, seuil qui n'est pas un nombre fini supérieur à 0, ou plusieurs sources
de couverture (`--coverage`, `--coverage-command`, `--use-existing-coverage`, `--no-coverage`) : code 1, avec un renvoi
vers `--help`.

### 4.6 JSON (`.metrics/crap.json`)

`scope` (`complete`, `changed`, `filter`, `files`), `coverage` (`source`, `reports` : chemins des Cobertura utilisés) et
`entries`. Chaque entrée porte `crap` et `coverage` **tronqués** pour l'affichage, et `crapRaw`, le score exact
comparé au seuil.

## 5. Architecture

```
CrapSharp.sln
src/CrapSharp/                 net10.0, RollForward=Major, Roslyn (Microsoft.CodeAnalysis.CSharp)
  Program.cs                   une ligne : App.Run(args)
  App.cs                       options -> fichiers -> couverture -> rapport -> code de sortie
  Options.cs                   analyse stricte des arguments, Usage (--help), CoverageMode
  ExitCodes.cs                 0 / 1 / 2 / 3
  SourceDiscovery.cs, PathComparison.cs
  Complexity/                  ComplexityCalculator, DecisionCounter, MemberNaming, MethodComplexity, CoverageExclusion
  Coverage/                    CoberturaParser, CoverageMap, CoverageReportFinder, CoverageProvider
  Scoring/                     CrapCalculator, MethodReport, ReportFormatter, JsonReportWriter, ReportContext, ThresholdCheck, Truncation
  Projects/ProjectDiscovery.cs, Git/ChangedFiles.cs, Processes/ (IProcessRunner, SystemProcessRunner)
tests/CrapSharp.Tests/         xUnit (155 tests rapides + 14 de bout en bout, voir étape 6), FakeProcessRunner, TempWorkspace
tests/CrapSharp.Tests/EndToEnd/ outil publié lancé sur tests/fixtures (CRAPSHARP_E2E=1)
tests/fixtures/                mini-solutions réelles ; projets stockés en *.csproj.template
skill/<nom>/SKILL.md           phase B
```

## 6. Feuille de route

| # | Étape | État |
|---|---|---|
| 1 | Solution, CC par membre avec Roslyn | fait |
| 2 | Lecture Cobertura, rapprochement par plages de lignes | fait |
| 3 | Formule, tableau, `.metrics/crap.json`, `--threshold` | fait |
| 4 | CLI : lancement de `dotnet test`, détection des projets, `--changed`, `--filter`, `--coverage-command`… | fait |
| 5 | Corrections issues des reviews 1 et 2 (détail en §9) | **fait** |
| 6 | **Fixtures réelles et tests de bout en bout** (détail ci-dessous) | **fait, à relire** (reste : voir ci-dessous) |
| 7 | Validation indépendante des scores attendus sur les fixtures ; comparaison complémentaire avec ReportGenerator et évaluation de son éventuelle réutilisation | **fait, à relire** |
| 8 | Skill portable (phase B) : discipline ub-mode pour .NET, `crapsharp`, Stryker.NET ; politique d'exécution des mutations ; README | à faire |
| 9 | Packaging `dotnet tool` (`PackAsTool`, `ToolCommandName=crapsharp`), installation locale et commandes du skill vérifiées | à faire |
| 10 | Support des projets sur Microsoft.Testing.Platform (si l'étape 6 confirme le problème) | à faire |
| 11 | Contrôle de duplication (phase C) : choisir entre un outil existant (ex. jscpd) et une brique Roslyn, puis l'intégrer et le valider | à faire |
| 12 | Essai de livraison (phase D) : installation reproductible, CLI/CI, skill dans plusieurs agents et pilote sur un projet .NET réel | à faire |
| 13 | Adaptateur Spec Kit (phase E) : constitution, workflow et exemple de projet | à terme |

**Étape 6 en détail.** Des mini-solutions dans `tests/fixtures/`, chacune avec un vrai projet de tests et coverlet.
L'outil généré par **`dotnet publish`** est lancé dessus (pas `dotnet run`, qui verrouille la DLL sous Windows), et on vérifie le tableau,
le JSON et le code de sortie. Cas à couvrir :
- méthode non couverte, couverture partielle, score exactement égal au seuil ;
- lambdas, fonctions locales, `async`, itérateurs (`yield`), deux membres sur une même ligne ;
- `[ExcludeFromCodeCoverage]` (vérifier que coverlet l'omet bien) ;
- instructions de niveau supérieur ;
- test en échec (code 3), projet sans coverlet (code 1), rapport invalide ;
- build déterministe (`ContinuousIntegrationBuild=true`) : chemins `/_/` et erreur si aucun fichier ne correspond ;
- projet de tests sur Microsoft.Testing.Platform (constater le comportement, puis décider de l'étape 10) ;
- plusieurs projets de tests, projet multi-cible.

Ajouter des cas de **complétude du rapport**, car le seuil n'a de sens que si toutes les sources attendues ont été
mesurées : un projet de tests sans rapport parmi plusieurs, une seule cible d'un projet multi-cible, un rapport périmé
réutilisé avec `--use-existing-coverage`, une correspondance partielle des chemins, aucun fichier de production trouvé,
et aucun membre trouvé. Vérifier aussi `--changed` / `--filter` avec une sélection volontairement vide.

Pour chaque fixture représentative, fixer **à la main** la complexité, les lignes couvertes et non couvertes, le score
brut et le code de sortie attendus. Conserver les rapports Cobertura produits comme pièces de diagnostic et vérifier
leur provenance (projet de tests, cible, date), pas seulement le JSON final. Le rapprochement par lignes doit être
examiné particulièrement sur les lambdas, les fonctions locales et plusieurs membres sur une ligne.

**Critères de sortie des étapes 6 et 7 avant d'utiliser `--threshold` comme gate CI :**
- aucun rapport attendu n'est perdu par la sélection « le plus récent par run » ; la fusion des cibles et des projets
  reflète bien l'union des lignes exécutées ;
- un projet de production absent de la couverture reçoit 0 % et un avertissement qui le nomme ; une correspondance
  partielle ne fait pas échouer le gate. Si aucun fichier sélectionné ne correspond, l'outil échoue ;
- distinguer ce cas d'un **rapport attendu d'un projet de tests** qui n'a pas été produit, d'un rapport périmé et d'une
  sélection volontairement vide ; établir leur diagnostic et leur code de sortie à partir des fixtures ;
- les écarts de complexité et de couverture par rapport aux valeurs établies à la main sont expliqués et documentés ;
- les entrées CLI qui changent le verdict sont validées : seuil fini et positif, modes de couverture incompatibles,
  valeur manquante suivie d'une autre option ;
- le JSON distingue le score brut utilisé pour le verdict de sa valeur tronquée d'affichage, et indique au minimum le
  périmètre analysé et la provenance de la couverture. Éviter qu'un rapport `--changed` / `--filter` passe pour un
  rapport complet.

**Résultats de l'étape 7** (mesurés le 2026-11-01, ReportGenerator 5.5.12, coverlet 6.0.0, SDK 10.0.401) :

- *Oracle de complexité indépendant.* L'analyseur Microsoft **CA1502** (règle de métriques de code de `Microsoft.CodeAnalysis.NetAnalyzers`,
  même métrique que Visual Studio ; seuil réglé à 1 par `CodeMetricsConfig.txt` pour qu'il rapporte toutes les méthodes) ne
  partage aucun code avec CrapSharp. Le test `ComplexityOracleTests` (5 fixtures, de bout en bout) vérifie que notre CC est
  **identique** à la sienne pour chaque méthode : `Ten` 10, `Score` 5 (fonction locale et lambda comprises), `Evens` 3,
  `LoadAsync` 2, `Sign` 3, instructions de niveau supérieur 2, etc. Les conventions de §4.2 sont donc cohérentes avec cet outil
  de référence pour les constructions des fixtures ; les autres constructions de §4.2 ne sont pas recoupées.
- *ReportGenerator n'est pas un oracle de CC.* Il affiche la complexité du Cobertura de coverlet, calculée sur l'IL : `Ten` 18
  (au lieu de 10), `Sign` 4 (3), `Evens` 4 (3), `LoadAsync` 4 (2), `Score` 6 (5) avec la fonction locale `Weight` comptée à part.
  Son CRAP en découle (`Ten` : 18 au lieu de 10). Même seuil, verdicts différents.
- *Couverture par méthode.* Celle de ReportGenerator coïncide avec la nôtre, sauf là où coverlet sépare une fonction locale du
  membre englobant (`Score` : 66,66 % pour lui, 76,5 % pour nous, qui additionnons `Weight` conformément à §4.2).
  Coverlet n'écrit aucune entrée pour `Second` dans `sameline` (il n'y figure que `First`) : notre calcul par lignes lui donne 100 %.
- *Gate strict.* ReportGenerator sort toujours avec le code 0, même avec `metricThresholdForCrapScore=5` alors que `Ten` (18)
  le dépasse ; les « risk hotspots » n'apparaissent que dans le HTML. Il ne peut pas servir de gate par méthode sans script
  supplémentaire. Mesure faite à la main, une fois, non automatisée (outil installé hors du dépôt).
- *Conclusion proposée* (à valider) : garder le calcul de CrapSharp ; ReportGenerator reste utile pour visualiser la
  couverture. Voir §7.

**Étape 7 (plan d'origine) :** ReportGenerator est utile pour confronter la présentation de la couverture et examiner une solution
existante. Il expose déjà une métrique CRAP et des seuils de risque
([réglages](https://github.com/danielpalme/ReportGenerator/wiki/Settings)), mais sa
[documentation](https://github.com/danielpalme/ReportGenerator/wiki/Calculation-details) précise qu'il affiche les
métriques fournies par le rapport d'entrée sans les recalculer. Il ne constitue donc pas un oracle indépendant pour
notre CC ou notre CRAP. Comparer également, sur une fixture, sa capacité réelle à satisfaire le gate strict par
méthode avant de conclure qu'un outil spécifique est nécessaire.

**Réalisé à l'étape 6** (14 tests de bout en bout, 12 fixtures dans `tests/fixtures/`). Ils sont dans le projet de tests
mais marqués `[EndToEndFact]` (trait `Category=EndToEnd`) : **ignorés sauf si `CRAPSHARP_E2E=1`**, donc `dotnet test`
reste rapide (≈ 0,2 s) et l'auto-contrôle de l'outil ne les lance pas. Commande : `CRAPSHARP_E2E=1 dotnet test --filter
Category=EndToEnd` (≈ 40 s, réseau NuGet requis au premier passage). Les fixtures sont copiées dans un dossier
temporaire ; leurs rapports Cobertura et JSON sont conservés dans `artifacts/e2e/<fixture>/` (ignoré par git). Les
projets des fixtures sont en `*.csproj.template` pour que l'outil, lancé sur ce dépôt, ne les découvre pas.

Écarts entre le calcul à la main et coverlet : un seul, sur `closures` (voir §4.4, la ligne `};` du lambda). Les autres
valeurs (CC, lignes, CRAP brut, code de sortie) ont été retrouvées telles que calculées.

**Reste de la liste de l'étape 6, non couvert de bout en bout** : un rapport réellement périmé (seul le test
unitaire existe) ; `--changed` dans un vrai dépôt git (sélection vide, fichier modifié) ; une correspondance partielle de
chemins (testée seulement avec des rapports écrits à la main) ; un projet sans aucun membre ; Microsoft.Testing.Platform
avec adhésion explicite au nouveau `dotnet test` (voir §7, étape 10).

**Ordre retenu** : on valide la mesure (6, 7) avant de figer le skill (8), parce que le skill s'appuie sur des chiffres
qu'il faut pouvoir croire. Le skill peut toutefois être rédigé en parallèle, car il dépend peu des détails de l'outil.
Ses commandes d'installation et d'exécution devront être vérifiées avec le package réel de l'étape 9 avant de le
considérer terminé.

**Étape 11 — définition de terminé.** Comparer l'outil choisi à des exemples C# de duplication réelle et de code
semblable sans duplication problématique. Établir la signification de son seuil : le rang `≥ 0,8` de dryer ne se
transpose pas automatiquement à un autre détecteur. Le contrôle doit fournir des emplacements et des diagnostics
exploitables, un code de sortie stable pour la CI, des exclusions documentées (code généré, tests si pertinent) et une
commande exacte dans le skill. Vérifier le temps d'exécution et les faux positifs sur un dépôt .NET représentatif.

**Étape 12 — preuve d'utilisation autonome et portable.** Depuis une copie propre d'un projet .NET **sans Spec Kit** :
- restaurer les outils avec des versions fixées (manifeste `dotnet tool` pour `crapsharp` et Stryker.NET, ou procédure
  équivalente) et exécuter les commandes documentées sur Windows et Linux ;
- exécuter `crapsharp . --threshold 10`, Stryker.NET et le contrôle de duplication directement en terminal et dans une
  CI qui interprète leurs codes de sortie, sans agent IA ;
- rédiger le cœur du skill avec le format ouvert `SKILL.md`, sans syntaxe propre à un agent. Documenter et essayer
  son installation et son invocation dans Claude Code et dans Codex / ChatGPT sur une surface qui donne accès au
  dépôt et à la CLI. Les emplacements et commandes d'invocation propres à chaque hôte restent dans de courts guides
  séparés ([Claude Code](https://code.claude.com/docs/en/skills),
  [documentation officielle OpenAI](https://learn.chatgpt.com/docs/build-skills)) ;
- démontrer sur le pilote un résultat conforme et un échec attendu pour chaque contrôle, mesurer leur durée et
  documenter les limites (par exemple configuration MSBuild, frameworks ciblés, collecteur de couverture). Un skill
  chargé dans un environnement sans accès aux fichiers ou sans exécution de commandes ne peut pas lancer le gate ;
- décider séparément si un plugin est nécessaire pour distribuer le skill dans ChatGPT web/mobile. Cette distribution
  ne conditionne pas l'usage de la CLI et du skill dans les hôtes locaux déjà vérifiés.

**Étape 13 — adaptation à Spec Kit.** Sur un projet pilote utilisant Spec Kit, inscrire les règles de qualité dans la
constitution, invoquer le skill pendant l'implémentation et fournir un exemple d'extension ou d'overlay qui lance
les contrôles après `implement` ([extensions](https://github.github.io/spec-kit/reference/extensions.html),
[workflows](https://github.github.io/spec-kit/reference/workflows.html)). Vérifier les résultats et les codes de
sortie dans ce contexte. L'adaptateur réutilise les commandes de l'étape 12 ; le cœur ne dépend pas de Spec Kit.

**Critère de valeur ajoutée.** À l'étape 7 puis sur un pilote, comparer trois situations : (1) IA + Spec Kit avec sa
configuration initiale ; (2) IA + Spec Kit configuré pour la qualité .NET avec ReportGenerator, Stryker.NET et l'outil
de duplication retenu ; (3) le skill et les contrôles de CrapSharp, avec ou sans Spec Kit. Mesurer les problèmes
détectés, les faux positifs, le temps d'installation et de configuration, le temps d'exécution et l'effort pour répéter
les contrôles sur plusieurs projets ou agents. Le scénario (2) représente une équipe ayant déjà investi dans une
configuration équivalente ; il ne doit pas être confondu avec Spec Kit installé sans ces règles. La formule CRAP seule
n'est pas nouvelle : CrapSharp doit apporter une vérification et une expérience d'usage qui justifient sa maintenance.
Un score CRAP < 10 reste un indicateur de risque, pas une preuve de couverture ni de correction : une méthode de CC 1
non couverte obtient CRAP 2.

## 7. Décisions restantes, au moment où elles deviennent nécessaires

Les orientations ci-dessous sont des propositions, pas des décisions déjà prises. Une étape se termine lorsque son
choix est consigné ici ou dans §8, mis en œuvre si nécessaire, et vérifié par le critère de l'étape. On ne doit pas
attendre de trancher tous les points avant de commencer l'étape 6.

| Quand | Décision à prendre | Orientation actuelle / preuve attendue |
|---|---|---|
| 6 | Projet de tests exécuté avec succès mais sans rapport, alors que d'autres rapports existent ; sélection vide avec `--changed` ou `--filter` | **Pris provisoirement à l'étape 6, à valider à la relecture** : avertissement nommant le projet (0 % pour ce que lui seul couvre) ; erreur si aucun projet n'a de rapport ; `--filter` sans correspondance → 1, `--changed` sans changement → 0 (§4.4). |
| 6–7 | Une ou plusieurs cibles .NET, `DefineConstants`, fichiers `Compile`, projets hors racine de `.sln`, `Directory.Build.props` | Choisir le périmètre réellement compilé que l'outil promet ; mesurer les écarts sur fixtures avant de décider du support MSBuild. |
| 7 | Couverture des branches de Cobertura | Conserver la couverture par lignes pour le CRAP tant qu'un cas réel ne justifie pas une autre convention ; documenter l'écart avec les autres outils. |
| 7 | Réutiliser ReportGenerator ou poursuivre le calcul propre à CrapSharp | **Proposé, à valider** : poursuivre le calcul propre (CC alignée sur CA1502, gate avec code de sortie) ; ReportGenerator n'a ni gate ni la même CC (§6, résultats de l'étape 7). |
| 8 | Nom du skill (`/crap-mode` pour CRAP seul, `/ub-dotnet` ou autre pour la discipline complète) | Choisir un nom cohérent avec son périmètre et vérifier l'invocation dans les hôtes visés. |
| 8, 12 | Règle de mutation Stryker.NET : quand lancer le contrôle et quel verdict rend la CI rouge | Séparer le retour rapide du contrôle potentiellement long ; documenter la commande, le seuil retenu et un cas d'échec attendu. |
| 9, 12 | Identifiant et version du package, mode de distribution, licence du projet | Tester au moins un manifeste `dotnet tool` depuis une copie propre ; décider ensuite de la publication NuGet. Fournir une licence explicite avant toute diffusion publique. |
| 6, 12 | `--changed` / `--filter` et `.metrics/crap.json` | **Partiel** : métadonnées `scope.complete` ajoutées (§4.6) ; un rapport partiel écrase toujours le fichier. Reste à décider d'un second fichier, et à vérifier les consommateurs (skill). |
| 10 | Support Microsoft.Testing.Platform | Tester `Microsoft.Testing.Extensions.CodeCoverage` (`--coverage --coverage-output-format cobertura`) et `coverlet.MTP` (`--coverlet --coverlet-output-format cobertura`) ; intégrer la voie retenue ou documenter la limite ([documentation .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage)). |
| 11 | Détecteur de duplication, seuil, exclusions et coût | Comparer un outil existant (ex. jscpd) à une brique Roslyn sur des exemples C# ; le rang `0,8` de dryer n'est pas directement transférable (§6). |
| 12 | Hôtes testés pour le skill et niveau de support promis | Vérifier au minimum Claude Code et Codex / ChatGPT avec accès au dépôt et à la CLI ; publier une matrice « testé / documenté / non pris en charge ». Un skill seul ne donne pas accès à un shell. |
| 12 ou plus tard | Distribution du skill dans ChatGPT web/mobile | Décider si un plugin est utile ; l'usage CLI/CI et les hôtes locaux ne dépendent pas de cette option. |
| 13 | Forme de l'adaptateur Spec Kit | Choisir extension, overlay de workflow ou simple guide sur le pilote ; conserver les mêmes commandes CLI que hors Spec Kit. |

L'export EDN compatible `uml-viewer` reste hors périmètre, sauf demande explicite.

**Jalons de livraison.** La CLI peut être annoncée fiable pour son périmètre déclaré après 6–7 et 9. Le produit
portable (CLI + skill + mutations + duplication) est prêt à être diffusé après 8–12, si le pilote et la matrice d'hôtes
passent. L'étape 13 valide ensuite Spec Kit ; elle n'est pas un prérequis à l'usage autonome.

## 8. Décisions prises

- Un seul dépôt `CrapSharp` (outil et skill) ; nom de l'outil `crapsharp`.
- Outil autonome en .NET (pas d'ajout du C# à crapper en Python).
- JSON plutôt que l'EDN de crapper ; couverture en pourcentage, chemins absolus.
- Rapprochement par plages de lignes, pas par nom CLR.
- Chaque `case` compte (McCabe, crapper) ; lambdas et fonctions locales dans le membre englobant.
- Seuil strict (`≥ N` échoue) ; affichage tronqué.
- Cible .NET 10 (LTS), car .NET 8 n'est plus supporté à partir du 10 novembre 2026.
- Mutations : Stryker.NET, sans réécriture.
- Un seul choix de source de couverture par exécution : en donner plusieurs est une erreur, plus de priorité silencieuse (étape 6).
- Un commit initial existe (`3d8d55f`, étapes 1 à 5) ; aucun commit supplémentaire n'est demandé ici.
- CLI et contrôles utilisables indépendamment de tout agent ; cœur du skill portable, avec guides d'installation
  propres aux hôtes vérifiés. Spec Kit reste un adaptateur prévu à terme.
- Correspondance de couverture partielle : avertissement par projet, méthodes absentes notées à 0 % ; erreur seulement
  si aucun fichier analysé ne correspond au rapport.

## 9. Suivi des reviews

Texte complet dans [REVIEWS.md](REVIEWS.md).

| Source | Point | Traitement |
|---|---|---|
| Codex 1, Claude | Seuil : un score égal à 10 passait | **Corrigé** : `>=`, message explicite, tests au seuil exact (§4.1) |
| Codex 1, Claude | L'arrondi d'affichage trompe (9,96 → « 10.0 ») | **Corrigé** : troncature dans le tableau et le JSON |
| Claude | .NET 8 en fin de support le 10/11/2026 | **Corrigé** : net10.0 et `RollForward=Major` |
| Claude | `[ExcludeFromCodeCoverage]` noté à 0 % | **Corrigé** et **confirmé** sur coverlet 6.0.0 (fixture `excluded`) |
| Codex 3, Claude | Échec des tests annoncé comme un succès | **Corrigé** : code 3, rapport conservé (§4.5) |
| Codex 3, Claude | Chemins du rapport qui ne correspondent pas | **Corrigé** : erreur si aucun fichier ne correspond, avertissement par projet si la correspondance est partielle |
| Claude | Options inconnues ignorées, pas de `--help` | **Corrigé** : options strictes, `--help` / `-h` |
| Codex 4 | Instructions de niveau supérieur ignorées | **Corrigé** : une entrée par fichier (§4.3) |
| Codex 4 | `#if`, multi-cible, fichiers liés, code généré | **Documenté** (§4.3) ; multi-cible **vérifié** (fixture `multitarget`) |
| Codex 5 | Conventions de complexité à spécifier avec exemples | **Fait** (§4.2) ; recoupée avec CA1502 (identique) et ReportGenerator (autre métrique), étape 7 |
| Codex 2 | Limites du rapprochement par lignes à mesurer | **Mesuré** sur `closures` et `sameline` ; limites en §4.4 |
| Codex | Contrat de la CLI à détailler | **Fait** (§4.5, `--help`, PROGRESS.md) |
| Codex | Tests de bout en bout avec vrais rapports coverlet | **Fait** (14 tests, étape 6 ; manques listés en §6) |
| Codex | ReportGenerator plus tôt | **Avancé** : étape 7, juste après les fixtures |
| Codex, Claude | Périmètre : CRAP seulement ≠ ub-mode | **Intégré** : phases A, B, C (§2) |
| Codex | Le skill doit pousser vers des tests de comportement, pas des tests artificiels | **Retenu** pour l'étape 8 |
| Codex | Compatibilité uml-viewer | Hors périmètre (§7.5) |
| Claude | Microsoft.Testing.Platform incompatible avec `--collect` | **Constaté** (fixture `mtp`, SDK 10) : `dotnet test` refuse le projet, code 1 avec message ; support à l'étape 10 |
| Codex 3 | Un rapport partiel ou une seule cible peut produire un gate trompeur ; `NewestPerRun` ne garde qu'un rapport par projet | **Confirmé puis corrigé** : sur `multitarget`, un rapport était perdu (6,00 au lieu de 2,00) ; tous les rapports d'un run sont lus. Projet de tests sans rapport : avertissement (§4.4) |
| Codex 3 | ReportGenerator expose CRAP mais ne recalcule pas les métriques fournies | **Confirmé** : il relit la CC IL de coverlet ; oracle indépendant = CA1502 (étape 7) |
| Codex 3 | Seuil CLI non fini, modes concurrents, JSON tronqué et périmètre implicite | **Corrigé** (étape 6) : seuil fini > 0, sources de couverture exclusives, valeur manquante, `crapRaw`, `scope`, `coverage`, sélection vide (§4.4 à 4.6) |
| Utilisateur | Une correspondance partielle est légitime si aucun test n'utilise un projet de production | **Décidé et implémenté** : avertissement par projet, 0 % ; erreur seulement si aucun fichier n'est apparié |
| Utilisateur | L'usage avec Claude, ChatGPT et d'autres contextes prime ; Spec Kit viendra ensuite | **Intégré** : CLI/CI autonomes et skill portable à l'étape 12, adaptateur Spec Kit à l'étape 13 |

**Pour la prochaine review**, regarder en priorité : les choix de comportement pris à l'étape 6 sans décision préalable
(sources de couverture exclusives, sélection vide, fenêtre de 10 minutes de `--use-existing-coverage`, avertissement
pour un projet de tests sans rapport), les manques listés en §6, puis l'étape 7.
