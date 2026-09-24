# Architecture de la librairie CdaCrImg

Périmètre : CR d'imagerie **non structuré (CDA R2 niveau 1, PDF encapsulé)**, patients avec **INS**.
Le corps structuré (niveau 3) est hors périmètre.

## Contraintes

| Contrainte | Conséquence |
|---|---|
| **.NET Standard 2.0** | Consommable par .NET Framework ≥ 4.6.1, .NET Core 2.0+, .NET 5+ (et Mono/Xamarin). Pas de `System.Text.Json`, `Span` limité, pas de `DateOnly`. |
| **Autonome** (zéro dépendance NuGet) | XML via `System.Xml.Linq` ; aucune librairie HL7/FHIR tierce. Les JDV et règles utiles sont embarqués ou codés en dur. |
| **Conformité ANS** | Chaque fonctionnalité est livrée avec un test qui produit un document et le valide (XSD en .NET, profils transverses via Java : `AnsJavaValidator`). |

## Vue d'ensemble

```
 Code appelant (RIS/PACS, DRIM-Box, logiciel de radiologie…)
        │  construit un modèle métier (POCO)
        ▼
 ┌─────────────────────────────┐
 │ Model/        (POCO)        │  CompteRenduImagerie, Patient (INS), Professionnel, Organisation,
 │                             │  DemandeImagerie, ActeImagerie, PriseEnCharge, CorpsPdf…
 ├─────────────────────────────┤
 │ Validation/                 │  CrImgValidator : contrôles AVANT sérialisation (champs obligatoires,
 │                             │  traits INS, formats RPPS/UID…) → liste d'erreurs avec chemin
 ├─────────────────────────────┤
 │ Serialization/              │  CrImgWriter : modèle → XDocument (ordre XSD, templateIds, en-tête,
 │                             │  nonXMLBody PDF) ; CdaXml : types HL7 (II, CD, TS, PN, AD…)
 └─────────────────────────────┘
        │  XDocument / string / Stream (UTF-8)
        ▼
 Document CDA R2 niveau 1 : CR d'imagerie (en-tête IMG-CR-IMG) + PDF encapsulé
```

Principes :

