using System;
using System.Collections.Generic;
using System.Linq;

namespace CdaCrImg.Validation
{
    /// <summary>Non-conformité détectée sur le modèle avant production du document.</summary>
    public sealed class ValidationIssue
    {
        /// <summary>Crée une non-conformité.</summary>
        public ValidationIssue(string path, string message)
        {
            Path = path;
            Message = message;
        }

        /// <summary>Chemin de la propriété concernée, ex. <c>Actes[0].Modalites</c>.</summary>
        public string Path { get; }

        /// <summary>Description en français.</summary>
        public string Message { get; }

        /// <inheritdoc/>
        public override string ToString() => $"{Path} : {Message}";
    }

    /// <summary>Levée lorsqu'un compte rendu non conforme est sérialisé.</summary>
    public sealed class CrImgValidationException : Exception
    {
        /// <summary>Crée l'exception.</summary>
        public CrImgValidationException(IReadOnlyList<ValidationIssue> issues)
            : base("Compte rendu d'imagerie non conforme :" + Environment.NewLine
                   + string.Join(Environment.NewLine, issues.Select(i => " - " + i)))
        {
            Issues = issues;
        }

        /// <summary>Non-conformités détectées.</summary>
        public IReadOnlyList<ValidationIssue> Issues { get; }
    }
}
