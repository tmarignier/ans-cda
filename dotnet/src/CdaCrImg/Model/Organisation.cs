using System.Collections.Generic;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Model
{
    /// <summary>Structure de santé (organisation d'un PS, custodian…).</summary>
    public sealed class Organisation
    {
        /// <summary>Identifiant (national : <see cref="Identifier.FromFiness"/>, <see cref="Identifier.FromSiret"/>).</summary>
        public Identifier? Id { get; set; }

        /// <summary>Raison sociale.</summary>
        public string? Nom { get; set; }

        /// <summary>Coordonnées télécom.</summary>
        public IList<Telecom> Telecoms { get; } = new List<Telecom>();

        /// <summary>Adresses.</summary>
        public IList<Address> Adresses { get; } = new List<Address>();

        /// <summary>Secteur d'activité (<c>standardIndustryClassCode</c>, JDV_J04 ; système <see cref="CodeSystems.SecteurActivite"/>).</summary>
        public Code? SecteurActivite { get; set; }
    }
}
