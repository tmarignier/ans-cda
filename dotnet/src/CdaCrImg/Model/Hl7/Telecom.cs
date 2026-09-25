using System;

namespace CdaCrImg.Model.Hl7
{
    /// <summary>Coordonnée de télécommunication (type TEL), ex. <c>tel:0102030405</c>, <c>mailto:x@y.fr</c>.</summary>
    public sealed class Telecom
    {
        /// <summary>Crée une coordonnée.</summary>
        /// <param name="value">URI : <c>tel:</c>, <c>fax:</c>, <c>mailto:</c>, <c>http:</c>…</param>
        /// <param name="use">Usage : <c>H</c>, <c>WP</c>, <c>MC</c> (mobile)…</param>
        public Telecom(string value, string? use = null)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("La valeur est obligatoire.", nameof(value));
            Value = value;
            Use = use;
        }

        /// <summary>URI.</summary>
        public string Value { get; }

        /// <summary>Usage.</summary>
        public string? Use { get; }

        /// <summary>Téléphone.</summary>
        public static Telecom Phone(string number, string? use = null) => new Telecom("tel:" + number, use);

        /// <summary>Messagerie.</summary>
        public static Telecom Email(string address, string? use = null) => new Telecom("mailto:" + address, use);
    }
}
