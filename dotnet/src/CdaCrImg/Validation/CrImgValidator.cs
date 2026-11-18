using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Validation
{
    /// <summary>
    /// Contrôles du modèle avant production du document, alignés sur la structuration minimale CI-SIS et
    /// les spécifications techniques IMG-CR-IMG 2024.01 : complétude (ce fichier), formats et cohérence
    /// (<c>CrImgValidator.Formats.cs</c>), terminologies (<c>CrImgValidator.Terminologies.cs</c>).
    /// </summary>
    public static partial class CrImgValidator
    {
        private static readonly Regex OidPattern = new Regex(@"^[0-2](\.(0|[1-9][0-9]*))+$", RegexOptions.CultureInvariant);

        private static readonly string[] InsRoots =
        {
            IdentifierRoots.InsNir, IdentifierRoots.InsNia, IdentifierRoots.InsNirTest, IdentifierRoots.InsNiaTest,
        };

        /// <summary>Retourne la liste des non-conformités (vide si le compte rendu est conforme).</summary>
        /// <param name="cr">Compte rendu à contrôler.</param>
        /// <param name="options">Options ; par défaut <see cref="CrImgValidationOptions.Defaut"/>.</param>
        public static IReadOnlyList<ValidationIssue> Validate(CompteRenduImagerie cr, CrImgValidationOptions? options = null)
        {
            if (cr == null) throw new ArgumentNullException(nameof(cr));
            options ??= CrImgValidationOptions.Defaut;
            var issues = new List<ValidationIssue>();
            void Err(string path, string message) => issues.Add(new ValidationIssue(path, message));

            RequireId(cr.Id, "Id", Err);
            RequireId(cr.SetId, "SetId", Err);
            if (cr.NumeroVersion < 1) Err("NumeroVersion", "doit être supérieur ou égal à 1.");
            else if (cr.DocumentRemplace != null && cr.NumeroVersion == 1)
                Err("NumeroVersion", "un document qui en remplace un autre (DocumentRemplace) a un numéro de version supérieur ou égal à 2.");
            if (string.IsNullOrWhiteSpace(cr.Titre)) Err("Titre", "obligatoire.");
            if (cr.DateCreation == default) Err("DateCreation", "obligatoire.");

            ValidatePatient(cr.Patient, Err);

            if (cr.Auteurs.Count == 0) Err("Auteurs", "au moins un auteur est obligatoire.");
            for (var i = 0; i < cr.Auteurs.Count; i++)
            {
                if (cr.Auteurs[i].Horodatage == default) Err($"Auteurs[{i}].Horodatage", "date de rédaction obligatoire.");
                var path = $"Auteurs[{i}].Professionnel";
                ValidateProfessionnel(cr.Auteurs[i].Professionnel, path, requireProfession: true, Err);
                RequireDisplayName(cr.Auteurs[i].Professionnel?.Profession, path + ".Profession", Err);
                var organisation = cr.Auteurs[i].Professionnel?.Organisation;
                if (organisation == null)
                {
                    Err(path + ".Organisation", "obligatoire pour l'auteur d'un CR d'imagerie.");
                }
                else
                {
                    RequireId(organisation.Id, path + ".Organisation.Id", Err);
                    RequireDisplayName(organisation.SecteurActivite, path + ".Organisation.SecteurActivite", Err);
                }
            }

            if (cr.Custodian == null) Err("Custodian", "obligatoire.");
            else RequireId(cr.Custodian.Id, "Custodian.Id", Err);

            if (cr.SignataireLegal == null) Err("SignataireLegal", "obligatoire.");
            else
            {
                if (cr.SignataireLegal.Horodatage == default) Err("SignataireLegal.Horodatage", "date de signature obligatoire.");
                ValidateProfessionnel(cr.SignataireLegal.Professionnel, "SignataireLegal.Professionnel", requireProfession: false, Err);
            }

            for (var i = 0; i < cr.MedecinsDemandeurs.Count; i++)
            {
                var path = $"MedecinsDemandeurs[{i}].Professionnel";
                ValidateProfessionnel(cr.MedecinsDemandeurs[i].Professionnel, path, requireProfession: false, Err);
                RequireDisplayName(cr.MedecinsDemandeurs[i].Professionnel?.Profession, path + ".Profession", Err);
            }

            if (cr.Demandes.Count == 0) Err("Demandes", "au moins une demande d'examen est obligatoire (numéro de demande en nullFlavor si non dématérialisée).");
            for (var i = 0; i < cr.Demandes.Count; i++)
            {
                if (cr.Demandes[i].NumeroDemande == null) Err($"Demandes[{i}].NumeroDemande", "obligatoire (Identifier.Null() si absent).");
                var accessionNumber = cr.Demandes[i].AccessionNumber;
                RequireId(accessionNumber, $"Demandes[{i}].AccessionNumber", Err);
                // STD 3.3.4.5 : l'Accession Number est la valeur (extension) attribuée par le RIS dans l'espace de noms root.
                if (accessionNumber?.Root != null && string.IsNullOrWhiteSpace(accessionNumber.Extension))
                    Err($"Demandes[{i}].AccessionNumber", "valeur (extension) attribuée par le RIS obligatoire.");
            }

            if (cr.Actes.Count == 0) Err("Actes", "au moins un acte d'imagerie est obligatoire.");
            for (var i = 0; i < cr.Actes.Count; i++) ValidateActe(cr.Actes[i], $"Actes[{i}]", Err);

            ValidatePriseEnCharge(cr.PriseEnCharge, Err);

            switch (cr.Corps)
            {
                case null:
                    Err("Corps", "obligatoire.");
                    break;
                case CorpsPdf pdf when !IsPdf(pdf.Pdf):
                    Err("Corps.Pdf", "le contenu n'est pas un fichier PDF (signature %PDF- absente).");
                    break;
            }

            ValidateFormats(cr, Err);
            ValidateTerminologies(cr, options, Err);

            return issues;
        }

        /// <summary>Lève <see cref="CrImgValidationException"/> si le compte rendu n'est pas complet.</summary>
        public static void EnsureValid(CompteRenduImagerie cr, CrImgValidationOptions? options = null)
        {
            var issues = Validate(cr, options);
            if (issues.Count > 0) throw new CrImgValidationException(issues);
        }

        private static void ValidatePatient(Patient? patient, Action<string, string> err)
        {
            if (patient == null)
            {
                err("Patient", "obligatoire.");
                return;
            }
            // La librairie ne produit que des CR de patients identifiés par leur INS : l'INS et ses traits
            // d'identité (structuration minimale CI-SIS) sont donc toujours obligatoires.
            if (patient.Ins == null)
                err("Patient.Ins", "obligatoire : la librairie ne gère que les patients identifiés par leur INS.");
            else if (!InsRoots.Contains(patient.Ins.Root) || string.IsNullOrWhiteSpace(patient.Ins.Extension))
                err("Patient.Ins", "root INS-NIR/INS-NIA (1.2.250.1.213.1.4.8 à .11) et matricule (extension) attendus.");
            if (string.IsNullOrWhiteSpace(patient.NomNaissance)) err("Patient.NomNaissance", "trait INS obligatoire.");
            if (string.IsNullOrWhiteSpace(patient.PrenomsNaissance)) err("Patient.PrenomsNaissance", "trait INS obligatoire.");
            if (string.IsNullOrWhiteSpace(patient.PremierPrenomNaissance)) err("Patient.PremierPrenomNaissance", "trait INS obligatoire.");
            if (patient.DateNaissance == null) err("Patient.DateNaissance", "trait INS obligatoire.");
            if (string.IsNullOrWhiteSpace(patient.LieuNaissanceCog)) err("Patient.LieuNaissanceCog", "trait INS obligatoire.");
            if (patient.Sexe == Sexe.Inconnu) err("Patient.Sexe", "trait INS obligatoire (masculin ou féminin).");
        }

        private static void ValidateProfessionnel(Professionnel? ps, string path, bool requireProfession, Action<string, string> err)
        {
            if (ps == null)
            {
                err(path, "obligatoire.");
                return;
            }
            RequireId(ps.Id, path + ".Id", err);
            if (requireProfession && ps.Profession == null) err(path + ".Profession", "obligatoire.");
            // Structuration minimale : si l'identité est présente, name/family est obligatoire.
            if (ps.Nom != null && string.IsNullOrWhiteSpace(ps.Nom.Family))
                err(path + ".Nom.Family", "nom de famille obligatoire lorsque l'identité du professionnel est renseignée.");
        }

        private static void ValidateActe(ActeImagerie acte, string path, Action<string, string> err)
        {
            if (string.IsNullOrWhiteSpace(acte.StudyInstanceUid))
                err(path + ".StudyInstanceUid", "obligatoire.");
            else if (!OidPattern.IsMatch(acte.StudyInstanceUid) || acte.StudyInstanceUid.Length > 64)
                err(path + ".StudyInstanceUid", "doit être un UID DICOM (chiffres et points, 64 caractères max).");

            if (acte.Code == null) err(path + ".Code", "code LOINC de l'acte obligatoire.");
            else if (acte.Code.CodeSystem != CodeSystems.Loinc) err(path + ".Code", "doit être un code LOINC (jdv-code-document-imagerie-cisis).");
            if (acte.CodeCcam != null && acte.CodeCcam.CodeSystem != CodeSystems.Ccam) err(path + ".CodeCcam", "doit être un code CCAM.");

            if (acte.Modalites.Count == 0) err(path + ".Modalites", "au moins une modalité est obligatoire.");
            if (acte.Modalites.Any(m => m.CodeSystem != CodeSystems.Dcm)) err(path + ".Modalites", "les modalités sont des codes DICOM (DCM).");
            if (acte.RegionsAnatomiques.Count == 0) err(path + ".RegionsAnatomiques", "au moins une région anatomique est obligatoire.");

            if (acte.Debut == null) err(path + ".Debut", "date de réalisation obligatoire.");

            if (acte.Executant == null)
            {
                err(path + ".Executant", "obligatoire.");
            }
            else
            {
                ValidateProfessionnel(acte.Executant, path + ".Executant", requireProfession: false, err);
                if (acte.Executant.Organisation == null)
                {
                    err(path + ".Executant.Organisation", "obligatoire (DRIM-Box).");
                }
                else
                {
                    RequireId(acte.Executant.Organisation.Id, path + ".Executant.Organisation.Id", err);
                    // Structuration minimale : standardIndustryClassCode obligatoire (JDV_J04_XdsPracticeSettingCode_CISIS).
                    if (acte.Executant.Organisation.SecteurActivite == null)
                        err(path + ".Executant.Organisation.SecteurActivite", "secteur d'activité obligatoire (JDV_J04, ex. AMBULATOIRE).");
                    else
                        RequireDisplayName(acte.Executant.Organisation.SecteurActivite, path + ".Executant.Organisation.SecteurActivite", err);
                }
            }
        }

        private static void ValidatePriseEnCharge(PriseEnCharge? pec, Action<string, string> err)
        {
            if (pec == null)
            {
                err("PriseEnCharge", "obligatoire.");
                return;
            }
            if (pec.Debut == null) err("PriseEnCharge.Debut", "obligatoire.");
            RequireDisplayName(pec.Modalite, "PriseEnCharge.Modalite", err);
            if (pec.Lieu?.CadreExercice == null) err("PriseEnCharge.Lieu.CadreExercice", "obligatoire.");
            else RequireDisplayName(pec.Lieu.CadreExercice, "PriseEnCharge.Lieu.CadreExercice", err);
        }

        /// <summary>Structuration minimale : @displayName obligatoire sur certains codes de l'en-tête.</summary>
        private static void RequireDisplayName(Code? code, string path, Action<string, string> err)
        {
            if (code != null && string.IsNullOrWhiteSpace(code.DisplayName))
                err(path, "libellé (displayName) obligatoire pour ce code (structuration minimale).");
        }

        private static void RequireId(Identifier? id, string path, Action<string, string> err)
        {
            if (id == null || id.NullFlavor != null) err(path, "identifiant obligatoire.");
        }

        private static bool IsPdf(byte[] content) =>
            content.Length > 5 && content[0] == '%' && content[1] == 'P' && content[2] == 'D' && content[3] == 'F' && content[4] == '-';
    }
}
