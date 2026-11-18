using System;
using System.Collections.Generic;

namespace CdaCrImg.Model.Hl7
{
    /// <summary>Concept codé HL7 (types CD/CE/CV) : code, système de codage, libellé, traductions et qualificatifs.</summary>
    public sealed class Code
    {
        /// <summary>Crée un concept codé.</summary>
        public Code(string code, string codeSystem, string? displayName = null, string? codeSystemName = null)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Le code est obligatoire.", nameof(code));
            if (string.IsNullOrWhiteSpace(codeSystem)) throw new ArgumentException("Le système de codage est obligatoire.", nameof(codeSystem));
            Value = code;
            CodeSystem = codeSystem;
            DisplayName = displayName;
            CodeSystemName = codeSystemName;
        }

        /// <summary>Valeur du code (attribut <c>code</c>).</summary>
        public string Value { get; }

        /// <summary>OID du système de codage.</summary>
        public string CodeSystem { get; }

        /// <summary>Libellé.</summary>
        public string? DisplayName { get; }

        /// <summary>Nom du système de codage (ex. LOINC).</summary>
        public string? CodeSystemName { get; }

        /// <summary>Qualificatifs (élément <c>qualifier</c>).</summary>
        public IList<Qualifier> Qualifiers { get; } = new List<Qualifier>();

        /// <summary>Traductions dans d'autres systèmes (élément <c>translation</c>).</summary>
        public IList<Code> Translations { get; } = new List<Code>();

        /// <summary>Code LOINC.</summary>
        public static Code Loinc(string code, string? displayName = null) => new Code(code, CodeSystems.Loinc, displayName, "LOINC");

        /// <summary>Code SNOMED CT.</summary>
        public static Code Snomed(string code, string? displayName = null) => new Code(code, CodeSystems.SnomedCt, displayName, "SNOMED CT");

        /// <summary>Code DICOM (DCM), ex. modalité <c>CT</c>, <c>MR</c>.</summary>
        public static Code Dcm(string code, string? displayName = null) => new Code(code, CodeSystems.Dcm, displayName, "DCM");

        /// <summary>Code CCAM.</summary>
        public static Code Ccam(string code, string? displayName = null) => new Code(code, CodeSystems.Ccam, displayName, "CCAM");

        /// <summary>Code CIM-10.</summary>
        public static Code Cim10(string code, string? displayName = null) => new Code(code, CodeSystems.Cim10, displayName, "CIM-10");

        /// <summary>Profession / spécialité du PS (TRE_G15/R85), ex. <c>G15_10/SM44</c>.</summary>
        public static Code ProfessionSavoirFaire(string code, string? displayName = null) => new Code(code, CodeSystems.ProfessionSavoirFaire, displayName);

        /// <inheritdoc/>
        public override string ToString() => $"{Value} ({CodeSystem}){(DisplayName != null ? " " + DisplayName : "")}";
    }

    /// <summary>Qualificatif d'un code : nom (<c>name</c>) et valeur optionnelle (<c>value</c>).</summary>
    public sealed class Qualifier
    {
        /// <summary>Crée un qualificatif.</summary>
        public Qualifier(Code name, Code? value = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        /// <summary>Nom du qualificatif.</summary>
        public Code Name { get; }

        /// <summary>Valeur du qualificatif.</summary>
        public Code? Value { get; }
    }
}
