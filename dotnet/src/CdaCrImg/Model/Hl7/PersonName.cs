namespace CdaCrImg.Model.Hl7
{
    /// <summary>Nom d'une personne (hors patient) : civilité, prénom, nom, titre.</summary>
    public sealed class PersonName
    {
        /// <summary>Crée un nom.</summary>
        public PersonName(string family, string? given = null, string? prefix = null, string? suffix = null)
        {
            Family = family;
            Given = given;
            Prefix = prefix;
            Suffix = suffix;
        }

        /// <summary>Civilité (JDV_J245_Civilite_CISIS : <c>M</c>, <c>MME</c>…).</summary>
        public string? Prefix { get; set; }

        /// <summary>Prénom.</summary>
        public string? Given { get; set; }

        /// <summary>Nom de famille.</summary>
        public string Family { get; set; }

        /// <summary>Titre (JDV_J246_Titre_CISIS : <c>DR</c>, <c>PR</c>…).</summary>
        public string? Suffix { get; set; }
    }
}
