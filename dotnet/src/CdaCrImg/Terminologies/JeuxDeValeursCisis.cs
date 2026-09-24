using System;
using System.Collections.Generic;
using System.Linq;

namespace CdaCrImg.Terminologies
{
    /// <summary>
    /// Jeux de valeurs du CI-SIS embarqués dans la librairie (copies des fichiers du kit ANS
    /// TestContenuCDA) : ceux que contrôle la structuration minimale pour l'en-tête, et ceux du volet
    /// IMG-CR-IMG pour les actes. Chargés à la première utilisation.
    /// </summary>
    /// <remarks>
    /// Le jeu des codes LOINC d'actes (jdv-code-document-imagerie-cisis, 1.2.250.1.213.1.1.5.687, ~7 000
    /// codes) n'est pas embarqué : le fournir via <c>CrImgValidationOptions.ActesImagerie</c>.
    /// </remarks>
    public static class JeuxDeValeursCisis
    {
        private static readonly Lazy<JeuDeValeurs> J01 = Embedded("1.2.250.1.213.1.1.5.461");
        private static readonly Lazy<JeuDeValeurs> J02 = Embedded("1.2.250.1.213.1.1.5.466");
        private static readonly Lazy<JeuDeValeurs> J04 = Embedded("1.2.250.1.213.1.1.5.467");
        private static readonly Lazy<JeuDeValeurs> J142 = Embedded("1.2.250.1.213.1.1.5.589");
        private static readonly Lazy<JeuDeValeurs> J47 = Embedded("1.2.250.1.213.1.1.5.124");
        private static readonly Lazy<JeuDeValeurs> J245 = Embedded("1.2.250.1.213.1.1.5.718");
        private static readonly Lazy<JeuDeValeurs> J246 = Embedded("1.2.250.1.213.1.1.5.719");
        private static readonly Lazy<JeuDeValeurs> Confidentiality = Embedded("2.16.840.1.113883.1.11.16926");
        private static readonly Lazy<JeuDeValeurs> Modalite = Embedded("1.2.250.1.213.1.1.5.618");
        private static readonly Lazy<JeuDeValeurs> Region = Embedded("1.2.250.1.213.1.1.5.695");

        /// <summary>JDV_J01_XdsAuthorSpecialty_CISIS : profession / savoir-faire des PS (code des PS).</summary>
        public static JeuDeValeurs ProfessionSavoirFaire => J01.Value;

        /// <summary>JDV_J02_XdsHealthcareFacilityTypeCode_CISIS : cadre d'exercice (healthCareFacility/code).</summary>
        public static JeuDeValeurs CadreExercice => J02.Value;

        /// <summary>JDV_J04_XdsPracticeSettingCode_CISIS : secteur d'activité (standardIndustryClassCode).</summary>
        public static JeuDeValeurs SecteurActivite => J04.Value;

        /// <summary>JDV_J142_TypeRencontre_CISIS : type de prise en charge (encompassingEncounter/code).</summary>
        public static JeuDeValeurs TypeRencontre => J142.Value;

        /// <summary>JDV_J47_FunctionCode_CISIS : rôle fonctionnel (functionCode).</summary>
        public static JeuDeValeurs Fonction => J47.Value;

        /// <summary>JDV_J245_Civilite_CISIS : civilité (name/prefix).</summary>
        public static JeuDeValeurs Civilite => J245.Value;

        /// <summary>JDV_J246_Titre_CISIS : titre (name/suffix).</summary>
        public static JeuDeValeurs Titre => J246.Value;

        /// <summary>jdv-hl7-v3-xBasicConfidentialityKind-cisis : confidentialité (confidentialityCode).</summary>
        public static JeuDeValeurs Confidentialite => Confidentiality.Value;

        /// <summary>jdv-modalite-acquisition-cisis : modalités d'acquisition DICOM.</summary>
        public static JeuDeValeurs ModaliteAcquisition => Modalite.Value;

        /// <summary>jdv-region-anatomique-cisis : régions anatomiques (SNOMED CT).</summary>
        public static JeuDeValeurs RegionAnatomique => Region.Value;

        /// <summary>Tous les jeux de valeurs embarqués.</summary>
        public static IReadOnlyList<JeuDeValeurs> Tous => new[]
        {
            ProfessionSavoirFaire, CadreExercice, SecteurActivite, TypeRencontre, Fonction, Civilite, Titre,
            Confidentialite, ModaliteAcquisition, RegionAnatomique,
        };

        private static Lazy<JeuDeValeurs> Embedded(string oid) => new Lazy<JeuDeValeurs>(() =>
        {
            var name = $"CdaCrImg.Terminologies.{oid}.xml";
            using (var stream = typeof(JeuxDeValeursCisis).Assembly.GetManifestResourceStream(name)
                                ?? throw new InvalidOperationException($"Ressource {name} absente de la librairie."))
            {
                return JeuDeValeurs.Charger(stream);
            }
        });
    }
}
