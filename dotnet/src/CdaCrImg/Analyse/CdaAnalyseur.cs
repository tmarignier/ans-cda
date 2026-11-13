using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using CdaCrImg.Referentiel;

namespace CdaCrImg.Analyse
{
    /// <summary>
    /// Analyse d'un compte rendu d'imagerie CDA existant : liste, à partir du référentiel des champs
    /// (<see cref="ReferentielChamps"/>), les champs présents avec leur valeur, les champs absents avec leur
    /// caractère obligatoire ou facultatif, et les éléments que la librairie ne gère pas. L'analyse ne
    /// reconstruit pas de <c>CompteRenduImagerie</c> et ne remplace pas la validation XSD / schématron.
    /// </summary>
    public static class CdaAnalyseur
    {
        /// <summary>Analyse un document CDA déjà chargé (chargé avec <see cref="LoadOptions.SetLineInfo"/> pour les numéros de ligne).</summary>
        /// <exception cref="CdaAnalyseException">Le document n'est pas un CR d'imagerie CDA.</exception>
        public static AnalyseCda Analyser(XDocument cda)
        {
            if (cda == null) throw new ArgumentNullException(nameof(cda));
            var racine = cda.Root;
            if (racine == null || racine.Name != CdaNamespaces.Hl7 + "ClinicalDocument")
                throw new CdaAnalyseException("Le document n'est pas un CDA R2 : élément racine ClinicalDocument (urn:hl7-org:v3) attendu"
                    + (racine == null ? "." : $", « {racine.Name.LocalName} » trouvé."));
            var typeDocument = racine.Element(CdaNamespaces.Hl7 + "code")?.Attribute("code")?.Value;
            if (typeDocument != Codes.DocumentType)
                throw new CdaAnalyseException($"Le document n'est pas un compte rendu d'imagerie : code LOINC {Codes.DocumentType} attendu"
                    + (typeDocument == null ? ", code du document absent." : $", « {typeDocument} » trouvé."));

            var corpsStructure = racine.Element(CdaNamespaces.Hl7 + "component")?.Element(CdaNamespaces.Hl7 + "structuredBody") != null;
            var analyse = new Analyse(ReferentielChamps.CreerResolveurEspaces(), corpsStructure);
            analyse.Parcourir(ReferentielChamps.Champs.Where(c => c.Parent == null), racine, parentChemin: null, cheminParent: null);
            return new AnalyseCda(analyse.Resultats, analyse.NonGeres(racine), corpsStructure);
        }

        /// <summary>Analyse un document CDA lu depuis un flux.</summary>
        /// <exception cref="CdaAnalyseException">XML mal formé ou document qui n'est pas un CR d'imagerie CDA.</exception>
        public static AnalyseCda Analyser(Stream flux)
        {
            if (flux == null) throw new ArgumentNullException(nameof(flux));
            return Analyser(Charger(() => XDocument.Load(flux, LoadOptions.SetLineInfo)));
        }

        /// <summary>Analyse le fichier CDA <paramref name="cheminFichier"/>.</summary>
        /// <exception cref="CdaAnalyseException">XML mal formé ou document qui n'est pas un CR d'imagerie CDA.</exception>
        public static AnalyseCda Analyser(string cheminFichier)
        {
            if (cheminFichier == null) throw new ArgumentNullException(nameof(cheminFichier));
            return Analyser(Charger(() => XDocument.Load(cheminFichier, LoadOptions.SetLineInfo)));
        }

        private static XDocument Charger(Func<XDocument> charger)
        {
            try
            {
                return charger();
            }
            catch (XmlException e)
            {
                throw new CdaAnalyseException($"XML mal formé : {e.Message}", e);
            }
        }

        private sealed class Analyse
        {
            private readonly IXmlNamespaceResolver _espaces;
            private readonly bool _corpsStructure;
            private readonly HashSet<XElement> _couverts = new HashSet<XElement>();

            public Analyse(IXmlNamespaceResolver espaces, bool corpsStructure)
            {
                _espaces = espaces;
                _corpsStructure = corpsStructure;
            }

            public List<ChampAnalyse> Resultats { get; } = new List<ChampAnalyse>();

