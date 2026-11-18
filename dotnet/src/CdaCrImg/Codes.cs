namespace CdaCrImg
{
    /// <summary>Codes imposés par le volet IMG-CR-IMG (code + système). Voir docs/cr-img/specification.md.</summary>
    public static class Codes
    {
        /// <summary>ClinicalDocument/code : LOINC 18748-4 "CR d'imagerie médicale".</summary>
        public const string DocumentType = "18748-4";

        /// <summary>Codes LOINC des sections.</summary>
        public static class Sections
        {
            /// <summary>Addendum.</summary>
            public const string Addendum = "55107-7";
            /// <summary>Informations cliniques.</summary>
            public const string InformationsCliniques = "55752-0";
            /// <summary>Demande d'examen.</summary>
            public const string DemandeExamen = "55115-0";
            /// <summary>Historique médical.</summary>
            public const string HistoriqueMedical = "11329-0";
            /// <summary>Acte d'imagerie (Technique d'imagerie).</summary>
            public const string ActeImagerie = "55111-9";
            /// <summary>Complications.</summary>
            public const string Complications = "55109-3";
            /// <summary>Exposition aux rayonnements.</summary>
            public const string ExpositionRadiations = "73569-6";
            /// <summary>Dispositifs médicaux.</summary>
            public const string DispositifsMedicaux = "46264-8";
            /// <summary>Résultats.</summary>
            public const string Resultats = "59776-5";
            /// <summary>Résultats d'examens (non codés).</summary>
            public const string ResultatsExamensNonCode = "30954-2";
            /// <summary>Examen comparatif.</summary>
            public const string ExamenComparatif = "18834-2";
            /// <summary>Conclusion.</summary>
            public const string Conclusion = "19005-8";
            /// <summary>Commentaire non codé.</summary>
            public const string CommentaireNonCode = "55112-7";
            /// <summary>Documents ajoutés.</summary>
            public const string DocumentsAjoutes = "55107-7";
            /// <summary>Éducation du patient.</summary>
            public const string EducationPatient = "34895-3";
            /// <summary>Catalogue d'objets DICOM (système DCM, pas LOINC).</summary>
            public const string CatalogueObjetsDcm = "121181";
        }

        /// <summary>Codes des entrées et qualifiers.</summary>
        public static class Entries
        {
            /// <summary>DCM 121139 "Modalité" : qualifier/name obligatoire sur la translation de modalité (serviceEvent) et sur la série.</summary>
            public const string DcmModality = "121139";
            /// <summary>LOINC 39111-0 "Localisation anatomique" : qualifier/name de la région anatomique (serviceEvent).</summary>
            public const string LoincAnatomicLocation = "39111-0";
            /// <summary>SNOMED 106233006 : modificateur topographique (targetSiteCode/qualifier/name).</summary>
            public const string SnomedTopographicalModifier = "106233006";
            /// <summary>DCM 113014 "Examen" (Study).</summary>
            public const string DcmStudy = "113014";
            /// <summary>DCM 113015 "Séries".</summary>
            public const string DcmSeries = "113015";
            /// <summary>DCM 121290 "Exposition du patient aux rayonnements ionisants".</summary>
            public const string DcmPatientExposure = "121290";
            /// <summary>DCM 113850 "Autorisation d'irradiation" (participantRole/code).</summary>
            public const string DcmIrradiationAuthorizing = "113850";
            /// <summary>SNOMED 364320009 "statut de grossesse".</summary>
            public const string SnomedPregnancyStatus = "364320009";
            /// <summary>SNOMED 440252007 "administration de produits radiopharmaceutiques".</summary>
            public const string SnomedRadiopharmaceuticalAdministration = "440252007";
            /// <summary>LOINC 11348-0 "Antécédents médicaux" [1..*].</summary>
            public const string LoincPastMedicalHistory = "11348-0";
            /// <summary>LOINC 47519-4 "Antécédents chirurgicaux" [1..*].</summary>
            public const string LoincPastSurgicalHistory = "47519-4";
            /// <summary>LOINC 64100-1 "Contre-indications" [0..*].</summary>
            public const string LoincContraindications = "64100-1";
            /// <summary>LOINC 48767-8 "Commentaire".</summary>
            public const string LoincComment = "48767-8";
            /// <summary>LOINC 99622-3 : observation de la section Éducation du patient.</summary>
            public const string LoincPatientEducation = "99622-3";
        }
    }
}
