# CdaCrImg

Librairie **.NET Standard 2.0 sans dépendance** qui produit un **compte rendu d'imagerie médicale** au
format **HL7 CDA R2 non structuré (niveau 1)** : le CR au format PDF est encapsulé dans un document
dont l'en-tête est conforme au volet **CI-SIS IMG-CR-IMG 2024.01** de l'ANS, pour des patients
identifiés par leur **INS**.

Elle est conçue pour la **téléradiologie** : l'examen est réalisé dans la structure d'imagerie qui
accueille le patient et interprété à distance par le téléradiologue.

Les documents produits sont valides au regard du schéma XSD CDA R2 et des schématrons transverses du
CI-SIS (structuration minimale, modèles de contenus, IHE) du kit ANS TestContenuCDA.

## Utilisation

```csharp
using CdaCrImg;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;
using CdaCrImg.Serialization;
using CdaCrImg.Validation;

var cr = new CompteRenduImagerie
{
    Id = new Identifier("1.2.250.1.213.1.1.1.45.2024.2.1"),
    SetId = new Identifier("1.2.250.1.213.1.1.1.45.2024.2"),
    Titre = "CR d'imagerie médicale - Scanner thoracique",
    DateCreation = DateTimeOffset.Now,
    Patient = patient,                                   // INS + traits d'identité
    Custodian = centreImagerie,
    SignataireLegal = new Signature(radiologue, date),
    PriseEnCharge = priseEnCharge,
    Corps = new CorpsPdf(File.ReadAllBytes("cr.pdf")),
};
cr.Auteurs.Add(new Auteur(radiologue, date));
cr.Demandes.Add(new DemandeImagerie(numeroDemande, accessionNumber));
cr.Actes.Add(acte);                                      // Study UID, LOINC, modalité, région, exécutant

IReadOnlyList<ValidationIssue> issues = CrImgValidator.Validate(cr); // vide si conforme
string xml = CrImgWriter.WriteToString(cr);                         // lève CrImgValidationException sinon
```

## Téléradiologie

Selon les spécifications techniques du volet (STD 3.2), le CR a pour auteurs le téléradiologue **et** le
médecin responsable de la structure d'imagerie qui accueille le patient ; ce dernier est le responsable
du document :

```csharp
cr.Auteurs.Add(new Auteur(teleradiologue, dateRedaction));
cr.Auteurs.Add(new Auteur(responsableStructureAccueil, dateRedaction));
cr.SignataireLegal = new Signature(responsableStructureAccueil, dateSignature);
acte.Executant = teleradiologue;   // imageur qui supervise et interprète l'acte
```

L'identité du patient (INS), l'Accession Number, le Study Instance UID et le lieu de prise en charge
proviennent de la structure d'accueil.

## Contrôles

`CrImgValidator` vérifie, avant toute écriture :

- la présence des données obligatoires (en-tête, traits INS, acte, PDF…) ;
- les formats contrôlés par la structuration minimale (identifiants OID/UUID, codes, télécoms, matricule
  INS, code COG…) et la cohérence des dates ;
- l'appartenance des codes aux jeux de valeurs du CI-SIS embarqués (`JeuxDeValeursCisis` : professions,
  secteurs d'activité, cadres d'exercice, civilités, modalités, régions anatomiques…). Le jeu des codes
  LOINC d'actes peut être fourni via `CrImgValidationOptions.ActesImagerie`.

Chaque non-conformité indique le chemin de la propriété concernée (ex. `Actes[0].Modalites`).

## Référentiel des champs

`ReferentielChamps.Champs` (`CdaCrImg.Referentiel`) liste chaque champ du compte rendu avec son
emplacement dans le CDA (XPath), sa cardinalité et son caractère obligatoire ou facultatif :

```csharp
foreach (var champ in ReferentielChamps.Champs)
    Console.WriteLine($"{champ.Chemin} {champ.Cardinalite} {champ.MentionObligation} : {champ.XPathComplet}");
```

## Analyse d'un CDA existant

`CdaAnalyseur.Analyser` (`CdaCrImg.Analyse`) liste les champs d'un compte rendu d'imagerie CDA : valeur
de chaque champ présent, champs absents avec leur caractère obligatoire ou facultatif, éléments que la
librairie ne gère pas.

```csharp
AnalyseCda analyse = CdaAnalyseur.Analyser("cr.xml");   // ou un Stream, ou un XDocument
foreach (var champ in analyse.Champs)
    Console.WriteLine($"{champ.Chemin} [{champ.Champ.MentionObligation}] {champ.Statut} {champ.Valeur}");
bool complet = analyse.EstComplet;                       // aucun champ obligatoire absent
```

Documentation complète, liste des champs obligatoires et application de démonstration :
<https://github.com/tmarignier/ans-cda>.
