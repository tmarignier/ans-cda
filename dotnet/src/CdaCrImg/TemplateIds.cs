namespace CdaCrImg
{
    /// <summary>
    /// templateId du volet IMG-CR-IMG 2024.01 (CI-SIS) et des standards qu'il contraint (DICOM PS3.20, IHE, CCD).
    /// Source de vérité : schematrons/CI-SIS_IMG-CR-IMG_2024.01.sch et ExemplesCDA/IMG_CR_IMG_2024.01.xml.
    /// Voir docs/cr-img/specification.md.
    /// </summary>
    public static class TemplateIds
    {
        /// <summary>En-tête du document.</summary>
        public static class Document
        {
            /// <summary>HL7 France.</summary>
            public const string Hl7France = "2.16.840.1.113883.2.8.2.1";
            /// <summary>CI-SIS (modèle générique).</summary>
            public const string CiSis = "1.2.250.1.213.1.1.1.1";
            /// <summary>DICOM PS3.20 : CDA Imaging Report.</summary>
            public const string DicomImagingReport = "1.2.840.10008.9.1";
            /// <summary>DICOM PS3.20 : General Header.</summary>
            public const string DicomGeneralHeader = "1.2.840.10008.9.20";
            /// <summary>DICOM PS3.20 : Imaging Header.</summary>
            public const string DicomImagingHeader = "1.2.840.10008.9.21";
            /// <summary>Modèle IMG-CR-IMG du CI-SIS (extension = <see cref="CrImgVersion"/>).</summary>
            public const string CrImg = "1.2.250.1.213.1.1.1.45";
            /// <summary>Version du volet portée par templateId/@extension.</summary>
            public const string CrImgVersion = "2024.01";
            /// <summary>IHE XDS-SD / CDA R2 Niveau 1 (corps non structuré, PDF).</summary>
            public const string NonStructuredBody = "1.3.6.1.4.1.19376.1.2.20";
        }

        /// <summary>Sections : chaque section porte le templateId standard puis le templateId CI-SIS.</summary>
        public static class Sections
        {
            /// <summary>FR-DICOM-Addendum [0..1] (+ DICOM 1.2.840.10008.9.6).</summary>
            public const string Addendum = "1.2.250.1.213.1.1.2.210";
            /// <summary>DICOM Addendum.</summary>
            public const string DicomAddendum = "1.2.840.10008.9.6";

            /// <summary>FR-DICOM-informations-cliniques [0..1] (+ DICOM 1.2.840.10008.9.2).</summary>
            public const string InformationsCliniques = "1.2.250.1.213.1.1.2.205";
            /// <summary>DICOM Clinical Information.</summary>
            public const string DicomClinicalInformation = "1.2.840.10008.9.2";

            /// <summary>Sous-section FR-DICOM-Demande-examen [1..1] de Informations cliniques.</summary>
            public const string DemandeExamen = "1.2.250.1.213.1.1.2.211";
            /// <summary>DICOM Request (testé par le schématron sur ce templateId).</summary>
            public const string DicomRequest = "1.2.840.10008.9.7";

            /// <summary>Sous-section FR-DICOM-Historique-medical [1..1] de Informations cliniques.</summary>
            public const string HistoriqueMedical = "1.2.250.1.213.1.1.2.213";
            /// <summary>C-CDA History of Past Illness (testé par le schématron sur ce templateId).</summary>
            public const string CcdaHistoryOfPastIllness = "2.16.840.1.113883.10.20.22.2.39";

            /// <summary>FR-DICOM-Acte-imagerie [1..*] (+ DICOM 1.2.840.10008.9.3).</summary>
            public const string ActeImagerie = "1.2.250.1.213.1.1.2.206";
            /// <summary>DICOM Imaging Procedure Description.</summary>
            public const string DicomProcedureDescription = "1.2.840.10008.9.3";

            /// <summary>Sous-section FR-DICOM-Complications [0..1] de Acte imagerie.</summary>
            public const string Complications = "1.2.250.1.213.1.1.2.214";
            /// <summary>C-CDA Complications.</summary>
            public const string CcdaComplications = "2.16.840.1.113883.10.20.22.2.37";

            /// <summary>Sous-section FR-DICOM-Exposition-aux-radiations [0..1] de Acte imagerie.</summary>
            public const string ExpositionRadiations = "1.2.250.1.213.1.1.2.215";
            /// <summary>DICOM Radiation Exposure and Protection Information.</summary>
            public const string DicomRadiationExposure = "1.2.840.10008.9.8";

            /// <summary>Sous-section FR-DICOM-Object-Catalog [1..1] de Acte imagerie.</summary>
            public const string CatalogueObjets = "1.2.250.1.213.1.1.2.217";
            /// <summary>DICOM Object Catalog (CDA DIR).</summary>
            public const string DicomObjectCatalog = "2.16.840.1.113883.10.20.6.1.1";

            /// <summary>FR-Dispositifs-medicaux [0..1].</summary>
            public const string DispositifsMedicaux = "1.2.250.1.213.1.1.2.1";

            /// <summary>FR-DICOM-Resultats [0..1].</summary>
            public const string Resultats = "1.2.250.1.213.1.1.2.208";
            /// <summary>DIR Findings.</summary>
            public const string DicomFindings = "2.16.840.1.113883.10.20.6.1.2";

            /// <summary>FR-Resultats-examens-non-code [0..1].</summary>
            public const string ResultatsExamensNonCode = "1.2.250.1.213.1.1.2.150";

            /// <summary>FR-DICOM-Examen-comparatif [0..1] (+ DICOM 1.2.840.10008.9.4).</summary>
            public const string ExamenComparatif = "1.2.250.1.213.1.1.2.207";
            /// <summary>DICOM Comparison Study.</summary>
            public const string DicomComparisonStudy = "1.2.840.10008.9.4";

            /// <summary>FR-DICOM-Conclusion [1..1] (+ DICOM 1.2.840.10008.9.5).</summary>
            public const string Conclusion = "1.2.250.1.213.1.1.2.209";
            /// <summary>DICOM Impression.</summary>
            public const string DicomImpression = "1.2.840.10008.9.5";

            /// <summary>FR-Commentaire-non-code [0..1].</summary>
            public const string CommentaireNonCode = "1.2.250.1.213.1.1.2.73";

            /// <summary>FR-Documents-ajoutes [0..1].</summary>
            public const string DocumentsAjoutes = "1.2.250.1.213.1.1.2.37";

            /// <summary>FR-Education-patient [0..1].</summary>
            public const string EducationPatient = "1.2.250.1.213.1.1.2.107";
        }

        /// <summary>Entrées.</summary>
        public static class Entries
        {
            /// <summary>FR-DICOM-Technique-imagerie (procedure) [1..*] dans Acte imagerie.</summary>
            public const string TechniqueImagerie = "1.2.250.1.213.1.1.3.153";
            /// <summary>DICOM Procedure Technique.</summary>
            public const string DicomProcedureTechnique = "1.2.840.10008.9.14";

            /// <summary>FR-DICOM-Administration-produit-de-sante (substanceAdministration) [0..*].</summary>
            public const string AdministrationProduitSante = "1.2.250.1.213.1.1.3.151";
            /// <summary>DICOM Procedural Medication.</summary>
            public const string DicomProceduralMedication = "1.2.840.10008.9.13";

            /// <summary>FR-DICOM-Administration-radiopharmaceutique [0..*] (code SNOMED 440252007).</summary>
            public const string AdministrationRadiopharmaceutique = "1.2.250.1.213.1.1.3.173";

            /// <summary>FR-DICOM-Exposition-patient (procedure, code DCM 121290) [0..1].</summary>
            public const string ExpositionPatient = "1.2.250.1.213.1.1.3.165";

            /// <summary>FR-DICOM-Observation (observation) : antécédents, contre-indications, grossesse.</summary>
            public const string Observation = "1.2.250.1.213.1.1.3.150";
            /// <summary>DIR Text Observation.</summary>
            public const string DirTextObservation = "2.16.840.1.113883.10.20.6.2.13";

            /// <summary>FR-DICOM-Quantite (observation, dose) [0..*].</summary>
            public const string Quantite = "1.2.250.1.213.1.1.3.154";
            /// <summary>DIR Quantity Measurement.</summary>
            public const string DirQuantityMeasurement = "2.16.840.1.113883.10.20.6.2.14";

            /// <summary>FR-DICOM-Examen-imagerie (act, code DCM 113014) dans le catalogue d'objets.</summary>
            public const string ExamenImagerie = "1.2.250.1.213.1.1.3.155";
            /// <summary>DICOM Study Act.</summary>
            public const string DicomStudyAct = "1.2.840.10008.9.16";

            /// <summary>FR-DICOM-Serie-imagerie (act, code DCM 113015) [1..*].</summary>
            public const string SerieImagerie = "1.2.250.1.213.1.1.3.156";
            /// <summary>DICOM Series Act.</summary>
            public const string DicomSeriesAct = "1.2.840.10008.9.17";

            /// <summary>FR-DICOM-SOP-instance-observation (observation classCode=DGIMG) [1..*].</summary>
            public const string SopInstance = "1.2.250.1.213.1.1.3.157";
            /// <summary>DICOM SOP Instance Observation.</summary>
            public const string DicomSopInstance = "1.2.840.10008.9.18";

            /// <summary>FR-Commentaire-ER (act, code LOINC 48767-8).</summary>
            public const string CommentaireEr = "1.2.250.1.213.1.1.3.32";
            /// <summary>CCD Comment.</summary>
            public const string CcdComment = "2.16.840.1.113883.10.20.1.40";
            /// <summary>IHE PCC Comment Entry.</summary>
            public const string IheCommentEntry = "1.3.6.1.4.1.19376.1.5.3.1.4.2";

            /// <summary>FR-Simple-Observation (IHE PCC 1.3.6.1.4.1.19376.1.5.3.1.4.13).</summary>
            public const string SimpleObservation = "1.2.250.1.213.1.1.3.48";
            /// <summary>IHE PCC Simple Observation.</summary>
            public const string IheSimpleObservation = "1.3.6.1.4.1.19376.1.5.3.1.4.13";
        }
    }
}
