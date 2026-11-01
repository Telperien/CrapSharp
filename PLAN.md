# CrapSharp — plan du projet

> À relire par une autre IA / un humain avant de critiquer ou de continuer le travail.
> État détaillé et mode d'emploi : [PROGRESS.md](PROGRESS.md). Texte intégral des reviews : [REVIEWS.md](REVIEWS.md).
> Dernière mise à jour : 2026-11-01, après une troisième revue du plan (points ajoutés aux étapes 6 et 7).

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
vaut `1 − ((seuil − CC) / CC²)^(1/3)` si CC < seuil (si CC ≥ seuil, seul un découpage peut aider).

## 2. Objectif et périmètre

**Objectif final : un équivalent .NET d'ub-mode**, c'est-à-dire un skill de discipline adapté à .NET qui appelle des
contrôles déterministes. Il se construit en trois phases.

| Phase | Contenu | État |
|---|---|---|
| A | Outil `crapsharp` (CRAP par méthode), fiable et validé | en cours (étapes 1 à 5 faites, 6 et 7 à faire) |
| B | Skill complet (discipline ub-mode + `crapsharp` + **Stryker.NET** pour les mutations), packaging | à faire |
| C | Contrôle de duplication (choix d'outil à faire) | à faire |

Pour les mutations, on utilise **Stryker.NET** tel quel, un outil existant et mature : pas de réécriture.

## 3. Livrables (un seul dépôt `CrapSharp`)

1. **Outil CLI `crapsharp`** (.NET 10, publié en `dotnet tool`).
2. **Skill** (`skill/<nom>/SKILL.md`, nom à confirmer, voir §7) : la discipline d'ub-mode reformulée pour .NET
   (texte original, car ub-mode n'a pas de licence), avec les commandes exactes de `crapsharp` et de Stryker.NET.
3. **Tests** : tests unitaires xUnit, plus des tests de bout en bout sur de vraies mini-solutions avec coverlet.
4. Plus tard : contrôle de duplication.

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
différer de Visual Studio ou de ReportGenerator ; l'étape 7 mesure ces écarts au lieu de les supposer nuls.

### 4.3 Ce qui est mesuré

- **Membres** avec un corps : méthodes, constructeurs, destructeurs, opérateurs, conversions, accesseurs (`get`,
  `set`, `init`, `add`, `remove`), propriétés et indexeurs à expression. Membres abstraits, `extern` et propriétés auto :
  rien à mesurer.
- **Instructions de niveau supérieur** (`Program.cs` sans classe) : **une entrée** `<top-level statements>`, de la
  première à la dernière instruction (le compilateur en fait un seul `Main`).
- **Exclus** : membres portant `[ExcludeFromCodeCoverage]` ou `[ExcludeFromCoverage]`, sur le membre, sur sa
  propriété ou son événement, ou sur un type englobant (nom simple, suffixé `Attribute` ou qualifié). Coverlet les
  retire de son rapport par défaut : sans cette règle, ils seraient notés à 0 %. *Comportement de coverlet à confirmer
  sur fixture réelle (étape 6).*
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
- Plusieurs rapports : fusion, on garde le maximum de hits par ligne.
- Couverture des **branches** (`condition-coverage`) : ignorée (lignes uniquement).
- Si certains fichiers analysés sont absents du rapport : leurs membres restent à 0 % et un avertissement nomme les
  projets concernés. Un projet de production qu'aucun test n'exécute est un cas normal de cette règle.
- Si **aucun** fichier analysé n'apparaît dans les rapports : erreur (code 1, sans rapport produit). Vérifier notamment
  les chemins `/_/…` des builds déterministes. Une sélection volontairement vide reste un cas distinct.

### 4.5 Codes de sortie

| Code | Sens | Rapport produit ? |
|---|---|---|
| 0 | OK | oui |
| 1 | Erreur : options invalides, chemin absent, aucun rapport de couverture lisible, aucun fichier analysé apparié, `--changed` hors dépôt git | non |
| 2 | `--threshold N` : au moins une méthode à N ou plus | oui |
| 3 | `dotnet test` ou la commande de couverture a échoué : couverture peut-être incomplète | oui |

Priorité si plusieurs cas s'appliquent : 1, puis 3, puis 2. Options inconnues, valeur manquante ou second chemin :
code 1, avec un renvoi vers `--help`.

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
  Scoring/                     CrapCalculator, MethodReport, ReportFormatter, JsonReportWriter, ThresholdCheck, Truncation
  Projects/ProjectDiscovery.cs, Git/ChangedFiles.cs, Processes/ (IProcessRunner, SystemProcessRunner)
tests/CrapSharp.Tests/         xUnit (127 tests), FakeProcessRunner, TempWorkspace
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
| 6 | **Fixtures réelles et tests de bout en bout** (détail ci-dessous) | **à faire, prochaine étape** |
| 7 | Validation indépendante des scores attendus sur les fixtures ; comparaison complémentaire avec ReportGenerator et évaluation de son éventuelle réutilisation | à faire |
| 8 | Skill complet (phase B) : discipline ub-mode pour .NET, `crapsharp`, Stryker.NET ; README | à faire |
| 9 | Packaging `dotnet tool` (`PackAsTool`, `ToolCommandName=crapsharp`), essai d'installation locale | à faire |
| 10 | Support des projets sur Microsoft.Testing.Platform (si l'étape 6 confirme le problème) | à faire |
| 11 | Contrôle de duplication (phase C) : choisir entre un outil existant (ex. jscpd) et une brique Roslyn | à faire |

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

**Étape 7 :** ReportGenerator est utile pour confronter la présentation de la couverture et examiner une solution
existante. Il expose déjà une métrique CRAP et des seuils de risque
([réglages](https://github.com/danielpalme/ReportGenerator/wiki/Settings)), mais sa
[documentation](https://github.com/danielpalme/ReportGenerator/wiki/Calculation-details) précise qu'il affiche les
métriques fournies par le rapport d'entrée sans les recalculer. Il ne constitue donc pas un oracle indépendant pour
notre CC ou notre CRAP. Comparer également, sur une fixture, sa capacité réelle à satisfaire le gate strict par
méthode avant de conclure qu'un outil spécifique est nécessaire.

Ces tests sont lents : les isoler (trait xUnit ou projet séparé) pour que `dotnet test` reste rapide.

**Ordre retenu** : on valide la mesure (6, 7) avant de figer le skill (8), parce que le skill s'appuie sur des chiffres
qu'il faut pouvoir croire. Le skill peut toutefois être rédigé en parallèle, car il dépend peu des détails de l'outil.
Ses commandes d'installation et d'exécution devront être vérifiées avec le package réel de l'étape 9 avant de le
considérer terminé.

## 7. Questions ouvertes

1. **Nom du skill** : `/crap-mode` avait été proposé pour un skill limité au CRAP. Pour un équivalent complet
   d'ub-mode, un nom comme `/ub-dotnet` serait plus juste. À trancher avec l'utilisateur.
2. **Couverture des branches** : faut-il pondérer par la `condition-coverage` de Cobertura ? Pour l'instant, lignes
   uniquement, comme crapper.
3. **`--changed` et `--filter`** écrasent `.metrics/crap.json` avec un rapport partiel. Faut-il un fichier séparé, ou
   fusionner avec le rapport complet précédent ?
4. **Ouverture des `.sln`** et prise en compte de `Directory.Build.props` pour détecter les projets de tests : à faire
   seulement si un vrai projet en a besoin.
5. **Export compatible uml-viewer** (EDN de crapper) : hors périmètre, sauf demande.
6. **Commits** : rien n'est commité pour l'instant (demande de l'utilisateur), donc il n'y a aucun historique ni
   retour arrière possible. À rediscuter.
7. **Rapport de test manquant et sélection vide** : définir le comportement lorsqu'un projet de tests a réussi sans
   produire de rapport alors que d'autres en ont produit, ou lorsque `--changed` / `--filter` ne sélectionne rien.
   Cela ne change pas la règle « source absente du rapport valide = 0 % ».
8. **Configuration analysée** : l'analyse Roslyn syntaxique ne lit ni les symboles `DefineConstants` ni les fichiers
   `Compile` évalués par MSBuild. Définir si le contrat cible une configuration / un framework précis ou l'union des
   cibles, puis aligner découverte des sources et rapports de couverture sur ce choix.
9. **Microsoft.Testing.Platform** : tester deux voies qui savent produire Cobertura :
   `Microsoft.Testing.Extensions.CodeCoverage` (`--coverage --coverage-output-format cobertura`) et `coverlet.MTP`
   (`--coverlet --coverlet-output-format cobertura`). Les options et dépendances sont distinctes
   ([documentation .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage)).

## 8. Décisions prises

- Un seul dépôt `CrapSharp` (outil et skill) ; nom de l'outil `crapsharp`.
- Outil autonome en .NET (pas d'ajout du C# à crapper en Python).
- JSON plutôt que l'EDN de crapper ; couverture en pourcentage, chemins absolus.
- Rapprochement par plages de lignes, pas par nom CLR.
- Chaque `case` compte (McCabe, crapper) ; lambdas et fonctions locales dans le membre englobant.
- Seuil strict (`≥ N` échoue) ; affichage tronqué.
- Cible .NET 10 (LTS), car .NET 8 n'est plus supporté à partir du 10 novembre 2026.
- Mutations : Stryker.NET, sans réécriture.
- Correspondance de couverture partielle : avertissement par projet, méthodes absentes notées à 0 % ; erreur seulement
  si aucun fichier analysé ne correspond au rapport.

## 9. Suivi des reviews

Texte complet dans [REVIEWS.md](REVIEWS.md).

| Source | Point | Traitement |
|---|---|---|
| Codex 1, Claude | Seuil : un score égal à 10 passait | **Corrigé** : `>=`, message explicite, tests au seuil exact (§4.1) |
| Codex 1, Claude | L'arrondi d'affichage trompe (9,96 → « 10.0 ») | **Corrigé** : troncature dans le tableau et le JSON |
| Claude | .NET 8 en fin de support le 10/11/2026 | **Corrigé** : net10.0 et `RollForward=Major` |
| Claude | `[ExcludeFromCodeCoverage]` noté à 0 % | **Corrigé** côté analyse ; comportement de coverlet à confirmer (étape 6) |
| Codex 3, Claude | Échec des tests annoncé comme un succès | **Corrigé** : code 3, rapport conservé (§4.5) |
| Codex 3, Claude | Chemins du rapport qui ne correspondent pas | **Corrigé** : erreur si aucun fichier ne correspond, avertissement par projet si la correspondance est partielle |
| Claude | Options inconnues ignorées, pas de `--help` | **Corrigé** : options strictes, `--help` / `-h` |
| Codex 4 | Instructions de niveau supérieur ignorées | **Corrigé** : une entrée par fichier (§4.3) |
| Codex 4 | `#if`, multi-cible, fichiers liés, code généré | **Documenté** (§4.3) ; multi-cible à vérifier à l'étape 6 |
| Codex 5 | Conventions de complexité à spécifier avec exemples | **Fait** (§4.2) ; comparaison avec ReportGenerator à l'étape 7 |
| Codex 2 | Limites du rapprochement par lignes à mesurer | **À faire** (étape 6) ; limites décrites en §4.4 |
| Codex | Contrat de la CLI à détailler | **Fait** (§4.5, `--help`, PROGRESS.md) |
| Codex | Tests de bout en bout avec vrais rapports coverlet | **À faire** (étape 6) |
| Codex | ReportGenerator plus tôt | **Avancé** : étape 7, juste après les fixtures |
| Codex, Claude | Périmètre : CRAP seulement ≠ ub-mode | **Intégré** : phases A, B, C (§2) |
| Codex | Le skill doit pousser vers des tests de comportement, pas des tests artificiels | **Retenu** pour l'étape 8 |
| Codex | Compatibilité uml-viewer | Hors périmètre (§7.5) |
| Claude | Microsoft.Testing.Platform incompatible avec `--collect` | **Message d'erreur** ajouté ; à vérifier à l'étape 6, éventuel support à l'étape 10 |
| Codex 3 | Un rapport partiel ou une seule cible peut produire un gate trompeur ; `NewestPerRun` ne garde qu'un rapport par projet | **Nuancé** : l'absence d'un projet de production donne 0 % avec avertissement ; la perte d'une cible reste à vérifier à l'étape 6 |
| Codex 3 | ReportGenerator expose CRAP mais ne recalcule pas les métriques fournies | **Ajouté** à l'étape 7 : valeurs attendues établies indépendamment |
| Codex 3 | Seuil CLI non fini, modes concurrents, JSON tronqué et périmètre implicite | **Ajouté** aux critères de sortie avant le skill |
| Utilisateur | Une correspondance partielle est légitime si aucun test n'utilise un projet de production | **Décidé et implémenté** : avertissement par projet, 0 % ; erreur seulement si aucun fichier n'est apparié |

**Pour la prochaine review**, regarder en priorité le contrat de complétude de la couverture et les résultats des
fixtures de l'étape 6, avant de figer la CLI et le skill.
