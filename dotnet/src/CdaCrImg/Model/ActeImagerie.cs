using System;
using System.Collections.Generic;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Model
{
    /// <summary>
    /// Acte d'imagerie documenté (un <c>documentationOf/serviceEvent</c> par acte). Son code LOINC est aussi
    /// reporté en <c>translation</c> du code du document.
    /// </summary>
    public sealed class ActeImagerie
    {
        /// <summary>DICOM Study Instance UID de l'examen (identifiant racine seul, sans extension).</summary>
        public string StudyInstanceUid { get; set; } = "";

        /// <summary>Code LOINC de l'acte (jdv-code-document-imagerie-cisis, 1.2.250.1.213.1.1.5.687).</summary>
        public Code? Code { get; set; }

        /// <summary>Code CCAM de l'acte (optionnel).</summary>
        public Code? CodeCcam { get; set; }

        /// <summary>Modalités d'acquisition [1..*] (<see cref="Code.Dcm"/>, jdv-modalite-acquisition-cisis), ex. <c>CT</c>.</summary>
        public IList<Code> Modalites { get; } = new List<Code>();

        /// <summary>Régions anatomiques [1..*] (<see cref="Code.Snomed"/>, jdv-region-anatomique-cisis).</summary>
        public IList<Code> RegionsAnatomiques { get; } = new List<Code>();

        /// <summary>Début de réalisation de l'acte.</summary>
        public DateTimeOffset? Debut { get; set; }

        /// <summary>Fin de réalisation de l'acte (optionnelle).</summary>
        public DateTimeOffset? Fin { get; set; }

        /// <summary>PS exécutant (radiologue) ; l'identifiant de son organisation est obligatoire (DRIM-Box).</summary>
        public Professionnel? Executant { get; set; }
    }
}
