using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CdaCrImg.Demo.Form;

/// <summary>Exemple de compte rendu proposé pour pré-remplir le formulaire.</summary>
/// <param name="Id">Identifiant de l'exemple (nom du fichier CDA sans extension, ex. <c>CR_C</c>).</param>
/// <param name="Libelle">Libellé affiché dans la liste de sélection.</param>
/// <param name="Values">Valeurs des champs du formulaire (clé du champ → valeur).</param>
/// <param name="Pdf">PDF encapsulé dans le CDA d'exemple, utilisé si aucun fichier n'est transmis.</param>
public sealed record ExempleCda(string Id, string Libelle, IReadOnlyDictionary<string, string?> Values, byte[]? Pdf)
{
    /// <summary>Valeurs de tous les champs du formulaire (vides pour les données absentes de l'exemple).</summary>
    public Dictionary<string, string?> FormValues() =>
        FormCatalog.Fields.Where(f => f.Kind != FieldKind.File).ToDictionary(f => f.Key, f => Values.GetValueOrDefault(f.Key));
}

/// <summary>
/// Exemples de comptes rendus de la démonstration : chaque CDA de <c>Resources/*.xml</c> (ressource
/// embarquée) est relu pour pré-remplir le formulaire. Pour ajouter un exemple, déposer le fichier CDA
/// dans <c>Resources/</c>. La lecture ne couvre que les données saisissables dans le formulaire ; elle
/// est propre à la démonstration (la librairie CdaCrImg ne fait que produire des CDA).
/// </summary>
public static class ExemplesCda
{
    private const string ResourcePrefix = "CdaCrImg.Demo.Exemples.";
    private static readonly XNamespace Hl7 = "urn:hl7-org:v3";
    private static readonly XNamespace Dicom = "urn:dicom-org:ps3-20";
    private static readonly Lazy<IReadOnlyList<ExempleCda>> Loaded = new(Load);

    /// <summary>Exemples disponibles, triés par identifiant.</summary>
    public static IReadOnlyList<ExempleCda> All => Loaded.Value;

    /// <summary>Exemple d'identifiant <paramref name="id"/>, ou <c>null</c>.</summary>
    public static ExempleCda? Find(string? id) =>
        string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// CDA de l'exemple <paramref name="id"/> sans ses commentaires (UTF-8, indenté), pour le comparer au CDA
    /// produit par la librairie ; <c>null</c> si l'exemple n'existe pas. Le fichier d'exemple n'est pas modifié.
    /// </summary>
    public static byte[]? CdaSansCommentaires(string? id)
    {
        var assembly = typeof(ExemplesCda).Assembly;
        var resource = Find(id) == null ? null : assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Equals(ResourcePrefix + id + ".xml", StringComparison.OrdinalIgnoreCase));
        if (resource == null) return null;

        XDocument cda;
        using (var stream = assembly.GetManifestResourceStream(resource)!)
            cda = XDocument.Load(stream);
        cda.DescendantNodes().OfType<XComment>().ToList().ForEach(c => c.Remove());

