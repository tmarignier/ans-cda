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

## Lot 2 — Robustesse et diffusion

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
