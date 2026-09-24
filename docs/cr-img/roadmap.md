# Feuille de route CdaCrImg

Périmètre : CR d'imagerie **non structuré (CDA R2 niveau 1, PDF encapsulé)**, patients avec **INS**.
Le corps structuré (niveau 3) est hors périmètre.

Chaque lot se termine par : `dotnet test dotnet/CdaCrImg.sln` vert **et** un document produit par
la librairie valide XSD et sans `failed-assert` sur les trois profils transverses (structuration
minimale, modèles de contenus CI-SIS, IHE), via les tests `Category=Schematron`.

## Lot 0 — Socle ✅

- [x] Solution `dotnet/` : lib netstandard2.0 sans dépendance + tests net10.0 (xUnit)
- [x] Constantes du volet (`TemplateIds`, `Codes`, `CodeSystems`, `IdentifierRoots`, `CdaNamespaces`) vérifiées contre les artefacts ANS
- [x] Validation XSD CDA en .NET (`CdaXsdValidator`) + tests négatifs
- [x] Script `tools/validate-cda.sh` (XSD + schématrons, Linux)
- [x] Documentation : `CLAUDE.md`, `docs/cr-img/{specification,architecture,roadmap}.md`
- [x] Hook de session (installation SDK .NET 10)

## Lot 1 — Types HL7 et en-tête ✅

- [x] Types de base : `Identifier` (+ nullFlavor, RPPS/FINESS/SIRET), `Code` (+ translation/qualifier), `PersonName`, `Address`, `Telecom`, horodatage TS avec fuseau (`Hl7Format`)
- [x] Modèle : `Patient` (INS + traits), `Professionnel`, `Organisation`, `Auteur`, `Signature`, `MedecinDemandeur`, `DemandeImagerie`, `ActeImagerie`, `PriseEnCharge`, `CompteRenduImagerie`
- [x] En-tête complet : templateIds, code 18748-4 (+ translation par acte), recordTarget, author, custodian, legalAuthenticator, participant REF, `inFulfillmentOf` (accessionNumber ps3-20), `documentationOf/serviceEvent` (Study UID, LOINC, CCAM, modalité, région, performer), dépistage Z13.9, `relatedDocument` RPLC, `componentOf`
- [x] Corps niveau 1 (PDF base64, `CorpsPdf`)
- [x] `CrImgValidator` : contrôles de complétude avant écriture (`CrImgValidationException`)
- **Critère atteint** : document N1 produit valide XSD (.NET et Java) + `structurationMinimale`, `CI-SIS_ModelesDeContenusCDA`, `IHE` sans failed-assert (tests `Category=Schematron`)
- Reporté : `authenticator`, `informant`, `participant INF` (médecin traitant), `informationRecipient`

## Lot 2 — Application web de démonstration ✅

Application ASP.NET Core (`dotnet/demo/CdaCrImg.Demo`) exposant un formulaire qui montre le bon
fonctionnement de la librairie : `dotnet run --project dotnet/demo/CdaCrImg.Demo`.

- [x] Un champ de formulaire pour chaque donnée gérée par la librairie (en-tête complet + PDF), décrit
      dans un catalogue unique (`Form/FormCatalog.cs`) ; listes déroulantes issues des JDV ANS
      (embarqués dans la librairie depuis le lot 3)
- [x] Pour chaque champ : mention **obligatoire / facultatif / obligatoire si le groupe est renseigné**,
      **valeur d'exemple** pré-remplie, **description** et propriété CdaCrImg correspondante
- [x] À la validation : le CDA XML est renvoyé (affichage ou téléchargement) ; en cas de données
      incomplètes, le formulaire est réaffiché avec les erreurs par champ et celles de `CrImgValidator`
- [x] Tests : la mention de chaque champ est vérifiée contre `CrImgValidator` ; tests HTTP de bout en bout ;
      le CDA produit avec les valeurs d'exemple est valide XSD et passe les trois profils transverses
- [x] Librairie complétée grâce au formulaire : identifiant de l'organisation de l'auteur, dates de l'auteur
      et du signataire, nom de famille d'un PS identifié, `displayName` des codes contraints par la
      structuration minimale
- **Critère atteint** : formulaire pré-rempli soumis → CDA XML conforme (XSD + structuration minimale,
  modèles de contenus, IHE)
- Limites : un auteur, une demande, un acte, une modalité et une région (la librairie en accepte plusieurs)

## Lot 3 — Robustesse et diffusion

- [ ] Règles métier complémentaires en C# (messages alignés sur la structuration minimale)
- [ ] Lecture des JDV SVS (embarqués ou fournis par l'appelant) pour contrôler les codes (modalités, régions, secteur d'activité…)
- [ ] Test de non-régression : reconstruire l'exemple ANS niveau 1 (`ExemplesCDA/IMG_CR_IMG_2024.01_CDA-R2-Niveau-1.xml`) depuis le modèle et comparer structurellement
- [ ] Packaging NuGet (métadonnées, README, SourceLink), CI GitHub Actions (build + tests + validation Java)

## Points d'attention

- Nom définitif du package / espace de noms : `CdaCrImg`
- Niveau de structuration : niveau 1 (PDF) uniquement ; le niveau 3 (corps structuré) est hors périmètre du projet
- Lecture/parsing d'un CR existant (CDA → modèle) : Pas dans le périmètre
- Génération d'une archive IHE XDM / métadonnées XDS (DMP) : hors périmètre pour le moment
- Signature / horodatage : hors périmètre pour le moment
- Patients : uniquement des patients identifiés par leur INS (INS et traits d'identité obligatoires)
