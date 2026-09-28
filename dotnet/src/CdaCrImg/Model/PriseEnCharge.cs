using System;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Model
{
    /// <summary>Contexte de la prise en charge (<c>componentOf/encompassingEncounter</c>).</summary>
    public sealed class PriseEnCharge
    {
        /// <summary>Modalité de prise en charge (HL7 ActCode, ex. <c>AMB</c>, <c>EMER</c>, <c>IMP</c>).</summary>
        public Code? Modalite { get; set; }

        /// <summary>Début de la prise en charge.</summary>
        public DateTimeOffset? Debut { get; set; }

        /// <summary>Fin de la prise en charge (optionnelle).</summary>
        public DateTimeOffset? Fin { get; set; }

        /// <summary>Lieu de la prise en charge.</summary>
        public LieuPriseEnCharge? Lieu { get; set; }
    }

    /// <summary>Lieu de prise en charge (<c>location/healthCareFacility</c>).</summary>
    public sealed class LieuPriseEnCharge
    {
        /// <summary>Identifiant de la structure (optionnel).</summary>
        public Identifier? Id { get; set; }

        /// <summary>Cadre d'exercice (JDV_J02 ; système <see cref="CodeSystems.CadreExercice"/>), ex. <c>SA08</c>.</summary>
        public Code? CadreExercice { get; set; }

        /// <summary>Nom du lieu.</summary>
        public string? Nom { get; set; }

        /// <summary>Adresse du lieu.</summary>
        public Address? Adresse { get; set; }
    }
}
