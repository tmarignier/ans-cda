using static CdaCrImg.Demo.Form.Requirement;

namespace CdaCrImg.Demo.Form;

/// <summary>
/// Catalogue des champs du formulaire : un champ par donnée gérée par la librairie CdaCrImg, avec son
/// caractère obligatoire, sa description et une valeur d'exemple (reprise de l'exemple ANS niveau 1).
/// Le caractère obligatoire est vérifié par les tests contre <c>CrImgValidator</c>.
/// Voir docs/cr-img/champs-obligatoires.md.
/// </summary>
public static class FormCatalog
{
    public const string Document = "Document";
    public const string Patient = "Patient (identifié par son INS)";
    public const string Auteur = "Auteur du CR (radiologue)";
    public const string Custodian = "Structure de conservation du document";
    public const string Signataire = "Signataire légal";
    public const string Demandeur = "Médecin demandeur";
    public const string Demande = "Demande d'examen";
    public const string Acte = "Acte d'imagerie";
    public const string PriseEnCharge = "Prise en charge";
    public const string Corps = "Compte rendu (PDF)";

    private static readonly IReadOnlyList<Option> TypesIns = new Option[]
    {
        new(IdentifierRoots.InsNir, "", "INS-NIR (matricule NIR)"),
        new(IdentifierRoots.InsNia, "", "INS-NIA (identifiant d'attente)"),
        new(IdentifierRoots.InsNirTest, "", "INS-NIR de test"),
        new(IdentifierRoots.InsNiaTest, "", "INS-NIA de test"),
    };

    private static readonly IReadOnlyList<Option> Sexes = new Option[]
    {
        new("F", CodeSystems.Hl7AdministrativeGender, "Féminin"),
        new("M", CodeSystems.Hl7AdministrativeGender, "Masculin"),
    };

    /// <summary>Sections dans l'ordre d'affichage.</summary>
    public static IReadOnlyList<string> Sections { get; } = new[]
    {
        Document, Patient, Auteur, Custodian, Signataire, Demandeur, Demande, Acte, PriseEnCharge, Corps,
    };

