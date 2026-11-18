using System.Collections.Generic;

namespace CdaCrImg.Referentiel
{
    /// <summary>Caractère obligatoire d'un champ dans un compte rendu d'imagerie.</summary>
    public enum Obligation
    {
        /// <summary>Toujours présent dans le document.</summary>
        Obligatoire,

        /// <summary>Peut être absent.</summary>
        Facultatif,

        /// <summary>
        /// Obligatoire dès que son groupe (<see cref="ChampCda.Groupe"/>, champ facultatif qui le contient)
        /// est renseigné ; ex. le nom de famille d'un professionnel dont l'identité est renseignée.
        /// </summary>
        ObligatoireSiGroupe,
    }

    /// <summary>Origine de la valeur d'un champ dans un document produit par la librairie.</summary>
    public enum OrigineValeur
    {
        /// <summary>Fournie par l'appelant (logiciel du radiologue).</summary>
        Appelant,

        /// <summary>Fournie par l'appelant ; à défaut, la librairie produit <see cref="ChampCda.ValeurParDefaut"/>.</summary>
        ValeurParDefaut,

        /// <summary>Valeur fixe produite par la librairie (templateId, code du document…).</summary>
        Librairie,
    }

    /// <summary>
    /// Champ de l'en-tête ou du corps d'un compte rendu d'imagerie CDA R2 niveau 1 : emplacement dans le
    /// document, cardinalité et caractère obligatoire. Voir <see cref="ReferentielChamps"/>.
    /// </summary>
    public sealed class ChampCda
    {
        private readonly List<ChampCda> _enfants = new List<ChampCda>();

        internal ChampCda(string chemin, ChampCda? parent, string section, string libelle, string description,
            string xPath, int min, int? max, string source, OrigineValeur origine, string? valeurParDefaut, string sourceDonnee, string voletTeleradiologie)
        {
            Chemin = chemin;
            Parent = parent;
            Section = section;
            Libelle = libelle;
            Description = description;
            XPath = xPath;
            Min = min;
            Max = max;
            Source = source;
            Origine = origine;
            ValeurParDefaut = valeurParDefaut;
            SourceDonnee = sourceDonnee;
            VoletTeleradiologie = voletTeleradiologie;
            parent?._enfants.Add(this);

            var groupe = parent;
            while (groupe != null && groupe.Min > 0) groupe = groupe.Parent;
            Obligation = min == 0 ? Obligation.Facultatif : groupe == null ? Obligation.Obligatoire : Obligation.ObligatoireSiGroupe;
            Groupe = Obligation == Obligation.ObligatoireSiGroupe ? groupe : null;
        }

        /// <summary>
        /// Identifiant du champ : chemin de la propriété CdaCrImg, <c>[]</c> marquant une liste
        /// (ex. <c>Auteurs[].Professionnel.Id</c>) ; même convention que <c>ValidationIssue.Path</c>, sans les
        /// index. Les valeurs fixes produites par la librairie ont un chemin propre (ex. <c>TemplateIds</c>).
        /// </summary>
        public string Chemin { get; }

        /// <summary>Champ qui contient celui-ci (<c>null</c> pour un champ de premier niveau du document).</summary>
        public ChampCda? Parent { get; }

        /// <summary>Champs contenus dans celui-ci.</summary>
        public IReadOnlyList<ChampCda> Enfants => _enfants;

        /// <summary>Section du compte rendu (<see cref="ReferentielChamps.Sections"/>).</summary>
        public string Section { get; }

        /// <summary>Libellé court.</summary>
        public string Libelle { get; }

        /// <summary>
        /// Nom complet du champ : section puis libellés des champs qui le contiennent, ex.
        /// « Auteur › Structure d'exercice › Identifiant de la structure ».
        /// </summary>
        public string NomComplet
        {
            get
            {
                var libelles = new List<string>();
                // Les libellés égaux à la section, et « Professionnel » chez les ancêtres, n'apportent rien au nom.
                for (var champ = this; champ != null; champ = champ.Parent)
                    if (champ.Libelle != Section && (champ == this || champ.Libelle != "Professionnel")) libelles.Insert(0, champ.Libelle);
                libelles.Insert(0, Section);
                return string.Join(" › ", libelles);
            }
        }

        /// <summary>Description métier.</summary>
        public string Description { get; }

        /// <summary>
        /// Expression XPath 1.0 du champ, relative à l'élément de son <see cref="Parent"/> (ou à
        /// <c>ClinicalDocument</c>) ; préfixes <c>cda</c> (HL7 v3) et <c>ps3-20</c> (DICOM), voir
        /// <see cref="ReferentielChamps.CreerResolveurEspaces"/>. Sélectionne un élément ou un attribut.
        /// </summary>
        public string XPath { get; }

        /// <summary>Expression XPath complète, depuis <c>/cda:ClinicalDocument</c>.</summary>
        public string XPathComplet => (Parent?.XPathComplet ?? "/cda:ClinicalDocument") + "/" + XPath;

        /// <summary>Nombre minimal d'occurrences par occurrence du parent.</summary>
        public int Min { get; }

        /// <summary>Nombre maximal d'occurrences par occurrence du parent (<c>null</c> : illimité).</summary>
        public int? Max { get; }

        /// <summary>Cardinalité au format CDA, ex. <c>1..1</c>, <c>0..*</c>.</summary>
        public string Cardinalite => $"{Min}..{(Max == null ? "*" : Max.ToString())}";

        /// <summary>Caractère obligatoire, compte tenu des groupes facultatifs qui contiennent le champ.</summary>
        public Obligation Obligation { get; }

        /// <summary>
        /// Pour un champ <see cref="Obligation.ObligatoireSiGroupe"/> : champ facultatif le plus proche qui
        /// le contient ; <c>null</c> sinon.
        /// </summary>
        public ChampCda? Groupe { get; }

        /// <summary>Sources de la règle : STD, SFD (volet IMG-CR-IMG), SM (structuration minimale), XSD, INS…</summary>
        public string Source { get; }

        /// <summary>Origine de la valeur dans un document produit par la librairie.</summary>
        public OrigineValeur Origine { get; }

        /// <summary>Valeur produite par la librairie (valeur fixe ou valeur par défaut), sinon <c>null</c>.</summary>
        public string? ValeurParDefaut { get; }

        /// <summary>
        /// Source de la donnée : acteur ou système qui la fournit (logiciel du radiologue, RIS de la structure
        /// d'imagerie, médecin demandeur…), y compris en téléradiologie, d'après les rôles décrits par l'ANS
        /// (SFD 3.2, STD 3.2) ; « Librairie CdaCrImg » pour une valeur fixe.
        /// </summary>
        public string SourceDonnee { get; }

        /// <summary>
        /// Élément du volet Téléradiologie du CI-SIS (guide HL7 v2.5.1, 0.2.0) qui véhicule la donnée : flux, segment
        /// et champ HL7 v2 (ex. <c>Flux 4 : IPC-3</c>), ou <c>Absent du volet</c>.
        /// </summary>
        public string VoletTeleradiologie { get; }

        /// <summary>Mention affichable du caractère obligatoire, ex. « Obligatoire si « Identité » est renseigné ».</summary>
        public string MentionObligation => Obligation switch
        {
            Obligation.Obligatoire => "Obligatoire",
            Obligation.Facultatif => "Facultatif",
            _ => $"Obligatoire si « {Groupe!.Libelle} » est renseigné",
        };

        /// <inheritdoc/>
        public override string ToString() => $"{Chemin} [{Cardinalite}] {MentionObligation}";
    }
}
