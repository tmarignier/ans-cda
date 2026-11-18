using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Model
{
    /// <summary>Demande d'examen d'imagerie honorée par le CR (<c>inFulfillmentOf/order</c>).</summary>
    public sealed class DemandeImagerie
    {
        /// <summary>Crée une demande.</summary>
        /// <param name="numeroDemande">Order Placer Number attribué par le demandeur ; <see cref="Identifier.Null"/> s'il n'y a pas de demande dématérialisée.</param>
        /// <param name="accessionNumber">Accession Number (extension DICOM <c>ps3-20:accessionNumber</c>).</param>
        public DemandeImagerie(Identifier numeroDemande, Identifier accessionNumber)
        {
            NumeroDemande = numeroDemande;
            AccessionNumber = accessionNumber;
        }

        /// <summary>Order Placer Number.</summary>
        public Identifier NumeroDemande { get; set; }

        /// <summary>Accession Number.</summary>
        public Identifier AccessionNumber { get; set; }
    }
}
