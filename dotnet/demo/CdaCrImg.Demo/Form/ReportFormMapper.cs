using System.Globalization;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Demo.Form;

/// <summary>
/// Transforme les valeurs saisies dans le formulaire en <see cref="CompteRenduImagerie"/>. Aucune règle
/// d'obligation n'est appliquée ici : un champ vide produit simplement une donnée absente, que
/// <c>CrImgValidator</c> signale ensuite. Seules les saisies inexploitables (date mal formée, valeur
/// sans OID…) produisent une erreur de saisie.
/// </summary>
public static class ReportFormMapper
{
    private static readonly TimeZoneInfo Paris = FindParis();

    /// <summary>Résultat de la transformation.</summary>
    public sealed record Result(CompteRenduImagerie Report, IReadOnlyDictionary<string, string> InputErrors);

    public static Result Map(IReadOnlyDictionary<string, string?> form, byte[]? pdf)
    {
        var errors = new Dictionary<string, string>();
        string? V(string key) => form.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
        bool Any(params string[] keys) => keys.Any(k => V(k) != null);

        DateTimeOffset? DateTime(string key)
        {
            var value = V(key);
            if (value == null) return null;
            if (System.DateTime.TryParseExact(value, new[] { "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            {
                return new DateTimeOffset(local, Paris.GetUtcOffset(local));
            }
            errors[key] = "Date et heure invalides (format attendu : AAAA-MM-JJTHH:MM).";
            return null;
        }

        Identifier? Id(string rootKey, string? extensionKey)
        {
            var root = V(rootKey);
            var extension = extensionKey == null ? null : V(extensionKey);
            if (root == null)
            {
                if (extension != null) errors[rootKey] = "OID obligatoire lorsqu'une valeur est renseignée.";
                return null;
            }
            return new Identifier(root, extension);
        }

        Identifier? Rpps(string key) => V(key) is { } rpps ? Identifier.FromRpps(rpps) : null;
        Identifier? Finess(string key) => V(key) is { } finess ? Identifier.FromFiness(finess) : null;

        Code? Option(string key, IReadOnlyList<Option> options)
        {
            var value = V(key);
            if (value == null) return null;
            var option = options.FirstOrDefault(o => o.Code == value);
            if (option == null)
            {
                errors[key] = "Valeur inconnue.";
                return null;
            }
            return new Code(option.Code, option.CodeSystem, option.Label);
        }

        Code? Profession(string codeKey, string? labelKey = null)
        {
            var label = labelKey == null ? null : V(labelKey);
            if (V(codeKey) is { } code) return Code.ProfessionSavoirFaire(code, label);
            if (label != null) errors[codeKey] = "Code obligatoire lorsqu'un libellé est renseigné.";
            return null;
        }

        PersonName? Name(string prefix)
        {
            if (!Any($"{prefix}.civilite", $"{prefix}.prenom", $"{prefix}.nom", $"{prefix}.titre")) return null;
            return new PersonName(V($"{prefix}.nom") ?? "", V($"{prefix}.prenom"), V($"{prefix}.civilite"), V($"{prefix}.titre"));
        }

        Organisation? Organisation(string finessKey, string? nameKey, string? sectorKey)
        {
            var nom = nameKey == null ? null : V(nameKey);
            var secteur = sectorKey == null ? null : Option(sectorKey, JeuxDeValeurs.SecteurActivite);
            var id = Finess(finessKey);
            return id == null && nom == null && secteur == null ? null : new Organisation { Id = id, Nom = nom, SecteurActivite = secteur };
        }

        // Document
        var cr = new CompteRenduImagerie
        {
            Id = Id("document.id", null),
            SetId = Id("document.setId", null),
            NumeroVersion = V("document.version") is { } version
                ? int.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : Invalid("document.version", "Nombre entier attendu.")
                : 0,
            DocumentRemplace = Id("document.remplace", null),
            Titre = V("document.titre") ?? "",
            DateCreation = DateTime("document.dateCreation") ?? default,
        };
        int Invalid(string key, string message)
        {
            errors[key] = message;
            return 0;
        }
        if (Option("document.confidentialite", FormCatalog.Confidentialites) is { } confidentialite)
        {
            cr.Confidentialite = confidentialite;
        }
        if (V("document.langue") is { } langue) cr.Langue = langue;

        // Patient
        var patient = new Patient
        {
            Ins = V("patient.insType") is { } insRoot ? new Identifier(insRoot, V("patient.insMatricule")) : null,
            NomNaissance = V("patient.nomNaissance") ?? "",
            PrenomsNaissance = V("patient.prenomsNaissance") ?? "",
            PremierPrenomNaissance = V("patient.premierPrenom") ?? "",
            NomUtilise = V("patient.nomUtilise"),
            PrenomUtilise = V("patient.prenomUtilise"),
            Sexe = V("patient.sexe") switch { "M" => Sexe.Masculin, "F" => Sexe.Feminin, _ => Sexe.Inconnu },
            LieuNaissanceCog = V("patient.lieuNaissanceCog"),
            LieuNaissanceCommune = V("patient.lieuNaissanceCommune"),
        };
        if (V("patient.insType") == null && V("patient.insMatricule") != null)
            errors["patient.insType"] = "Type d'INS obligatoire lorsqu'un matricule est renseigné.";
        if (V("patient.dateNaissance") is { } naissance)
        {
            if (System.DateTime.TryParseExact(naissance, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                patient.DateNaissance = date;
            else
                errors["patient.dateNaissance"] = "Date invalide (format attendu : AAAA-MM-JJ).";
        }
        if (Id("patient.ippRoot", "patient.ippValeur") is { } ipp) patient.AutresIdentifiants.Add(ipp);
        if (Any("patient.adresseNumero", "patient.adresseVoie", "patient.adresseCodePostal", "patient.adresseVille"))
        {
            patient.Adresses.Add(new Address
            {
                HouseNumber = V("patient.adresseNumero"),
                StreetName = V("patient.adresseVoie"),
                PostalCode = V("patient.adresseCodePostal"),
                City = V("patient.adresseVille"),
            });
        }
        if (V("patient.telephone") is { } telPatient) patient.Telecoms.Add(Telecom.Phone(telPatient, "H"));
        cr.Patient = patient;

        // Auteur
        var auteur = new Professionnel
        {
            Id = Rpps("auteur.rpps"),
            Profession = Profession("auteur.professionCode", "auteur.professionLibelle"),
            Nom = Name("auteur"),
            Organisation = Organisation("auteur.orgFiness", "auteur.orgNom", "auteur.orgSecteur"),
        };
        if (V("auteur.telephone") is { } telAuteur) auteur.Telecoms.Add(Telecom.Phone(telAuteur, "WP"));
        cr.Auteurs.Add(new Auteur(auteur, DateTime("auteur.horodatage") ?? default)
        {
            Fonction = Option("auteur.fonction", JeuxDeValeurs.FonctionAuteur),
        });

        // Custodian
        cr.Custodian = Organisation("custodian.finess", "custodian.nom", null);

        // Signataire
        cr.SignataireLegal = new Signature(
            new Professionnel
            {
                Id = Rpps("signataire.rpps"),
                Profession = Profession("signataire.professionCode"),
                Nom = Name("signataire"),
            },
            DateTime("signataire.horodatage") ?? default);

        // Médecin demandeur (facultatif)
        if (Any("demandeur.rpps", "demandeur.professionCode", "demandeur.professionLibelle", "demandeur.dateDemande",
                "demandeur.civilite", "demandeur.prenom", "demandeur.nom"))
        {
            cr.MedecinsDemandeurs.Add(new MedecinDemandeur(
                new Professionnel
                {
                    Id = Rpps("demandeur.rpps"),
                    Profession = Profession("demandeur.professionCode", "demandeur.professionLibelle"),
                    Nom = Name("demandeur"),
                },
                DateTime("demandeur.dateDemande")));
        }

        // Demande : sans numéro de demande, nullFlavor (pas de demande dématérialisée).
        cr.Demandes.Add(new DemandeImagerie(
            Id("demande.numeroRoot", "demande.numeroValeur") ?? Identifier.Null(),
            Id("demande.accessionRoot", "demande.accessionValeur")!));

        // Acte
        var acte = new ActeImagerie
        {
            StudyInstanceUid = V("acte.studyUid") ?? "",
            Code = V("acte.loincCode") is { } loinc ? Code.Loinc(loinc, V("acte.loincLibelle")) : null,
            CodeCcam = V("acte.ccamCode") is { } ccam ? Code.Ccam(ccam, V("acte.ccamLibelle")) : null,
            Debut = DateTime("acte.debut"),
            Fin = DateTime("acte.fin"),
            Executant = new Professionnel
            {
                Id = Rpps("acte.executantRpps"),
                Profession = Profession("acte.executantProfessionCode"),
                Organisation = Organisation("acte.executantFiness", "acte.executantOrgNom", "acte.executantSecteur"),
            },
        };
        if (V("acte.ccamCode") == null && V("acte.ccamLibelle") != null)
            errors["acte.ccamCode"] = "Code CCAM obligatoire lorsqu'un libellé est renseigné.";
        if (Option("acte.modalite", JeuxDeValeurs.ModaliteAcquisition) is { } modalite) acte.Modalites.Add(modalite);
        if (Option("acte.region", JeuxDeValeurs.RegionAnatomique) is { } region) acte.RegionsAnatomiques.Add(region);
        cr.Actes.Add(acte);
        cr.Depistage = V("acte.depistage") is "true" or "on";

        // Prise en charge
        cr.PriseEnCharge = new PriseEnCharge
        {
            Modalite = Option("pec.modalite", JeuxDeValeurs.TypeRencontre),
            Debut = DateTime("pec.debut"),
            Fin = DateTime("pec.fin"),
            Lieu = new LieuPriseEnCharge
            {
                Id = Finess("pec.lieuFiness"),
                CadreExercice = Option("pec.cadreExercice", JeuxDeValeurs.CadreExercice),
                Nom = V("pec.lieuNom"),
            },
        };

        // Corps
        cr.Corps = pdf == null ? null : new CorpsPdf(pdf);

        return new Result(cr, errors);
    }

    private static TimeZoneInfo FindParis()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
