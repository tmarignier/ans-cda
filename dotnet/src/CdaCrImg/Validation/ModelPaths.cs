using System.Collections.Generic;
using System.Linq;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Validation
{
    /// <summary>
    /// Parcours du modèle : énumère les professionnels, organisations, identifiants, codes, télécoms et adresses
    /// du compte rendu avec leur chemin (même convention que <see cref="ValidationIssue.Path"/>).
    /// </summary>
    internal static class ModelPaths
    {
        public static IEnumerable<(string Path, Professionnel Ps)> Professionnels(CompteRenduImagerie cr)
        {
            for (var i = 0; i < cr.Auteurs.Count; i++)
                if (cr.Auteurs[i].Professionnel != null) yield return ($"Auteurs[{i}].Professionnel", cr.Auteurs[i].Professionnel);
            if (cr.SignataireLegal?.Professionnel != null) yield return ("SignataireLegal.Professionnel", cr.SignataireLegal.Professionnel);
            for (var i = 0; i < cr.MedecinsDemandeurs.Count; i++)
                if (cr.MedecinsDemandeurs[i].Professionnel != null)
                    yield return ($"MedecinsDemandeurs[{i}].Professionnel", cr.MedecinsDemandeurs[i].Professionnel);
            for (var i = 0; i < cr.Actes.Count; i++)
                if (cr.Actes[i].Executant != null) yield return ($"Actes[{i}].Executant", cr.Actes[i].Executant!);
        }

        public static IEnumerable<(string Path, Organisation Organisation)> Organisations(CompteRenduImagerie cr)
        {
            foreach (var (path, ps) in Professionnels(cr))
                if (ps.Organisation != null) yield return (path + ".Organisation", ps.Organisation);
            if (cr.Custodian != null) yield return ("Custodian", cr.Custodian);
        }

        public static IEnumerable<(string Path, Identifier? Id)> Identifiers(CompteRenduImagerie cr)
        {
            yield return ("Id", cr.Id);
            yield return ("SetId", cr.SetId);
            yield return ("DocumentRemplace", cr.DocumentRemplace);
            if (cr.Patient != null)
            {
                yield return ("Patient.Ins", cr.Patient.Ins);
                for (var i = 0; i < cr.Patient.AutresIdentifiants.Count; i++)
                    yield return ($"Patient.AutresIdentifiants[{i}]", cr.Patient.AutresIdentifiants[i]);
            }
            foreach (var (path, ps) in Professionnels(cr)) yield return (path + ".Id", ps.Id);
            foreach (var (path, organisation) in Organisations(cr)) yield return (path + ".Id", organisation.Id);
            for (var i = 0; i < cr.Demandes.Count; i++)
            {
                yield return ($"Demandes[{i}].NumeroDemande", cr.Demandes[i].NumeroDemande);
                yield return ($"Demandes[{i}].AccessionNumber", cr.Demandes[i].AccessionNumber);
            }
            yield return ("PriseEnCharge.Lieu.Id", cr.PriseEnCharge?.Lieu?.Id);
        }

        public static IEnumerable<(string Path, Code? Code)> Codes(CompteRenduImagerie cr)
        {
            yield return ("Confidentialite", cr.Confidentialite);
            for (var i = 0; i < cr.Auteurs.Count; i++) yield return ($"Auteurs[{i}].Fonction", cr.Auteurs[i].Fonction);
            foreach (var (path, ps) in Professionnels(cr)) yield return (path + ".Profession", ps.Profession);
            foreach (var (path, organisation) in Organisations(cr)) yield return (path + ".SecteurActivite", organisation.SecteurActivite);
            for (var i = 0; i < cr.Actes.Count; i++)
            {
                var acte = cr.Actes[i];
                yield return ($"Actes[{i}].Code", acte.Code);
                yield return ($"Actes[{i}].CodeCcam", acte.CodeCcam);
                for (var j = 0; j < acte.Modalites.Count; j++) yield return ($"Actes[{i}].Modalites[{j}]", acte.Modalites[j]);
                for (var j = 0; j < acte.RegionsAnatomiques.Count; j++) yield return ($"Actes[{i}].RegionsAnatomiques[{j}]", acte.RegionsAnatomiques[j]);
            }
            yield return ("PriseEnCharge.Modalite", cr.PriseEnCharge?.Modalite);
            yield return ("PriseEnCharge.Lieu.CadreExercice", cr.PriseEnCharge?.Lieu?.CadreExercice);
        }

        public static IEnumerable<(string Path, Telecom Telecom)> Telecoms(CompteRenduImagerie cr)
        {
            var owners = new List<(string Path, IList<Telecom> Telecoms)>();
            if (cr.Patient != null) owners.Add(("Patient", cr.Patient.Telecoms));
            owners.AddRange(Professionnels(cr).Select(p => (p.Path, p.Ps.Telecoms)));
            owners.AddRange(Organisations(cr).Select(o => (o.Path, o.Organisation.Telecoms)));
            foreach (var (path, telecoms) in owners)
                for (var i = 0; i < telecoms.Count; i++)
                    yield return ($"{path}.Telecoms[{i}]", telecoms[i]);
        }

        public static IEnumerable<(string Path, Address Address)> Addresses(CompteRenduImagerie cr)
        {
            var owners = new List<(string Path, IList<Address> Addresses)>();
            if (cr.Patient != null) owners.Add(("Patient", cr.Patient.Adresses));
            owners.AddRange(Professionnels(cr).Select(p => (p.Path, p.Ps.Adresses)));
            owners.AddRange(Organisations(cr).Select(o => (o.Path, o.Organisation.Adresses)));
            foreach (var (path, addresses) in owners)
                for (var i = 0; i < addresses.Count; i++)
                    yield return ($"{path}.Adresses[{i}]", addresses[i]);
            if (cr.PriseEnCharge?.Lieu?.Adresse != null) yield return ("PriseEnCharge.Lieu.Adresse", cr.PriseEnCharge.Lieu.Adresse);
        }
    }
}
