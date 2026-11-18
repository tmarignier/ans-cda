using System;

namespace CdaCrImg.Model.Hl7
{
    /// <summary>Identifiant HL7 (type II) : <c>root</c> (OID ou UUID) et <c>extension</c> optionnelle.</summary>
    public sealed class Identifier
    {
        /// <summary>Crée un identifiant.</summary>
        /// <param name="root">OID ou UUID de l'autorité d'affectation (ou identifiant complet si pas d'extension).</param>
        /// <param name="extension">Valeur de l'identifiant dans l'espace de noms <paramref name="root"/>.</param>
        public Identifier(string root, string? extension = null)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("Le root d'un identifiant est obligatoire.", nameof(root));
            }
            Root = root;
            Extension = extension;
        }

        private Identifier()
        {
        }

        /// <summary>OID ou UUID.</summary>
        public string? Root { get; }

        /// <summary>Extension (valeur locale).</summary>
        public string? Extension { get; }

        /// <summary>Raison de l'absence de valeur (ex. <c>UNK</c>, <c>NA</c>), exclusif de <see cref="Root"/>.</summary>
        public string? NullFlavor { get; private set; }

        /// <summary>Identifiant absent, avec sa raison HL7 (<c>UNK</c> inconnu, <c>NA</c> non applicable, <c>NAV</c> non disponible…).</summary>
        public static Identifier Null(string nullFlavor = "UNK") => new Identifier { NullFlavor = nullFlavor };

        /// <summary>Identifiant national de PS à partir d'un n° RPPS (idNat = "8" + RPPS).</summary>
        public static Identifier FromRpps(string rpps) => new Identifier(IdentifierRoots.IdNatPs, "8" + rpps);

        /// <summary>Identifiant national de structure à partir d'un n° FINESS (idNat = "1" + FINESS).</summary>
        public static Identifier FromFiness(string finess) => new Identifier(IdentifierRoots.IdNatStruct, "1" + finess);

        /// <summary>Identifiant national de structure à partir d'un n° SIRET (idNat = "3" + SIRET).</summary>
        public static Identifier FromSiret(string siret) => new Identifier(IdentifierRoots.IdNatStruct, "3" + siret);

        /// <inheritdoc/>
        public override string ToString() => NullFlavor != null ? "nullFlavor=" + NullFlavor : Root + (Extension != null ? "^" + Extension : "");
    }
}
