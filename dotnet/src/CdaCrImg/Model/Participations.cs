using System;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Model
{
    /// <summary>Auteur du document (<c>author</c>) : l'imageur qui réalise le CR.</summary>
    public sealed class Auteur
    {
        /// <summary>Crée un auteur.</summary>
        public Auteur(Professionnel professionnel, DateTimeOffset horodatage)
        {
            Professionnel = professionnel;
            Horodatage = horodatage;
        }

        /// <summary>Professionnel auteur ; son organisation est obligatoire pour le CR d'imagerie.</summary>
        public Professionnel Professionnel { get; set; }

        /// <summary>Date de rédaction / validation par l'auteur.</summary>
        public DateTimeOffset Horodatage { get; set; }

        /// <summary>Rôle fonctionnel (<c>functionCode</c>, optionnel), ex. <c>ATTPHYS</c>.</summary>
        public Code? Fonction { get; set; }
    }

    /// <summary>Signature (<c>legalAuthenticator</c>) : responsable du document.</summary>
    public sealed class Signature
    {
        /// <summary>Crée une signature.</summary>
        public Signature(Professionnel professionnel, DateTimeOffset horodatage)
        {
            Professionnel = professionnel;
            Horodatage = horodatage;
        }

        /// <summary>Signataire (en téléradiologie : médecin responsable de la structure qui accueille le patient).</summary>
        public Professionnel Professionnel { get; set; }

        /// <summary>Date de signature.</summary>
        public DateTimeOffset Horodatage { get; set; }
    }

    /// <summary>Médecin demandeur d'examens d'imagerie (<c>participant typeCode="REF"</c>).</summary>
    public sealed class MedecinDemandeur
    {
        /// <summary>Crée un médecin demandeur.</summary>
        public MedecinDemandeur(Professionnel professionnel, DateTimeOffset? dateDemande = null)
        {
            Professionnel = professionnel;
            DateDemande = dateDemande;
        }

        /// <summary>Professionnel demandeur.</summary>
        public Professionnel Professionnel { get; set; }

        /// <summary>Date de la demande (optionnelle ; inconnue sinon).</summary>
        public DateTimeOffset? DateDemande { get; set; }
    }
}
