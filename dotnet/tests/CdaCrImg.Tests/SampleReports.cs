using System.Xml.Linq;
using CdaCrImg.Model;
using CdaCrImg.Model.Hl7;

namespace CdaCrImg.Tests;

/// <summary>Comptes rendus de test, repris des données de l'exemple ANS IMG_CR_IMG_2024.01 (niveau 1).</summary>
internal static class SampleReports
{
    private static readonly TimeSpan ParisHiver = TimeSpan.FromHours(1);

    /// <summary>PDF encapsulé dans l'exemple ANS niveau 1.</summary>
    public static byte[] AnsPdf { get; } = Convert.FromBase64String(
        XDocument.Load(RepoPaths.CrImgLevel1Example)
            .Descendants(CdaNamespaces.Hl7 + "nonXMLBody").Single()
            .Element(CdaNamespaces.Hl7 + "text")!.Value);

    public static CompteRenduImagerie Level1()
    {
        var centre = new Organisation
        {
            Id = Identifier.FromFiness("920008059"),
            Nom = "Centre de radiologie Ambroise",
            SecteurActivite = new Code("AMBULATOIRE", CodeSystems.SecteurActivite, "Ambulatoire"),
        };
        centre.Telecoms.Add(Telecom.Phone("0146000000"));
        centre.Adresses.Add(new Address { HouseNumber = "12", StreetName = "Rue Ambroise", PostalCode = "75010", City = "PARIS" });

        var radiologue = new Professionnel
        {
            Id = Identifier.FromRpps("01234560801"),
            Profession = Code.ProfessionSavoirFaire("G15_10/SM44", "Médecin - Radio-diagnostic (SM)"),
            Nom = new PersonName("BIDEAULT", "Jacques", prefix: "M", suffix: "DR"),
            Organisation = centre,
        };
        radiologue.Telecoms.Add(Telecom.Phone("0146000000", "WP"));

        var demandeur = new Professionnel
        {
            Id = Identifier.FromRpps("01234567897"),
            Profession = Code.ProfessionSavoirFaire("G15_10/SM26", "Médecin - Qualifié en Médecine Générale (SM)"),
            Nom = new PersonName("MEDIONI", "Stéphane", prefix: "M", suffix: "DR"),
        };

        var patient = new Patient
        {
            Ins = new Identifier(IdentifierRoots.InsNirTest, "279035121518989"),
            NomNaissance = "PAT-TROIS",
            PrenomsNaissance = "DOMINIQUE MARIE-LOUISE",
            PremierPrenomNaissance = "DOMINIQUE",
            NomUtilise = "PAT-TROIS",
            PrenomUtilise = "DOMINIQUE",
            Sexe = Sexe.Feminin,
            DateNaissance = new DateTime(1979, 3, 28),
            LieuNaissanceCog = "51215",
            LieuNaissanceCommune = "DOMPREMY",
        };
        patient.AutresIdentifiants.Add(new Identifier("1.2.3.4.567.8.9.10", "1234567890121"));
        patient.Adresses.Add(new Address { HouseNumber = "28", StreetName = "Avenue de Breteuil", UnitId = "Escalier A", PostalCode = "75007", City = "PARIS", Country = "FRANCE" });
        patient.Telecoms.Add(Telecom.Phone("0144534551", "H"));

        var date = new DateTimeOffset(2021, 1, 8, 11, 17, 0, ParisHiver);
        var cr = new CompteRenduImagerie
        {
            Id = new Identifier("1.2.250.1.213.1.1.1.45.2024.2.1"),
            SetId = new Identifier("1.2.250.1.213.1.1.1.45.2024.2"),
            NumeroVersion = 2,
            DocumentRemplace = new Identifier("90E1C8EC-F951-4B26-A305-A34848818DD6"),
            Titre = "CR d'imagerie médicale - Scanner Tête + Cou avec injection",
            DateCreation = date,
            Patient = patient,
            Custodian = centre,
            SignataireLegal = new Signature(radiologue, date),
            Depistage = true,
            PriseEnCharge = new PriseEnCharge
            {
                Modalite = new Code("AMB", CodeSystems.Hl7ActCode, "Ambulatoire (hors établissement)"),
                Debut = new DateTimeOffset(2021, 1, 8, 10, 25, 0, ParisHiver),
                Fin = date,
                Lieu = new LieuPriseEnCharge
                {
                    Id = Identifier.FromFiness("920008059"),
                    CadreExercice = new Code("SA08", CodeSystems.CadreExercice, "Cabinet de groupe"),
                    Nom = "Centre de radiologie Ambroise",
                    Adresse = centre.Adresses[0],
                },
            },
            Corps = new CorpsPdf(AnsPdf),
        };
        cr.Auteurs.Add(new Auteur(radiologue, date) { Fonction = new Code("ATTPHYS", CodeSystems.Hl7ParticipationFunction, "Référent") });
        cr.MedecinsDemandeurs.Add(new MedecinDemandeur(demandeur));
        cr.Demandes.Add(new DemandeImagerie(
            new Identifier("1.2.250.1.748.12345678.12", "984375862"),
            new Identifier("1.2.250.1.925.994044785528.27", "105234751")));
        cr.Demandes.Add(new DemandeImagerie(
            Identifier.Null(),
            new Identifier("1.2.250.1.925.994044.27", "105234752")));

        cr.Actes.Add(Acte("1.2.250.1.925.994044.27.123.1876360", Code.Loinc("24727-0", "CT tête avec contraste IV"),
            Code.Ccam("ACQH004", "Scanographie du crâne, de son contenu et du tronc, avec injection intraveineuse de produit de contraste"),
            Code.Snomed("774007", "tête et cou"), radiologue));
        cr.Actes.Add(Acte("1.2.250.1.925.994044.27.123.1876361", Code.Loinc("36235-0", "CT cou avec contraste IV"),
            null, Code.Snomed("774007", "tête et cou"), radiologue));
        return cr;
    }

    private static ActeImagerie Acte(string studyUid, Code loinc, Code? ccam, Code region, Professionnel executant)
    {
        var acte = new ActeImagerie
        {
            StudyInstanceUid = studyUid,
            Code = loinc,
            CodeCcam = ccam,
            Debut = new DateTimeOffset(2021, 1, 8, 10, 25, 0, ParisHiver),
            Fin = new DateTimeOffset(2021, 1, 8, 11, 17, 0, ParisHiver),
            Executant = executant,
        };
        acte.Modalites.Add(Code.Dcm("CT", "Tomodensitométrie"));
        acte.RegionsAnatomiques.Add(region);
        return acte;
    }
}
