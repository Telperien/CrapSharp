# CrapSharp — historique des reviews

> Texte des reviews conservé tel quel. Le suivi point par point (ce qui a été fait, ce qui reste) est dans
> [PLAN.md](PLAN.md), section 9.

## Review 1 — Codex (2026-11-01, sur le plan et les étapes 1 à 3)

Texte d'origine (alors section 9 de PLAN.md) :

Ces remarques complètent le plan sans remettre en cause la décision de commencer par le CRAP. Elles sont à examiner avant de poursuivre ; les propositions de conception restent ouvertes à discussion.

#### Périmètre : une première brique, pas encore tout ub-mode

Le découpage entre outil déterministe et skill est pertinent. En revanche, le skill prévu à l'étape 5 ne couvre pour l'instant que le CRAP. [ub-mode](https://github.com/unclebob/ub-mode/blob/master/SKILL.md) comprend aussi des principes de travail (notamment TDD, conception, correction des causes et refactoring), ainsi que des contrôles de duplication et de mutations. Présenter CrapSharp / crap-mode comme une première étape ciblée ; si l'objectif final reste un équivalent plus complet, prévoir une phase ultérieure pour ces pratiques et ces contrôles. Stryker.NET est une piste pour les mutations, mais son intégration et le choix d'un outil de duplication restent à définir.

#### Fiabilité de la mesure et du garde-fou

1. **Seuil strict** : l'objectif est CRAP **< 10**, mais `ThresholdCheck.Exceeds` utilise actuellement `>` : un score égal à 10 passe. Décider explicitement de la règle et aligner le code, les messages et les tests, notamment au seuil exact. Comparer la valeur non arrondie ; l'arrondi relève de l'affichage.
2. **Couverture par plage de lignes** : c'est une approximation utile, pas une correspondance exacte avec un membre. Deux membres sur la même ligne peuvent se voir attribuer les mêmes hits ; les lambdas et fonctions locales peuvent également compliquer l'attribution. Construire de petites fixtures avec de vrais rapports Coverlet pour mesurer ces limites, puis documenter ou signaler les cas ambigus. Étudier si les informations de méthode du rapport peuvent aider sans supposer que les noms CLR correspondent directement au source.
3. **Absence de couverture et échec de collecte** : distinguer un membre absent d'un rapport valide (règle 0 % choisie) d'un rapport absent, périmé, incomplet ou dont les chemins ne correspondent pas. Un échec des tests ou de la collecte ne doit pas être annoncé comme un contrôle réussi. Définir les diagnostics et codes de sortie associés.
4. **Périmètre effectivement analysé** : traiter les instructions de premier niveau, actuellement ignorées, et vérifier les directives `#if`, projets multitarget, fichiers liés, code généré et projets de tests. Avec un parsing syntaxique sans configuration de projet, documenter les limites et éviter de donner l'impression que tous les membres ont été contrôlés si certains sont omis.
5. **Conventions de complexité** : les choix pour les `case`, `when`, opérateurs conditionnels, lambdas et fonctions locales sont défendables, mais les outils peuvent compter différemment. Les spécifier avec des exemples et résultats attendus, y compris les éventuels doubles comptages. Comparer tôt avec ReportGenerator sur des exemples maîtrisés ; expliquer les différences de convention au lieu de supposer que les deux outils doivent systématiquement donner le même chiffre.

#### CLI, tests et intégration

- Détailler le contrat de l'étape 4 : découverte des solutions, projets et rapports ; plusieurs projets de test ; collecteur manquant ; échec de `dotnet test` ; fraîcheur des rapports ; signification exacte de `--changed` ; exclusions ; codes de sortie et emplacement des résultats.
- Ajouter des tests de bout en bout avec de petites solutions C# et des rapports Coverlet réels, en lançant la CLI et en vérifiant les résultats et codes de sortie. Couvrir notamment l'égalité au seuil, une méthode non couverte, un rapport invalide ou absent et les chemins de fichier.
- Le JSON est un choix raisonnable, mais il n'assure pas la compatibilité avec `uml-viewer`, qui attend le format et les identifiants de l'écosystème crapper. Décider explicitement si cette intégration est hors périmètre ou si un export compatible sera nécessaire plus tard.
- Le futur skill devrait indiquer comment agir sur un score élevé avec des tests de comportement et du refactoring, sans encourager des tests artificiels destinés uniquement à faire baisser le chiffre.

#### Ordre proposé

Avant d'étendre la CLI, préciser les conventions et les cas limites, puis les valider sur des fixtures réelles. Ensuite terminer la CLI, son packaging et le skill CRAP. La comparaison avec ReportGenerator, actuellement en étape 7, gagnerait à être engagée plus tôt. Une phase distincte pourra ensuite étendre le périmètre vers le reste d'ub-mode.

Vérification effectuée lors de cette review : `dotnet test --no-restore --verbosity quiet` réussit avec 62 tests. Cela confirme les tests existants, sans constituer une validation exhaustive de la mesure sur des projets .NET variés.

## Review 2 — Claude (2026-11-01, après l'étape 4)

Résumé (la review complète a été faite en conversation) :

- Le code est sain, lisible et bien testé ; le rapprochement par plages de lignes est le bon choix.
- Le plan n'avait pas intégré la review 1 : feuille de route inchangée, seuil toujours en `>`, et l'étape 4 a été faite
  avant la clarification des conventions que Codex proposait.
- Bloquants relevés : seuil (`>` au lieu de `>=`), fin du support de .NET 8 le 10 novembre 2026, méthodes
  `[ExcludeFromCodeCoverage]` notées à 0 % (faux positifs), échec des tests sans code de sortie dédié.
- Peu coûteux : alerte si aucun fichier analysé n'apparaît dans la couverture (chemins `/_/` des builds déterministes),
  options inconnues ignorées en silence, pas de `--help`, arrondi d'affichage trompeur (9,96 affiché « 10.0 »).
- À vérifier : les projets de tests sur Microsoft.Testing.Platform n'acceptent pas `--collect:"XPlat Code Coverage"`.
- Périmètre : ub-mode est surtout une discipline de travail (TDD, causes, tests de comportement, journal, petits
  commits) qui appelle trois outils (crapper, dryer, mutator). L'équivalent .NET demande donc le skill complet ;
  Stryker.NET existe déjà pour les mutations ; rien n'est prévu pour la duplication.
- Rien n'est commité : aucun filet de sécurité.

## Review 3 — Codex (2026-11-01, sur le plan après l'étape 5)

Le découpage outil de mesure → skill → duplication est cohérent avec l'objectif d'un équivalent .NET d'ub-mode.
L'ordre 6 → 7 → 8 reste pertinent, à condition de valider les commandes du skill sur le package `dotnet tool` réel
de l'étape 9.

- **Complétude avant seuil CI.** `CoverageReportFinder.NewestPerRun` retient un seul Cobertura par dossier de projet
  de tests : un projet multi-cible peut produire plusieurs rapports et en perdre un. `App` n'avertit que lorsqu'aucun
  fichier analysé n'apparaît dans la couverture. Un rapport partiellement apparié, périmé ou absent pour un projet
  parmi plusieurs peut donner un verdict faussement rassurant. Les fixtures doivent contrôler le nombre et l'origine
  des rapports, les fichiers appariés et le code de sortie.
- **Oracle indépendant.** Fixer à la main la complexité, les lignes couvertes et le CRAP brut d'une petite série de
  fixtures. ReportGenerator expose déjà une métrique CRAP et des seuils, mais indique qu'il ne recalcule pas les
  métriques présentes dans les rapports d'entrée. Il sert de comparaison complémentaire et de candidat à la
  réutilisation, pas de preuve indépendante de notre calcul.
- **Contrat de la CLI et du JSON.** Valider un seuil positif et fini, expliciter les modes de couverture incompatibles,
  distinguer le score brut du score tronqué en JSON et marquer le périmètre d'un rapport `--changed` ou `--filter`.
  Choisir le comportement d'une sélection vide et d'un rapport de couverture incomplet avant de présenter le seuil
  comme gate CI.
- **Configuration .NET.** La découverte actuelle par parcours de dossiers et l'analyse Roslyn sans propriétés MSBuild
  ne représentent pas forcément la configuration compilée (`DefineConstants`, fichiers `Compile`, frameworks cibles).
  La prochaine étape doit définir le contrat de configuration et tester ces écarts. Pour Microsoft.Testing.Platform,
  examiner les deux collecteurs capables de produire Cobertura, avec leurs options propres.
- **Interprétation du seuil.** Si CC ≥ N, une couverture parfaite ne suffit pas ; réduire les décisions ou simplifier
  l'algorithme peut aussi faire passer le seuil. Le découpage n'est pas la seule voie.

### Décision de l'utilisateur après la review 3

Une correspondance partielle est normale lorsqu'aucun test n'utilise un projet de production. Ses méthodes restent
notées à 0 % ; l'outil avertit en nommant les projets absents de la couverture. Il retourne une erreur seulement si
aucun fichier analysé ne correspond au rapport. Le plan et l'outil ont été alignés sur cette décision.
