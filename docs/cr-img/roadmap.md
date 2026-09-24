# Feuille de route CdaCrImg

Chaque lot se termine par : `dotnet test dotnet/CdaCrImg.sln` vert **et** un document produit par
la librairie validé sans `failed-assert` par `tools/validate-cda.sh` (volet + profils transverses
concernés).

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

## Lot 2 — Corps structuré minimal conforme

- [ ] Infrastructure section/narratif (`NarrativeBuilder`, `ID` + références)
- [ ] Section Acte imagerie + entrée Technique-imagerie
- [ ] Sous-section Catalogue d'objets DICOM (Examen → Séries → Instances SOP)
- [ ] Section Conclusion
- **Critère** : CR minimal (1 acte, catalogue, conclusion) sans failed-assert sur `CI-SIS_IMG-CR-IMG_2024.01`, `profils/CI-SIS_ModelesDeContenusCDA`, `profils/IHE`

## Lot 3 — Robustesse et diffusion

- [ ] `Validate()` : règles métier du volet en C# (messages alignés sur le schématron)
- [ ] Lecture des JDV SVS (embarqués ou fournis par l'appelant) pour contrôler les codes
- [ ] Test de non-régression : reconstruire l'exemple ANS de référence depuis le modèle
- [ ] Packaging NuGet (métadonnées, README, SourceLink), CI GitHub Actions (build + tests + validation Java)

## Lot 4 — Sections et entrées optionnelles

- [ ] Informations cliniques (Demande d'examen, Historique médical + observations)
- [ ] Administration de produits de santé, commentaires (FR-Commentaire-ER)
- [ ] Complications, Exposition aux radiations (exposition patient, grossesse, quantités, radiopharmaceutiques)
- [ ] Résultats, Résultats d'examens non codés, Examen comparatif, Commentaire non codé
- [ ] Addendum, Documents ajoutés (PDF), Dispositifs médicaux, Éducation du patient

## Points d'attention

- Nom définitif du package / espace de noms : `CdaCrImg`
- Périmètre du niveau 1 (PDF seul) vs niveau 3 : La priorité est le niveau 1
- Lecture/parsing d'un CR existant (CDA → modèle) : Pas dans le périmètre
- Génération d'une archive IHE XDM / métadonnées XDS (DMP) : hors périmètre pour le moment
- Signature / horodatage : hors périmètre pour le moment
