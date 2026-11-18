using System;
using System.Collections.Generic;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Model
{
    /// <summary>Sexe administratif du patient (HL7 AdministrativeGender).</summary>
    public enum Sexe
    {
        /// <summary>Masculin (<c>M</c>).</summary>
        Masculin,
        /// <summary>Féminin (<c>F</c>).</summary>
        Feminin,
        /// <summary>Inconnu (<c>U</c>).</summary>
        Inconnu,
    }

    /// <summary>
    /// Patient (<c>recordTarget/patientRole</c>). La librairie ne gère que des patients identifiés par leur
    /// INS : l'INS et ses traits d'identité sont obligatoires (nom et prénoms de naissance, premier prénom,
    /// sexe, date et lieu de naissance).
    /// </summary>
    public sealed class Patient
    {
        /// <summary>
        /// Matricule INS (obligatoire : INS-NIR, INS-NIA ou leurs équivalents de test), ex.
        /// <c>new Identifier(IdentifierRoots.InsNir, "279035121518989")</c>.
        /// </summary>
        public Identifier? Ins { get; set; }

        /// <summary>Autres identifiants (IPP : root = OID de l'établissement).</summary>
        public IList<Identifier> AutresIdentifiants { get; } = new List<Identifier>();

        /// <summary>Nom de naissance (nom de l'acte de naissance).</summary>
        public string NomNaissance { get; set; } = "";

        /// <summary>Liste des prénoms de l'acte de naissance, séparés par des espaces.</summary>
        public string PrenomsNaissance { get; set; } = "";

        /// <summary>Premier prénom de l'acte de naissance.</summary>
        public string PremierPrenomNaissance { get; set; } = "";

        /// <summary>Nom utilisé (optionnel).</summary>
        public string? NomUtilise { get; set; }

        /// <summary>Prénom utilisé (optionnel).</summary>
        public string? PrenomUtilise { get; set; }

        /// <summary>Sexe (trait INS : masculin ou féminin).</summary>
        public Sexe Sexe { get; set; } = Sexe.Inconnu;

        /// <summary>Date de naissance.</summary>
        public DateTime? DateNaissance { get; set; }

        /// <summary>Code officiel géographique (COG INSEE) du lieu de naissance ; 99999 si inconnu/à l'étranger selon l'INS.</summary>
        public string? LieuNaissanceCog { get; set; }

        /// <summary>Commune de naissance (libellé, optionnel).</summary>
        public string? LieuNaissanceCommune { get; set; }

        /// <summary>Adresses.</summary>
        public IList<Address> Adresses { get; } = new List<Address>();

        /// <summary>Coordonnées télécom.</summary>
        public IList<Telecom> Telecoms { get; } = new List<Telecom>();
    }
}
