using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Serialization
{
    /// <summary>Fabriques d'éléments CDA pour les types HL7 et les rôles communs (ordre imposé par le XSD CDA R2).</summary>
    internal static class CdaXml
    {
        public static readonly XNamespace V3 = CdaNamespaces.Hl7;

        public static XElement El(string name, params object?[] content) => new XElement(V3 + name, content);

        public static XAttribute? Attr(string name, string? value) => value == null ? null : new XAttribute(name, value);

        public static XElement TemplateId(string root, string? extension = null) =>
            El("templateId", Attr("root", root), Attr("extension", extension));

        public static XElement Ii(Identifier id, string name = "id") => Ii(id, V3 + name);

        public static XElement Ii(Identifier id, XName name) => new XElement(name,
            Attr("nullFlavor", id.NullFlavor), Attr("root", id.Root), Attr("extension", id.Extension));

        /// <summary>CD/CE : attributs, puis <c>qualifier*</c>, puis <c>translation*</c>.</summary>
        public static XElement Cd(Code code, string name = "code", params object?[] extraContent) => El(name,
            Attr("code", code.Value), Attr("displayName", code.DisplayName),
            Attr("codeSystem", code.CodeSystem), Attr("codeSystemName", code.CodeSystemName),
            extraContent,
            code.Qualifiers.Select(q => El("qualifier", Cd(q.Name, "name"), q.Value == null ? null : Cd(q.Value, "value"))),
            code.Translations.Select(t => Cd(t, "translation")));

        public static XElement Ts(string name, System.DateTimeOffset value) => El(name, Attr("value", Hl7Format.Timestamp(value)));

        public static XElement Ad(Address a) => El("addr",
            Attr("use", a.Use),
            Part("houseNumber", a.HouseNumber), Part("streetName", a.StreetName),
            Part("additionalLocator", a.AdditionalLocator), Part("unitID", a.UnitId), Part("postBox", a.PostBox),
            Part("postalCode", a.PostalCode), Part("city", a.City), Part("county", a.County), Part("country", a.Country));

        public static IEnumerable<XElement> Ads(IEnumerable<Address> addresses) => addresses.Select(Ad);

        public static IEnumerable<XElement> Tels(IEnumerable<Telecom> telecoms) =>
            telecoms.Select(t => El("telecom", Attr("value", t.Value), Attr("use", t.Use)));

        public static XElement Pn(PersonName n) => El("name",
            Part("prefix", n.Prefix), Part("given", n.Given), Part("family", n.Family), Part("suffix", n.Suffix));

        /// <summary>Organization : id*, name, telecom*, addr*, standardIndustryClassCode.</summary>
        public static XElement Organization(string elementName, Organisation o) => El(elementName,
            o.Id == null ? null : Ii(o.Id),
            Part("name", o.Nom),
            Tels(o.Telecoms), Ads(o.Adresses),
            o.SecteurActivite == null ? null : Cd(o.SecteurActivite, "standardIndustryClassCode"));

        /// <summary>
        /// Rôle d'un PS (assignedAuthor, assignedEntity, associatedEntity) : id, code, addr*, telecom*,
        /// personne, organisation.
        /// </summary>
        public static XElement Role(string roleName, string personName, string organizationName, Professionnel ps,
            params XAttribute[] attributes) => El(roleName,
            attributes,
            ps.Id == null ? null : Ii(ps.Id),
            ps.Profession == null ? null : Cd(ps.Profession),
            Ads(ps.Adresses), Tels(ps.Telecoms),
            ps.Nom == null ? null : El(personName, Pn(ps.Nom)),
            ps.Organisation == null ? null : Organization(organizationName, ps.Organisation));

        private static XElement? Part(string name, string? value) => string.IsNullOrEmpty(value) ? null : El(name, value);
    }
}
