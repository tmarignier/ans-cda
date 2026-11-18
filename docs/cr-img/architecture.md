# Architecture de la librairie CdaCrImg

Périmètre : CR d'imagerie **non structuré (CDA R2 niveau 1, PDF encapsulé)**, patients avec **INS**.
Le corps structuré (niveau 3) est hors périmètre.

## Contexte d'utilisation : la téléradiologie

La librairie est utilisée dans le cadre de la **téléradiologie** (télé-imagerie, SFD 3.2.2) : l'examen est
réalisé dans la **structure d'imagerie qui accueille le patient** (site demandeur : manipulateur,
médecin de proximité, RIS / PACS) et **interprété à distance par le téléradiologue**, qui supervise la
réalisation de l'acte et rédige le compte rendu. Conséquences sur l'en-tête (STD 3.2) :

| Élément CDA | Modèle CdaCrImg | En téléradiologie |
|---|---|---|
| `author` [1..*] | `Auteurs` | le **téléradiologue** qui rédige le CR, **plus** un auteur pour le **médecin responsable de la structure d'imagerie qui accueille le patient** |
| `legalAuthenticator` [1..1] | `SignataireLegal` | le **médecin responsable de la structure d'imagerie qui accueille le patient** (responsable du document) |
| `documentationOf/serviceEvent/performer` | `Actes[i].Executant` | l'imageur qui supervise et interprète l'acte (le téléradiologue), avec son établissement de rattachement |
| `inFulfillmentOf/order`, `serviceEvent/id` | `Demandes`, `Actes[i].StudyInstanceUid` | numéro de demande du médecin demandeur ; Accession Number et Study Instance UID attribués par le **RIS de la structure d'accueil** |
| `recordTarget`, `componentOf` | `Patient`, `PriseEnCharge` | identité INS (identitovigilance) et lieu de prise en charge : **structure d'accueil** |
| `custodian` | `Custodian` | structure qui conserve le CR, selon la convention de téléradiologie |

Cadre d'interopérabilité : le **volet Téléradiologie** du CI-SIS (guide d'implémentation HL7 v2,
version 0.2.0 : <https://interop.esante.gouv.fr/ig/hl7v2/teleradiologie/>) décrit les échanges entre la structure d'accueil et le
téléradiologue. CdaCrImg produit le compte rendu CDA côté plateforme de téléradiologie ; identifiants
partagés, transmission du CR et écarts : [teleradiologie.md](teleradiologie.md).

Exemple généré et validé (XSD + trois profils transverses) :
[`ExemplesCDA/CdaCrImg_IMG-CR-IMG_2024.01_CDA-R2-Niveau-1_teleradiologie.xml`](../../ExemplesCDA/CdaCrImg_IMG-CR-IMG_2024.01_CDA-R2-Niveau-1_teleradiologie.xml)
(`SampleReports.Teleradiologie`, test `ExampleFileTests.GeneratesTeleradiologyExampleInExemplesCda`).
La colonne « Source de la donnée » de [`champs.xlsx`](champs.xlsx) indique, champ par champ, qui
fournit la donnée en téléradiologie.

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
6. **Validation intégrée** : la librairie ne peut pas embarquer Saxon/XSLT2 ; `CrImgValidator`
   réimplémente en C# les règles utiles, en trois familles : complétude (`CrImgValidator.cs`), formats
   et cohérence (`CrImgValidator.Formats.cs` : uid, cs, idNat, télécoms, adresses, INS, COG, dates, unicité des examens) et terminologies
   (`CrImgValidator.Terminologies.cs`). Chaque non-conformité porte le chemin de la propriété
   (`ModelPaths` énumère identifiants, codes, télécoms, adresses, professionnels). La validation schématron
   officielle reste externe (tests `Category=Schematron`, `tools/validate-cda.sh`).
7. **Jeux de valeurs embarqués** (`CdaCrImg.Terminologies`) : les JDV du CI-SIS contrôlés par la
   structuration minimale et ceux du volet sont des ressources liées aux fichiers du kit ANS (source
   unique), chargées à la demande. `JeuDeValeurs.Charger` lit aussi un JDV fourni par l'appelant (SVS
   ou ART-DECOR), par exemple le JDV LOINC des actes (1,5 Mo, non embarqué).
8. **Référentiel des champs** (`CdaCrImg.Referentiel`) : `ReferentielChamps.Champs` décrit chaque champ
   du document (`ChampCda`) : chemin de la propriété CdaCrImg (`Auteurs[].Professionnel.Id`, convention de
   `ValidationIssue.Path` sans index), section, libellé, description, XPath 1.0 relatif au champ parent
   (préfixes `cda` et `ps3-20`, `CreerResolveurEspaces`), cardinalité, source de la règle, origine de la
   valeur (appelant, valeur par défaut, valeur fixe de la librairie) et caractère **obligatoire /
   facultatif / obligatoire si le groupe est renseigné** (déduit de la cardinalité et des champs parents
   facultatifs). C'est la source unique du caractère obligatoire ; `ReferentielChampsTests` prouve qu'il
   décrit fidèlement `CrImgValidator` (vider chaque champ sur un CR complet), le modèle (chaque chemin
   est une propriété), `CrImgWriter` et l'exemple ANS niveau 1 (XPath et cardinalités).
