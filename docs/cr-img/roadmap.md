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
- [x] Sélection d'un exemple de CDA (`Resources/*.xml`, ex. `CR_C.xml`) pour pré-remplir le formulaire,
      PDF de l'exemple compris ; chaque exemple est testé de bout en bout (XSD + trois profils)
- [x] Téléchargement du CDA d'exemple sans commentaires, attributs triés dans l'ordre de la librairie,
      pour le comparer au CDA généré
- **Critère atteint** : formulaire pré-rempli soumis → CDA XML conforme (XSD + structuration minimale,
  modèles de contenus, IHE)
- Limites : un auteur, une demande, un acte, une modalité et une région (la librairie en accepte plusieurs)

## Lot 3 — Robustesse et diffusion ✅

- [x] Règles de format et de cohérence en C# (`CrImgValidator.Formats.cs`), alignées sur les types de données
      contrôlés par la structuration minimale : `@root` de type uid (OID ou UUID), codes de type cs, télécoms
      au format URL, matricule INS (15 caractères, clé contrôlée pour l'INS-NIR de production), code COG,
      code de langue, cohérence des dates
- [x] Jeux de valeurs (`CdaCrImg.Terminologies`) : `JeuDeValeurs` lit les formats IHE SVS et ART-DECOR ;
      `JeuxDeValeursCisis` embarque dix JDV liés aux fichiers du kit ANS (J01 professions, J02, J04, J142,
      J47, J245, J246, confidentialité, modalités, régions) ; `CrImgValidator` contrôle les codes
      (`CrImgValidator.Terminologies.cs`) ; `CrImgValidationOptions` : désactivation, JDV LOINC des actes
      fourni par l'appelant. La démo tire ses listes de ces JDV
- [x] Non-régression (`AnsExampleNonRegressionTests`) : l'exemple ANS niveau 1 est reconstruit depuis le
      modèle et comparé structurellement à l'original ; écarts listés explicitement (éléments non gérés,
      incohérence de l'exemple ANS sur la 3e translation du code du document)
- [x] Paquet NuGet `CdaCrImg` 0.1.0 (README, SourceLink, symboles snupkg, zéro dépendance) ;
      CI GitHub Actions `.github/workflows/cdacrimg.yml` (Linux + Windows : build, tests dont schématrons
      Java, pack)
- **Critère atteint** : tests verts (266), document produit conforme XSD + trois profils transverses
- À décider : licence du paquet (aucune licence de projet dans le dépôt) et publication sur nuget.org

## Lot 4 — Analyse d'un CDA existant ✅

Objectif : à partir d'un fichier CDA fourni en entrée, présenter la **liste précise des champs
renseignés** dans le document, avec le **caractère obligatoire ou facultatif** de chaque champ.

- [x] Référentiel des champs dans la librairie (`CdaCrImg.Referentiel`, source unique) : pour chaque champ
      de l'en-tête et le corps, chemin CdaCrImg, emplacement CDA (XPath relatif au parent), cardinalité,
      description, caractère **obligatoire / facultatif / obligatoire si le groupe est renseigné**, source
      de la règle, origine de la valeur (appelant, valeur par défaut, valeur fixe) ; vérifié par les tests
      contre `CrImgValidator`, le modèle, `CrImgWriter` et l'exemple ANS niveau 1 ; le `FormCatalog` de la
      démo y est relié (élément CDA affiché, mention obligatoire cohérente)
- [x] API d'analyse (`CdaCrImg.Analyse.CdaAnalyseur`, zéro dépendance, `System.Xml.Linq` + XPath) :
      entrée `XDocument`, `Stream` ou chemin de fichier ; sortie (`AnalyseCda`) : liste ordonnée des champs,
      une entrée par occurrence (chemin indexé, ex. `Actes[1].StudyInstanceUid`) avec la valeur lue,
      l'emplacement, le numéro de ligne et le champ du référentiel (caractère obligatoire / facultatif)
- [x] Statut de chaque champ : **présent**, **obligatoire absent**, **facultatif absent**, **en trop**
      (cardinalité dépassée) ; les champs d'un groupe facultatif absent ne sont pas listés ; les éléments
      présents dans le CDA mais non gérés par la librairie (ex. `informant`, `authenticator`,
      `participant` INF, `guardian`) sont listés à part comme **non gérés**
- [x] Documents acceptés : CR d'imagerie CDA R2 niveau 1 (`nonXMLBody`) ; un CDA niveau 3 est analysé
      pour son en-tête, le corps structuré étant signalé hors périmètre ; un document qui n'est pas un CR
      d'imagerie (code 18748-4) ou pas un CDA est refusé avec un message explicite
- [x] Démo : page « Analyser un CDA » (`/Analyser` : dépôt d'un fichier ou choix d'un exemple de
      `Resources/`) affichant le récapitulatif, le tableau des champs par section (libellé, chemin, valeur,
      obligatoire / facultatif, cardinalité, statut, emplacement et ligne), filtrable (statut, obligation,
      valeurs fixes, recherche), et les éléments non gérés
- [x] Tests : analyse de l'exemple ANS niveau 1, des exemples de la démo et des CDA produits par la
      librairie (tous les champs obligatoires présents, aucun champ manquant entre écriture et analyse) ;
      cas négatifs (champ obligatoire absent, document qui n'est pas un CR d'imagerie)
- **Critère atteint** : pour tout CDA d'exemple du dépôt, la liste des champs est produite, chaque champ
  y porte son caractère obligatoire / facultatif conforme au référentiel, et les champs obligatoires
  absents sont signalés
- Hors lot : reconstruction complète du modèle `CompteRenduImagerie` depuis un CDA (CDA → modèle → CDA)

## Téléradiologie (contexte d'utilisation)

La librairie est utilisée en téléradiologie : examen réalisé dans la structure qui accueille le patient,
interprété à distance par le téléradiologue (SFD 3.2.2, STD 3.2).

- [x] En-tête : plusieurs auteurs (téléradiologue + médecin responsable de la structure d'accueil) et
      signataire distinct du rédacteur, déjà permis par le modèle (`Auteurs`, `SignataireLegal`)
- [x] Exemple généré `CdaCrImg_…_teleradiologie.xml` (`SampleReports.Teleradiologie`), validé XSD, trois
      profils transverses et analyse complète
- [x] Documentation : rôles et correspondance avec le modèle (architecture.md, specification.md,
      champs-obligatoires.md) ; colonne « Source de la donnée » du classeur `champs.xlsx`
- [ ] Démo : saisie d'un second auteur (médecin responsable de la structure d'accueil) et d'un
      signataire distinct de l'auteur ; exemple de téléradiologie proposé dans le formulaire
- [x] Analyser le **volet Téléradiologie** du CI-SIS (guide HL7 v2, 0.2.0) : acteurs et flux, identifiants
      partagés avec le CR CDA, place et transmission du compte rendu ([teleradiologie.md](teleradiologie.md))
- [x] Reporter les écarts dans le référentiel des champs (colonne « Source de la donnée » et `champs.xlsx`,
      légende « Volet TLR ») : teleradiologie.md, section 6
- [x] Colonne « Volet téléradiologie » de `champs.xlsx` : flux, segment et champ HL7 v2 du volet qui véhicule
      chaque donnée, ou « Absent du volet » (comparaison avec une librairie conçue selon ce volet)
- [ ] À préciser avec les utilisateurs : médecin ou manipulateur comme PS responsable (E1), correspondance
      des modalités (E5), organisation de l'exécutant (E9) ; circuit de transmission du CR (MSS, DMP),
      question ouverte du volet. Le `custodian` est la structure d'imagerie (E8)

## Points d'attention

- Nom définitif du package / espace de noms : `CdaCrImg`
- Niveau de structuration : niveau 1 (PDF) uniquement ; le niveau 3 (corps structuré) est hors périmètre du projet
- Lecture d'un CR existant : limitée à l'analyse des champs présents (lot 4) ; la reconstruction complète
  du modèle (CDA → `CompteRenduImagerie`) reste hors périmètre
- Génération d'une archive IHE XDM / métadonnées XDS (DMP) : hors périmètre pour le moment
- Signature / horodatage : hors périmètre pour le moment
- Patients : uniquement des patients identifiés par leur INS (INS et traits d'identité obligatoires)