        using var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
            cda.Save(writer);
        return output.ToArray();
    }

    private static IReadOnlyList<ExempleCda> Load()
    {
        var assembly = typeof(ExemplesCda).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Select(n => Read(n[ResourcePrefix.Length..^".xml".Length], assembly, n))
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static ExempleCda Read(string id, Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)!;
        return Parse(id, XDocument.Load(stream));
    }

    /// <summary>Relit un CDA d'imagerie et en extrait les valeurs du formulaire et le PDF encapsulé.</summary>
    public static ExempleCda Parse(string id, XDocument cda)
    {
        var values = new Dictionary<string, string?>();
        var doc = cda.Root!;

        void Set(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) values[key] = value.Trim();
        }

        void Person(string prefix, XElement? name)
        {
            Set($"{prefix}.civilite", name?.Element(Hl7 + "prefix")?.Value);
            Set($"{prefix}.prenom", name?.Element(Hl7 + "given")?.Value);
            Set($"{prefix}.nom", name?.Element(Hl7 + "family")?.Value);
            Set($"{prefix}.titre", name?.Element(Hl7 + "suffix")?.Value);
        }

        // Document
        Set("document.id", Root(doc.Element(Hl7 + "id")));
        Set("document.setId", Root(doc.Element(Hl7 + "setId")));
        Set("document.version", Attr(doc.Element(Hl7 + "versionNumber"), "value"));
        Set("document.remplace", Root(doc.Elements(Hl7 + "relatedDocument").Elements(Hl7 + "parentDocument").Elements(Hl7 + "id").FirstOrDefault()));
        Set("document.titre", doc.Element(Hl7 + "title")?.Value);
        Set("document.dateCreation", DateTime(Attr(doc.Element(Hl7 + "effectiveTime"), "value")));
        Set("document.confidentialite", Attr(doc.Element(Hl7 + "confidentialityCode"), "code"));
        Set("document.langue", Attr(doc.Element(Hl7 + "languageCode"), "code"));

        // Patient
        var patientRole = doc.Element(Hl7 + "recordTarget")?.Element(Hl7 + "patientRole");
        var ids = patientRole?.Elements(Hl7 + "id").ToList() ?? new List<XElement>();
        var ins = ids.FirstOrDefault(i => IsIns(Root(i)));
        Set("patient.insType", Root(ins));
        Set("patient.insMatricule", Attr(ins, "extension"));
        var ipp = ids.FirstOrDefault(i => i != ins && Attr(i, "nullFlavor") == null);
        Set("patient.ippRoot", Root(ipp));
        Set("patient.ippValeur", Attr(ipp, "extension"));
        var adresse = patientRole?.Element(Hl7 + "addr");
        Set("patient.adresseNumero", adresse?.Element(Hl7 + "houseNumber")?.Value);
        Set("patient.adresseVoie", adresse?.Element(Hl7 + "streetName")?.Value ?? adresse?.Element(Hl7 + "streetAddressLine")?.Value);
        Set("patient.adresseCodePostal", adresse?.Element(Hl7 + "postalCode")?.Value);
        Set("patient.adresseVille", adresse?.Element(Hl7 + "city")?.Value);
        Set("patient.telephone", Phone(patientRole?.Elements(Hl7 + "telecom"), "H"));
        var patient = patientRole?.Element(Hl7 + "patient");
        var names = patient?.Elements(Hl7 + "name").Elements().ToList() ?? new List<XElement>();
        XElement? NamePart(string part, string? qualifier) =>
            names.FirstOrDefault(e => e.Name == Hl7 + part && Attr(e, "qualifier") == qualifier);
        Set("patient.nomNaissance", NamePart("family", "BR")?.Value);
        Set("patient.prenomsNaissance", NamePart("given", null)?.Value);
        Set("patient.premierPrenom", NamePart("given", "BR")?.Value);
        Set("patient.nomUtilise", NamePart("family", "CL")?.Value);
        Set("patient.prenomUtilise", NamePart("given", "CL")?.Value);
        Set("patient.sexe", Attr(patient?.Element(Hl7 + "administrativeGenderCode"), "code"));
        Set("patient.dateNaissance", Date(Attr(patient?.Element(Hl7 + "birthTime"), "value")));
        var naissance = patient?.Element(Hl7 + "birthplace")?.Element(Hl7 + "place")?.Element(Hl7 + "addr");
        Set("patient.lieuNaissanceCog", naissance?.Element(Hl7 + "county")?.Value);
        Set("patient.lieuNaissanceCommune", naissance?.Element(Hl7 + "city")?.Value);

        // Auteur
        var author = doc.Element(Hl7 + "author");
        var assignedAuthor = author?.Element(Hl7 + "assignedAuthor");
        Set("auteur.horodatage", DateTime(Attr(author?.Element(Hl7 + "time"), "value")));
        Set("auteur.fonction", Attr(author?.Element(Hl7 + "functionCode"), "code"));
        Set("auteur.rpps", Rpps(assignedAuthor?.Element(Hl7 + "id")));
        Set("auteur.professionCode", Attr(assignedAuthor?.Element(Hl7 + "code"), "code"));
        Set("auteur.professionLibelle", Attr(assignedAuthor?.Element(Hl7 + "code"), "displayName"));
        Person("auteur", assignedAuthor?.Element(Hl7 + "assignedPerson")?.Element(Hl7 + "name"));
        Set("auteur.telephone", Phone(assignedAuthor?.Elements(Hl7 + "telecom"), "WP"));
        var authorOrg = assignedAuthor?.Element(Hl7 + "representedOrganization");
        Set("auteur.orgFiness", Finess(authorOrg?.Element(Hl7 + "id")));
        Set("auteur.orgNom", authorOrg?.Element(Hl7 + "name")?.Value);
        Set("auteur.orgSecteur", Attr(authorOrg?.Element(Hl7 + "standardIndustryClassCode"), "code"));

        // Custodian
        var custodian = doc.Element(Hl7 + "custodian")?.Element(Hl7 + "assignedCustodian")?.Element(Hl7 + "representedCustodianOrganization");
        Set("custodian.finess", Finess(custodian?.Element(Hl7 + "id")));
        Set("custodian.nom", custodian?.Element(Hl7 + "name")?.Value);

        // Signataire
        var legal = doc.Element(Hl7 + "legalAuthenticator");
        var legalEntity = legal?.Element(Hl7 + "assignedEntity");
        Set("signataire.horodatage", DateTime(Attr(legal?.Element(Hl7 + "time"), "value")));
        Set("signataire.rpps", Rpps(legalEntity?.Element(Hl7 + "id")));
        Set("signataire.professionCode", Attr(legalEntity?.Element(Hl7 + "code"), "code"));
        Person("signataire", legalEntity?.Element(Hl7 + "assignedPerson")?.Element(Hl7 + "name"));

        // Médecin demandeur (participant REF)
        var demandeur = doc.Elements(Hl7 + "participant").FirstOrDefault(p => Attr(p, "typeCode") == "REF");
        if (demandeur != null)
        {
            var entity = demandeur.Element(Hl7 + "associatedEntity");
            Set("demandeur.dateDemande", DateTime(Attr(demandeur.Element(Hl7 + "time"), "value")));
            Set("demandeur.rpps", Rpps(entity?.Element(Hl7 + "id")));
            Set("demandeur.professionCode", Attr(entity?.Element(Hl7 + "code"), "code"));
            Set("demandeur.professionLibelle", Attr(entity?.Element(Hl7 + "code"), "displayName"));
            Person("demandeur", entity?.Element(Hl7 + "associatedPerson")?.Element(Hl7 + "name"));
        }

        // Demande
        var order = doc.Element(Hl7 + "inFulfillmentOf")?.Element(Hl7 + "order");
        var numero = order?.Element(Hl7 + "id");
        Set("demande.numeroRoot", Root(numero));
        Set("demande.numeroValeur", Attr(numero, "extension"));
        var accession = order?.Element(Dicom + "accessionNumber");
        Set("demande.accessionRoot", Root(accession));
        Set("demande.accessionValeur", Attr(accession, "extension"));

        // Acte : l'acte documenté est codé en LOINC ; le dépistage est un documentationOf CIM-10 Z13.9.
        var serviceEvents = doc.Elements(Hl7 + "documentationOf").Elements(Hl7 + "serviceEvent").ToList();
        var acte = serviceEvents.FirstOrDefault(e => Attr(e.Element(Hl7 + "code"), "codeSystem") == CodeSystems.Loinc);
        var code = acte?.Element(Hl7 + "code");
        var translations = code?.Elements(Hl7 + "translation").ToList() ?? new List<XElement>();
        XElement? Translation(Func<string?, bool> codeSystem) =>
            translations.FirstOrDefault(t => codeSystem(Attr(t, "codeSystem")));
        // CCAM : OID actuel, ou ancien OID (1.2.250.1.213.2.5) encore présent dans certains exemples.
        var ccam = Translation(s => s is CodeSystems.Ccam or "1.2.250.1.213.2.5");
        var modalite = translations.FirstOrDefault(t => JeuxDeValeurs.ModaliteAcquisition.Any(o => Matches(o, t)));
        var region = translations.FirstOrDefault(t => JeuxDeValeurs.RegionAnatomique.Any(o => Matches(o, t)));
        Set("acte.studyUid", Root(acte?.Element(Hl7 + "id")));
        Set("acte.loincCode", Attr(code, "code"));
        Set("acte.loincLibelle", Attr(code, "displayName"));
        Set("acte.ccamCode", Attr(ccam, "code"));
        Set("acte.ccamLibelle", Attr(ccam, "displayName"));
        Set("acte.modalite", Attr(modalite, "code"));
        Set("acte.region", Attr(region, "code"));
        Set("acte.debut", DateTime(Attr(acte?.Element(Hl7 + "effectiveTime")?.Element(Hl7 + "low"), "value")));
        Set("acte.fin", DateTime(Attr(acte?.Element(Hl7 + "effectiveTime")?.Element(Hl7 + "high"), "value")));
        var executant = acte?.Element(Hl7 + "performer")?.Element(Hl7 + "assignedEntity");
        Set("acte.executantRpps", Rpps(executant?.Element(Hl7 + "id")));
        Set("acte.executantProfessionCode", Attr(executant?.Element(Hl7 + "code"), "code"));
        var executantOrg = executant?.Element(Hl7 + "representedOrganization");
        Set("acte.executantFiness", Finess(executantOrg?.Element(Hl7 + "id")));
        Set("acte.executantOrgNom", executantOrg?.Element(Hl7 + "name")?.Value);
        Set("acte.executantSecteur", Attr(executantOrg?.Element(Hl7 + "standardIndustryClassCode"), "code"));
        if (serviceEvents.Any(e => Attr(e.Element(Hl7 + "code"), "code") == "Z13.9")) Set("acte.depistage", "true");

        // Prise en charge
        var encounter = doc.Element(Hl7 + "componentOf")?.Element(Hl7 + "encompassingEncounter");
        var facility = encounter?.Element(Hl7 + "location")?.Element(Hl7 + "healthCareFacility");
        Set("pec.modalite", Attr(encounter?.Element(Hl7 + "code"), "code"));
        Set("pec.debut", DateTime(Attr(encounter?.Element(Hl7 + "effectiveTime")?.Element(Hl7 + "low"), "value")));
        Set("pec.fin", DateTime(Attr(encounter?.Element(Hl7 + "effectiveTime")?.Element(Hl7 + "high"), "value")));
        Set("pec.cadreExercice", Attr(facility?.Element(Hl7 + "code"), "code"));
        Set("pec.lieuFiness", Finess(facility?.Element(Hl7 + "id")));
        Set("pec.lieuNom", facility?.Element(Hl7 + "location")?.Element(Hl7 + "name")?.Value);

        // Corps : PDF encapsulé (nonXMLBody, base64)
        var text = doc.Element(Hl7 + "component")?.Element(Hl7 + "nonXMLBody")?.Element(Hl7 + "text");
        var pdf = text != null && Attr(text, "representation") == "B64"
            ? Convert.FromBase64String(string.Concat(text.Value.Where(c => !char.IsWhiteSpace(c))))
            : null;

        var titre = values.GetValueOrDefault("acte.loincLibelle") ?? values.GetValueOrDefault("document.titre") ?? id;
        return new ExempleCda(id, $"{id} — {titre}", values, pdf);
    }

    private static string? Attr(XElement? element, string name) => element?.Attribute(name)?.Value;

    private static string? Root(XElement? id) => Attr(id, "nullFlavor") == null ? Attr(id, "root") : null;

    private static bool IsIns(string? root) =>
        root is IdentifierRoots.InsNir or IdentifierRoots.InsNia or IdentifierRoots.InsNirTest or IdentifierRoots.InsNiaTest;

    private static bool Matches(Option option, XElement code) =>
        option.Code == Attr(code, "code") && option.CodeSystem == Attr(code, "codeSystem");

    /// <summary>N° RPPS saisi dans le formulaire : identifiant national sans le préfixe 8.</summary>
    private static string? Rpps(XElement? id) => NationalId(id, IdentifierRoots.IdNatPs, "8");

    /// <summary>N° FINESS saisi dans le formulaire : identifiant national sans le préfixe 1.</summary>
    private static string? Finess(XElement? id) => NationalId(id, IdentifierRoots.IdNatStruct, "1");

    private static string? NationalId(XElement? id, string root, string prefix)
    {
        var extension = Attr(id, "extension");
        return Root(id) == root && extension != null && extension.StartsWith(prefix, StringComparison.Ordinal)
            ? extension[prefix.Length..]
            : extension;
    }

    /// <summary>Premier numéro de téléphone, de préférence avec l'usage <paramref name="use"/>.</summary>
    private static string? Phone(IEnumerable<XElement>? telecoms, string use)
    {
        var phones = telecoms?.Where(t => Attr(t, "value")?.StartsWith("tel:", StringComparison.Ordinal) == true).ToList();
        var phone = phones?.FirstOrDefault(t => Attr(t, "use") == use) ?? phones?.FirstOrDefault();
        return Attr(phone, "value")?["tel:".Length..];
    }

    /// <summary>Date HL7 (AAAAMMJJ…) au format du champ date du formulaire.</summary>
    private static string? Date(string? value) =>
        value is { Length: >= 8 } && System.DateTime.TryParseExact(value[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;

    /// <summary>
    /// Horodatage HL7 (AAAAMMJJHHMM[SS][±ZZZZ]) au format du champ date et heure du formulaire, en heure
    /// de Paris ; sans fuseau, l'heure est supposée déjà locale.
    /// </summary>
    private static string? DateTime(string? value)
    {
        if (value == null) return null;
        var sign = value.IndexOfAny(new[] { '+', '-' });
        var local = sign < 0 ? value : value[..sign];
        if (!System.DateTime.TryParseExact(local, new[] { "yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMdd" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            return null;
        if (sign >= 0 && TimeSpan.TryParseExact(value[(sign + 1)..], "hhmm", CultureInfo.InvariantCulture, out var offset))
        {
            var instant = new DateTimeOffset(dateTime, value[sign] == '-' ? -offset : offset);
            dateTime = TimeZoneInfo.ConvertTime(instant, ReportFormMapper.Paris).DateTime;
        }
        return dateTime.ToString(dateTime.Second == 0 ? "yyyy-MM-ddTHH:mm" : "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }
}
