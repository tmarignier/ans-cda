using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CdaCrImg.Terminologies
{
    /// <summary>Concept d'un jeu de valeurs.</summary>
    public sealed class Concept
    {
        /// <summary>Crée un concept.</summary>
        public Concept(string code, string codeSystem, string? displayName)
        {
            Code = code;
            CodeSystem = codeSystem;
            DisplayName = displayName;
        }

        /// <summary>Code.</summary>
        public string Code { get; }

        /// <summary>OID du système de codage.</summary>
        public string CodeSystem { get; }

        /// <summary>Libellé.</summary>
        public string? DisplayName { get; }

        /// <inheritdoc/>
        public override string ToString() => $"{Code} ({CodeSystem}) {DisplayName}";
    }

    /// <summary>
    /// Jeu de valeurs (JDV) du CI-SIS : liste de concepts (code + système de codage). Chargeable depuis les
    /// deux formats publiés par l'ANS : IHE SVS (<c>jeuxDeValeurs/*.xml</c>) et ART-DECOR (<c>voc-*.xml</c>
    /// des schématrons).
    /// </summary>
    public sealed class JeuDeValeurs
    {
        private readonly HashSet<(string Code, string CodeSystem)> _index;

        /// <summary>Crée un jeu de valeurs.</summary>
        public JeuDeValeurs(string oid, string nom, IEnumerable<Concept> concepts)
        {
            Oid = oid ?? throw new ArgumentNullException(nameof(oid));
            Nom = nom ?? throw new ArgumentNullException(nameof(nom));
            Concepts = concepts.ToList();
            _index = new HashSet<(string, string)>(Concepts.Select(c => (c.Code, c.CodeSystem)));
        }

        /// <summary>OID du jeu de valeurs.</summary>
        public string Oid { get; }

        /// <summary>Nom du jeu de valeurs (ex. JDV_J04_XdsPracticeSettingCode_CISIS).</summary>
        public string Nom { get; }

        /// <summary>Concepts du jeu de valeurs.</summary>
        public IReadOnlyList<Concept> Concepts { get; }

        /// <summary>Vrai si le couple code / système de codage appartient au jeu de valeurs.</summary>
        public bool Contient(string code, string codeSystem) => _index.Contains((code, codeSystem));

        /// <summary>Concept correspondant au code et au système de codage, ou null.</summary>
        public Concept? Trouver(string code, string codeSystem) =>
            Concepts.FirstOrDefault(c => c.Code == code && c.CodeSystem == codeSystem);

        /// <summary>Charge un jeu de valeurs au format IHE SVS ou ART-DECOR (détection automatique).</summary>
        public static JeuDeValeurs Charger(Stream xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (var reader = XmlReader.Create(xml, settings))
            {
                return Charger(XDocument.Load(reader));
            }
        }

        /// <summary>Charge un jeu de valeurs depuis un fichier (IHE SVS ou ART-DECOR).</summary>
        public static JeuDeValeurs Charger(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return Charger(stream);
            }
        }

        private static JeuDeValeurs Charger(XDocument document)
        {
            // SVS : ValueSet/ConceptList/Concept ; ART-DECOR : valueSet/conceptList/concept.
            var valueSet = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "ValueSet" || e.Name.LocalName == "valueSet")
                           ?? throw new FormatException("Jeu de valeurs introuvable (élément ValueSet ou valueSet attendu).");
            var concepts = valueSet.Descendants()
                .Where(e => e.Name.LocalName == "Concept" || e.Name.LocalName == "concept")
                .Select(e => new Concept(
                    (string?)e.Attribute("code") ?? throw new FormatException("Concept sans attribut code."),
                    (string?)e.Attribute("codeSystem") ?? throw new FormatException("Concept sans attribut codeSystem."),
                    (string?)e.Attribute("displayName")));
            var oid = (string?)valueSet.Attribute("id") ?? throw new FormatException("Jeu de valeurs sans attribut id.");
            var nom = (string?)valueSet.Attribute("displayName") ?? (string?)valueSet.Attribute("name") ?? oid;
            return new JeuDeValeurs(oid, nom, concepts);
        }
    }
}
