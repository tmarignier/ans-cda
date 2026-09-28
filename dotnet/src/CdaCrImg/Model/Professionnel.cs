using System.Collections.Generic;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Model
{
    /// <summary>Professionnel de santé (auteur, signataire, exécutant, médecin demandeur…).</summary>
    public sealed class Professionnel
    {
        /// <summary>Identifiant national (<see cref="Identifier.FromRpps"/>).</summary>
        public Identifier? Id { get; set; }

        /// <summary>Profession / spécialité (<see cref="Code.ProfessionSavoirFaire"/>), ex. <c>G15_10/SM44</c> radio-diagnostic.</summary>
        public Code? Profession { get; set; }

        /// <summary>Identité.</summary>
        public PersonName? Nom { get; set; }

        /// <summary>Adresses.</summary>
        public IList<Address> Adresses { get; } = new List<Address>();

        /// <summary>Coordonnées télécom.</summary>
        public IList<Telecom> Telecoms { get; } = new List<Telecom>();

        /// <summary>Structure d'exercice.</summary>
        public Organisation? Organisation { get; set; }
    }
}
