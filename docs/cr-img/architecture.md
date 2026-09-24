# Architecture de la librairie CdaCrImg

## Contraintes

| Contrainte | Conséquence |
|---|---|
| **.NET Standard 2.0** | Consommable par .NET Framework ≥ 4.6.1, .NET Core 2.0+, .NET 5+ (et Mono/Xamarin). Pas de `System.Text.Json`, `Span` limité, pas de `DateOnly`. |
| **Autonome** (zéro dépendance NuGet) | XML via `System.Xml.Linq` ; aucune librairie HL7/FHIR tierce. Les JDV et règles utiles sont embarqués ou codés en dur. |
| **Conformité ANS** | Chaque fonctionnalité est livrée avec un test qui produit un document et le valide (XSD en .NET, schématron via `tools/validate-cda.sh`). |

## Vue d'ensemble

```
 Code appelant (RIS/PACS, DRIM-Box, logiciel de radiologie…)
        │  construit un modèle métier (POCO)
        ▼
 ┌─────────────────────────────┐
 │ Model/        (POCO)        │  CompteRenduImagerie, Patient, Professionnel, Organisation,
 │                             │  ActeImagerie, ExamenDicom/Serie/Instance, Conclusion…
 ├─────────────────────────────┤
 │ Validation/   (optionnel)   │  Contrôles métier AVANT sérialisation (cardinalités, formats
 │                             │  RPPS/INS/UID, présence des sections obligatoires) → liste d'erreurs
 ├─────────────────────────────┤
 │ Serialization/              │  CrImgWriter : modèle → XDocument (ordre XSD, templateIds, narratif
 │                             │  généré + références #ID)  ; helpers types HL7 (II, CD, TS, PN, AD…)
 └─────────────────────────────┘
        │  XDocument / string / Stream (UTF-8)
        ▼
 Document CDA R2 IMG-CR-IMG 2024.01
```

Principes :

1. **Modèle métier découplé du XML** : l'appelant ne manipule jamais de `XElement`. Les noms
   reprennent le vocabulaire du volet (Acte d'imagerie, Conclusion, Catalogue d'objets…).
2. **Types HL7 minimaux** (`Model/Hl7/`) : `Identifier` (root/extension), `Code`
   (code/codeSystem/displayName/translations/qualifiers), `PersonName`, `Address`, `Telecom`,
   horodatage avec fuseau (`DateTimeOffset` → `yyyyMMddHHmmss+zzzz`).
3. **Constantes centralisées** : `TemplateIds`, `Codes`, `CodeSystems`, `IdentifierRoots`,
   `CdaNamespaces` (déjà en place, vérifiées par test contre les artefacts ANS).
4. **Narratif généré** : chaque section produit son `<text>` (tableaux XHTML CDA) à partir du
   modèle, avec des `ID` stables référencés par les entrées (`originalText/reference`,
   `text/reference`). Possibilité de fournir un narratif libre (conclusion, résultats).
5. **Déterminisme** : pas d'horloge ni de `Guid.NewGuid()` implicites dans le sérialiseur ; les
   identifiants et dates viennent du modèle (ou d'un `IIdGenerator` injectable) → sorties
   reproductibles et testables par comparaison.
6. **Deux niveaux** : `structuredBody` (niveau 3, cible) et `nonXMLBody` PDF (niveau 1), le même
   en-tête étant partagé.
7. **Validation intégrée légère** : la librairie ne peut pas embarquer Saxon/XSLT2 ; elle expose
   `Validate()` (règles métier du volet réimplémentées en C#, messages alignés sur le schématron).
   La validation XSD .NET est possible par l'appelant ; la validation schématron officielle reste
   externe (tests + `tools/validate-cda.sh`).

## API cible (esquisse, à affiner au lot 1)

```csharp
var cr = new CompteRenduImagerie
{
    Id = new Identifier("1.2.250.1.213.1.1.1.45.2024.1.1"),
    SetId = new Identifier("1.2.250.1.213.1.1.1.45.2024.1"),
    Version = 1,
    Titre = "CR d'imagerie médicale - Scanner thoracique",
    DateCreation = DateTimeOffset.Parse("2021-01-08T11:17:00+01:00"),
    Patient = patient,                  // INS + traits d'identité
    Auteur = radiologue,                // RPPS, spécialité, organisation
    Custodian = centreImagerie,
    SignataireLegal = radiologue,
    Demandes = { new DemandeImagerie(orderId, accessionNumber) },
    Actes =
    {
        new ActeImagerie
        {
            StudyInstanceUid = "1.2.250.1.925.994044.27.123.1876360",
            CodeLoinc = new Code("24727-0", CodeSystems.Loinc, "CT tête avec contraste IV"),
            CodeCcam = new Code("ACQH004", CodeSystems.Ccam),
            Modalites = { Modalite.CT },
            // technique, produits administrés, exposition, catalogue DICOM (séries/instances)…
        }
    },
    Conclusion = "Pas d'anomalie décelée.",
};

XDocument xml = CrImgWriter.Write(cr);         // ou WriteTo(Stream)
IReadOnlyList<ValidationIssue> issues = cr.Validate();
```

## Organisation du code

```
dotnet/
├── CdaCrImg.sln
├── Directory.Build.props           # LangVersion latest, Nullable, TreatWarningsAsErrors
├── src/CdaCrImg/                   # netstandard2.0, zéro dépendance
│   ├── CdaNamespaces.cs  CodeSystems.cs  Codes.cs  TemplateIds.cs   (en place)
│   ├── Model/            (POCO métier + types HL7)
│   ├── Serialization/    (CrImgWriter, builders par section/entrée, NarrativeBuilder)
│   └── Validation/       (règles métier du volet)
└── tests/CdaCrImg.Tests/           # net8.0, xUnit
    ├── RepoPaths.cs                # accès ExemplesCDA/, infrastructure/, schematrons/
    ├── CdaXsdValidator.cs          # XSD CDA en .NET (adaptations documentées)
    └── ReferenceExampleTests.cs    # garde-fous sur les constantes et le harnais
```

## Stratégie de test

| Niveau | Outil | Quand |
|---|---|---|
| Unitaire (types HL7, formats, builders) | xUnit | chaque commit |
| XSD CDA R2 | `CdaXsdValidator` (.NET, en test) | chaque document produit en test |
| Schématron du volet + profils | `tools/validate-cda.sh` (Java/Saxon) | fin de chaque lot ; possible via test d'intégration qui écrit le XML puis appelle le script si Java est présent |
| Non-régression | reproduire `ExemplesCDA/IMG_CR_IMG_2024.01.xml` depuis le modèle et comparer structurellement | lot 4 |

Note XSD .NET : `CdaXsdValidator` omet l'import XSLT de `CDA_extended.xsd` et retire en mémoire le
`xs:any ##other` du type ED (`general/datatypes-base.xsd`) qui viole la contrainte UPA refusée par
`System.Xml`. Le validateur Java du kit reste la référence.
