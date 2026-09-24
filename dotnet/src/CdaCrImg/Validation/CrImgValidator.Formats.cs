using System;
using System.Linq;
using System.Text.RegularExpressions;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Validation
{
    /// <summary>Règles de format et de cohérence (types de données HL7 contrôlés par la structuration minimale).</summary>
    public static partial class CrImgValidator
    {
        private static readonly Regex UuidPattern = new Regex(
            @"^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$", RegexOptions.CultureInvariant);

        /// <summary>Matricule NIR/NIA : 13 caractères (département corse 2A/2B admis) + clé de 2 chiffres.</summary>
        private static readonly Regex InsPattern = new Regex(@"^[0-9]{5}([0-9]{2}|2[AB])[0-9]{6}[0-9]{2}$", RegexOptions.CultureInvariant);

        private static readonly Regex CogPattern = new Regex(@"^([0-9]{5}|2[AB][0-9]{3})$", RegexOptions.CultureInvariant);
        private static readonly Regex LanguagePattern = new Regex(@"^[a-z]{2,3}(-[A-Z]{2})?$", RegexOptions.CultureInvariant);
        private static readonly Regex TelecomPattern = new Regex(@"^(tel|fax|mailto|http|https|ftp|mllp):\S+$", RegexOptions.CultureInvariant);

        private static void ValidateFormats(CompteRenduImagerie cr, Action<string, string> err)
        {
            // Structuration minimale : « Attribute @root SHALL be of data type 'uid' » (OID ou UUID).
            foreach (var (path, id) in ModelPaths.Identifiers(cr))
            {
                if (id?.Root != null && !IsUid(id.Root))
                    err(path, $"root « {id.Root} » invalide : un OID ou un UUID est attendu (type uid).");
            }

            // Structuration minimale : « Attribute @code SHALL be of data type 'cs' » (sans espace).
            foreach (var (path, code) in ModelPaths.Codes(cr))
            {
                if (code != null && code.Value.Any(char.IsWhiteSpace))
                    err(path, $"code « {code.Value} » invalide : un code (type cs) ne contient pas d'espace.");
            }

            foreach (var (path, telecom) in ModelPaths.Telecoms(cr))
            {
                if (!TelecomPattern.IsMatch(telecom.Value))
                    err(path, $"« {telecom.Value} » invalide : une URL de type tel:, fax:, mailto: ou http(s): est attendue.");
            }

            if (!LanguagePattern.IsMatch(cr.Langue ?? ""))
                err("Langue", "code de langue invalide (ex. fr-FR).");

            ValidatePatientFormats(cr, err);

            for (var i = 0; i < cr.Actes.Count; i++)
            {
                var acte = cr.Actes[i];
                if (acte.Debut != null && acte.Fin != null && acte.Fin < acte.Debut)
                    err($"Actes[{i}].Fin", "la fin de l'acte précède son début.");
            }

            var pec = cr.PriseEnCharge;
            if (pec?.Debut != null && pec.Fin != null && pec.Fin < pec.Debut)
                err("PriseEnCharge.Fin", "la fin de la prise en charge précède son début.");
        }

        private static void ValidatePatientFormats(CompteRenduImagerie cr, Action<string, string> err)
        {
            var patient = cr.Patient;
            if (patient == null) return;

            var ins = patient.Ins;
            if (ins?.Extension != null && InsRoots.Contains(ins.Root))
            {
                if (!InsPattern.IsMatch(ins.Extension))
                    err("Patient.Ins", "matricule INS invalide : 15 caractères attendus (13 caractères + clé de 2 chiffres).");
                // La clé n'est contrôlée que pour l'INS-NIR de production : les matricules de test sont souvent fictifs.
                else if (ins.Root == IdentifierRoots.InsNir && !HasValidNirKey(ins.Extension))
                    err("Patient.Ins", "clé du matricule INS-NIR invalide.");
            }

            if (!string.IsNullOrWhiteSpace(patient.LieuNaissanceCog) && !CogPattern.IsMatch(patient.LieuNaissanceCog))
                err("Patient.LieuNaissanceCog", "code officiel géographique invalide : 5 caractères attendus (ex. 51215, 2A004, 99xxx).");

            if (patient.DateNaissance != null && cr.DateCreation != default && patient.DateNaissance.Value.Date > cr.DateCreation.Date)
                err("Patient.DateNaissance", "la date de naissance est postérieure à la date du document.");
        }

        private static bool IsUid(string root) => OidPattern.IsMatch(root) || UuidPattern.IsMatch(root);

        /// <summary>Clé NIR = 97 - (13 premiers caractères modulo 97), 2A et 2B remplacés par 19 et 18.</summary>
        private static bool HasValidNirKey(string nir)
        {
            var body = nir.Substring(0, 13).Replace("2A", "19").Replace("2B", "18");
            var key = int.Parse(nir.Substring(13, 2), System.Globalization.CultureInfo.InvariantCulture);
            return 97 - (long.Parse(body, System.Globalization.CultureInfo.InvariantCulture) % 97) == key;
        }
    }
}