            /// <summary>
            /// Analyse les champs dans le contexte d'une occurrence de leur parent. <paramref name="parentChemin"/> :
            /// chemin du champ parent dans le référentiel ; <paramref name="cheminParent"/> : chemin indexé de l'occurrence.
            /// </summary>
            public void Parcourir(IEnumerable<ChampCda> champs, XElement contexte, string? parentChemin, string? cheminParent)
            {
                foreach (var champ in champs)
                {
                    var chemin = parentChemin == null ? champ.Chemin : cheminParent + champ.Chemin.Substring(parentChemin.Length);
                    var noeuds = ((IEnumerable)contexte.XPathEvaluate(champ.XPath, _espaces)).Cast<XObject>().ToList();
                    if (noeuds.Count == 0)
                    {
                        var statut = champ.Chemin == "Corps" && _corpsStructure ? StatutChamp.HorsPerimetre
                            : champ.Min > 0 ? StatutChamp.ObligatoireAbsent : StatutChamp.FacultatifAbsent;
                        Resultats.Add(new ChampAnalyse(champ, SansIndex(chemin), statut, null, null, null));
                        continue;
                    }

                    for (var i = 0; i < noeuds.Count; i++)
                    {
                        var noeud = noeuds[i];
                        var cheminOccurrence = champ.Max == 1 ? chemin : SansIndex(chemin) + $"[{i}]";
                        var statut = champ.Max != null && i >= champ.Max ? StatutChamp.EnTrop : StatutChamp.Present;
                        var element = noeud as XElement ?? noeud.Parent;
                        if (element != null) Couvrir(element, avecDescendants: champ.Enfants.Count == 0);
                        Resultats.Add(new ChampAnalyse(champ, cheminOccurrence, statut, Valeur(champ, noeud), Emplacement(noeud), Ligne(noeud)));
                        if (noeud is XElement occurrence && champ.Enfants.Count > 0)
                            Parcourir(champ.Enfants, occurrence, champ.Chemin, cheminOccurrence);
                    }
                }
            }

            private void Couvrir(XElement element, bool avecDescendants)
            {
                _couverts.Add(element);
                if (avecDescendants) _couverts.UnionWith(element.Descendants());
            }

            /// <summary>
            /// Éléments non couverts par le référentiel ni ancêtres d'un élément couvert, dont le parent est
            /// couvert ou ancêtre d'un élément couvert (seul le plus haut élément non géré est signalé).
            /// </summary>
            public List<ElementNonGere> NonGeres(XElement racine)
            {
                var connus = new HashSet<XElement>(_couverts.SelectMany(e => e.AncestorsAndSelf()));
                return racine.Descendants()
                    .Where(e => !connus.Contains(e) && connus.Contains(e.Parent!))
                    .Select(e => new ElementNonGere(Emplacement(e), Ligne(e)))
                    .ToList();
            }

            private static string SansIndex(string chemin) => chemin.EndsWith("[]", StringComparison.Ordinal) ? chemin.Substring(0, chemin.Length - 2) : chemin;
        }

        /// <summary>Valeur affichable d'un nœud : identifiant, code, horodatage, adresse, nom, texte ou PDF.</summary>
        private static string? Valeur(ChampCda champ, XObject noeud)
        {
            if (noeud is XAttribute attribut) return attribut.Value;
            var e = (XElement)noeud;
            string? A(string nom) => e.Attribute(nom)?.Value;

            if (A("nullFlavor") is { } nullFlavor && A("root") == null) return "nullFlavor=" + nullFlavor;
            if (e.Name.LocalName == "text" && A("representation") == "B64")
                return $"{A("mediaType")} encapsulé en base64 ({TailleBase64(e.Value)} octets)";
            if (A("root") is { } root) return A("extension") is { } extension ? root + "^" + extension : root;
            if (A("code") is { } code)
                return A("displayName") is { } libelle ? $"{code} — {libelle}" : code;
            if (A("value") is { } value) return value;
            // Groupe de champs (patientRole, author…) : pas de valeur propre, sauf un nom (PN) affiché en entier.
            if (champ.Enfants.Count > 0 && e.Name.LocalName != "name") return null;
            var texte = string.Join(" ", e.DescendantNodes().OfType<XText>().Select(t => t.Value.Trim()).Where(t => t.Length > 0));
            return texte.Length == 0 ? null : texte;
        }

        private static long TailleBase64(string base64)
        {
            var longueur = base64.Count(c => !char.IsWhiteSpace(c));
            var remplissage = base64.TrimEnd().EndsWith("==", StringComparison.Ordinal) ? 2 : base64.TrimEnd().EndsWith("=", StringComparison.Ordinal) ? 1 : 0;
            return longueur / 4 * 3 - remplissage;
        }

        /// <summary>Emplacement lisible : noms locaux (préfixe <c>ps3-20</c> pour DICOM), index si plusieurs frères homonymes.</summary>
        private static string Emplacement(XObject noeud)
        {
            if (noeud is XAttribute attribut) return Emplacement(attribut.Parent!) + "/@" + attribut.Name.LocalName;
            var element = (XElement)noeud;
            var segments = new List<string>();
            for (var e = element; e != null; e = e.Parent)
            {
                var nom = (e.Name.Namespace == CdaNamespaces.Ps320 ? ReferentielChamps.PrefixeDicom + ":" : "") + e.Name.LocalName;
                var freres = e.Parent?.Elements(e.Name).ToList();
                segments.Add(freres != null && freres.Count > 1 ? $"{nom}[{freres.IndexOf(e) + 1}]" : nom);
            }
            segments.Reverse();
            return "/" + string.Join("/", segments);
        }

        private static int? Ligne(XObject noeud) =>
            noeud is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : (int?)null;
    }
}
