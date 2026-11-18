using System.Collections;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;
using CdaCrImg.Referentiel;
using CdaCrImg.Serialization;
using CdaCrImg.Validation;

namespace CdaCrImg.Tests;

/// <summary>
/// Le référentiel des champs dit vrai : son caractère obligatoire / facultatif correspond à
/// <see cref="CrImgValidator"/>, ses chemins aux propriétés du modèle, et ses XPath et cardinalités aux
/// documents produits par <see cref="CrImgWriter"/> et aux exemples ANS.
/// </summary>
public class ReferentielChampsTests
{
    private static readonly IReadOnlyList<ChampCda> Champs = ReferentielChamps.Champs;

    public static TheoryData<string> Chemins(Func<ChampCda, bool> filtre)
    {
        var data = new TheoryData<string>();
        foreach (var champ in Champs.Where(filtre)) data.Add(champ.Chemin);
        return data;
    }

    public static TheoryData<string> Obligatoires() =>
        Chemins(c => c.Origine == OrigineValeur.Appelant && c.Obligation == Obligation.Obligatoire);

    public static TheoryData<string> Conditionnels() =>
        Chemins(c => c.Origine == OrigineValeur.Appelant && c.Obligation == Obligation.ObligatoireSiGroupe);

    public static TheoryData<string> Facultatifs() =>
        Chemins(c => c.Origine == OrigineValeur.Appelant && c.Obligation == Obligation.Facultatif);

    public static TheoryData<string> ParDefaut() => Chemins(c => c.Origine == OrigineValeur.ValeurParDefaut);

    [Fact]
    public void Structure_IsConsistent()
    {
        Assert.Equal(Champs.Count, Champs.Select(c => c.Chemin).Distinct().Count());
        var resolveur = ReferentielChamps.CreerResolveurEspaces();
        for (var i = 0; i < Champs.Count; i++)
        {
            var champ = Champs[i];
            Assert.Contains(champ.Section, ReferentielChamps.Sections.Toutes);
            Assert.False(string.IsNullOrWhiteSpace(champ.Libelle), champ.Chemin);
            Assert.False(string.IsNullOrWhiteSpace(champ.Description), champ.Chemin);
            Assert.True(champ.Max == null || champ.Max >= Math.Max(champ.Min, 1), champ.Chemin);
            Assert.True(champ.Origine == OrigineValeur.Appelant || champ.Min > 0, champ.Chemin);
            Assert.True(champ.Origine != OrigineValeur.ValeurParDefaut || champ.ValeurParDefaut != null, champ.Chemin);
            XPathExpression.Compile(champ.XPathComplet, resolveur);
            if (champ.Parent != null)
            {
                Assert.True(Champs.Take(i).Contains(champ.Parent), $"{champ.Chemin} : parent déclaré après l'enfant.");
                Assert.StartsWith(champ.Parent.Chemin + ".", champ.Chemin);
                Assert.Contains(champ, champ.Parent.Enfants);
            }
            Assert.Equal(champ.Obligation == Obligation.ObligatoireSiGroupe, champ.Groupe != null);
        }
        Assert.Same(Champs.Single(c => c.Chemin == "Patient.Ins"), ReferentielChamps.TrouverParChemin("Patient.Ins"));
        Assert.Same(Champs.Single(c => c.Chemin == "Auteurs[].Horodatage"), ReferentielChamps.TrouverParChemin("Auteurs[3].Horodatage"));
        Assert.Same(Champs.Single(c => c.Chemin == "Auteurs[]"), ReferentielChamps.TrouverParChemin("Auteurs"));
    }

    [Theory]
    [MemberData(nameof(Obligatoires))]
    public void RequiredField_IsRequiredByValidator(string chemin)
    {
        var cr = SampleReports.Complete();
        Vider(cr, chemin);

        Assert.Contains(CrImgValidator.Validate(cr), i => Concerne(i, chemin));
    }

    [Theory]
    [MemberData(nameof(Conditionnels))]
    public void ConditionalField_IsRequiredOnlyWhenItsGroupIsFilled(string chemin)
    {
        var groupe = ReferentielChamps.Trouver(chemin)!.Groupe!;

        var sansChamp = SampleReports.Complete();
        Vider(sansChamp, chemin);
        Assert.Contains(CrImgValidator.Validate(sansChamp), i => Concerne(i, chemin));

        var sansGroupe = SampleReports.Complete();
        Vider(sansGroupe, groupe.Chemin);
        Assert.Empty(CrImgValidator.Validate(sansGroupe));
    }

    [Theory]
    [MemberData(nameof(Facultatifs))]
    public void OptionalField_CanBeOmitted(string chemin)
    {
        var cr = SampleReports.Complete();
        Vider(cr, chemin);

        Assert.Empty(CrImgValidator.Validate(cr));
    }

