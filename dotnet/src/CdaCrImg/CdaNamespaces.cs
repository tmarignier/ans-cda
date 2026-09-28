using System.Xml.Linq;

namespace CdaCrImg
{
    /// <summary>Espaces de noms XML utilisés par un document CDA IMG-CR-IMG.</summary>
    public static class CdaNamespaces
    {
        /// <summary>HL7 v3 / CDA R2 (espace de noms par défaut du document).</summary>
        public static readonly XNamespace Hl7 = "urn:hl7-org:v3";

        /// <summary>XML Schema instance (xsi:type, xsi:schemaLocation).</summary>
        public static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

        /// <summary>Extensions DICOM PS3.20 (ps3-20:accessionNumber dans inFulfillmentOf/order).</summary>
        public static readonly XNamespace Ps320 = "urn:dicom-org:ps3-20";
    }
}
