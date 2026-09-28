# TestContenuCDA 

#### Contenu du répertoire ####
Le répertoire testContenuCDA contient :
 - un outil testContenuCDA permettant de vérifier la conformité d’un document CDA R2 au volet correspondant.
   - L'outil testContenuCDA vient en complément des validateurs de l'[espace de test](https://interop.esante.gouv.fr/) qui sont utilisés pour la vérification de conformité (ex : conformité Ségur).
   - L'outil testContenuCDA ne permet pas la vérification des archives IHE_XDM.
 - des exemples de documents CDA 

# Viewer CDA

- Un visualiseur de documents CDA est fourni par l'ANS, il est publié comme Release sous le nom ANS_Viewer-CDA_2023.01
- Un document Lisez-Moi y est associé pour expliquer le fonctionnement du Viewer

# Librairie .NET CdaCrImg (branche de travail)

Librairie .NET Standard 2.0 autonome de production du compte rendu d'imagerie au format CDA R2
non structuré (niveau 1, PDF encapsulé), en-tête conforme au volet CI-SIS IMG-CR-IMG 2024.01 : voir `dotnet/`, `CLAUDE.md` et `docs/cr-img/`.

Démonstration : `dotnet run --project dotnet/demo/CdaCrImg.Demo` puis ouvrir l'URL affichée : un
formulaire pré-rempli produit le CDA XML du compte rendu.