9. **Analyse d'un CDA existant** (`CdaCrImg.Analyse`) : `CdaAnalyseur.Analyser` parcourt le référentiel
   sur le document (chaque XPath évalué dans chaque occurrence de son parent) et renvoie `AnalyseCda` :
   une entrée par occurrence (chemin indexé, valeur affichable, emplacement, ligne) ou par champ absent,
   avec un statut (présent, obligatoire absent, facultatif absent, en trop, hors périmètre), et les
   éléments non gérés (le plus haut élément du document qui n'est ni un champ ni l'ancêtre d'un champ).
   Un document qui n'est pas un CDA ou pas un CR d'imagerie (code 18748-4) est refusé
   (`CdaAnalyseException`) ; un niveau 3 est analysé pour son en-tête (`CorpsStructure`). L'analyse ne
   reconstruit pas de modèle et ne remplace pas la validation XSD / schématron.

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

## Application de démonstration

`dotnet/demo/CdaCrImg.Demo` (ASP.NET Core Razor Pages, net10.0) : un formulaire généré depuis un
catalogue de champs (`Form/FormCatalog.cs` : libellé, obligatoire / facultatif / obligatoire si le groupe
est renseigné, description, valeur d'exemple, propriété CdaCrImg). Chaque champ du formulaire est
relié au référentiel des champs de la librairie (`FieldDefinition.Champ`) : l'élément CDA et la
cardinalité sont affichés, et un champ affiché obligatoire doit l'être dans le référentiel.

**Analyse d'un CDA** (`Pages/Analyser`, `/Analyser`) : un fichier déposé ou un exemple de `Resources/`
(fichier d'origine, pour des numéros de ligne exacts) est analysé par `CdaAnalyseur` ; la page affiche
le récapitulatif (complet ou anomalies, compteurs), un tableau par section du référentiel, filtrable
côté navigateur, et les éléments non gérés. Les deux pages se renvoient l'une à l'autre. À la soumission, `ReportFormMapper`
construit le `CompteRenduImagerie`, `CrImgValidator` le contrôle et `CrImgWriter` renvoie le CDA XML
(affiché ou téléchargé). Les listes déroulantes viennent des jeux de valeurs embarqués dans la
librairie (`JeuxDeValeursCisis`). Sans fichier transmis, le PDF d'exemple
de l'ANS (`wwwroot/exemple-cr.pdf`) est encapsulé.

**Exemples de CDA** : chaque fichier `Resources/*.xml` de la démo (ressource embarquée) est proposé dans
une liste « Pré-remplir avec un exemple » (`/?exemple=<nom du fichier>`). `Form/ExemplesCda.cs` relit
l'en-tête du CDA (en-tête limité aux champs du formulaire : RPPS et FINESS sans leur préfixe d'identifiant
national, horodatages convertis en heure de Paris, médecin demandeur = `participant` `REF`) et le PDF
encapsulé, utilisé si aucun fichier n'est transmis. Le CDA de l'exemple sélectionné peut être téléchargé sans ses
commentaires (`/?exemple=<nom>&handler=Cda`, XML ré-indenté, attributs triés dans l'ordre d'écriture de
la librairie) pour le comparer ligne à ligne au CDA généré ; le fichier
d'exemple n'est jamais modifié. Ajouter un exemple = déposer son CDA dans
`Resources/` ; `DemoExemplesTests` vérifie que chaque exemple ne remplit que des champs connus avec des
valeurs sélectionnables, et que le formulaire ainsi pré-rempli produit un CDA valide XSD et conforme aux
trois profils transverses. Cette relecture, qui reconstruit les valeurs du formulaire, est propre à la
démo ; la librairie n'analyse un CDA que pour en lister les champs (`CdaAnalyseur`).

## Organisation du code

```
dotnet/
├── CdaCrImg.sln
├── Directory.Build.props           # LangVersion latest, Nullable, TreatWarningsAsErrors
├── src/CdaCrImg/                   # netstandard2.0, zéro dépendance
│   ├── CdaNamespaces.cs  CodeSystems.cs  Codes.cs  TemplateIds.cs
│   ├── Model/            (POCO métier + Hl7/ : types HL7)
│   ├── Referentiel/      (ReferentielChamps, ChampCda : emplacement, cardinalité, obligation de chaque champ)
│   ├── Analyse/          (CdaAnalyseur, AnalyseCda : champs présents / absents / non gérés d'un CDA)
│   ├── Serialization/    (CrImgWriter, CdaXml, Hl7Format)
│   ├── Terminologies/    (JeuDeValeurs, JeuxDeValeursCisis + JDV du kit ANS en ressources)
│   └── Validation/       (CrImgValidator [complétude, formats, terminologies], options, ModelPaths)
├── demo/CdaCrImg.Demo/            # application web de démonstration (formulaire → CDA)
└── tests/
    ├── CdaCrImg.Testing/           # utilitaires partagés (pas de tests) : RepoPaths, SampleReports,
    │                               #   CdaXsdValidator (XSD .NET), AnsJavaValidator (XSD Java + schématrons)
    ├── CdaCrImg.Tests/             # tests de la librairie (sans dépendance à la démo) : writer, validateur,
    │                               #   formats, terminologies, schématrons, non-régression, exemples générés
    └── CdaCrImg.Demo.Tests/        # tests de la démo : catalogue du formulaire vs CrImgValidator et le
                                    #   référentiel des champs, tests HTTP
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

## Diffusion

- Paquet NuGet `CdaCrImg` : `dotnet pack dotnet/src/CdaCrImg -c Release` (README, documentation XML,
  SourceLink, symboles `.snupkg`, aucune dépendance).
- CI : `.github/workflows/cdacrimg.yml` (Linux et Windows) : build, tests (dont validation XSD Java et
  schématrons ANS), pack, artefacts de tests et du paquet.
