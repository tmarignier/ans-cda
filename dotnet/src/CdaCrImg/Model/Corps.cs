using System;

namespace CdaCrImg.Model
{
    /// <summary>Corps du document CDA.</summary>
    public abstract class CorpsDocument
    {
        private protected CorpsDocument()
        {
        }
    }

    /// <summary>
    /// Corps non structuré (CDA R2 niveau 1) : le CR est un PDF encapsulé en base64 dans <c>nonXMLBody</c>.
    /// Le document porte alors le templateId IHE XDS-SD <c>1.3.6.1.4.1.19376.1.2.20</c> à la place de ceux du volet.
    /// </summary>
    public sealed class CorpsPdf : CorpsDocument
    {
        /// <summary>Crée un corps PDF.</summary>
        public CorpsPdf(byte[] pdf)
        {
            Pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));
        }

        /// <summary>Contenu du fichier PDF.</summary>
        public byte[] Pdf { get; }
    }
}
