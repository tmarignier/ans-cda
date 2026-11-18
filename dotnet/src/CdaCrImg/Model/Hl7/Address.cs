namespace CdaCrImg.Model.Hl7
{
    /// <summary>Adresse postale (type AD).</summary>
    public sealed class Address
    {
        /// <summary>Usage (<c>H</c> domicile, <c>WP</c> professionnelle…).</summary>
        public string? Use { get; set; }

        /// <summary>Numéro dans la voie.</summary>
        public string? HouseNumber { get; set; }

        /// <summary>Nom de la voie.</summary>
        public string? StreetName { get; set; }

        /// <summary>Complément de localisation (bâtiment, résidence…).</summary>
        public string? AdditionalLocator { get; set; }

        /// <summary>Complément (escalier, appartement…).</summary>
        public string? UnitId { get; set; }

        /// <summary>Boîte postale / lieu-dit.</summary>
        public string? PostBox { get; set; }

        /// <summary>Code postal.</summary>
        public string? PostalCode { get; set; }

        /// <summary>Commune.</summary>
        public string? City { get; set; }

        /// <summary>Code officiel géographique (COG) de la commune (élément <c>county</c>).</summary>
        public string? County { get; set; }

        /// <summary>Pays.</summary>
        public string? Country { get; set; }
    }
}
