# CLAUDE.md

## Mission

Développer **CdaCrImg**, une librairie **.NET Standard 2.0 autonome** (aucune dépendance NuGet)
qui encapsule la production d'un **compte rendu d'imagerie médicale** au format **HL7 CDA R2**,
conforme au volet **CI-SIS IMG-CR-IMG 2024.01** de l'ANS (templateId `1.2.250.1.213.1.1.1.45`).

Le dépôt est un fork du kit ANS **TestContenuCDA** : les exemples, schémas XSD, schématrons et
jeux de valeurs du kit sont la **source de vérité** de la conformité. Le code .NET vit dans `dotnet/`.

## Carte du dépôt

| Chemin | Rôle |
|---|---|
| `dotnet/` | Solution .NET (`CdaCrImg.sln`) : `src/CdaCrImg` (netstandard2.0), `tests/CdaCrImg.Tests` (net10.0, xUnit) |
| `docs/cr-img/specification.md` | **Synthèse du volet IMG-CR-IMG** : en-tête, sections, entrées, OID, codes, cardinalités |
| `docs/cr-img/architecture.md` | Choix techniques et API cible de la librairie |
| `docs/cr-img/roadmap.md` | Lots de développement et critères de fin |
| `ExemplesCDA/IMG_CR_IMG_2024.01.xml` | **Exemple de référence ANS** du CR d'imagerie structuré (niveau 3) |
| `ExemplesCDA/IMG_CR_IMG_2024.01_CDA-R2-Niveau-1.xml` | Exemple ANS non structuré (PDF base64, `nonXMLBody`) |
| `schematrons/CI-SIS_IMG-CR-IMG_2024.01.sch` | Schématron du volet (+ `schematrons/include/specificationsVolets/IMG-CR-IMG_2024.01/`) |
| `schematrons/profils/` | Schématrons transverses : structuration minimale, modèles de contenus CI-SIS, IHE |
| `infrastructure/cda/CDA_extended.xsd` | XSD CDA R2 étendu (DICOM PS3.20, pharmacie, SDTC) |
| `jeuxDeValeurs/*.xml` | Jeux de valeurs (JDV) au format IHE SVS (`urn:ihe:iti:svs:2008`) |
| `tools/validate-cda.sh` | Validation XSD + schématron d'un document (Java, outillage du kit) |
| `FeuilleDeStyle/` | Feuilles XSL de rendu (CDA-FO.xsl) — hors périmètre de la librairie |
| `docs/cr-img/ans/CI-SIS_VOLET_CONTENUS_IMG-CR-IMG_2024.01_SFD_20251212.pdf` | Volet CR d’imagerie Spécifications fonctionnelles |
| `docs/cr-img/ans/CI-SIS_VOLET_CONTENUS_IMG-CR-IMG_2024.01_STD_CDA_20251212.pdf` | Volet CR d’imagerie Spécifications techniques |

Tout le reste (autres volets : BIO, ANEST, CSE…) est hors périmètre : ne pas modifier.

## Commandes

```bash
# Build + tests .NET (depuis la racine, Windows ou Linux). Les tests "Schematron" appellent
# directement Java (JAVA_HOME ou PATH) via AnsJavaValidator ; ils sont ignorés si Java est absent.
dotnet test dotnet/CdaCrImg.sln
dotnet test dotnet/CdaCrImg.sln --filter "Category!=Schematron"   # exclure la validation Java

# Validation manuelle d'un CDA (bash + Java ; XSD + schématron du volet ; ~15 s la 1re fois, compilation mise en cache)
tools/validate-cda.sh ExemplesCDA/IMG_CR_IMG_2024.01.xml
tools/validate-cda.sh <doc.xml> profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin
tools/validate-cda.sh <doc.xml> profils/CI-SIS_ModelesDeContenusCDA
tools/validate-cda.sh <doc.xml> profils/IHE
```

Le SDK .NET 10 (épinglé par `global.json`) est installé par `.claude/hooks/session-start.sh` (via apt ; `dot.net` est bloqué
par le proxy de l'environnement web). Java 21 est disponible pour `tools/validate-cda.sh`.

## Règles de travail

- **Zéro dépendance** dans `src/CdaCrImg` : BCL netstandard2.0 uniquement (`System.Xml.Linq`).
  Pas de `PackageReference`. Les dépendances de test (xUnit) sont autorisées dans `tests/`.
- **C# compatible netstandard2.0** : `LangVersion latest` est activé, mais pas d'API runtime
  absente de netstandard2.0 (`string.Contains(char)`, `System.Text.Json`, `DateOnly`, `init` sans
  polyfill `IsExternalInit`, etc.).
- `Nullable` activé et `TreatWarningsAsErrors` : le build doit rester sans avertissement.
- **Conformité prouvée, pas supposée** : tout document produit par la librairie doit, en test,
  être valide XSD (`CdaXsdValidator` en .NET) et, pour les jalons, passer `tools/validate-cda.sh`
  (schématron du volet + profils transverses) sans `failed-assert`.
- En cas de doute sur une règle : spécifications ANS (`docs/cr-img/ans/*.pdf`) et schématron du
  volet font foi (appliquer la contrainte la plus stricte), puis l'exemple de référence, puis
  `docs/cr-img/specification.md`. Corriger la doc si elle diverge.
- Ne jamais modifier les artefacts ANS (`schematrons/`, `infrastructure/`, `jeuxDeValeurs/`,
  `ExemplesCDA/`) pour faire passer un test.
- Constantes (OID, codes) : les ajouter dans `TemplateIds.cs`, `Codes.cs`, `CodeSystems.cs` ;
  le test `SpecConstant_IsFoundInAnsArtifacts` vérifie qu'elles existent dans les artefacts ANS.
- Documentation XML (`///`) en français sur l'API publique ; noms de types/méthodes en anglais
  ou français métier cohérents avec l'existant (ex. `TemplateIds.Sections.ActeImagerie`).
- Commits en français, petits et thématiques.
