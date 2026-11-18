using System;
using System.Collections.Generic;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Model
{
    /// <summary>Compte rendu d'imagerie médicale (volet CI-SIS IMG-CR-IMG 2024.01).</summary>
    public sealed class CompteRenduImagerie
    {
        /// <summary>Identifiant unique de cette version du document.</summary>
        public Identifier? Id { get; set; }

        /// <summary>Identifiant commun à toutes les versions du document.</summary>
        public Identifier? SetId { get; set; }

        /// <summary>Numéro de version (1 pour la première).</summary>
        public int NumeroVersion { get; set; } = 1;

        /// <summary>Identifiant du document remplacé par cette version (<c>relatedDocument typeCode="RPLC"</c>).</summary>
        public Identifier? DocumentRemplace { get; set; }

        /// <summary>Titre du document.</summary>
        public string Titre { get; set; } = "";

        /// <summary>Date de création du document.</summary>
        public DateTimeOffset DateCreation { get; set; }

        /// <summary>Niveau de confidentialité (défaut : <c>N</c> normal).</summary>
        public Code Confidentialite { get; set; } = new Code("N", CodeSystems.Hl7Confidentiality, "Normal");

        /// <summary>Langue (défaut : <c>fr-FR</c>).</summary>
        public string Langue { get; set; } = "fr-FR";

        /// <summary>Patient.</summary>
        public Patient? Patient { get; set; }

        /// <summary>Auteurs [1..*] (en téléradiologie, ajouter le médecin responsable de la structure d'accueil).</summary>
        public IList<Auteur> Auteurs { get; } = new List<Auteur>();

        /// <summary>Structure chargée de la conservation du document.</summary>
        public Organisation? Custodian { get; set; }

        /// <summary>Responsable du document.</summary>
        public Signature? SignataireLegal { get; set; }

        /// <summary>Médecins demandeurs [0..*].</summary>
        public IList<MedecinDemandeur> MedecinsDemandeurs { get; } = new List<MedecinDemandeur>();

        /// <summary>Demandes d'examen honorées [1..*].</summary>
        public IList<DemandeImagerie> Demandes { get; } = new List<DemandeImagerie>();

        /// <summary>Actes d'imagerie documentés [1..*].</summary>
        public IList<ActeImagerie> Actes { get; } = new List<ActeImagerie>();

        /// <summary>Examen réalisé dans le cadre d'un dépistage (<c>documentationOf</c> CIM-10 <c>Z13.9</c>).</summary>
        public bool Depistage { get; set; }

        /// <summary>Contexte de la prise en charge.</summary>
        public PriseEnCharge? PriseEnCharge { get; set; }

        /// <summary>Corps du document.</summary>
        public CorpsDocument? Corps { get; set; }
    }
}
