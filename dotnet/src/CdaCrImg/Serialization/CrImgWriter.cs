using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;
using CdaCrImg.Validation;
using static CdaCrImg.Serialization.CdaXml;

namespace CdaCrImg.Serialization
{
    /// <summary>Sérialise un <see cref="CompteRenduImagerie"/> en document HL7 CDA R2 (volet IMG-CR-IMG 2024.01).</summary>
    public static class CrImgWriter
    {
        /// <summary>
        /// Produit le document CDA. Le modèle est d'abord contrôlé (<see cref="CrImgValidator"/>) :
        /// <see cref="CrImgValidationException"/> est levée s'il est incomplet.
        /// </summary>
        public static XDocument Write(CompteRenduImagerie cr)
        {
            CrImgValidator.EnsureValid(cr);

            var root = El("ClinicalDocument",
                // Déclaration explicite : les valeurs xsi:type (ex. IVL_TS) se résolvent via l'espace de noms par défaut.
                new XAttribute("xmlns", V3.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsi", CdaNamespaces.Xsi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "ps3-20", CdaNamespaces.Ps320.NamespaceName),
                El("realmCode", Attr("code", "FR")),
                El("typeId", Attr("root", "2.16.840.1.113883.1.3"), Attr("extension", "POCD_HD000040")),
                TemplateIds(cr.Corps!),
                Ii(cr.Id!),
                DocumentCode(cr),
                El("title", cr.Titre),
                Ts("effectiveTime", cr.DateCreation),
                Cd(cr.Confidentialite, "confidentialityCode"),
                El("languageCode", Attr("code", cr.Langue)),
                Ii(cr.SetId!, "setId"),
                El("versionNumber", Attr("value", cr.NumeroVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                RecordTarget(cr.Patient!),
                cr.Auteurs.Select(Author),
                Custodian(cr.Custodian!),
                LegalAuthenticator(cr.SignataireLegal!),
                cr.MedecinsDemandeurs.Select(Referrer),
                cr.Demandes.Select(InFulfillmentOf),
                cr.Actes.Select(DocumentationOf),
                cr.Depistage ? ScreeningDocumentationOf() : null,
                cr.DocumentRemplace == null ? null : El("relatedDocument", Attr("typeCode", "RPLC"), El("parentDocument", Ii(cr.DocumentRemplace))),
                ComponentOf(cr.PriseEnCharge!),
                Body(cr.Corps!));

            return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        }

        /// <summary>Écrit le document CDA en UTF-8 (sans BOM) dans un flux.</summary>
        public static void Write(CompteRenduImagerie cr, Stream output)
        {
            var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };
            using (var writer = XmlWriter.Create(output, settings))
            {
                Write(cr).Save(writer);
            }
        }

        /// <summary>Retourne le document CDA sous forme de chaîne XML.</summary>
        public static string WriteToString(CompteRenduImagerie cr)
        {
            using (var stream = new MemoryStream())
            {
                Write(cr, stream);
                return new UTF8Encoding(false).GetString(stream.ToArray());
            }
        }

        private static object[] TemplateIds(CorpsDocument corps)
        {
            var common = new object[]
            {
                TemplateId(CdaCrImg.TemplateIds.Document.Hl7France),
                TemplateId(CdaCrImg.TemplateIds.Document.CiSis),
            };
            switch (corps)
            {
                // Structuration minimale : en non structuré, seuls ces trois templateId sont autorisés.
                case CorpsPdf _:
                    return common.Append(TemplateId(CdaCrImg.TemplateIds.Document.NonStructuredBody)).ToArray();
                default:
                    throw new NotSupportedException($"Corps non pris en charge : {corps.GetType().Name}.");
            }
        }

        /// <summary>Code LOINC 18748-4, avec une translation par acte (code LOINC de l'acte).</summary>
        private static XElement DocumentCode(CompteRenduImagerie cr) => El("code",
            Attr("code", Codes.DocumentType), Attr("displayName", "CR d'imagerie médicale"),
            Attr("codeSystem", CodeSystems.Loinc), Attr("codeSystemName", "LOINC"),
            cr.Actes.Select(a => Cd(Strip(a.Code!), "translation")));

        private static XElement RecordTarget(Patient p)
        {
            var ids = (p.Ins == null ? Enumerable.Empty<Identifier>() : new[] { p.Ins }).Concat(p.AutresIdentifiants);
            return El("recordTarget", El("patientRole",
                ids.Select(id => Ii(id)),
                Ads(p.Adresses), Tels(p.Telecoms),
                El("patient", Attr("classCode", "PSN"),
                    PatientName(p),
                    Cd(Gender(p.Sexe), "administrativeGenderCode"),
                    p.DateNaissance == null ? El("birthTime", Attr("nullFlavor", "UNK")) : El("birthTime", Attr("value", Hl7Format.Date(p.DateNaissance.Value))),
                    p.LieuNaissanceCog == null && p.LieuNaissanceCommune == null ? null
                        : El("birthplace", El("place", El("addr",
                            p.LieuNaissanceCog == null ? null : El("county", p.LieuNaissanceCog),
                            p.LieuNaissanceCommune == null ? null : El("city", p.LieuNaissanceCommune)))))));
        }

        /// <summary>Nom INS : nom de naissance (BR), prénoms de naissance, premier prénom (BR), nom et prénom utilisés (CL).</summary>
        private static XElement PatientName(Patient p) => El("name",
            El("family", Attr("qualifier", "BR"), p.NomNaissance),
            string.IsNullOrEmpty(p.PrenomsNaissance) ? null : El("given", p.PrenomsNaissance),
            string.IsNullOrEmpty(p.PremierPrenomNaissance) ? null : El("given", Attr("qualifier", "BR"), p.PremierPrenomNaissance),
            string.IsNullOrEmpty(p.NomUtilise) ? null : El("family", Attr("qualifier", "CL"), p.NomUtilise),
            string.IsNullOrEmpty(p.PrenomUtilise) ? null : El("given", Attr("qualifier", "CL"), p.PrenomUtilise));

        private static Code Gender(Sexe sexe)
        {
            switch (sexe)
            {
                case Sexe.Masculin: return new Code("M", CodeSystems.Hl7AdministrativeGender, "Masculin");
                case Sexe.Feminin: return new Code("F", CodeSystems.Hl7AdministrativeGender, "Féminin");
                default: return new Code("U", CodeSystems.Hl7AdministrativeGender, "Inconnu");
            }
        }

        private static XElement Author(Auteur a) => El("author",
            a.Fonction == null ? null : Cd(a.Fonction, "functionCode"),
            Ts("time", a.Horodatage),
            Role("assignedAuthor", "assignedPerson", "representedOrganization", a.Professionnel));

        private static XElement Custodian(Organisation o) => El("custodian", Attr("typeCode", "CST"),
            El("assignedCustodian", Attr("classCode", "ASSIGNED"),
                El("representedCustodianOrganization", Attr("classCode", "ORG"), Attr("determinerCode", "INSTANCE"),
                    Ii(o.Id!),
                    o.Nom == null ? null : El("name", o.Nom),
                    Tels(o.Telecoms).Take(1),
                    Ads(o.Adresses).Take(1))));

        private static XElement LegalAuthenticator(Signature s) => El("legalAuthenticator",
            Ts("time", s.Horodatage),
            El("signatureCode", Attr("code", "S")),
            Role("assignedEntity", "assignedPerson", "representedOrganization", s.Professionnel));

        private static XElement Referrer(MedecinDemandeur m) => El("participant", Attr("typeCode", "REF"),
            m.DateDemande == null
                ? El("time", new XAttribute(CdaNamespaces.Xsi + "type", "IVL_TS"), Attr("nullFlavor", "UNK"))
                : El("time", new XAttribute(CdaNamespaces.Xsi + "type", "IVL_TS"), Ts("high", m.DateDemande.Value)),
            Role("associatedEntity", "associatedPerson", "scopingOrganization", m.Professionnel, new XAttribute("classCode", "PROV")));

        private static XElement InFulfillmentOf(DemandeImagerie d) => El("inFulfillmentOf",
            El("order",
                Ii(d.NumeroDemande),
                Ii(d.AccessionNumber, CdaNamespaces.Ps320 + "accessionNumber")));

        /// <summary>
        /// serviceEvent : id = Study Instance UID (root seul), code LOINC + translations CCAM [0..1],
        /// modalités [1..*] (qualifier DCM 121139) et régions anatomiques [1..*] (qualifier LOINC 39111-0).
        /// </summary>
        private static XElement DocumentationOf(ActeImagerie a)
        {
            var code = Strip(a.Code!);
            if (a.CodeCcam != null) code.Translations.Add(Strip(a.CodeCcam));
            foreach (var modalite in a.Modalites)
            {
                var t = Strip(modalite);
                t.Qualifiers.Add(new Qualifier(Code.Dcm(Codes.Entries.DcmModality, "Modalité")));
                code.Translations.Add(t);
            }
            foreach (var region in a.RegionsAnatomiques)
            {
                var t = Strip(region);
                t.Qualifiers.Add(new Qualifier(Code.Loinc(Codes.Entries.LoincAnatomicLocation, "Localisation anatomique")));
                code.Translations.Add(t);
            }

            return El("documentationOf", El("serviceEvent", Attr("classCode", "ACT"),
                El("id", Attr("root", a.StudyInstanceUid)),
                Cd(code),
                El("effectiveTime",
                    Ts("low", a.Debut!.Value),
                    a.Fin == null ? null : Ts("high", a.Fin.Value)),
                El("performer", Attr("typeCode", "PRF"),
                    Role("assignedEntity", "assignedPerson", "representedOrganization", a.Executant!))));
        }

        private static XElement ScreeningDocumentationOf() => El("documentationOf", El("serviceEvent", Attr("classCode", "ACT"),
            Cd(Code.Cim10("Z13.9", "Examen spécial de dépistage, sans précision"))));

        private static XElement ComponentOf(PriseEnCharge pec) => El("componentOf", El("encompassingEncounter",
            pec.Modalite == null ? null : Cd(pec.Modalite),
            El("effectiveTime",
                Ts("low", pec.Debut!.Value),
                pec.Fin == null ? null : Ts("high", pec.Fin.Value)),
            El("location", El("healthCareFacility",
                pec.Lieu!.Id == null ? null : Ii(pec.Lieu.Id),
                Cd(pec.Lieu.CadreExercice!),
                pec.Lieu.Nom == null && pec.Lieu.Adresse == null ? null
                    : El("location",
                        pec.Lieu.Nom == null ? null : El("name", pec.Lieu.Nom),
                        pec.Lieu.Adresse == null ? null : Ad(pec.Lieu.Adresse))))));

        private static XElement Body(CorpsDocument corps)
        {
            switch (corps)
            {
                case CorpsPdf pdf:
                    return El("component", El("nonXMLBody",
                        El("text", Attr("mediaType", "application/pdf"), Attr("representation", "B64"),
                            Convert.ToBase64String(pdf.Pdf))));
                default:
                    throw new NotSupportedException($"Corps non pris en charge : {corps.GetType().Name}.");
            }
        }

        /// <summary>Copie d'un code sans ses traductions ni qualificatifs (ils sont recomposés par le writer).</summary>
        private static Code Strip(Code c) => new Code(c.Value, c.CodeSystem, c.DisplayName, c.CodeSystemName);
    }
}
