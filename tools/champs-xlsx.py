#!/usr/bin/env python3
"""Produit docs/cr-img/champs.xlsx (classeur Excel mis en forme) à partir de docs/cr-img/champs.csv.

Le CSV est généré depuis le référentiel des champs de la librairie par le test ReferentielExportTests
(dotnet test dotnet/tests/CdaCrImg.Tests). Usage : python3 tools/champs-xlsx.py  (requiert openpyxl).
"""
import csv
from pathlib import Path

from openpyxl import Workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side

RACINE = Path(__file__).resolve().parent.parent
CSV = RACINE / "docs" / "cr-img" / "champs.csv"
XLSX = RACINE / "docs" / "cr-img" / "champs.xlsx"

POLICE = "Arial"
LARGEURS = [52, 46, 22, 90, 60]
ENTETE = PatternFill("solid", fgColor="1F5FA8")
OBLIGATOIRE = {"Oui": "FDECEA", "Non": "EAF4EC"}
CONDITIONNEL = "FFF4DC"
BORDURE = Border(bottom=Side(style="thin", color="D6DAE1"))

LEGENDE = [
    ("Colonne / abréviation", "Signification"),
    ("Nom du champ", "Section du compte rendu puis libellés des champs qui le contiennent."),
    ("Identifiant technique", "Chemin de la propriété dans la librairie CdaCrImg ([] = liste), "
                              "identique au chemin des erreurs de CrImgValidator (sans les index)."),
    ("Obligatoire ?", "Oui / Non, ou « Oui si … est renseigné » : obligatoire dès que le groupe "
                      "facultatif qui le contient est présent."),
    ("Description", "Description métier, cardinalité dans le CDA, valeur fixe ou par défaut produite "
                    "par la librairie, et élément CDA (XPath, préfixe cda = urn:hl7-org:v3)."),
    ("Référence à la documentation", "Paragraphe de docs/cr-img/champs-obligatoires.md et sources de la règle."),
    ("STD", "Volet CI-SIS IMG-CR-IMG 2024.01, spécifications techniques "
            "(docs/cr-img/ans/CI-SIS_VOLET_CONTENUS_IMG-CR-IMG_2024.01_STD_CDA_20251212.pdf)."),
    ("SM", "Structuration minimale des documents de santé CI-SIS "
           "(schematrons/profils/structurationMinimale)."),
    ("XSD", "Schéma CDA R2 (infrastructure/cda/CDA_extended.xsd)."),
    ("INS", "Identité nationale de santé : matricule et traits d'identité."),
    ("Source des données", "Référentiel des champs de la librairie (CdaCrImg.Referentiel.ReferentielChamps), "
                           "vérifié par les tests contre CrImgValidator, CrImgWriter et l'exemple ANS niveau 1."),
]


def entete(feuille, largeurs):
    for cellule in feuille[1]:
        cellule.font = Font(name=POLICE, bold=True, color="FFFFFF")
        cellule.fill = ENTETE
        cellule.alignment = Alignment(vertical="center", wrap_text=True)
    for i, largeur in enumerate(largeurs):
        feuille.column_dimensions[chr(ord("A") + i)].width = largeur
    feuille.freeze_panes = "A2"
    # Impression : paysage, une page en largeur, en-tête répété.
    feuille.page_setup.orientation = "landscape"
    feuille.page_setup.paperSize = feuille.PAPERSIZE_A4
    feuille.sheet_properties.pageSetUpPr.fitToPage = True
    feuille.page_setup.fitToWidth = 1
    feuille.page_setup.fitToHeight = 0
    feuille.print_title_rows = "1:1"


def main():
    with CSV.open(encoding="utf-8-sig", newline="") as f:
        lignes = list(csv.reader(f, delimiter=";"))

    classeur = Workbook()
    champs = classeur.active
    champs.title = "Champs"
    for ligne in lignes:
        champs.append(ligne)
    entete(champs, LARGEURS)
    for rang in champs.iter_rows(min_row=2):
        for cellule in rang:
            cellule.font = Font(name=POLICE, size=10, bold=cellule.column == 1 and "›" not in str(cellule.value))
            cellule.alignment = Alignment(vertical="top", wrap_text=True)
            cellule.border = BORDURE
        obligatoire = rang[2]
        couleur = OBLIGATOIRE.get(obligatoire.value, CONDITIONNEL)
        obligatoire.fill = PatternFill("solid", fgColor=couleur)
        rang[1].font = Font(name="Consolas", size=10)
    champs.auto_filter.ref = champs.dimensions

    legende = classeur.create_sheet("Légende")
    for ligne in LEGENDE:
        legende.append(ligne)
    entete(legende, [30, 110])
    for rang in legende.iter_rows(min_row=2):
        for cellule in rang:
            cellule.font = Font(name=POLICE, size=10, bold=cellule.column == 1)
            cellule.alignment = Alignment(vertical="top", wrap_text=True)

    classeur.save(XLSX)
    print(f"{XLSX.relative_to(RACINE)} : {len(lignes) - 1} champs")


if __name__ == "__main__":
    main()
