using CdaCrImg.Terminologies;

namespace CdaCrImg.Demo.Form;

/// <summary>
/// Listes déroulantes du formulaire, issues des jeux de valeurs CI-SIS embarqués dans la librairie
/// (<see cref="JeuxDeValeursCisis"/>) : le formulaire propose exactement les codes que la librairie accepte.
/// </summary>
public static class JeuxDeValeurs
{
    public static readonly IReadOnlyList<Option> SecteurActivite = From(JeuxDeValeursCisis.SecteurActivite, CodeSystems.SecteurActivite);
    public static readonly IReadOnlyList<Option> CadreExercice = From(JeuxDeValeursCisis.CadreExercice, CodeSystems.CadreExercice);
    public static readonly IReadOnlyList<Option> Confidentialite = From(JeuxDeValeursCisis.Confidentialite);
    public static readonly IReadOnlyList<Option> TypeRencontre = From(JeuxDeValeursCisis.TypeRencontre);
    public static readonly IReadOnlyList<Option> Civilite = From(JeuxDeValeursCisis.Civilite);
    public static readonly IReadOnlyList<Option> Titre = From(JeuxDeValeursCisis.Titre);
    public static readonly IReadOnlyList<Option> FonctionAuteur = From(JeuxDeValeursCisis.Fonction, CodeSystems.Hl7ParticipationFunction);
    public static readonly IReadOnlyList<Option> ModaliteAcquisition = From(JeuxDeValeursCisis.ModaliteAcquisition);
    public static readonly IReadOnlyList<Option> RegionAnatomique = From(JeuxDeValeursCisis.RegionAnatomique);

    /// <summary>Options d'un jeu de valeurs, éventuellement restreintes à un système de codage.</summary>
    private static IReadOnlyList<Option> From(JeuDeValeurs jdv, string? codeSystem = null) =>
        jdv.Concepts
            .Where(c => codeSystem == null || c.CodeSystem == codeSystem)
            .Select(c => new Option(c.Code, c.CodeSystem, c.DisplayName ?? c.Code))
            .ToList();
}
