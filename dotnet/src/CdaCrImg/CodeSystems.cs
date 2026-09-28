namespace CdaCrImg
{
    /// <summary>OID des systèmes de codage rencontrés dans le volet IMG-CR-IMG.</summary>
    public static class CodeSystems
    {
        /// <summary>LOINC.</summary>
        public const string Loinc = "2.16.840.1.113883.6.1";
        /// <summary>SNOMED CT.</summary>
        public const string SnomedCt = "2.16.840.1.113883.6.96";
        /// <summary>DICOM Controlled Terminology (DCM).</summary>
        public const string Dcm = "1.2.840.10008.2.16.4";
        /// <summary>CCAM (Classification commune des actes médicaux).</summary>
        public const string Ccam = "1.2.250.1.215.300.1";
        /// <summary>CIM-10.</summary>
        public const string Cim10 = "2.16.840.1.113883.6.3";
        /// <summary>CIP (code produit médicament).</summary>
        public const string Cip = "1.2.250.1.213.2.3.2";
        /// <summary>ATC.</summary>
        public const string Atc = "2.16.840.1.113883.6.73";
        /// <summary>EDQM Standard Terms (voies d'administration).</summary>
        public const string Edqm = "0.4.0.127.0.16.1.1.2.1";
        /// <summary>TRE_G15 / R85 : profession / spécialité du PS (codes "G15_10/SM44").</summary>
        public const string ProfessionSavoirFaire = "1.2.250.1.213.1.1.4.5";
        /// <summary>HL7 AdministrativeGender.</summary>
        public const string Hl7AdministrativeGender = "2.16.840.1.113883.5.1";
        /// <summary>HL7 Confidentiality.</summary>
        public const string Hl7Confidentiality = "2.16.840.1.113883.5.25";
        /// <summary>HL7 ParticipationFunction.</summary>
        public const string Hl7ParticipationFunction = "2.16.840.1.113883.5.88";
        /// <summary>HL7 ActCode (ex. code de l'encompassingEncounter).</summary>
        public const string Hl7ActCode = "2.16.840.1.113883.5.4";
        /// <summary>Secteur d'activité (standardIndustryClassCode).</summary>
        public const string SecteurActivite = "1.2.250.1.213.1.1.4.9";
        /// <summary>Cadre d'exercice / type de structure (healthCareFacility/code).</summary>
        public const string CadreExercice = "1.2.250.1.71.4.2.4";
    }

    /// <summary>Racines d'identifiants (OID) fréquentes.</summary>
    public static class IdentifierRoots
    {
        /// <summary>INS-NIR (production).</summary>
        public const string InsNir = "1.2.250.1.213.1.4.8";
        /// <summary>INS-NIR de test.</summary>
        public const string InsNirTest = "1.2.250.1.213.1.4.10";
        /// <summary>INS-NIA (identifiant d'attente).</summary>
        public const string InsNia = "1.2.250.1.213.1.4.9";
        /// <summary>INS-NIA de test.</summary>
        public const string InsNiaTest = "1.2.250.1.213.1.4.11";
        /// <summary>Identifiant national de PS (RPPS / ADELI préfixé).</summary>
        public const string IdNatPs = "1.2.250.1.71.4.2.1";
        /// <summary>Identifiant national de structure (FINESS / SIRET préfixé).</summary>
        public const string IdNatStruct = "1.2.250.1.71.4.2.2";
    }
}
