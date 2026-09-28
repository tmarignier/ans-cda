namespace CdaCrImg.Demo.Form;

/// <summary>Valeur d'une liste déroulante, issue d'un jeu de valeurs ANS.</summary>
public sealed record Option(string Code, string CodeSystem, string Label);

/// <summary>Type de saisie d'un champ.</summary>
public enum FieldKind
{
    Text,
    Number,
    Date,
    DateTime,
    Select,
    Checkbox,
    File,
}

/// <summary>Caractère obligatoire d'un champ, tel qu'imposé par la librairie CdaCrImg.</summary>
public enum Requirement
{
    /// <summary>Toujours obligatoire.</summary>
    Required,

    /// <summary>Facultatif.</summary>
    Optional,

    /// <summary>Obligatoire dès qu'un autre champ du même groupe (ou d'un sous-groupe) est renseigné.</summary>
    RequiredIfGroup,
}

/// <summary>Description d'un champ du formulaire.</summary>
/// <param name="Key">Nom du champ HTML.</param>
/// <param name="Section">Section du formulaire.</param>
/// <param name="Label">Libellé.</param>
/// <param name="Requirement">Obligatoire, facultatif ou obligatoire si le groupe est renseigné.</param>
/// <param name="Description">Description métier et technique.</param>
/// <param name="Example">Valeur d'exemple pré-remplie.</param>
/// <param name="ModelPath">Propriété CdaCrImg correspondante (chemin utilisé par <c>CrImgValidator</c>).</param>
/// <param name="Kind">Type de saisie.</param>
/// <param name="Options">Valeurs possibles (listes déroulantes).</param>
/// <param name="Group">Groupe de champs facultatif (ex. <c>demandeur</c>, <c>demandeur.identite</c>).</param>
/// <param name="GroupLabel">Libellé du groupe, pour les champs <see cref="Requirement.RequiredIfGroup"/>.</param>
public sealed record FieldDefinition(
    string Key,
    string Section,
    string Label,
    Requirement Requirement,
    string Description,
    string Example,
    string ModelPath,
    FieldKind Kind = FieldKind.Text,
    IReadOnlyList<Option>? Options = null,
    string? Group = null,
    string? GroupLabel = null)
{
    /// <summary>Mention affichée à côté du libellé.</summary>
    public string RequirementLabel => Requirement switch
    {
        Requirement.Required => "Obligatoire",
        Requirement.Optional => "Facultatif",
        _ => $"Obligatoire si « {GroupLabel} » est renseigné",
    };

    /// <summary>Vrai si le champ appartient au groupe <paramref name="group"/> ou à l'un de ses sous-groupes.</summary>
    public bool IsInGroup(string group) =>
        Group != null && (Group == group || Group.StartsWith(group + ".", StringComparison.Ordinal));
}