    /// <summary>Tous les champs, dans l'ordre d'affichage.</summary>
    public static IReadOnlyList<FieldDefinition> Fields { get; } = new FieldDefinition[]
    {
        // Document
        new("document.id", Document, "Identifiant du document", Required,
            "Identifiant unique de cette version du document (OID ou UUID) : ClinicalDocument/id. Généré par le logiciel du radiologue.",
            "1.2.250.1.213.1.1.1.45.2024.2.1", "Id"),
        new("document.setId", Document, "Identifiant du lot de versions", Required,
            "Identifiant commun à toutes les versions du document (setId) ; égal à l'identifiant du document pour la première version.",
            "1.2.250.1.213.1.1.1.45.2024.2", "SetId"),
        new("document.version", Document, "Numéro de version", Required,
            "Numéro de version du document (versionNumber), entier supérieur ou égal à 1.",
            "2", "NumeroVersion", FieldKind.Number),
        new("document.remplace", Document, "Identifiant du document remplacé", Optional,
            "Pour une nouvelle version : identifiant du document qu'elle remplace (relatedDocument typeCode=\"RPLC\").",
            "90E1C8EC-F951-4B26-A305-A34848818DD6", "DocumentRemplace"),
        new("document.titre", Document, "Titre", Required,
            "Titre du compte rendu (title).",
            "CR d'imagerie médicale - Scanner Tête + Cou avec injection", "Titre"),
        new("document.dateCreation", Document, "Date de création", Required,
            "Date et heure de création du document (effectiveTime), heure de Paris.",
            "2021-01-08T11:17", "DateCreation", FieldKind.DateTime),
        new("document.confidentialite", Document, "Confidentialité", Optional,
            "Niveau de confidentialité (confidentialityCode) ; « Normal » si non renseigné.",
            "N", "Confidentialite", FieldKind.Select, JeuxDeValeurs.Confidentialite),
        new("document.langue", Document, "Langue", Optional,
            "Langue du document (languageCode) ; fr-FR si non renseignée.",
            "fr-FR", "Langue"),

        // Patient
        new("patient.insType", Patient, "Type d'INS", Required,
            "Nature du matricule INS (root de patientRole/id). La librairie ne gère que des patients identifiés par leur INS.",
            IdentifierRoots.InsNirTest, "Patient.Ins", FieldKind.Select, TypesIns),
        new("patient.insMatricule", Patient, "Matricule INS", Required,
            "Matricule INS du patient (extension de patientRole/id), obtenu par le téléservice INSi.",
            "279035121518989", "Patient.Ins"),
        new("patient.nomNaissance", Patient, "Nom de naissance", Required,
            "Trait INS : nom de l'acte de naissance (name/family qualifier=\"BR\").",
            "PAT-TROIS", "Patient.NomNaissance"),
        new("patient.prenomsNaissance", Patient, "Prénoms de naissance", Required,
            "Trait INS : liste des prénoms de l'acte de naissance, séparés par des espaces (name/given).",
            "DOMINIQUE MARIE-LOUISE", "Patient.PrenomsNaissance"),
        new("patient.premierPrenom", Patient, "Premier prénom de naissance", Required,
            "Trait INS : premier prénom de l'acte de naissance (name/given qualifier=\"BR\").",
            "DOMINIQUE", "Patient.PremierPrenomNaissance"),
        new("patient.sexe", Patient, "Sexe", Required,
            "Trait INS : sexe administratif (administrativeGenderCode).",
            "F", "Patient.Sexe", FieldKind.Select, Sexes),
        new("patient.dateNaissance", Patient, "Date de naissance", Required,
            "Trait INS : date de naissance (birthTime).",
            "1979-03-28", "Patient.DateNaissance", FieldKind.Date),
        new("patient.lieuNaissanceCog", Patient, "Lieu de naissance (code COG)", Required,
            "Trait INS : code officiel géographique INSEE de la commune de naissance (birthplace/place/addr/county) ; 99xxx pour l'étranger.",
            "51215", "Patient.LieuNaissanceCog"),
        new("patient.lieuNaissanceCommune", Patient, "Commune de naissance", Optional,
            "Libellé de la commune de naissance (birthplace/place/addr/city).",
            "DOMPREMY", "Patient.LieuNaissanceCommune"),
        new("patient.nomUtilise", Patient, "Nom utilisé", Optional,
            "Nom utilisé au quotidien (name/family qualifier=\"CL\").",
            "PAT-TROIS", "Patient.NomUtilise"),
        new("patient.prenomUtilise", Patient, "Prénom utilisé", Optional,
            "Prénom utilisé au quotidien (name/given qualifier=\"CL\").",
            "DOMINIQUE", "Patient.PrenomUtilise"),
        new("patient.ippRoot", Patient, "IPP : OID de l'établissement", RequiredIfGroup,
            "Identifiant permanent du patient dans l'établissement : OID de l'autorité d'affectation (root).",
            "1.2.3.4.567.8.9.10", "Patient.AutresIdentifiants", Group: "patient.ipp", GroupLabel: "IPP"),
        new("patient.ippValeur", Patient, "IPP : valeur", Optional,
            "Valeur de l'IPP (extension).",
            "1234567890121", "Patient.AutresIdentifiants", Group: "patient.ipp", GroupLabel: "IPP"),
        new("patient.adresseNumero", Patient, "Adresse : numéro", Optional,
            "Numéro dans la voie (addr/houseNumber).",
            "28", "Patient.Adresses", Group: "patient.adresse", GroupLabel: "Adresse"),
        new("patient.adresseVoie", Patient, "Adresse : voie", Optional,
            "Nom de la voie (addr/streetName).",
            "Avenue de Breteuil", "Patient.Adresses", Group: "patient.adresse", GroupLabel: "Adresse"),
        new("patient.adresseCodePostal", Patient, "Adresse : code postal", Optional,
            "Code postal (addr/postalCode).",
            "75007", "Patient.Adresses", Group: "patient.adresse", GroupLabel: "Adresse"),
        new("patient.adresseVille", Patient, "Adresse : commune", Optional,
            "Commune (addr/city).",
            "PARIS", "Patient.Adresses", Group: "patient.adresse", GroupLabel: "Adresse"),
        new("patient.telephone", Patient, "Téléphone", Optional,
            "Téléphone du domicile (telecom tel:, use=\"H\").",
            "0144534551", "Patient.Telecoms"),

        // Auteur
        new("auteur.horodatage", Auteur, "Date de rédaction", Required,
            "Date et heure de rédaction / validation du CR par l'auteur (author/time).",
            "2021-01-08T11:17", "Auteurs[0].Horodatage", FieldKind.DateTime),
        new("auteur.rpps", Auteur, "N° RPPS", Required,
            "Identifiant RPPS du radiologue (11 chiffres) ; la librairie produit l'identifiant national 8 + RPPS (assignedAuthor/id).",
            "01234560801", "Auteurs[0].Professionnel.Id"),
        new("auteur.professionCode", Auteur, "Profession / spécialité : code", Required,
            "Code profession / savoir-faire du PS (TRE_G15/R85, assignedAuthor/code), ex. G15_10/SM44 radio-diagnostic.",
            "G15_10/SM44", "Auteurs[0].Professionnel.Profession"),
        new("auteur.professionLibelle", Auteur, "Profession / spécialité : libellé", Required,
            "Libellé de la profession / spécialité (displayName), exigé par la structuration minimale.",
            "Médecin - Radio-diagnostic (SM)", "Auteurs[0].Professionnel.Profession"),
        new("auteur.fonction", Auteur, "Rôle fonctionnel", Optional,
            "Rôle fonctionnel de l'auteur (author/functionCode, JDV_J47).",
            "ATTPHYS", "Auteurs[0].Fonction", FieldKind.Select, JeuxDeValeurs.FonctionAuteur),
        new("auteur.civilite", Auteur, "Civilité", Optional,
            "Civilité (name/prefix, JDV_J245).",
            "M", "Auteurs[0].Professionnel.Nom", FieldKind.Select, JeuxDeValeurs.Civilite, "auteur.identite", "Identité de l'auteur"),
        new("auteur.prenom", Auteur, "Prénom", Optional,
            "Prénom (name/given).",
            "Jacques", "Auteurs[0].Professionnel.Nom", Group: "auteur.identite", GroupLabel: "Identité de l'auteur"),
        new("auteur.nom", Auteur, "Nom", RequiredIfGroup,
            "Nom de famille (name/family), exigé par la structuration minimale dès que l'identité est renseignée.",
            "BIDEAULT", "Auteurs[0].Professionnel.Nom", Group: "auteur.identite", GroupLabel: "Identité de l'auteur"),
        new("auteur.titre", Auteur, "Titre", Optional,
            "Titre (name/suffix, JDV_J246).",
            "DR", "Auteurs[0].Professionnel.Nom", FieldKind.Select, JeuxDeValeurs.Titre, "auteur.identite", "Identité de l'auteur"),
        new("auteur.telephone", Auteur, "Téléphone professionnel", Optional,
            "Téléphone professionnel (telecom tel:, use=\"WP\").",
            "0146000000", "Auteurs[0].Professionnel.Telecoms"),
        new("auteur.orgFiness", Auteur, "Structure : n° FINESS", Required,
            "FINESS de la structure d'exercice de l'auteur, obligatoire pour le CR d'imagerie (representedOrganization/id = 1 + FINESS).",
            "920008059", "Auteurs[0].Professionnel.Organisation.Id"),
        new("auteur.orgNom", Auteur, "Structure : nom", Optional,
            "Raison sociale de la structure (representedOrganization/name).",
            "Centre de radiologie Ambroise", "Auteurs[0].Professionnel.Organisation.Nom"),
        new("auteur.orgSecteur", Auteur, "Structure : secteur d'activité", Optional,
            "Secteur d'activité de la structure (standardIndustryClassCode, JDV_J04).",
            "AMBULATOIRE", "Auteurs[0].Professionnel.Organisation.SecteurActivite", FieldKind.Select, JeuxDeValeurs.SecteurActivite),

        // Custodian
        new("custodian.finess", Custodian, "N° FINESS", Required,
            "FINESS de la structure chargée de la conservation du document (custodian/.../representedCustodianOrganization/id).",
            "920008059", "Custodian.Id"),
        new("custodian.nom", Custodian, "Nom", Optional,
            "Raison sociale de la structure de conservation.",
            "Centre de radiologie Ambroise", "Custodian.Nom"),

        // Signataire
        new("signataire.horodatage", Signataire, "Date de signature", Required,
            "Date et heure de signature du document (legalAuthenticator/time).",
            "2021-01-08T11:17", "SignataireLegal.Horodatage", FieldKind.DateTime),
        new("signataire.rpps", Signataire, "N° RPPS", Required,
            "RPPS du responsable du document (legalAuthenticator/assignedEntity/id) ; en téléradiologie, médecin responsable de la structure qui accueille le patient.",
            "01234560801", "SignataireLegal.Professionnel.Id"),
        new("signataire.professionCode", Signataire, "Profession / spécialité : code", Optional,
            "Code profession / savoir-faire du signataire (TRE_G15/R85).",
            "G15_10/SM44", "SignataireLegal.Professionnel.Profession"),
        new("signataire.civilite", Signataire, "Civilité", Optional,
            "Civilité (JDV_J245).",
            "M", "SignataireLegal.Professionnel.Nom", FieldKind.Select, JeuxDeValeurs.Civilite, "signataire.identite", "Identité du signataire"),
        new("signataire.prenom", Signataire, "Prénom", Optional,
            "Prénom du signataire.",
            "Jacques", "SignataireLegal.Professionnel.Nom", Group: "signataire.identite", GroupLabel: "Identité du signataire"),
        new("signataire.nom", Signataire, "Nom", RequiredIfGroup,
            "Nom de famille du signataire, exigé dès que son identité est renseignée.",
            "BIDEAULT", "SignataireLegal.Professionnel.Nom", Group: "signataire.identite", GroupLabel: "Identité du signataire"),

        // Médecin demandeur
        new("demandeur.rpps", Demandeur, "N° RPPS", RequiredIfGroup,
            "RPPS du médecin qui a demandé l'examen (participant typeCode=\"REF\"/associatedEntity/id).",
            "01234567897", "MedecinsDemandeurs[0].Professionnel.Id", Group: "demandeur", GroupLabel: "Médecin demandeur"),
        new("demandeur.professionCode", Demandeur, "Profession / spécialité : code", RequiredIfGroup,
            "Code profession / savoir-faire du médecin demandeur (TRE_G15/R85, associatedEntity/code).",
            "G15_10/SM26", "MedecinsDemandeurs[0].Professionnel.Profession", Group: "demandeur.profession", GroupLabel: "Profession du médecin demandeur"),
        new("demandeur.professionLibelle", Demandeur, "Profession / spécialité : libellé", RequiredIfGroup,
            "Libellé de la profession (displayName), exigé par la structuration minimale dès que le code est renseigné.",
            "Médecin - Qualifié en Médecine Générale (SM)", "MedecinsDemandeurs[0].Professionnel.Profession", Group: "demandeur.profession", GroupLabel: "Profession du médecin demandeur"),
        new("demandeur.dateDemande", Demandeur, "Date de la demande", Optional,
            "Date de la demande d'examen (participant/time/high) ; inconnue si non renseignée.",
            "2021-01-05T09:30", "MedecinsDemandeurs[0].DateDemande", FieldKind.DateTime, Group: "demandeur", GroupLabel: "Médecin demandeur"),
        new("demandeur.civilite", Demandeur, "Civilité", Optional,
            "Civilité (JDV_J245).",
            "M", "MedecinsDemandeurs[0].Professionnel.Nom", FieldKind.Select, JeuxDeValeurs.Civilite, "demandeur.identite", "Identité du médecin demandeur"),
        new("demandeur.prenom", Demandeur, "Prénom", Optional,
            "Prénom du médecin demandeur.",
            "Stéphane", "MedecinsDemandeurs[0].Professionnel.Nom", Group: "demandeur.identite", GroupLabel: "Identité du médecin demandeur"),
        new("demandeur.nom", Demandeur, "Nom", RequiredIfGroup,
            "Nom de famille du médecin demandeur, exigé dès que son identité est renseignée.",
            "MEDIONI", "MedecinsDemandeurs[0].Professionnel.Nom", Group: "demandeur.identite", GroupLabel: "Identité du médecin demandeur"),

        // Demande
        new("demande.numeroRoot", Demande, "N° de demande : OID du demandeur", RequiredIfGroup,
            "Order Placer Number attribué par le demandeur : OID (inFulfillmentOf/order/id). Laisser le groupe vide s'il n'y a pas de demande dématérialisée : le document portera nullFlavor=\"UNK\".",
            "1.2.250.1.748.12345678.12", "Demandes[0].NumeroDemande", Group: "demande.numero", GroupLabel: "N° de demande"),
        new("demande.numeroValeur", Demande, "N° de demande : valeur", Optional,
            "Valeur du numéro de demande (extension).",
            "984375862", "Demandes[0].NumeroDemande", Group: "demande.numero", GroupLabel: "N° de demande"),
        new("demande.accessionRoot", Demande, "Accession Number : OID du RIS", Required,
            "Accession Number attribué par le RIS : OID (ps3-20:accessionNumber/@root).",
            "1.2.250.1.925.994044785528.27", "Demandes[0].AccessionNumber"),
        new("demande.accessionValeur", Demande, "Accession Number : valeur", Required,
            "Valeur de l'Accession Number attribuée par le RIS (ps3-20:accessionNumber/@extension).",
            "105234751", "Demandes[0].AccessionNumber"),

        // Acte
        new("acte.studyUid", Acte, "Study Instance UID", Required,
            "Identifiant DICOM de l'examen attribué par le RIS (serviceEvent/id, root seul) : chiffres et points, 64 caractères max.",
            "1.2.250.1.925.994044.27.123.1876360", "Actes[0].StudyInstanceUid"),
        new("acte.loincCode", Acte, "Acte : code LOINC", Required,
            "Code LOINC de l'acte réalisé (serviceEvent/code), jdv-code-document-imagerie-cisis ; reporté en translation du code du document.",
            "24727-0", "Actes[0].Code"),
        new("acte.loincLibelle", Acte, "Acte : libellé", Optional,
            "Libellé de l'acte LOINC (displayName).",
            "CT tête avec contraste IV", "Actes[0].Code"),
        new("acte.ccamCode", Acte, "Acte CCAM : code", RequiredIfGroup,
            "Code CCAM de l'acte (translation CCAM du serviceEvent/code).",
            "ACQH004", "Actes[0].CodeCcam", Group: "acte.ccam", GroupLabel: "Acte CCAM"),
        new("acte.ccamLibelle", Acte, "Acte CCAM : libellé", Optional,
            "Libellé CCAM de l'acte.",
            "Scanographie du crâne, de son contenu et du tronc, avec injection intraveineuse de produit de contraste",
            "Actes[0].CodeCcam", Group: "acte.ccam", GroupLabel: "Acte CCAM"),
        new("acte.modalite", Acte, "Modalité d'acquisition", Required,
            "Modalité DICOM de l'acte (translation DCM avec qualifier 121139), jdv-modalite-acquisition-cisis.",
            "CT", "Actes[0].Modalites", FieldKind.Select, JeuxDeValeurs.ModaliteAcquisition),
        new("acte.region", Acte, "Région anatomique", Required,
            "Région anatomique examinée (translation SNOMED CT avec qualifier 39111-0), jdv-region-anatomique-cisis.",
            "774007", "Actes[0].RegionsAnatomiques", FieldKind.Select, JeuxDeValeurs.RegionAnatomique),
        new("acte.debut", Acte, "Début de l'acte", Required,
            "Date et heure de début de réalisation (serviceEvent/effectiveTime/low).",
            "2021-01-08T10:25", "Actes[0].Debut", FieldKind.DateTime),
        new("acte.fin", Acte, "Fin de l'acte", Optional,
            "Date et heure de fin de réalisation (serviceEvent/effectiveTime/high).",
            "2021-01-08T11:17", "Actes[0].Fin", FieldKind.DateTime),
        new("acte.executantRpps", Acte, "Exécutant : n° RPPS", Required,
            "RPPS du radiologue exécutant (serviceEvent/performer/assignedEntity/id).",
            "01234560801", "Actes[0].Executant.Id"),
        new("acte.executantProfessionCode", Acte, "Exécutant : profession / spécialité", Optional,
            "Code profession / savoir-faire de l'exécutant (TRE_G15/R85).",
            "G15_10/SM44", "Actes[0].Executant.Profession"),
        new("acte.executantFiness", Acte, "Exécutant : FINESS de la structure", Required,
            "FINESS de l'établissement de rattachement de l'exécutant (representedOrganization/id, extension FR pour la DRIM-Box).",
            "920008059", "Actes[0].Executant.Organisation.Id"),
        new("acte.executantOrgNom", Acte, "Exécutant : nom de la structure", Optional,
            "Raison sociale de l'établissement de l'exécutant.",
            "Centre de radiologie Ambroise", "Actes[0].Executant.Organisation.Nom"),
        new("acte.executantSecteur", Acte, "Exécutant : secteur d'activité", Required,
            "Secteur d'activité de l'établissement de l'exécutant (standardIndustryClassCode, JDV_J04), exigé par la structuration minimale.",
            "AMBULATOIRE", "Actes[0].Executant.Organisation.SecteurActivite", FieldKind.Select, JeuxDeValeurs.SecteurActivite),
        new("acte.depistage", Acte, "Examen de dépistage", Optional,
            "Cocher si l'examen est réalisé dans le cadre d'un dépistage (documentationOf CIM-10 Z13.9).",
            "false", "Depistage", FieldKind.Checkbox),

        // Prise en charge
        new("pec.modalite", PriseEnCharge, "Type de prise en charge", Optional,
            "Modalité de la prise en charge (encompassingEncounter/code, JDV_J142).",
            "AMB", "PriseEnCharge.Modalite", FieldKind.Select, JeuxDeValeurs.TypeRencontre),
        new("pec.debut", PriseEnCharge, "Début de la prise en charge", Required,
            "Date et heure de début de la prise en charge (encompassingEncounter/effectiveTime/low).",
            "2021-01-08T10:25", "PriseEnCharge.Debut", FieldKind.DateTime),
        new("pec.fin", PriseEnCharge, "Fin de la prise en charge", Optional,
            "Date et heure de fin de la prise en charge (effectiveTime/high).",
            "2021-01-08T11:17", "PriseEnCharge.Fin", FieldKind.DateTime),
        new("pec.cadreExercice", PriseEnCharge, "Cadre d'exercice", Required,
            "Type de lieu de la prise en charge (healthCareFacility/code, JDV_J02).",
            "SA08", "PriseEnCharge.Lieu.CadreExercice", FieldKind.Select, JeuxDeValeurs.CadreExercice),
        new("pec.lieuFiness", PriseEnCharge, "Lieu : n° FINESS", Optional,
            "FINESS du lieu de prise en charge (healthCareFacility/id).",
            "920008059", "PriseEnCharge.Lieu.Id"),
        new("pec.lieuNom", PriseEnCharge, "Lieu : nom", Optional,
            "Nom du lieu de prise en charge (healthCareFacility/location/name).",
            "Centre de radiologie Ambroise", "PriseEnCharge.Lieu.Nom"),

        // Corps
        new("corps.pdf", Corps, "Compte rendu au format PDF", Required,
            "Le compte rendu rédigé par le radiologue, encapsulé en base64 (nonXMLBody/text, application/pdf). Si aucun fichier n'est choisi, le PDF d'exemple est utilisé.",
            "exemple-cr.pdf", "Corps", FieldKind.File),
    };

    /// <summary>Valeurs d'exemple de tous les champs.</summary>
    public static Dictionary<string, string?> Examples() =>
        Fields.Where(f => f.Kind != FieldKind.File).ToDictionary(f => f.Key, f => (string?)f.Example);
}
