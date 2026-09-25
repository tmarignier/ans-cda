# Champs minimaux obligatoires d'un compte rendu d'imagerie (IMG-CR-IMG 2024.01)

> **Périmètre CdaCrImg** : CR d'imagerie **non structuré (CDA R2 niveau 1)**, le compte rendu étant un
> PDF encapsulé, pour des patients identifiés par leur **INS**. Le corps structuré (niveau 3) est hors
> périmètre du projet.

Liste des données **à fournir obligatoirement** pour produire un CR d'imagerie conforme, avec pour
chacune : son nom dans la librairie CdaCrImg, sa description métier et les informations utiles
(élément CDA, format, terminologie, source de la règle).

- **Sources** : spécifications ANS SFD et STD (`docs/cr-img/ans/*.pdf`), schématron du volet
  (`schematrons/CI-SIS_IMG-CR-IMG_2024.01.sch`), structuration minimale CI-SIS
  (`schematrons/profils/structurationMinimale`), XSD CDA R2. Quand les sources divergent, la règle la
  plus stricte est retenue (voir [§ 5](#5-écarts-entre-les-sources)).
- **Contrôle** : les champs des § 1 et 2 sont vérifiés par `CrImgValidator` avant l'écriture ; un
  champ manquant lève `CrImgValidationException` avec son chemin (ex. `Actes[0].Modalites`).
- **Colonne « Source »** : STD = spécification technique, SFD = spécification fonctionnelle,
  SCH = schématron du volet, SM = structuration minimale, XSD = schéma CDA R2.
- **Colonne « Qui fournit la donnée »** : acteur ou système à l'origine de la donnée, d'après les
  rôles décrits par l'ANS, avec la référence (§ de la SFD ou de la STD) entre crochets.
  **†** = non écrit explicitement dans le volet ; déduit des rôles ANS et de l'exemple de référence.
  Acteurs (SFD § 3.2.3 et 4.3) :
  - **Médecin demandeur** : rédige la demande d'actes d'imagerie (indications, acte demandé, numéro de demande).
  - **Médecin effecteur** (radiologue ou médecin nucléaire) : décide du protocole, réalise l'acte,
    rédige et valide le CR. C'est le **créateur** du CR, dans son **LPS** (logiciel de professionnel de santé).
  - **Structure d'imagerie** : RIS (accession number, Study Instance UID), PACS / DRIM-Box source (images, URL d'accès).
  - La **librairie CdaCrImg** reçoit ces données du LPS : elle ne les invente pas, sauf les valeurs fixes du § 3.
- Les valeurs **fixes** (templateId, code du document…) sont produites automatiquement :
  voir [§ 3](#3-valeurs-produites-automatiquement-par-la-librairie).

## Synthèse : nombre de champs obligatoires par section

Un champ = une ligne des tableaux ci-dessous (les lignes de regroupement comme `Auteurs` ou
`Demandes` sont comptées). Tous ces champs sont toujours obligatoires. **Répétition** : le groupe de
champs est à fournir pour chaque élément (un par auteur, par demande, par acte).

| Section | Champs | Répétition |
|---|---:|---|
| [1.1 Document](#11-document--5-champs) | 5 | — |
| [1.2 Patient](#12-patient-patient--7-champs) | 7 | — |
| [1.3 Professionnels et structures](#13-professionnels-et-structures--9-champs-dont-5-par-auteur) | 9 | 5 champs par auteur |
| [1.4 Demande d'examen](#14-demande-dexamen-demandes-1--3-champs-dont-2-par-demande) | 3 | 2 champs par demande |
| [1.5 Acte(s) d'imagerie](#15-actes-dimagerie-documentés-actes-1--8-champs-par-acte) | 8 | 8 champs par acte |
| [1.6 Prise en charge](#16-prise-en-charge-priseencharge--3-champs) | 3 | — |
| **Sous-total en-tête** | **35** | |
| [2. Corps (PDF)](#2-corps-pdf--1-champ) | 1 | — |
| **Total d'un CR minimal** (un auteur, une demande, un acte) | **36** | |

Les 8 valeurs du [§ 3](#3-valeurs-produites-automatiquement-par-la-librairie), produites
automatiquement par la librairie, ne sont pas comptées.

Exemple de CR niveau 1 ne contenant **que** ces champs obligatoires, validé par le XSD et les trois
profils transverses (structuration minimale, modèles de contenus, IHE) :
[`ExemplesCDA/CdaCrImg_IMG-CR-IMG_2024.01_CDA-R2-Niveau-1_minimal.xml`](../../ExemplesCDA/CdaCrImg_IMG-CR-IMG_2024.01_CDA-R2-Niveau-1_minimal.xml)
(généré par le test `ExampleFileTests.GeneratesMinimalExampleInExemplesCda`).

## 1. En-tête

### 1.1 Document — 5 champs

| Nom (CdaCrImg) | Description | Qui fournit la donnée (selon l'ANS) | Informations utiles | Source |
|---|---|---|---|---|
| `Id` | Identifiant unique de cette version du document | LPS du créateur (radiologue / médecin nucléaire) [SFD 4.3] | `ClinicalDocument/id` ; OID ou UUID (`root`, `extension` optionnelle) ; unique pour chaque version | SM, XSD |
| `SetId` | Identifiant commun à toutes les versions du document | LPS du créateur [SFD 4.3] | `setId` ; identique d'une version à l'autre (égal à `Id` pour la 1re version) | SM |
| `NumeroVersion` | Numéro de version | LPS du créateur [SFD 4.3] | `versionNumber` ; entier ≥ 1 (défaut 1) | SM |
| `Titre` | Titre du document | Médecin effecteur, via son LPS [SFD 3.2.3.4, 4.3] | `title` ; texte libre, ex. « CR d'imagerie médicale - Scanner thoracique » | STD |
| `DateCreation` | Date et heure de création du document | LPS du créateur, à la création [SFD 4.3] | `effectiveTime` ; `DateTimeOffset` → `yyyyMMddHHmmss+hhmm` (fuseau obligatoire) | SM |

### 1.2 Patient (`Patient`) — 7 champs

La librairie ne gère que des **patients identifiés par leur INS** : l'INS et tous ses traits
d'identité sont obligatoires (un patient sans INS est refusé par `CrImgValidator`).

| Nom (CdaCrImg) | Description | Qui fournit la donnée (selon l'ANS) | Informations utiles | Source |
|---|---|---|---|---|
| `Ins` | Matricule INS du patient | Structure d'imagerie (LPS / RIS), identité reprise de la demande et qualifiée (identitovigilance) [SFD 3.2.2.1, 3.3.1] † | `patientRole/id` : root `1.2.250.1.213.1.4.8` (INS-NIR), `.9` (INS-NIA), `.10`/`.11` (NIR/NIA de test) + matricule en `extension` | SM, choix CdaCrImg |
| `NomNaissance` | Nom de naissance (acte de naissance) | Structure d'imagerie (LPS / RIS), traits INS [SFD 3.3.1] † | `name/family@qualifier="BR"` | SM (traits INS) |
| `PrenomsNaissance` | Tous les prénoms de l'acte de naissance, séparés par des espaces | Structure d'imagerie (LPS / RIS), traits INS [SFD 3.3.1] † | `name/given` (sans qualifier) | SM (traits INS) |
| `PremierPrenomNaissance` | Premier prénom de l'acte de naissance | Structure d'imagerie (LPS / RIS), traits INS [SFD 3.3.1] † | `name/given@qualifier="BR"` | SM (traits INS) |
| `Sexe` | Sexe administratif | Structure d'imagerie (LPS / RIS), traits INS [SFD 3.3.1] † | `administrativeGenderCode` (HL7 `2.16.840.1.113883.5.1`) : `M` ou `F` (trait INS ; `Sexe.Inconnu` refusé) | SM, XSD |
| `DateNaissance` | Date de naissance | Structure d'imagerie (LPS / RIS), traits INS [SFD 3.3.1] † | `birthTime` `yyyyMMdd` | SM (traits INS) |
| `LieuNaissanceCog` | Code officiel géographique INSEE de la commune de naissance | Structure d'imagerie (LPS / RIS), traits INS [SFD 3.3.1] † | `birthplace/place/addr/county` (ex. `51215` ; `99xxx` pour l'étranger) | SM (traits INS) |

Facultatifs : autres identifiants (`AutresIdentifiants`, ex. IPP : root = OID de l'établissement), nom et prénom utilisés (`qualifier="CL"`), commune de naissance, adresses, télécoms.

### 1.3 Professionnels et structures — 9 champs (dont 5 par auteur)

| Nom (CdaCrImg) | Description | Qui fournit la donnée (selon l'ANS) | Informations utiles | Source |
|---|---|---|---|---|
| `Auteurs` [1..*] | Imageur (radiologue ou médecin nucléaire) qui réalise le CR. En téléradiologie, ajouter le médecin responsable de la structure d'imagerie qui accueille le patient | Médecin effecteur (radiologue / médecin nucléaire) qui rédige le CR ; + médecin responsable de la structure d'accueil en téléradiologie [SFD 3.2.3.4, STD 3.2] | `author` | STD, SM |
| `Auteurs[i].Horodatage` | Date de rédaction / validation par l'auteur | LPS du créateur, à la validation du CR [SFD 4.4] | `author/time` | SM |
| `Auteurs[i].Professionnel.Id` | Identifiant national du PS | LPS du créateur (annuaire des PS / carte CPS) † | `assignedAuthor/id` : root `1.2.250.1.71.4.2.1`, extension `8` + n° RPPS (`Identifier.FromRpps`) | SM |
| `Auteurs[i].Professionnel.Profession` | Profession / spécialité (code **et libellé**) | LPS du créateur (annuaire des PS) † | `assignedAuthor/code`, TRE_G15/R85 (`1.2.250.1.213.1.1.4.5`), `displayName` obligatoire (SM) ; ex. `G15_10/SM44` Radio-diagnostic | SM |
| `Auteurs[i].Professionnel.Organisation` | Structure d'exercice de l'auteur, avec son identifiant | Structure d'imagerie de l'auteur [STD 3.2] | `representedOrganization` **[1..1] pour le CR d'imagerie**, `id` obligatoire (choix CdaCrImg) ; id `1.2.250.1.71.4.2.2` + `1`+FINESS ou `3`+SIRET | **STD** |
| `Custodian.Id` | Structure chargée de la conservation du document | Structure d'imagerie qui produit et conserve le CR † | `custodian/.../representedCustodianOrganization/id` (FINESS/SIRET) ; nom recommandé | SM, XSD |
| `SignataireLegal` | Responsable du document (en téléradiologie : médecin responsable de la structure qui accueille le patient) | Médecin effecteur ; en téléradiologie, médecin responsable de la structure qui accueille le patient [STD 3.2] | `legalAuthenticator` avec `signatureCode@code="S"` (émis automatiquement) | SM, STD |
| `SignataireLegal.Horodatage` | Date de signature | LPS du signataire, à la signature [SFD 4.4] | `legalAuthenticator/time` | SM |
| `SignataireLegal.Professionnel.Id` | Identifiant national du signataire | LPS du signataire (annuaire des PS / carte CPS) † | `assignedEntity/id` (RPPS) | SM |

Facultatif : `MedecinsDemandeurs` (`participant typeCode="REF"`, [0..*]), repris de la demande d'actes d'imagerie [SFD 3.3.1]. S'il est fourni, son
identifiant (`Professionnel.Id`) est exigé, et le libellé de sa profession si le code est renseigné.
Pour tout professionnel dont l'identité est renseignée, le nom de famille est obligatoire (SM : `name/family`).

### 1.4 Demande d'examen (`Demandes`, [1..*]) — 3 champs (dont 2 par demande)

| Nom (CdaCrImg) | Description | Qui fournit la donnée (selon l'ANS) | Informations utiles | Source |
|---|---|---|---|---|
| `Demandes` | Demande(s) d'examen honorée(s) par le CR, au moins une | Médecin demandeur (demande d'actes d'imagerie) [SFD 3.2.3.3, 3.3.1] | `inFulfillmentOf/order` | **STD** (SCH : 0..*) |
| `NumeroDemande` | Order Placer Number, numéro attribué par le demandeur | **Médecin demandeur** : « numéro attribué par le demandeur » [STD 3.2, SFD 3.3.1] | `order/id` ; `Identifier.Null()` (nullFlavor) autorisé si pas de demande dématérialisée | STD, SCH |
| `AccessionNumber` | Accession Number attribué par le RIS | **RIS** : « identifiant de la demande attribué par le RIS » [SFD 3.3.1, STD 3.3.4.5] | `order/ps3-20:accessionNumber` (extension DICOM, `urn:dicom-org:ps3-20`) ; valeur réelle obligatoire | STD, SCH |

### 1.5 Acte(s) d'imagerie documenté(s) (`Actes`, [1..*]) — 8 champs par acte

Un acte par examen réalisé. Chacun produit un `documentationOf/serviceEvent` et une
`translation` du code du document.

| Nom (CdaCrImg) | Description | Qui fournit la donnée (selon l'ANS) | Informations utiles | Source |
|---|---|---|---|---|
| `StudyInstanceUid` | Identifiant DICOM de l'examen (Study Instance UID) | **RIS** : « identifiant de l'examen attribué par le RIS » [STD 3.3.4.5] | `serviceEvent/id@root` **seul** (pas d'extension) ; UID DICOM : chiffres et points, 64 caractères max | STD |
| `Code` | Acte d'imagerie réalisé | Médecin demandeur (acte demandé), confirmé par le médecin effecteur qui décide du protocole [SFD 3.3.1, 3.2.3.4] | `serviceEvent/code`, **LOINC**, jdv-code-document-imagerie-cisis (`1.2.250.1.213.1.1.5.687`), ex. `24727-0` CT tête avec contraste IV | STD, SCH |
| `Modalites` [1..*] | Modalité(s) d'acquisition | Médecin demandeur (modalité demandée), confirmée par le médecin effecteur [SFD 3.3.1, 3.2.3.4] | `code/translation` DCM (`1.2.840.10008.2.16.4`) + `qualifier/name` DCM `121139` (ajouté automatiquement) ; jdv-modalite-acquisition-cisis (`1.2.250.1.213.1.1.5.618`), ex. `CT`, `MR`, `US`, `DX` | STD, SCH |
| `RegionsAnatomiques` [1..*] | Région(s) anatomique(s) examinée(s) | Médecin demandeur (région demandée), confirmée par le médecin effecteur [SFD 3.3.1, 3.2.3.4] | `code/translation` SNOMED CT + `qualifier/name` LOINC `39111-0` (ajouté automatiquement) ; jdv-region-anatomique-cisis (`1.2.250.1.213.1.1.5.695`) | **STD** (SCH : 0..*) |
| `Debut` | Date et heure de début de réalisation | Structure d'imagerie (RIS / modalité), date et heure de l'acte [SFD 3.2.2.1] † | `serviceEvent/effectiveTime/low` | SM |
| `Executant.Id` | Radiologue exécutant | Structure d'imagerie : médecin effecteur responsable de l'exécution [SFD 3.2.3.4] | `serviceEvent/performer@typeCode="PRF"/assignedEntity/id` (RPPS) | SM, STD |
| `Executant.Organisation.Id` | Établissement de rattachement de l'exécutant | Structure d'imagerie (établissement de rattachement, DRIM-Box) [STD 3.2] | `performer/assignedEntity/representedOrganization/id` (extension FR pour la DRIM-Box) | STD |
| `Executant.Organisation.SecteurActivite` | Secteur d'activité de l'établissement de l'exécutant | Structure d'imagerie † | `performer/assignedEntity/representedOrganization/standardIndustryClassCode`, JDV_J04_XdsPracticeSettingCode_CISIS (`1.2.250.1.213.1.1.5.467`), système `1.2.250.1.213.1.1.4.9`, ex. `AMBULATOIRE`, `ETABLISSEMENT` ; `displayName` obligatoire | SM |

Facultatifs : `CodeCcam` (translation CCAM [0..1]), `Fin`, `Depistage` (ajoute un `documentationOf` CIM-10 `Z13.9` ; contexte de la demande fourni par le médecin demandeur [SFD 3.3.1]).

### 1.6 Prise en charge (`PriseEnCharge`) — 3 champs

| Nom (CdaCrImg) | Description | Qui fournit la donnée (selon l'ANS) | Informations utiles | Source |
|---|---|---|---|---|
| `PriseEnCharge` | Contexte de la prise en charge | Structure d'imagerie (lieu de réalisation de l'examen, cf. exemple ANS) ; le contexte de la demande vient du demandeur [SFD 3.3.1] † | `componentOf/encompassingEncounter` | SM |
| `Debut` | Début de la prise en charge | Structure d'imagerie (début de la prise en charge) † | `encompassingEncounter/effectiveTime/low` | SM |
| `Lieu.CadreExercice` | Type de lieu de prise en charge | Structure d'imagerie † | `location/healthCareFacility/code`, JDV_J02 (`1.2.250.1.71.4.2.4`), ex. `SA08` Cabinet de groupe ; `displayName` obligatoire | SM |

Facultatifs mais recommandés : `Modalite` (JDV_J142 : `AMB`, `EMER`, `IMP`… ; `displayName` obligatoire si renseignée), `Fin`, `Lieu.Id`, `Lieu.Nom`, `Lieu.Adresse`.

## 2. Corps (PDF) — 1 champ

| Nom (CdaCrImg) | Description | Qui fournit la donnée (selon l'ANS) | Informations utiles | Source |
|---|---|---|---|---|
| `Corps = new CorpsPdf(pdf)` | Le compte rendu au format PDF | Médecin effecteur, qui rédige le CR dans son LPS [SFD 3.2.3.4, 4.4] | `component/nonXMLBody/text` `mediaType="application/pdf"` `representation="B64"` ; le contenu doit être un PDF (signature `%PDF-`) | SM |

## 3. Valeurs produites automatiquement par la librairie

Ces éléments sont obligatoires dans le document mais **ne sont pas à fournir** :

| Élément CDA | Valeur |
|---|---|
| `realmCode` | `FR` |
| `typeId` | `2.16.840.1.113883.1.3` / `POCD_HD000040` |
| `templateId` | `2.16.840.1.113883.2.8.2.1` (HL7 France), `1.2.250.1.213.1.1.1.1` (CI-SIS), `1.3.6.1.4.1.19376.1.2.20` (document non structuré, IHE XDS-SD) : seuls templateId autorisés par la structuration minimale en non structuré |
| `code` | LOINC `18748-4` « CR d'imagerie médicale » + une `translation` par acte (code LOINC de l'acte, [1..*] STD) |
| `confidentialityCode` | `N` Normal (modifiable : `Confidentialite`) |
| `languageCode` | `fr-FR` (modifiable : `Langue`) |
| `legalAuthenticator/signatureCode` | `S` |
| Qualifiers du `serviceEvent` | `121139` (modalité), `39111-0` (localisation anatomique) |

## 4. Formats et jeux de valeurs contrôlés

Au-delà de leur présence, `CrImgValidator` contrôle la forme des valeurs :

| Donnée | Règle | Source |
|---|---|---|
| Tout identifiant (`@root`) | OID ou UUID (type `uid`) | SM |
| Tout code (`@code`) | sans espace (type `cs`) | SM |
| Télécoms | URL `tel:`, `fax:`, `mailto:`, `http(s):` | SM (type TEL) |
| `Patient.Ins` | 15 caractères (13 + clé ; Corse 2A/2B) ; clé contrôlée pour l'INS-NIR de production | INS |
| `Patient.LieuNaissanceCog` | 5 caractères (ex. `51215`, `2A004`, `99xxx`) | INS |
| `Langue` | code de langue (ex. `fr-FR`) | SM |
| Identifiants nationaux (idNat, roots `1.2.250.1.71.4.2.1` et `.2`) | extension obligatoire ; `8` + RPPS (11 chiffres), `1` + FINESS (9 caractères, Corse 2A/2B), `3` + SIRET (14 chiffres) ; autres préfixes (ADELI…) non contrôlés | idNat (annuaire santé) |
| `Demandes[i].AccessionNumber` | valeur (`extension`) attribuée par le RIS obligatoire | STD 3.3.4.5 |
| `NumeroVersion` | ≥ 2 si `DocumentRemplace` est renseigné | cohérence |
| `Actes[i].StudyInstanceUid` | unique parmi les actes du compte rendu | cohérence |
| Dates | fin d'acte et de prise en charge après le début ; actes et naissance avant la date du document | cohérence |
| Professions (`Profession`) | JDV_J01_XdsAuthorSpecialty_CISIS (1.2.250.1.213.1.1.5.461) | SM |
| Secteurs d'activité | JDV_J04_XdsPracticeSettingCode_CISIS (1.2.250.1.213.1.1.5.467) | SM |
| `PriseEnCharge.Lieu.CadreExercice` | JDV_J02_XdsHealthcareFacilityTypeCode_CISIS (1.2.250.1.213.1.1.5.466) | SM |
| `PriseEnCharge.Modalite` | JDV_J142_TypeRencontre_CISIS (1.2.250.1.213.1.1.5.589) | SM |
| `Auteurs[i].Fonction` | JDV_J47_FunctionCode_CISIS (1.2.250.1.213.1.1.5.124) | SM |
| Civilité / titre des PS | JDV_J245_Civilite_CISIS / JDV_J246_Titre_CISIS | SM |
| `Confidentialite` | jdv-hl7-v3-xBasicConfidentialityKind-cisis | SM |
| `Actes[i].Modalites` | jdv-modalite-acquisition-cisis (1.2.250.1.213.1.1.5.618) | STD |
| `Actes[i].RegionsAnatomiques` | jdv-region-anatomique-cisis (1.2.250.1.213.1.1.5.695) | STD |
| `Actes[i].Code` | jdv-code-document-imagerie-cisis (1.2.250.1.213.1.1.5.687), si fourni via `CrImgValidationOptions.ActesImagerie` | STD |

## 5. Écarts entre les sources

La librairie applique la règle la plus stricte :

| Point | Schématron | STD / SFD | Retenu |
|---|---|---|---|
| `inFulfillmentOf` (demande) | 0..* | 1..* | 1..* |
| Organisation de l'auteur | non contrôlée | 1..1 | 1..1 |
| Région anatomique du `serviceEvent` | 0..* | 1..* | 1..* |
| Translation du code document | non contrôlée | 1..* (une par acte) | une par acte |
