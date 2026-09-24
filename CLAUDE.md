# CLAUDE.md

## Mission

Développer **CdaCrImg**, une librairie **.NET Standard 2.0 autonome** (aucune dépendance NuGet)
qui encapsule la production d'un **compte rendu d'imagerie médicale** au format **HL7 CDA R2**,
en-tête conforme au volet **CI-SIS IMG-CR-IMG 2024.01** de l'ANS.

**Périmètre** : document **non structuré (CDA R2 niveau 1)**, le CR étant un PDF encapsulé
(`nonXMLBody`, templateId IHE XDS-SD `1.3.6.1.4.1.19376.1.2.20`), pour des patients identifiés par
leur **INS**. Le corps structuré (niveau 3, templateId `1.2.250.1.213.1.1.1.45`) est **hors
périmètre** : ne pas l'implémenter. La conformité se prouve par le XSD CDA et les trois profils
transverses (structuration minimale, modèles de contenus CI-SIS, IHE) ; le schématron du volet ne
s'applique qu'au niveau 3.

Le dépôt est un fork du kit ANS **TestContenuCDA** : les exemples, schémas XSD, schématrons et
jeux de valeurs du kit sont la **source de vérité** de la conformité. Le code .NET vit dans `dotnet/`.

## Carte du dépôt

| Chemin | Rôle |
|---|---|
| `dotnet/` | Solution .NET (`CdaCrImg.sln`) : `src/CdaCrImg` (netstandard2.0), `demo/CdaCrImg.Demo` (application web de démonstration, ASP.NET Core net10.0), `tests/CdaCrImg.Tests` (net10.0, xUnit) |
| `docs/cr-img/specification.md` | **Synthèse du volet IMG-CR-IMG** : en-tête (dans le périmètre) ; sections et entrées du niveau 3 (référence, hors périmètre) |
| `docs/cr-img/champs-obligatoires.md` | Champs minimaux obligatoires d'un CR (nom CdaCrImg, description, règles, sources) |
| `docs/cr-img/architecture.md` | Choix techniques et API cible de la librairie |
| `docs/cr-img/roadmap.md` | Lots de développement et critères de fin |
| `ExemplesCDA/IMG_CR_IMG_2024.01_CDA-R2-Niveau-1.xml` | **Exemple de référence ANS** du CR non structuré (PDF base64, `nonXMLBody`) |
| `ExemplesCDA/IMG_CR_IMG_2024.01.xml` | Exemple ANS du CR structuré (niveau 3, hors périmètre ; utile pour l'en-tête) |
| `ExemplesCDA/CdaCrImg_*.xml` | Exemples générés par la librairie (complet et minimal), écrits par `ExampleFileTests` |
| `schematrons/profils/` | **Schématrons transverses applicables** : structuration minimale, modèles de contenus CI-SIS, IHE |
| `schematrons/CI-SIS_IMG-CR-IMG_2024.01.sch` | Schématron du volet : niveau 3 uniquement (hors périmètre) ; ses règles d'en-tête restent une référence |
| `infrastructure/cda/CDA_extended.xsd` | XSD CDA R2 étendu (DICOM PS3.20, pharmacie, SDTC) |
| `jeuxDeValeurs/*.xml` | Jeux de valeurs (JDV) au format IHE SVS (`urn:ihe:iti:svs:2008`) |
| `tools/validate-cda.sh` | Validation XSD + schématron d'un document (Java, outillage du kit) |
| `.github/workflows/cdacrimg.yml` | CI de la librairie : build, tests (dont schématrons), pack, sous Linux et Windows |
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

# Application web de démonstration (formulaire → CDA XML), http://localhost:5000 par défaut
dotnet run --project dotnet/demo/CdaCrImg.Demo

# Paquet NuGet (CdaCrImg.nupkg + symboles .snupkg)
dotnet pack dotnet/src/CdaCrImg -c Release -o artifacts

# Validation manuelle d'un CDA (bash + Java ; XSD + schématron ; ~15 s la 1re fois, compilation mise en cache)
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
  être valide XSD (`CdaXsdValidator` en .NET) et passer les trois profils transverses sans
  `failed-assert` (tests `Category=Schematron`, via `AnsJavaValidator`).
- En cas de doute sur une règle : spécifications ANS (`docs/cr-img/ans/*.pdf`, en-tête) et
  schématrons transverses font foi (appliquer la contrainte la plus stricte), puis l'exemple ANS
  niveau 1, puis `docs/cr-img/specification.md` et `docs/cr-img/champs-obligatoires.md`.
  Corriger la doc si elle diverge.
- Ne jamais modifier les artefacts ANS (`schematrons/`, `infrastructure/`, `jeuxDeValeurs/`,
  `ExemplesCDA/`) pour faire passer un test.
  Seule exception : les exemples générés par la librairie, préfixés `CdaCrImg_` dans `ExemplesCDA/`
  (ex. `CdaCrImg_IMG-CR-IMG_2024.01_CDA-R2-Niveau-1.xml`, écrit par `ExampleFileTests`).
- **Démo** : tout champ ajouté à la librairie doit être ajouté au catalogue du formulaire
  (`dotnet/demo/CdaCrImg.Demo/Form/FormCatalog.cs`) et au mapper ; `DemoFormCatalogTests` vérifie que
  la mention obligatoire / facultatif de chaque champ correspond à `CrImgValidator`.
- **Jeux de valeurs** : un code contraint par un JDV du CI-SIS est contrôlé par
  `CrImgValidator.Terminologies.cs`. Un JDV embarqué est une `EmbeddedResource` liée au fichier du kit
  ANS dans `CdaCrImg.csproj` (pas de copie), exposée par `JeuxDeValeursCisis`.
- **CI** : `.github/workflows/cdacrimg.yml` (Linux et Windows) doit rester verte ; tester aussi les
  chemins et fins de ligne Windows (voir `.gitattributes`).
- Constantes (OID, codes) : les ajouter dans `TemplateIds.cs`, `Codes.cs`, `CodeSystems.cs` ;
  le test `SpecConstant_IsFoundInAnsArtifacts` vérifie qu'elles existent dans les artefacts ANS.
- Documentation XML (`///`) en français sur l'API publique ; noms de types/méthodes en anglais
  ou français métier cohérents avec l'existant (ex. `TemplateIds.Sections.ActeImagerie`).
- Commits en français, petits et thématiques.
