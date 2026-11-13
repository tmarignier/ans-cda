using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace CdaCrImg.Referentiel
{
    /// <summary>
    /// Référentiel des champs d'un compte rendu d'imagerie CDA R2 niveau 1 (IMG-CR-IMG 2024.01) : source
    /// unique de l'emplacement, de la cardinalité et du caractère obligatoire ou facultatif de chaque donnée
    /// produite par la librairie. Les tests vérifient qu'il décrit fidèlement <c>CrImgValidator</c> et
    /// <c>CrImgWriter</c>. Voir docs/cr-img/champs-obligatoires.md.
    /// </summary>
    public static class ReferentielChamps
    {
        /// <summary>Préfixe XPath de l'espace de noms HL7 v3.</summary>
        public const string PrefixeCda = "cda";

        /// <summary>Préfixe XPath de l'espace de noms DICOM PS3.20.</summary>
        public const string PrefixeDicom = "ps3-20";

        /// <summary>Noms des sections du compte rendu.</summary>
        public static class Sections
        {
            /// <summary>Identification et métadonnées du document.</summary>
            public const string Document = "Document";

            /// <summary>Patient (recordTarget).</summary>
            public const string Patient = "Patient";

            /// <summary>Auteur(s) du compte rendu (author).</summary>
            public const string Auteur = "Auteur";

            /// <summary>Structure de conservation du document (custodian).</summary>
            public const string Custodian = "Structure de conservation";

            /// <summary>Signataire légal (legalAuthenticator).</summary>
            public const string Signataire = "Signataire légal";

            /// <summary>Médecin(s) demandeur(s) (participant REF).</summary>
            public const string Demandeur = "Médecin demandeur";

            /// <summary>Demande(s) d'examen (inFulfillmentOf).</summary>
            public const string Demande = "Demande d'examen";

            /// <summary>Acte(s) d'imagerie (documentationOf).</summary>
            public const string Acte = "Acte d'imagerie";

            /// <summary>Prise en charge (componentOf).</summary>
            public const string PriseEnCharge = "Prise en charge";

            /// <summary>Corps du document (PDF encapsulé).</summary>
            public const string Corps = "Corps";

            /// <summary>Toutes les sections, dans l'ordre du document.</summary>
            public static IReadOnlyList<string> Toutes { get; } = new[]
            {
                Document, Patient, Auteur, Custodian, Signataire, Demandeur, Demande, Acte, PriseEnCharge, Corps,
            };
        }

        private static readonly Lazy<IReadOnlyList<ChampCda>> Liste = new Lazy<IReadOnlyList<ChampCda>>(Construire);

        /// <summary>Tous les champs, dans l'ordre du document (un parent précède ses enfants).</summary>
        public static IReadOnlyList<ChampCda> Champs => Liste.Value;

        /// <summary>Champ de chemin <paramref name="chemin"/> (ex. <c>Auteurs[].Horodatage</c>), ou <c>null</c>.</summary>
        public static ChampCda? Trouver(string chemin) => Champs.FirstOrDefault(c => c.Chemin == chemin);

        /// <summary>
        /// Champ décrivant la propriété d'un chemin de validation (ex. <c>Auteurs[0].Professionnel.Id</c> ou
        /// <c>Auteurs</c>, tel que <c>ValidationIssue.Path</c>) : les index sont ignorés ; <c>null</c> si aucun
        /// champ ne correspond.
        /// </summary>
        public static ChampCda? TrouverParChemin(string cheminModele)
        {
            if (cheminModele == null) throw new ArgumentNullException(nameof(cheminModele));
            var chemin = Index.Replace(cheminModele, "[]");
            return Trouver(chemin) ?? Trouver(chemin + "[]");
        }

        private static readonly Regex Index = new Regex(@"\[\d+\]", RegexOptions.CultureInvariant);

        /// <summary>Résolveur des préfixes <c>cda</c> et <c>ps3-20</c> utilisés par <see cref="ChampCda.XPath"/>.</summary>
        public static IXmlNamespaceResolver CreerResolveurEspaces()
        {
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace(PrefixeCda, CdaNamespaces.Hl7.NamespaceName);
            manager.AddNamespace(PrefixeDicom, CdaNamespaces.Ps320.NamespaceName);
            return manager;
        }

        private static IReadOnlyList<ChampCda> Construire()
        {
            var champs = new List<ChampCda>();
            const OrigineValeur Appelant = OrigineValeur.Appelant;
            const OrigineValeur ParDefaut = OrigineValeur.ValeurParDefaut;
            const OrigineValeur Librairie = OrigineValeur.Librairie;

            ChampCda C(string chemin, ChampCda? parent, string section, string libelle, string description, string xPath,
                int min, int? max, string source, OrigineValeur origine = Appelant, string? valeur = null)
            {
                var champ = new ChampCda(chemin, parent, section, libelle, description, xPath, min, max, source, origine, valeur);
                champs.Add(champ);
                return champ;
            }

            var insRoots = string.Join(" or ", new[]
            {
                IdentifierRoots.InsNir, IdentifierRoots.InsNia, IdentifierRoots.InsNirTest, IdentifierRoots.InsNiaTest,
            }.Select(r => $"@root='{r}'"));

            // Nom d'un professionnel (PN) : nom de famille obligatoire si l'identité est renseignée (SM).
            void Nom(string chemin, ChampCda parent, string section, string personne)
            {
                var nom = C(chemin, parent, section, "Identité", "Identité du professionnel.",
                    $"cda:{personne}/cda:name", 0, 1, "SM");
                C(chemin + ".Prefix", nom, section, "Civilité", "Civilité (JDV_J245).", "cda:prefix", 0, 1, "SM");
                C(chemin + ".Given", nom, section, "Prénom", "Prénom.", "cda:given", 0, 1, "SM");
                C(chemin + ".Family", nom, section, "Nom de famille", "Nom de famille, exigé dès que l'identité est renseignée.",
                    "cda:family", 1, 1, "SM");
                C(chemin + ".Suffix", nom, section, "Titre", "Titre (JDV_J246).", "cda:suffix", 0, 1, "SM");
            }

            // Organisation d'un professionnel (representedOrganization, scopingOrganization).
            void Organisation(string chemin, ChampCda parent, string section, string element, int min,
                bool idObligatoire, bool secteurObligatoire, bool libelleSecteurObligatoire, string libelle, string source)
            {
                var org = C(chemin, parent, section, libelle, "Structure d'exercice du professionnel.",
                    $"cda:{element}", min, 1, source);
                C(chemin + ".Id", org, section, "Identifiant de la structure",
                    "Identifiant national de la structure : 1 + FINESS ou 3 + SIRET (root 1.2.250.1.71.4.2.2).",
                    "cda:id", idObligatoire ? 1 : 0, 1, idObligatoire ? source : "SM");
                C(chemin + ".Nom", org, section, "Nom de la structure", "Raison sociale.", "cda:name", 0, 1, "SM");
                C(chemin + ".Telecoms", org, section, "Télécoms de la structure", "Coordonnées télécom.", "cda:telecom", 0, null, "SM");
                C(chemin + ".Adresses", org, section, "Adresses de la structure", "Adresses postales.", "cda:addr", 0, null, "SM");
                var secteur = C(chemin + ".SecteurActivite", org, section, "Secteur d'activité",
                    "Secteur d'activité de la structure (standardIndustryClassCode, JDV_J04).",
                    "cda:standardIndustryClassCode", secteurObligatoire ? 1 : 0, 1, "SM");
                if (libelleSecteurObligatoire)
                    C(chemin + ".SecteurActivite.DisplayName", secteur, section, "Libellé du secteur d'activité",
                        "Libellé (displayName) du secteur d'activité.", "@displayName", 1, 1, "SM");
            }

            // Professionnel de santé (assignedAuthor, assignedEntity, associatedEntity).
            void Professionnel(string chemin, ChampCda role, string section, string personne, string organisation,
                bool professionObligatoire, bool libelleProfessionObligatoire, int minOrganisation,
                bool idOrganisationObligatoire, bool secteurObligatoire, bool libelleSecteurObligatoire)
            {
                C(chemin + ".Id", role, section, "Identifiant national (RPPS)",
                    "Identifiant national du professionnel : 8 + n° RPPS (root 1.2.250.1.71.4.2.1).", "cda:id", 1, 1, "SM");
                var profession = C(chemin + ".Profession", role, section, "Profession / spécialité",
                    "Profession / savoir-faire (TRE_G15/R85, JDV_J01).", "cda:code", professionObligatoire ? 1 : 0, 1, "SM");
                if (libelleProfessionObligatoire)
                    C(chemin + ".Profession.DisplayName", profession, section, "Libellé de la profession",
                        "Libellé (displayName) de la profession / spécialité.", "@displayName", 1, 1, "SM");
                C(chemin + ".Adresses", role, section, "Adresses", "Adresses professionnelles.", "cda:addr", 0, null, "SM");
                C(chemin + ".Telecoms", role, section, "Télécoms", "Coordonnées télécom professionnelles.", "cda:telecom", 0, null, "SM");
                Nom(chemin + ".Nom", role, section, personne);
                Organisation(chemin + ".Organisation", role, section, organisation, minOrganisation,
                    idOrganisationObligatoire, secteurObligatoire, libelleSecteurObligatoire,
                    "Structure d'exercice", minOrganisation > 0 ? "STD" : "SM");
            }

            // Document
            const string doc = Sections.Document;
            C("RealmCode", null, doc, "Domaine", "Domaine d'application : FR.", "cda:realmCode", 1, 1, "SM", Librairie, "FR");
            C("TypeId", null, doc, "Type CDA", "Conformité CDA R2 : 2.16.840.1.113883.1.3 / POCD_HD000040.", "cda:typeId", 1, 1, "XSD", Librairie,
                "2.16.840.1.113883.1.3");
            C("TemplateIds", null, doc, "Modèles de conformité",
                "templateId HL7 France, CI-SIS et document non structuré (IHE XDS-SD).", "cda:templateId", 1, null, "SM", Librairie,
                $"{TemplateIds.Document.Hl7France}, {TemplateIds.Document.CiSis}, {TemplateIds.Document.NonStructuredBody}");
            C("Id", null, doc, "Identifiant du document", "Identifiant unique de cette version du document (OID ou UUID).",
                "cda:id", 1, 1, "SM, XSD");
            var typeDocument = C("TypeDocument", null, doc, "Type de document", "Code LOINC 18748-4 « CR d'imagerie médicale ».",
                "cda:code", 1, 1, "STD", Librairie, Codes.DocumentType);
            C("TypeDocument.Translations", typeDocument, doc, "Actes du document",
                "Une translation par acte : code LOINC de l'acte.", "cda:translation", 1, null, "STD", Librairie);
            C("Titre", null, doc, "Titre", "Titre du compte rendu.", "cda:title", 1, 1, "STD");
            C("DateCreation", null, doc, "Date de création", "Date et heure de création du document, avec fuseau horaire.",
                "cda:effectiveTime", 1, 1, "SM");
            C("Confidentialite", null, doc, "Confidentialité", "Niveau de confidentialité.", "cda:confidentialityCode", 1, 1, "SM",
                ParDefaut, "N");
            C("Langue", null, doc, "Langue", "Langue du document.", "cda:languageCode", 1, 1, "SM", ParDefaut, "fr-FR");
            C("SetId", null, doc, "Identifiant du lot de versions", "Identifiant commun à toutes les versions du document.",
                "cda:setId", 1, 1, "SM");
            C("NumeroVersion", null, doc, "Numéro de version", "Numéro de version du document (entier ≥ 1).",
                "cda:versionNumber", 1, 1, "SM", ParDefaut, "1");
            C("DocumentRemplace", null, doc, "Document remplacé", "Identifiant du document remplacé par cette version (RPLC).",
                "cda:relatedDocument[@typeCode='RPLC']/cda:parentDocument/cda:id", 0, 1, "SM");

            // Patient
            const string pat = Sections.Patient;
            var patient = C("Patient", null, pat, "Patient", "Patient concerné par le compte rendu.",
                "cda:recordTarget/cda:patientRole", 1, 1, "SM, XSD");
            C("Patient.Ins", patient, pat, "INS", "Matricule INS (INS-NIR ou INS-NIA, de production ou de test).",
                $"cda:id[{insRoots}]", 1, 1, "SM, INS");
            C("Patient.AutresIdentifiants", patient, pat, "Autres identifiants", "Autres identifiants, ex. IPP (root = OID de l'établissement).",
                $"cda:id[not({insRoots})]", 0, null, "SM");
            C("Patient.Adresses", patient, pat, "Adresses", "Adresses du patient.", "cda:addr", 0, null, "SM");
            C("Patient.Telecoms", patient, pat, "Télécoms", "Coordonnées télécom du patient.", "cda:telecom", 0, null, "SM");
            C("Patient.NomNaissance", patient, pat, "Nom de naissance", "Trait INS : nom de l'acte de naissance.",
                "cda:patient/cda:name/cda:family[@qualifier='BR']", 1, 1, "SM, INS");
            C("Patient.PrenomsNaissance", patient, pat, "Prénoms de naissance", "Trait INS : prénoms de l'acte de naissance.",
                "cda:patient/cda:name/cda:given[not(@qualifier)]", 1, 1, "SM, INS");
            C("Patient.PremierPrenomNaissance", patient, pat, "Premier prénom de naissance", "Trait INS : premier prénom de l'acte de naissance.",
                "cda:patient/cda:name/cda:given[@qualifier='BR']", 1, 1, "SM, INS");
            C("Patient.NomUtilise", patient, pat, "Nom utilisé", "Nom utilisé.",
                "cda:patient/cda:name/cda:family[@qualifier='CL']", 0, 1, "SM");
            C("Patient.PrenomUtilise", patient, pat, "Prénom utilisé", "Prénom utilisé.",
                "cda:patient/cda:name/cda:given[@qualifier='CL']", 0, 1, "SM");
            C("Patient.Sexe", patient, pat, "Sexe", "Trait INS : sexe administratif (M ou F).",
                "cda:patient/cda:administrativeGenderCode", 1, 1, "SM, INS");
            C("Patient.DateNaissance", patient, pat, "Date de naissance", "Trait INS : date de naissance.",
                "cda:patient/cda:birthTime[@value]", 1, 1, "SM, INS");
            C("Patient.LieuNaissanceCog", patient, pat, "Lieu de naissance (COG)", "Trait INS : code officiel géographique de la commune de naissance.",
                "cda:patient/cda:birthplace/cda:place/cda:addr/cda:county", 1, 1, "SM, INS");
            C("Patient.LieuNaissanceCommune", patient, pat, "Commune de naissance", "Libellé de la commune de naissance.",
                "cda:patient/cda:birthplace/cda:place/cda:addr/cda:city", 0, 1, "SM");

            // Auteur
            const string aut = Sections.Auteur;
            var auteur = C("Auteurs[]", null, aut, "Auteur", "Radiologue ou médecin nucléaire qui rédige le compte rendu.",
                "cda:author", 1, null, "STD, SM");
            C("Auteurs[].Fonction", auteur, aut, "Rôle fonctionnel", "Rôle fonctionnel de l'auteur (JDV_J47).", "cda:functionCode", 0, 1, "SM");
            C("Auteurs[].Horodatage", auteur, aut, "Date de rédaction", "Date et heure de rédaction / validation par l'auteur.",
                "cda:time", 1, 1, "SM");
            var assignedAuthor = C("Auteurs[].Professionnel", auteur, aut, "Professionnel", "Auteur (assignedAuthor).",
                "cda:assignedAuthor", 1, 1, "SM");
            Professionnel("Auteurs[].Professionnel", assignedAuthor, aut, "assignedPerson", "representedOrganization",
                professionObligatoire: true, libelleProfessionObligatoire: true, minOrganisation: 1,
                idOrganisationObligatoire: true, secteurObligatoire: false, libelleSecteurObligatoire: true);

            // Custodian
            const string cus = Sections.Custodian;
            var custodian = C("Custodian", null, cus, "Structure de conservation", "Structure chargée de la conservation du document.",
                "cda:custodian/cda:assignedCustodian/cda:representedCustodianOrganization", 1, 1, "SM, XSD");
            C("Custodian.Id", custodian, cus, "Identifiant de la structure", "Identifiant national de la structure (FINESS ou SIRET).",
                "cda:id", 1, 1, "SM");
            C("Custodian.Nom", custodian, cus, "Nom de la structure", "Raison sociale.", "cda:name", 0, 1, "SM");
            C("Custodian.Telecoms", custodian, cus, "Télécom de la structure", "Coordonnée télécom.", "cda:telecom", 0, 1, "SM");
            C("Custodian.Adresses", custodian, cus, "Adresse de la structure", "Adresse postale.", "cda:addr", 0, 1, "SM");

            // Signataire légal
            const string sig = Sections.Signataire;
            var signataire = C("SignataireLegal", null, sig, "Signataire légal", "Responsable du document.",
                "cda:legalAuthenticator", 1, 1, "SM, STD");
            C("SignataireLegal.Horodatage", signataire, sig, "Date de signature", "Date et heure de signature.", "cda:time", 1, 1, "SM");
            C("SignataireLegal.SignatureCode", signataire, sig, "Code de signature", "Document signé : S.", "cda:signatureCode", 1, 1, "SM",
                Librairie, "S");
            var assignedEntity = C("SignataireLegal.Professionnel", signataire, sig, "Professionnel", "Signataire (assignedEntity).",
                "cda:assignedEntity", 1, 1, "SM");
            Professionnel("SignataireLegal.Professionnel", assignedEntity, sig, "assignedPerson", "representedOrganization",
                professionObligatoire: false, libelleProfessionObligatoire: false, minOrganisation: 0,
                idOrganisationObligatoire: false, secteurObligatoire: false, libelleSecteurObligatoire: false);

            // Médecin demandeur
            const string dem = Sections.Demandeur;
            var demandeur = C("MedecinsDemandeurs[]", null, dem, "Médecin demandeur", "Médecin à l'origine de la demande d'examen (participant REF).",
                "cda:participant[@typeCode='REF']", 0, null, "SM");
            C("MedecinsDemandeurs[].DateDemande", demandeur, dem, "Date de la demande", "Date de la demande d'examen.",
                "cda:time/cda:high", 0, 1, "SM");
            var associatedEntity = C("MedecinsDemandeurs[].Professionnel", demandeur, dem, "Professionnel", "Médecin demandeur (associatedEntity).",
                "cda:associatedEntity", 1, 1, "SM");
            Professionnel("MedecinsDemandeurs[].Professionnel", associatedEntity, dem, "associatedPerson", "scopingOrganization",
                professionObligatoire: false, libelleProfessionObligatoire: true, minOrganisation: 0,
                idOrganisationObligatoire: false, secteurObligatoire: false, libelleSecteurObligatoire: false);

            // Demande d'examen
            const string dmd = Sections.Demande;
            var demande = C("Demandes[]", null, dmd, "Demande d'examen", "Demande d'examen honorée par le compte rendu.",
                "cda:inFulfillmentOf/cda:order", 1, null, "STD");
            C("Demandes[].NumeroDemande", demande, dmd, "Numéro de demande",
                "Order Placer Number attribué par le demandeur (nullFlavor si pas de demande dématérialisée).", "cda:id", 1, 1, "STD");
            C("Demandes[].AccessionNumber", demande, dmd, "Accession Number", "Identifiant de la demande attribué par le RIS.",
                "ps3-20:accessionNumber", 1, 1, "STD");

            // Acte d'imagerie
            const string act = Sections.Acte;
            var acte = C("Actes[]", null, act, "Acte d'imagerie", "Acte d'imagerie documenté (serviceEvent codé en LOINC).",
                $"cda:documentationOf/cda:serviceEvent[cda:code/@codeSystem='{CodeSystems.Loinc}']", 1, null, "STD");
            C("Actes[].StudyInstanceUid", acte, act, "Study Instance UID", "Identifiant DICOM de l'examen (root seul).",
                "cda:id", 1, 1, "STD");
            C("Actes[].Code", acte, act, "Code de l'acte", "Code LOINC de l'acte (jdv-code-document-imagerie-cisis).",
                "cda:code", 1, 1, "STD");
            C("Actes[].CodeCcam", acte, act, "Code CCAM", "Code CCAM de l'acte (translation).",
                $"cda:code/cda:translation[@codeSystem='{CodeSystems.Ccam}']", 0, 1, "STD");
            C("Actes[].Modalites", acte, act, "Modalités d'acquisition", "Modalités d'acquisition (DCM, qualifier 121139).",
                $"cda:code/cda:translation[cda:qualifier/cda:name/@code='{Codes.Entries.DcmModality}']", 1, null, "STD");
            C("Actes[].RegionsAnatomiques", acte, act, "Régions anatomiques", "Régions anatomiques examinées (qualifier 39111-0).",
                $"cda:code/cda:translation[cda:qualifier/cda:name/@code='{Codes.Entries.LoincAnatomicLocation}']", 1, null, "STD");
            C("Actes[].Debut", acte, act, "Début de l'acte", "Date et heure de début de réalisation.", "cda:effectiveTime/cda:low", 1, 1, "SM");
            C("Actes[].Fin", acte, act, "Fin de l'acte", "Date et heure de fin de réalisation.", "cda:effectiveTime/cda:high", 0, 1, "SM");
            var executant = C("Actes[].Executant", acte, act, "Exécutant", "Radiologue exécutant (performer PRF).",
                "cda:performer[@typeCode='PRF']/cda:assignedEntity", 1, 1, "SM, STD");
            Professionnel("Actes[].Executant", executant, act, "assignedPerson", "representedOrganization",
                professionObligatoire: false, libelleProfessionObligatoire: false, minOrganisation: 1,
                idOrganisationObligatoire: true, secteurObligatoire: true, libelleSecteurObligatoire: true);
            C("Depistage", null, act, "Examen de dépistage", "Examen réalisé dans le cadre d'un dépistage (CIM-10 Z13.9).",
                "cda:documentationOf/cda:serviceEvent[cda:code/@code='Z13.9']", 0, 1, "STD");

            // Prise en charge
            const string pec = Sections.PriseEnCharge;
            var priseEnCharge = C("PriseEnCharge", null, pec, "Prise en charge", "Contexte de la prise en charge.",
                "cda:componentOf/cda:encompassingEncounter", 1, 1, "SM");
            var modalite = C("PriseEnCharge.Modalite", priseEnCharge, pec, "Type de prise en charge", "Modalité de la prise en charge (JDV_J142).",
                "cda:code", 0, 1, "SM");
            C("PriseEnCharge.Modalite.DisplayName", modalite, pec, "Libellé du type de prise en charge", "Libellé (displayName).",
                "@displayName", 1, 1, "SM");
            C("PriseEnCharge.Debut", priseEnCharge, pec, "Début de la prise en charge", "Date et heure de début.",
                "cda:effectiveTime/cda:low", 1, 1, "SM");
            C("PriseEnCharge.Fin", priseEnCharge, pec, "Fin de la prise en charge", "Date et heure de fin.",
                "cda:effectiveTime/cda:high", 0, 1, "SM");
            var lieu = C("PriseEnCharge.Lieu", priseEnCharge, pec, "Lieu de prise en charge", "Lieu de la prise en charge.",
                "cda:location/cda:healthCareFacility", 1, 1, "SM");
            C("PriseEnCharge.Lieu.Id", lieu, pec, "Identifiant du lieu", "Identifiant national du lieu (FINESS ou SIRET).", "cda:id", 0, 1, "SM");
            var cadre = C("PriseEnCharge.Lieu.CadreExercice", lieu, pec, "Cadre d'exercice", "Type de lieu de prise en charge (JDV_J02).",
                "cda:code", 1, 1, "SM");
            C("PriseEnCharge.Lieu.CadreExercice.DisplayName", cadre, pec, "Libellé du cadre d'exercice", "Libellé (displayName).",
                "@displayName", 1, 1, "SM");
            C("PriseEnCharge.Lieu.Nom", lieu, pec, "Nom du lieu", "Nom du lieu de prise en charge.", "cda:location/cda:name", 0, 1, "SM");
            C("PriseEnCharge.Lieu.Adresse", lieu, pec, "Adresse du lieu", "Adresse du lieu de prise en charge.", "cda:location/cda:addr", 0, 1, "SM");

            // Corps
            C("Corps", null, Sections.Corps, "Compte rendu PDF", "Compte rendu au format PDF, encapsulé en base64.",
                "cda:component/cda:nonXMLBody/cda:text[@mediaType='application/pdf']", 1, 1, "SM");

            return champs;
        }
    }
}
