using CdaCrImg.Terminologies;

namespace CdaCrImg.Validation
{
    /// <summary>Options de <see cref="CrImgValidator"/>.</summary>
    public sealed class CrImgValidationOptions
    {
        /// <summary>Options par défaut (nouvelle instance) : terminologies contrôlées avec les jeux de valeurs embarqués.</summary>
        public static CrImgValidationOptions Defaut => new CrImgValidationOptions();

        /// <summary>
        /// Contrôle des codes par les jeux de valeurs du CI-SIS (<see cref="JeuxDeValeursCisis"/>) :
        /// professions, secteurs d'activité, cadre d'exercice, civilités, modalités, régions… Vrai par défaut.
        /// </summary>
        public bool ControlerTerminologies { get; set; } = true;

        /// <summary>
        /// Jeu de valeurs des codes LOINC d'actes (jdv-code-document-imagerie-cisis, 1.2.250.1.213.1.1.5.687),
        /// non embarqué pour sa taille : s'il est fourni (<see cref="JeuDeValeurs.Charger(string)"/>), le code de
        /// chaque acte y est contrôlé.
        /// </summary>
        public JeuDeValeurs? ActesImagerie { get; set; }
    }
}
