using System;
using System.Collections.Generic;
using System.Linq;
using CdaCrImg.Referentiel;

namespace CdaCrImg.Analyse
{
    /// <summary>Statut d'un champ dans le document analysé.</summary>
    public enum StatutChamp
    {
        /// <summary>Champ présent.</summary>
        Present,

        /// <summary>Champ obligatoire (compte tenu de son groupe) absent.</summary>
        ObligatoireAbsent,

        /// <summary>Champ facultatif absent.</summary>
        FacultatifAbsent,

        /// <summary>Occurrence au-delà de la cardinalité maximale du champ.</summary>
        EnTrop,

        /// <summary>Champ hors du périmètre de la librairie dans ce document (ex. corps PDF d'un CDA niveau 3).</summary>
        HorsPerimetre,
    }

    /// <summary>Occurrence (ou absence) d'un champ du référentiel dans le document analysé.</summary>
    public sealed class ChampAnalyse
    {
        internal ChampAnalyse(ChampCda champ, string chemin, StatutChamp statut, string? valeur, string? emplacement, int? ligne)
        {
            Champ = champ;
            Chemin = chemin;
            Statut = statut;
            Valeur = valeur;
            Emplacement = emplacement;
            Ligne = ligne;
        }

        /// <summary>Champ du référentiel (libellé, cardinalité, caractère obligatoire, XPath).</summary>
        public ChampCda Champ { get; }

        /// <summary>Chemin CdaCrImg de l'occurrence, avec ses index (ex. <c>Actes[1].StudyInstanceUid</c>).</summary>
        public string Chemin { get; }

        /// <summary>Statut du champ dans le document.</summary>
        public StatutChamp Statut { get; }

        /// <summary>Valeur lue, mise en forme pour l'affichage (<c>null</c> pour un champ absent ou un groupe).</summary>
        public string? Valeur { get; }

        /// <summary>Emplacement de l'occurrence dans le document (ex. <c>/ClinicalDocument/author/time</c>).</summary>
        public string? Emplacement { get; }

        /// <summary>Numéro de ligne dans le fichier, si le document a été chargé avec les informations de ligne.</summary>
        public int? Ligne { get; }

        /// <inheritdoc/>
        public override string ToString() => $"{Chemin} [{Champ.MentionObligation}] {Statut}{(Valeur == null ? "" : " : " + Valeur)}";
    }

    /// <summary>Élément du document qui ne correspond à aucun champ du référentiel (non géré par la librairie).</summary>
    public sealed class ElementNonGere
    {
        internal ElementNonGere(string emplacement, int? ligne)
        {
            Emplacement = emplacement;
            Ligne = ligne;
        }

        /// <summary>Emplacement de l'élément (ex. <c>/ClinicalDocument/informant[1]</c>).</summary>
        public string Emplacement { get; }

        /// <summary>Numéro de ligne dans le fichier, si disponible.</summary>
        public int? Ligne { get; }

        /// <inheritdoc/>
        public override string ToString() => Emplacement;
    }

    /// <summary>Résultat de l'analyse d'un CDA : champs du référentiel trouvés ou absents, éléments non gérés.</summary>
    public sealed class AnalyseCda
    {
        internal AnalyseCda(IReadOnlyList<ChampAnalyse> champs, IReadOnlyList<ElementNonGere> nonGeres, bool corpsStructure)
        {
            Champs = champs;
            NonGeres = nonGeres;
            CorpsStructure = corpsStructure;
        }

        /// <summary>Champs dans l'ordre du référentiel : une entrée par occurrence, ou une entrée par champ absent.</summary>
        public IReadOnlyList<ChampAnalyse> Champs { get; }

        /// <summary>Éléments du document non décrits par le référentiel (ex. <c>informant</c>, <c>authenticator</c>).</summary>
        public IReadOnlyList<ElementNonGere> NonGeres { get; }

        /// <summary>Vrai si le document a un corps structuré (niveau 3, hors périmètre : seul l'en-tête est analysé).</summary>
        public bool CorpsStructure { get; }

        /// <summary>Champs présents.</summary>
        public IEnumerable<ChampAnalyse> Presents => Champs.Where(c => c.Statut == StatutChamp.Present);

        /// <summary>Champs obligatoires absents et occurrences en trop.</summary>
        public IEnumerable<ChampAnalyse> Anomalies =>
            Champs.Where(c => c.Statut == StatutChamp.ObligatoireAbsent || c.Statut == StatutChamp.EnTrop);

        /// <summary>Vrai si aucun champ obligatoire ne manque et aucune cardinalité n'est dépassée.</summary>
        public bool EstComplet => !Anomalies.Any();
    }

    /// <summary>Document refusé par l'analyse (XML mal formé, document qui n'est pas un CR d'imagerie CDA).</summary>
    public sealed class CdaAnalyseException : Exception
    {
        /// <summary>Crée l'exception.</summary>
        public CdaAnalyseException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