    [Theory]
    [MemberData(nameof(ParDefaut))]
    public void DefaultValue_IsTheModelDefault(string chemin)
    {
        var valeur = Lire(new CompteRenduImagerie(), chemin);

        Assert.Equal(ReferentielChamps.Trouver(chemin)!.ValeurParDefaut, valeur is Code code ? code.Value : Convert.ToString(valeur, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void EveryIssueOfAnEmptyReport_IsDescribed()
    {
        Assert.All(CrImgValidator.Validate(new CompteRenduImagerie()), i => Assert.NotNull(ReferentielChamps.TrouverParChemin(i.Path)));
    }

    [Fact]
    public void CompleteDocument_ContainsEveryFieldWithinCardinality()
    {
        var cda = CrImgWriter.Write(SampleReports.Complete());

        AssertCardinalites(cda);
        Assert.All(Champs, c => Assert.True(Selectionner(cda.Root!, c.XPathComplet).Any(), $"{c.Chemin} absent du document complet."));
    }

    [Fact]
    public void MinimalDocument_ContainsRequiredFieldsWithinCardinality()
    {
        AssertCardinalites(CrImgWriter.Write(SampleReports.Minimal()));
    }

    [Fact]
    public void AnsLevel1Example_ContainsRequiredFieldsWithinCardinality()
    {
        AssertCardinalites(XDocument.Load(RepoPaths.CrImgLevel1Example));
    }

    /// <summary>Pour chaque occurrence du parent, le nombre d'occurrences du champ respecte sa cardinalité.</summary>
    private static void AssertCardinalites(XDocument cda)
    {
        var erreurs = new List<string>();
        foreach (var champ in Champs)
        {
            var parents = champ.Parent == null ? new object[] { cda.Root! } : Selectionner(cda.Root!, champ.Parent.XPathComplet).ToArray();
            foreach (var parent in parents.OfType<XElement>())
            {
                var n = Selectionner(parent, champ.XPath).Count();
                if (n < champ.Min || (champ.Max != null && n > champ.Max))
                    erreurs.Add($"{champ.Chemin} : {n} occurrence(s), cardinalité {champ.Cardinalite}.");
            }
        }
        Assert.Empty(erreurs);
    }

    private static IEnumerable<object> Selectionner(XElement contexte, string xPath) =>
        ((IEnumerable)contexte.XPathEvaluate(xPath, ReferentielChamps.CreerResolveurEspaces())).Cast<object>();

    /// <summary>La non-conformité porte sur le champ, sur l'un de ses enfants ou sur la donnée qui le contient.</summary>
    private static bool Concerne(ValidationIssue issue, string chemin)
    {
        static string Normaliser(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"\[\d*\]", "");
        var path = Normaliser(issue.Path);
        var champ = Normaliser(chemin);
        return path == champ || champ.StartsWith(path + ".", StringComparison.Ordinal) || path.StartsWith(champ + ".", StringComparison.Ordinal);
    }

    private static object? Lire(object racine, string chemin)
    {
        var (objet, propriete, _) = Naviguer(racine, chemin);
        return propriete.GetValue(objet);
    }

    /// <summary>
    /// Vide la donnée du chemin (premier élément des listes) : liste vidée, référence ou valeur nullable à
    /// <c>null</c>, valeur par défaut sinon ; <c>X.DisplayName</c> retire le libellé du code <c>X</c>.
    /// </summary>
    private static void Vider(CompteRenduImagerie cr, string chemin)
    {
        if (chemin.EndsWith(".DisplayName", StringComparison.Ordinal))
        {
            var cheminCode = chemin[..^".DisplayName".Length];
            var (o, p, _) = Naviguer(cr, cheminCode);
            var code = (Code)p.GetValue(o)!;
            p.SetValue(o, new Code(code.Value, code.CodeSystem, null, code.CodeSystemName));
            return;
        }

        var (objet, propriete, liste) = Naviguer(cr, chemin);
        var type = propriete.PropertyType;
        if (liste || propriete.GetValue(objet) is IList)
            ((IList)propriete.GetValue(objet)!).Clear();
        else if (type == typeof(string))
            propriete.SetValue(objet, new NullabilityInfoContext().Create(propriete).WriteState == NullabilityState.Nullable ? null : "");
        else if (type == typeof(Sexe))
            propriete.SetValue(objet, Sexe.Inconnu);
        else if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            propriete.SetValue(objet, Activator.CreateInstance(type));
        else
            propriete.SetValue(objet, null);
    }

    /// <summary>Objet et propriété désignés par le chemin ; <c>Liste</c> si le dernier segment est une liste (<c>X[]</c>).</summary>
    private static (object Objet, PropertyInfo Propriete, bool Liste) Naviguer(object racine, string chemin)
    {
        var segments = chemin.Split('.');
        var objet = racine;
        for (var i = 0; ; i++)
        {
            var nom = segments[i].TrimEnd('[', ']');
            var propriete = objet.GetType().GetProperty(nom)
                ?? throw new InvalidOperationException($"{chemin} : propriété {nom} absente de {objet.GetType().Name}.");
            var liste = segments[i].EndsWith("[]", StringComparison.Ordinal);
            if (i == segments.Length - 1) return (objet, propriete, liste);
            var valeur = propriete.GetValue(objet) ?? throw new InvalidOperationException($"{chemin} : {nom} non renseigné.");
            objet = liste ? ((IList)valeur)[0]! : valeur;
        }
    }
}
