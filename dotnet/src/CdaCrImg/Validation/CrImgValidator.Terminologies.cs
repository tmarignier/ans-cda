using System;
using System.Linq;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;
using CdaCrImg.Terminologies;

namespace CdaCrImg.Validation
{
    /// <summary>
    /// Contrôle des codes par les jeux de valeurs du CI-SIS : ceux que la structuration minimale impose pour
    /// l'en-tête (« The value for code SHALL be selected from value set … ») et ceux du volet pour les actes.
    /// </summary>
    public static partial class CrImgValidator
    {
        private static void ValidateTerminologies(CompteRenduImagerie cr, CrImgValidationOptions options, Action<string, string> err)
        {
            if (!options.ControlerTerminologies) return;

            void Check(Code? code, JeuDeValeurs jdv, string path)
            {
                if (code != null && !jdv.Contient(code.Value, code.CodeSystem))
                    err(path, $"« {code.Value} » ({code.CodeSystem}) n'appartient pas au jeu de valeurs {jdv.Nom} ({jdv.Oid}).");
            }

            void CheckName(string? value, JeuDeValeurs jdv, string path)
            {
                if (!string.IsNullOrEmpty(value) && jdv.Concepts.All(c => c.Code != value))
                    err(path, $"« {value} » n'appartient pas au jeu de valeurs {jdv.Nom} ({jdv.Oid}).");
            }

            Check(cr.Confidentialite, JeuxDeValeursCisis.Confidentialite, "Confidentialite");
            for (var i = 0; i < cr.Auteurs.Count; i++)
                Check(cr.Auteurs[i].Fonction, JeuxDeValeursCisis.Fonction, $"Auteurs[{i}].Fonction");

            foreach (var (path, ps) in ModelPaths.Professionnels(cr))
            {
                Check(ps.Profession, JeuxDeValeursCisis.ProfessionSavoirFaire, path + ".Profession");
                CheckName(ps.Nom?.Prefix, JeuxDeValeursCisis.Civilite, path + ".Nom.Prefix");
                CheckName(ps.Nom?.Suffix, JeuxDeValeursCisis.Titre, path + ".Nom.Suffix");
            }
            foreach (var (path, organisation) in ModelPaths.Organisations(cr))
                Check(organisation.SecteurActivite, JeuxDeValeursCisis.SecteurActivite, path + ".SecteurActivite");

            for (var i = 0; i < cr.Actes.Count; i++)
            {
                var acte = cr.Actes[i];
                for (var j = 0; j < acte.Modalites.Count; j++)
                    Check(acte.Modalites[j], JeuxDeValeursCisis.ModaliteAcquisition, $"Actes[{i}].Modalites[{j}]");
                for (var j = 0; j < acte.RegionsAnatomiques.Count; j++)
                    Check(acte.RegionsAnatomiques[j], JeuxDeValeursCisis.RegionAnatomique, $"Actes[{i}].RegionsAnatomiques[{j}]");
                if (options.ActesImagerie != null) Check(acte.Code, options.ActesImagerie, $"Actes[{i}].Code");
            }

            Check(cr.PriseEnCharge?.Modalite, JeuxDeValeursCisis.TypeRencontre, "PriseEnCharge.Modalite");
            Check(cr.PriseEnCharge?.Lieu?.CadreExercice, JeuxDeValeursCisis.CadreExercice, "PriseEnCharge.Lieu.CadreExercice");
        }
    }
}
