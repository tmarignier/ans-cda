# Champs minimaux obligatoires d'un compte rendu d'imagerie (IMG-CR-IMG 2024.01)

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
- Les valeurs **fixes** (templateId, code du document…) sont produites automatiquement :
  voir [§ 4](#4-valeurs-produites-automatiquement-par-la-librairie).

## 1. En-tête (communs aux niveaux 1 et 3)

### 1.1 Document

| Nom (CdaCrImg) | Description | Informations utiles | Source |
|---|---|---|---|
| `Id` | Identifiant unique de cette version du document | `ClinicalDocument/id` ; OID ou UUID (`root`, `extension` optionnelle) ; unique pour chaque version | SM, XSD |
| `SetId` | Identifiant commun à toutes les versions du document | `setId` ; identique d'une version à l'autre (égal à `Id` pour la 1re version) | SM |
| `NumeroVersion` | Numéro de version | `versionNumber` ; entier ≥ 1 (défaut 1) | SM |
| `Titre` | Titre du document | `title` ; texte libre, ex. « CR d'imagerie médicale - Scanner thoracique » | STD |
| `DateCreation` | Date et heure de création du document | `effectiveTime` ; `DateTimeOffset` → `yyyyMMddHHmmss+hhmm` (fuseau obligatoire) | SM |

### 1.2 Patient (`Patient`)

| Nom (CdaCrImg) | Description | Informations utiles | Source |
|---|---|---|---|
| `Ins` et/ou `AutresIdentifiants` | Au moins un identifiant du patient | `patientRole/id` [1..*]. INS : root `1.2.250.1.213.1.4.8` (NIR), `.9` (NIA), `.10`/`.11` (NIR/NIA de test) + matricule en `extension`. IPP : root = OID de l'établissement | SM |
| `NomNaissance` | Nom de naissance (acte de naissance) | `name/family@qualifier="BR"` | SM |
| `PrenomsNaissance` | Tous les prénoms de l'acte de naissance, séparés par des espaces | `name/given` (sans qualifier). **Obligatoire si INS** | SM (traits INS) |
| `PremierPrenomNaissance` | Premier prénom de l'acte de naissance | `name/given@qualifier="BR"`. **Obligatoire si INS** | SM (traits INS) |
| `Sexe` | Sexe administratif | `administrativeGenderCode` (HL7 `2.16.840.1.113883.5.1`) : `M`, `F`, `U`. **Obligatoire si INS** (toujours émis, `U` par défaut) | SM, XSD |
| `DateNaissance` | Date de naissance | `birthTime` `yyyyMMdd`. **Obligatoire si INS** (sinon `nullFlavor="UNK"`) | SM (traits INS) |
| `LieuNaissanceCog` | Code officiel géographique INSEE de la commune de naissance | `birthplace/place/addr/county` (ex. `51215` ; `99xxx` pour l'étranger). **Obligatoire si INS** | SM (traits INS) |

Facultatifs : nom et prénom utilisés (`qualifier="CL"`), commune de naissance, adresses, télécoms.

### 1.3 Professionnels et structures

| Nom (CdaCrImg) | Description | Informations utiles | Source |
|---|---|---|---|
| `Auteurs` [1..*] | Imageur (radiologue ou médecin nucléaire) qui réalise le CR. En téléradiologie, ajouter le médecin responsable de la structure d'imagerie qui accueille le patient | `author` | STD, SM |
| `Auteurs[i].Horodatage` | Date de rédaction / validation par l'auteur | `author/time` | SM |
| `Auteurs[i].Professionnel.Id` | Identifiant national du PS | `assignedAuthor/id` : root `1.2.250.1.71.4.2.1`, extension `8` + n° RPPS (`Identifier.FromRpps`) | SM |
| `Auteurs[i].Professionnel.Profession` | Profession / spécialité | `assignedAuthor/code`, TRE_G15/R85 (`1.2.250.1.213.1.1.4.5`), ex. `G15_10/SM44` Radio-diagnostic | SM |
| `Auteurs[i].Professionnel.Organisation` | Structure d'exercice de l'auteur | `representedOrganization` **[1..1] pour le CR d'imagerie** ; id `1.2.250.1.71.4.2.2` + `1`+FINESS ou `3`+SIRET | **STD** |
| `Custodian.Id` | Structure chargée de la conservation du document | `custodian/.../representedCustodianOrganization/id` (FINESS/SIRET) ; nom recommandé | SM, XSD |
| `SignataireLegal` | Responsable du document (en téléradiologie : médecin responsable de la structure qui accueille le patient) | `legalAuthenticator` avec `signatureCode@code="S"` (émis automatiquement) | SM, STD |
| `SignataireLegal.Horodatage` | Date de signature | `legalAuthenticator/time` | SM |
| `SignataireLegal.Professionnel.Id` | Identifiant national du signataire | `assignedEntity/id` (RPPS) | SM |

Facultatif : `MedecinsDemandeurs` (`participant typeCode="REF"`, [0..*]). S'il est fourni, son
identifiant (`Professionnel.Id`) est exigé.

### 1.4 Demande d'examen (`Demandes`, [1..*])

| Nom (CdaCrImg) | Description | Informations utiles | Source |
|---|---|---|---|
| `Demandes` | Demande(s) d'examen honorée(s) par le CR, au moins une | `inFulfillmentOf/order` | **STD** (SCH : 0..*) |
| `NumeroDemande` | Order Placer Number, numéro attribué par le demandeur | `order/id` ; `Identifier.Null()` (nullFlavor) autorisé si pas de demande dématérialisée | STD, SCH |
| `AccessionNumber` | Accession Number attribué par le RIS | `order/ps3-20:accessionNumber` (extension DICOM, `urn:dicom-org:ps3-20`) ; valeur réelle obligatoire | STD, SCH |

### 1.5 Acte(s) d'imagerie documenté(s) (`Actes`, [1..*])

Un acte par examen réalisé. Chacun produit un `documentationOf/serviceEvent` et une
`translation` du code du document.

| Nom (CdaCrImg) | Description | Informations utiles | Source |
|---|---|---|---|
| `StudyInstanceUid` | Identifiant DICOM de l'examen (Study Instance UID) | `serviceEvent/id@root` **seul** (pas d'extension) ; UID DICOM : chiffres et points, 64 caractères max | STD |
| `Code` | Acte d'imagerie réalisé | `serviceEvent/code`, **LOINC**, jdv-code-document-imagerie-cisis (`1.2.250.1.213.1.1.5.687`), ex. `24727-0` CT tête avec contraste IV | STD, SCH |
| `Modalites` [1..*] | Modalité(s) d'acquisition | `code/translation` DCM (`1.2.840.10008.2.16.4`) + `qualifier/name` DCM `121139` (ajouté automatiquement) ; jdv-modalite-acquisition-cisis (`1.2.250.1.213.1.1.5.618`), ex. `CT`, `MR`, `US`, `DX` | STD, SCH |
| `RegionsAnatomiques` [1..*] | Région(s) anatomique(s) examinée(s) | `code/translation` SNOMED CT + `qualifier/name` LOINC `39111-0` (ajouté automatiquement) ; jdv-region-anatomique-cisis (`1.2.250.1.213.1.1.5.695`) | **STD** (SCH : 0..*) |
| `Debut` | Date et heure de début de réalisation | `serviceEvent/effectiveTime/low` | SM |
| `Executant.Id` | Radiologue exécutant | `serviceEvent/performer@typeCode="PRF"/assignedEntity/id` (RPPS) | SM, STD |
| `Executant.Organisation.Id` | Établissement de rattachement de l'exécutant | `performer/assignedEntity/representedOrganization/id` (extension FR pour la DRIM-Box) | STD |

Facultatifs : `CodeCcam` (translation CCAM [0..1]), `Fin`, `Depistage` (ajoute un `documentationOf` CIM-10 `Z13.9`).

### 1.6 Prise en charge (`PriseEnCharge`)

| Nom (CdaCrImg) | Description | Informations utiles | Source |
|---|---|---|---|
| `PriseEnCharge` | Contexte de la prise en charge | `componentOf/encompassingEncounter` | SM |
| `Debut` | Début de la prise en charge | `encompassingEncounter/effectiveTime/low` | SM |
| `Lieu.CadreExercice` | Type de lieu de prise en charge | `location/healthCareFacility/code`, JDV_J02 (`1.2.250.1.71.4.2.4`), ex. `SA08` Cabinet de groupe | SM |

Facultatifs mais recommandés : `Modalite` (HL7 ActCode : `AMB`, `EMER`, `IMP`…), `Fin`, `Lieu.Id`, `Lieu.Nom`, `Lieu.Adresse`.

## 2. Corps niveau 1 (non structuré, disponible)

| Nom (CdaCrImg) | Description | Informations utiles | Source |
|---|---|---|---|
| `Corps = new CorpsPdf(pdf)` | Le compte rendu au format PDF | `component/nonXMLBody/text` `mediaType="application/pdf"` `representation="B64"` ; le contenu doit être un PDF (signature `%PDF-`) | SM |

## 3. Corps niveau 3 (structuré, lot 2 à venir)

Sections obligatoires du corps structuré. Chaque section a un `code`, un `title` et un `text`
(narratif) obligatoires, produits par la librairie à partir des données ci-dessous.

| Donnée métier | Description | Informations utiles | Source |
|---|---|---|---|
| **Informations cliniques** | Contexte clinique de l'examen | Section FR-DICOM-Informations-cliniques `1.2.250.1.213.1.1.2.205`, LOINC `55752-0` | **STD/SFD [1..1]** (SCH : 0..1) |
| ↳ Justification de la demande | Indications, symptômes, signes cliniques motivant l'examen (texte) | Sous-section FR-DICOM-Demande-examen `…2.211`, LOINC `55115-0` ; la finalité de l'examen y est facultative | STD, SCH |
| ↳ Antécédents médicaux | Antécédents significatifs et pertinents pour l'examen ; si aucun, l'indiquer (texte) | Sous-section FR-DICOM-Historique-medical `…2.213` (LOINC `11329-0`), entrée FR-DICOM-Observation `…3.150` code LOINC `11348-0` | STD, SCH |
| ↳ Antécédents chirurgicaux | Idem pour les antécédents chirurgicaux (texte) | Même sous-section, entrée `…3.150` code LOINC `47519-4` | STD, SCH |
| **Technique d'imagerie** (une par acte) | Description de l'acte réalisé | Section FR-DICOM-Acte-imagerie `…2.206`, LOINC `55111-9`, titre « Technique d'imagerie » | STD, SCH |
| ↳ Acte d'imagerie | Acte réalisé | Entrée FR-DICOM-Technique-imagerie `…3.153` : `procedure/code` LOINC (jdv `…5.687`) | STD, SCH |
| ↳ Modalité(s) d'acquisition [1..*] | Modalité(s) de l'acte | `procedure/methodCode` DCM (jdv `…5.618`) | STD, SCH |
| **Catalogue d'objets DICOM** (un par acte) | Lien vers les images | Sous-section FR-DICOM-Object-Catalog `…2.217`, code DCM `121181`, titre « Catalogue d'objets DICOM » ; examens [0..*] | STD, SCH |
| ↳ Examen (si présent) | Study Instance UID et date/heure de l'examen | Entrée FR-DICOM-Examen-imagerie `…3.155` : `act/id`, code DCM `113014`, `effectiveTime` | STD, SCH |
| ↳ Série générique [1..1] | Série unique portant le lien vers les images | Entrée FR-DICOM-Serie-imagerie `…3.156` : `id`, code DCM `113015` + qualifier modalité (DCM `121139`) | STD, SCH |
| ↳ Objet référencé [1..1] | Classe SOP et URL d'accès aux images (DRIM-Box source) | Entrée FR-DICOM-SOP-instance-observation `…3.157`, `classCode="DGIMG"` : `code` classe SOP (jdv-sop-class-cisis `…5.689`), `text@mediaType="application/dicom"` + `reference` = URL IHE Invoke Image Display (`https://<location>/IHEInvokeImageDisplay?requestType=STUDY&studyUID=…&Accessionnumber=…&idCDA=…`) | STD |
| **Conclusion** | Réponse à la question posée : diagnostics, recommandations (texte) | Section FR-DICOM-Conclusion `…2.209`, LOINC `19005-8`, titre « Conclusions » | STD, SFD, SCH |

(`…` = `1.2.250.1.213.1.1`.) Toutes les autres sections (Addendum, Résultats, Examen comparatif,
Complications, Exposition aux rayonnements, Dispositifs médicaux, Documents ajoutés, Information
au patient…) sont facultatives.

## 4. Valeurs produites automatiquement par la librairie

Ces éléments sont obligatoires dans le document mais **ne sont pas à fournir** :

| Élément CDA | Valeur |
|---|---|
| `realmCode` | `FR` |
| `typeId` | `2.16.840.1.113883.1.3` / `POCD_HD000040` |
| `templateId` | Niveau 1 : `2.16.840.1.113883.2.8.2.1`, `1.2.250.1.213.1.1.1.1`, `1.3.6.1.4.1.19376.1.2.20`. Niveau 3 : les deux premiers + `1.2.840.10008.9.1`, `.9.20`, `.9.21`, `1.2.250.1.213.1.1.1.45` ext. `2024.01` |
| `code` | LOINC `18748-4` « CR d'imagerie médicale » + une `translation` par acte (code LOINC de l'acte, [1..*] STD) |
| `confidentialityCode` | `N` Normal (modifiable : `Confidentialite`) |
| `languageCode` | `fr-FR` (modifiable : `Langue`) |
| `legalAuthenticator/signatureCode` | `S` |
| Qualifiers du `serviceEvent` | `121139` (modalité), `39111-0` (localisation anatomique) |

## 5. Écarts entre les sources

La librairie applique la règle la plus stricte :

| Point | Schématron | STD / SFD | Retenu |
|---|---|---|---|
| `inFulfillmentOf` (demande) | 0..* | 1..* | 1..* |
| Organisation de l'auteur | non contrôlée | 1..1 | 1..1 |
| Région anatomique du `serviceEvent` | 0..* | 1..* | 1..* |
| Translation du code document | non contrôlée | 1..* (une par acte) | une par acte |
| Section Informations cliniques | 0..1 | 1..1 | 1..1 (lot 2) |
| Localisation anatomique de la technique (`targetSiteCode`) | non contrôlée | 0..* | 0..* |