1. **Modèle métier découplé du XML** : l'appelant ne manipule jamais de `XElement`. Les noms
   reprennent le vocabulaire du volet (acte d'imagerie, demande, prise en charge…).
2. **Types HL7 minimaux** (`Model/Hl7/`) : `Identifier` (root/extension), `Code`
   (code/codeSystem/displayName/translations/qualifiers), `PersonName`, `Address`, `Telecom`,
   horodatage avec fuseau (`DateTimeOffset` → `yyyyMMddHHmmss+zzzz`).
3. **Constantes centralisées** : `TemplateIds`, `Codes`, `CodeSystems`, `IdentifierRoots`,
   `CdaNamespaces` (déjà en place, vérifiées par test contre les artefacts ANS).
4. **Déterminisme** : pas d'horloge ni de `Guid.NewGuid()` implicites dans le sérialiseur ; les
   identifiants et dates viennent du modèle (ou d'un `IIdGenerator` injectable) → sorties
   reproductibles et testables par comparaison.
5. **Niveau 1 uniquement** : le corps est un PDF encapsulé (`nonXMLBody`, `CorpsPdf`) ; le document
   porte les seuls templateId autorisés par la structuration minimale en non structuré (HL7 France,
   CI-SIS, IHE XDS-SD `1.3.6.1.4.1.19376.1.2.20`). Pas de `structuredBody`.
6. **Validation intégrée légère** : la librairie ne peut pas embarquer Saxon/XSLT2 ; elle expose
   `CrImgValidator` (règles réimplémentées en C#). La validation XSD .NET est possible par
   l'appelant ; la validation schématron officielle reste externe (tests `Category=Schematron`,
   `tools/validate-cda.sh`).

## API

Espaces de noms : `CdaCrImg` (constantes), `CdaCrImg.Model` (+ `.Hl7`), `CdaCrImg.Serialization`,
`CdaCrImg.Validation`. Exemple complet : `dotnet/tests/CdaCrImg.Tests/SampleReports.cs`.

```csharp
var cr = new CompteRenduImagerie
{
    Id = new Identifier("1.2.250.1.213.1.1.1.45.2024.2.1"),
    SetId = new Identifier("1.2.250.1.213.1.1.1.45.2024.2"),
    Titre = "CR d'imagerie médicale - Scanner thoracique",
    DateCreation = DateTimeOffset.Now,
    Patient = patient,                                  // INS + traits d'identité
    Custodian = centreImagerie,
    SignataireLegal = new Signature(radiologue, date),
    PriseEnCharge = priseEnCharge,
    Corps = new CorpsPdf(File.ReadAllBytes("cr.pdf")),  // CR au format PDF
};
cr.Auteurs.Add(new Auteur(radiologue, date));           // radiologue.Organisation obligatoire
cr.Demandes.Add(new DemandeImagerie(numeroDemande, accessionNumber));
var acte = new ActeImagerie
{
    StudyInstanceUid = "1.2.250.1.925.994044.27.123.1876360",
    Code = Code.Loinc("24727-0", "CT tête avec contraste IV"),
    CodeCcam = Code.Ccam("ACQH004"),
    Debut = debutExamen,
    Executant = radiologue,
};
acte.Modalites.Add(Code.Dcm("CT", "Tomodensitométrie"));
acte.RegionsAnatomiques.Add(Code.Snomed("774007", "tête et cou"));
cr.Actes.Add(acte);

IReadOnlyList<ValidationIssue> issues = CrImgValidator.Validate(cr);  // vide si complet
XDocument xml = CrImgWriter.Write(cr);   // lève CrImgValidationException si incomplet
CrImgWriter.Write(cr, stream);           // UTF-8 ; ou CrImgWriter.WriteToString(cr)
```

## Organisation du code

```
dotnet/
├── CdaCrImg.sln
├── Directory.Build.props           # LangVersion latest, Nullable, TreatWarningsAsErrors
├── src/CdaCrImg/                   # netstandard2.0, zéro dépendance
│   ├── CdaNamespaces.cs  CodeSystems.cs  Codes.cs  TemplateIds.cs
│   ├── Model/            (POCO métier + Hl7/ : types HL7)
│   ├── Serialization/    (CrImgWriter, CdaXml, Hl7Format)
│   └── Validation/       (CrImgValidator, ValidationIssue, CrImgValidationException)
└── tests/CdaCrImg.Tests/           # net10.0, xUnit
    ├── RepoPaths.cs                # accès ExemplesCDA/, infrastructure/, schematrons/
    ├── SampleReports.cs            # CR de test : complet (Level1) et minimal (Minimal)
    ├── CdaXsdValidator.cs          # XSD CDA en .NET (adaptations documentées)
    ├── AnsJavaValidator.cs         # XSD Java + schématrons ANS (Saxon), multiplateforme
    ├── ExampleFileTests.cs         # génère ExemplesCDA/CdaCrImg_*.xml
    └── …Tests.cs                   # writer, validateur, formats, schématrons, constantes
```

## Stratégie de test

| Niveau | Outil | Quand |
|---|---|---|
| Unitaire (types HL7, formats, builders) | xUnit | chaque commit |
| XSD CDA R2 | `CdaXsdValidator` (.NET, en test) | chaque document produit en test |
| Profils transverses (structuration minimale, modèles de contenus, IHE) | `AnsJavaValidator` (Java/Saxon), tests `Category=Schematron` | chaque exécution des tests (ignorés si Java absent) |
| Non-régression | reproduire `ExemplesCDA/IMG_CR_IMG_2024.01_CDA-R2-Niveau-1.xml` depuis le modèle et comparer structurellement | lot 2 |

Note XSD .NET : `CdaXsdValidator` omet l'import XSLT de `CDA_extended.xsd` et retire en mémoire le
`xs:any ##other` du type ED (`general/datatypes-base.xsd`) qui viole la contrainte UPA refusée par
`System.Xml`. Le validateur Java du kit reste la référence.
