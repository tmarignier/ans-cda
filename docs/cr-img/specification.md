# Volet IMG-CR-IMG 2024.01 — synthèse pour l'implémentation

> Synthèse de travail établie à partir des artefacts ANS du dépôt. Sources normatives : les
> spécifications ANS `docs/cr-img/ans/*_SFD_*.pdf` (fonctionnelles) et `*_STD_CDA_*.pdf`
> (techniques), et le schématron `schematrons/CI-SIS_IMG-CR-IMG_2024.01.sch` (+ inclusions
> `schematrons/include/specificationsVolets/IMG-CR-IMG_2024.01/**`). **Lorsque la STD est plus stricte
> que le schématron, la librairie applique la STD** (marqué « STD » ci-dessous). L'exemple de référence
> `ExemplesCDA/IMG_CR_IMG_2024.01.xml` illustre un document complet (scanner tête + cou + rachis).
>
> Le document doit **aussi** satisfaire les couches transverses : XSD CDA R2
> (`infrastructure/cda/CDA_extended.xsd`), structuration minimale CI-SIS
> (`schematrons/profils/structurationMinimale`), modèles de contenus CI-SIS
> (`schematrons/profils/CI-SIS_ModelesDeContenusCDA.sch`) et IHE (`schematrons/profils/IHE.sch`).
> Les règles génériques (INS, auteur, custodian…) viennent de ces couches.
>

Espaces de noms : `urn:hl7-org:v3` (défaut), `xsi`, `ps3-20` = `urn:dicom-org:ps3-20`.

## 1. Niveaux de structuration

| Niveau | Corps | Conformité |
|---|---|---|
| **3 (structuré)** — cible principale | `structuredBody` avec sections/entrées DICOM PS3.20 | Schématron IMG-CR-IMG + profils transverses |
| **1 (non structuré)** | `nonXMLBody/text` PDF base64 (`mediaType="application/pdf" representation="B64"`) | Profils transverses uniquement. La STD définit IMG-CR-IMG comme un modèle **à corps structuré** ; le niveau 1 est le document CDA non structuré générique du CI-SIS, avec le même en-tête. Structuration minimale : **seuls** les templateId `2.16.840.1.113883.2.8.2.1`, `1.2.250.1.213.1.1.1.1` et `1.3.6.1.4.1.19376.1.2.20` sont autorisés (pas de templateId DICOM ni `…1.1.1.45`). Le schématron du volet **n'est pas applicable** (4 failed-assert attendus sur l'exemple N1). |

## 2. En-tête (ClinicalDocument)

Ordre XSD à respecter (CDA R2) : `realmCode, typeId, templateId*, id, code, title, effectiveTime,
confidentialityCode, languageCode, setId, versionNumber, recordTarget, author+, informant*,
custodian, informationRecipient*, legalAuthenticator, authenticator*, participant*,
inFulfillmentOf*, documentationOf+, relatedDocument*, componentOf, component`.

| Élément | Card. | Valeur / règle |
|---|---|---|
| `realmCode/@code` | 1..1 | `FR` |
| `typeId` | 1..1 | root `2.16.840.1.113883.1.3`, extension `POCD_HD000040` |
| `templateId` | 1..* | **tous requis** : `2.16.840.1.113883.2.8.2.1` (HL7 France), `1.2.250.1.213.1.1.1.1` (CI-SIS), `1.2.840.10008.9.1` (DICOM Imaging Report), `1.2.840.10008.9.20` (General Header), `1.2.840.10008.9.21` (Imaging Header), `1.2.250.1.213.1.1.1.45` extension `2024.01` (IMG-CR-IMG) |
| `id` | 1..1 | identifiant unique du document (OID/UUID) |
| `code` | 1..1 | **LOINC `18748-4`** « CR d'imagerie médicale » ; `translation` **1..* (STD), une par acte**, issues du `jdv-code-document-imagerie-cisis` (1.2.250.1.213.1.1.5.687, LOINC des examens) |
| `title` | 1..1 | libre |
| `effectiveTime` | 1..1 | TS avec fuseau (`yyyyMMddHHmmss+zzzz`) |
| `confidentialityCode` | 1..1 | `N` (2.16.840.1.113883.5.25) par défaut |
| `languageCode` | 1..1 | `fr-FR` |
| `setId` + `versionNumber` | 1..1 | gestion des versions ; `relatedDocument typeCode="RPLC"` si remplacement |
| `recordTarget/patientRole` | 1..1 | **CdaCrImg : patients avec INS uniquement.** INS : `id` root `1.2.250.1.213.1.4.8` (NIR) / `.9` (NIA) / `.10` `.11` (test) + IPP local ; si INS : nom de naissance `family@qualifier=BR`, `given` (tous prénoms), `given@qualifier=BR` (1er prénom), `birthTime`, `birthplace/place/addr/county` (code COG), `administrativeGenderCode` **obligatoires** |
| `author` | 1..* | `time`, `assignedAuthor/id` (RPPS : root `1.2.250.1.71.4.2.1`, extension `8…`), `code` profession/spécialité (TRE_G15/R85 `1.2.250.1.213.1.1.4.5`, ex. `G15_10/SM44` radio-diagnostic), `assignedPerson/name`, `representedOrganization` **1..1 (STD)**. En téléradiologie, ajouter un auteur pour le médecin responsable de la structure d'imagerie qui accueille le patient. |
| `custodian` | 1..1 | organisation (id `1.2.250.1.71.4.2.2` + FINESS/SIRET) |
| `legalAuthenticator` | 1..1 | `time`, `signatureCode@code=S`, `assignedEntity` (radiologue signataire ; en téléradiologie, médecin responsable de la structure qui accueille le patient) |
| `authenticator` | 0..* | idem |
| `participant typeCode=REF` | 0..* | **médecin demandeur** des examens (STD) : `time xsi:type=IVL_TS` (`nullFlavor=UNK` si inconnue), `associatedEntity classCode=PROV` |
| `participant typeCode=INF` | 0..* | ex. médecin traitant (`functionCode PCP`) |
| **`inFulfillmentOf/order`** | **1..* (STD ; 0..* au schématron)** | `order/id` **1..1** (Order Placer Number ; `nullFlavor` autorisé si pas de demande dématérialisée) **et `ps3-20:accessionNumber` 1..1** |
| **`documentationOf/serviceEvent`** | **1..*** | un par acte d'imagerie : `id` = **Study Instance UID** (`@root` seul, sans `@extension` — STD) ; `code` **LOINC** de l'examen avec `translation` : CCAM 0..1 (`1.2.250.1.215.300.1`), **modalité 1..*** (DCM `1.2.840.10008.2.16.4`, `qualifier/name@code=121139`, JDV `jdv-modalite-acquisition-cisis` 1.2.250.1.213.1.1.5.618), région anatomique **1..* (STD)** (SNOMED CT, `qualifier/name@code=39111-0` LOINC, JDV `jdv-region-anatomique-cisis` 1.2.250.1.213.1.1.5.695) ; `effectiveTime low/high` (**obligatoire**, structuration minimale) ; `performer typeCode=PRF` (**obligatoire**, structuration minimale) (radiologue + `representedOrganization/id` obligatoire pour la DRIM-Box) |
| `documentationOf` (dépistage) | 0..* | `serviceEvent/code` CIM-10 (ex. `Z13.9`) |
| `componentOf/encompassingEncounter` | 1..1 | `code` (ActCode, ex. `AMB`), `effectiveTime`, `location/healthCareFacility` (`code` cadre d'exercice 1.2.250.1.71.4.2.4) |

## 3. Corps structuré (niveau 3)

Chaque section porte le templateId du standard **puis** le templateId CI-SIS, un `id`, un `code`,
un `title` et un `text` (narratif) **obligatoire**. Les entrées référencent le narratif via
`<reference value="#id"/>` (les `ID` doivent exister dans `text`).

| Ordre | Section | templateIds | code | Card. |
|---|---|---|---|---|
| 1 | FR-DICOM-Addendum | `1.2.840.10008.9.6` + `1.2.250.1.213.1.1.2.210` | LOINC `55107-7` | 0..1 — `author/assignedAuthor/assignedPerson/name` requis |
| 2 | FR-DICOM-informations-cliniques | `1.2.840.10008.9.2` + `…2.205` | LOINC `55752-0` | **1..1 (STD/SFD** ; SCH : 0..1) |
| 2.1 | ↳ FR-DICOM-Demande-examen | `1.2.840.10008.9.7` + `…2.211` | LOINC `55115-0` | **1..1** si parent présent |
| 2.2 | ↳ FR-DICOM-Historique-medical | `2.16.840.1.113883.10.20.22.2.39` + `…2.213` | LOINC `11329-0` | **1..1** si parent présent |
| 3 | **FR-DICOM-Acte-imagerie** (un par acte) | `1.2.840.10008.9.3` + `…2.206` | LOINC `55111-9` | **1..*** |
| 3.1 | ↳ FR-DICOM-Complications | `2.16.840.1.113883.10.20.22.2.37` + `…2.214` | LOINC `55109-3` | 0..1 |
| 3.2 | ↳ FR-DICOM-Exposition-aux-radiations | `1.2.840.10008.9.8` + `…2.215` | LOINC `73569-6` | 0..1 |
| 3.3 | ↳ **FR-DICOM-Object-Catalog** | `2.16.840.1.113883.10.20.6.1.1` + `…2.217` | **DCM `121181`** | **1..1** |
| 4 | FR-Dispositifs-medicaux | `2.16.840.1.11383.10.20.1.7` (sic, coquille ANS) + `1.3.6.1.4.1.19376.1.5.3.1.1.5.3.5` + `…2.1` | LOINC `46264-8` | 0..1 |
| 5 | FR-DICOM-Resultats | `2.16.840.1.113883.10.20.6.1.2` + `…2.208` | LOINC `59776-5` | 0..1 |
| 6 | FR-Resultats-examens-non-code | `1.3.6.1.4.1.19376.1.5.3.1.3.27` + `…2.150` | LOINC `30954-2` | 0..1 |
| 7 | FR-DICOM-Examen-comparatif | `1.2.840.10008.9.4` + `…2.207` | LOINC `18834-2` | 0..1 |
| 8 | **FR-DICOM-Conclusion** | `1.2.840.10008.9.5` + `…2.209` | LOINC `19005-8` | **1..1** |
| — | FR-Commentaire-non-code | `…2.73` | LOINC `55112-7` | 0..1 (absente de l'exemple) |
| 9 | FR-Documents-ajoutes | `…2.37` | LOINC `55107-7` | 0..1 |
| 10 | FR-Education-patient | `1.3.6.1.4.1.19376.1.5.3.1.1.9.38` + `…1.9.39` + `…2.107` | LOINC `34895-3` | 0..1 |

(`…` = `1.2.250.1.213.1.1`.) L'ordre est celui de l'exemple de référence ; le schématron ne
contrôle pas l'ordre mais on le reproduit pour la lisibilité.

## 4. Entrées

### Section Historique médical (2.2)
- **FR-DICOM-Observation** (`2.16.840.1.113883.10.20.6.2.13` + `…3.150`), `observation OBS/EVN` :
  - Antécédents médicaux LOINC `11348-0` **1..*** ;
  - Antécédents chirurgicaux LOINC `47519-4` **1..*** ;
  - Contre-indications LOINC `64100-1` 0..* (valeur : `jdv-contre-indication-cisis`).

### Section Acte imagerie (3)
- **FR-DICOM-Technique-imagerie** (`1.2.840.10008.9.14` + `…3.153`), `procedure PROC/EVN` **1..*** :
  `code` LOINC de l'acte 1..1 (+ `translation` CCAM 0..1) ; `methodCode` **1..*** (modalité,
  `jdv-modalite-acquisition-cisis`) ; `targetSiteCode` 0..* (STD ; localisation anatomique ;
  `qualifier` 0..1 avec `name@code=106233006` SNOMED pour le modificateur topographique, latéralité :
  `jdv-lateralite-technique-imagerie-cisis`) ; `effectiveTime` ; commentaire FR-Commentaire-ER 0..1.
- **FR-DICOM-Administration-produit-de-sante** (`1.2.840.10008.9.13` + `…3.151`),
  `substanceAdministration SBADM/EVN` 0..* : `routeCode` 0..1 (EDQM), `doseQuantity` 0..1,
  `consumable/manufacturedProduct/manufacturedMaterial/code` **1..1** (translations CIP/UCD + ATC),
  `lotNumberText`.
- **FR-Commentaire-ER** (`2.16.840.1.113883.10.20.1.40` + `1.3.6.1.4.1.19376.1.5.3.1.4.2` + `…3.32`),
  `act`, code LOINC `48767-8`, via `entryRelationship typeCode="SUBJ" inversionInd="true"`.

### Sous-section Exposition aux radiations (3.2)
- **FR-DICOM-Exposition-patient** (`…3.165`) 0..1 : `procedure` code DCM `121290` ;
  `participant typeCode=RESP/participantRole` avec `id`, `code` DCM `113850`, `playingEntity/name`.
- **FR-DICOM-Observation** statut de grossesse 0..1 : code SNOMED `364320009`, `value`
  obligatoire (`jdv-statut-grossesse-cisis`).
- **FR-DICOM-Quantite** (`2.16.840.1.113883.10.20.6.2.14` + `…3.154`) 0..* : `code` + `value` (PQ)
  obligatoires (`jdv-quantite-exposition-rayonnements-cisis`, ex. DCM `113813` PDL total).
- **FR-DICOM-Administration-radiopharmaceutique** (`…3.173`) 0..* : code SNOMED `440252007`.

### Sous-section Catalogue d'objets DICOM (3.3)
Arborescence Examen [0..*] → Série générique [1..1] → Objet référencé [1..1] (STD : une seule série
« générale » par examen, un seul objet par série ; le schématron impose `count = 1`) :
- **FR-DICOM-Examen-imagerie** (`1.2.840.10008.9.16` + `…3.155`), `act` : code DCM `113014`,
  `id` = **Study Instance UID** (1..1), `effectiveTime` (1..1) ;
  - `entryRelationship typeCode=COMP` → **FR-DICOM-Serie-imagerie** (`1.2.840.10008.9.17` + `…3.156`)
    **1..1** : `id` = Series Instance UID, code DCM `113015` avec `qualifier` (`name` DCM `121139`,
    `value` = modalité) ;
    - `entryRelationship typeCode=COMP` → **FR-DICOM-SOP-instance-observation**
      (`1.2.840.10008.9.18` + `…3.157`) **1..1** : `observation classCode="DGIMG"`, `id` = SOP
      Instance UID, `code` = SOP Class UID (système DCMUID `1.2.840.10008.2.6.1`), `text`
      **1..1 (STD)** avec `mediaType="application/dicom"` + `reference` = URL IHE Invoke Image Display
      (`https://<location>/IHEInvokeImageDisplay?requestType=STUDY&studyUID=…&Accessionnumber=…&idCDA=…`).

### Section Documents ajoutés (9)
- FR-Document-attache (`…3.18`, `organizer CLUSTER`) : FR-Type-document-attache (`…3.48.18`) +
  `observationMedia` (PDF B64) référencé par `renderMultiMedia`.

### Section Éducation du patient (10)
- FR-Simple-Observation (`1.3.6.1.4.1.19376.1.5.3.1.4.13` + `…3.48`) : code LOINC `99622-3`,
  `text/reference` vers le narratif **obligatoire**.

## 5. Jeux de valeurs utiles (`jeuxDeValeurs/`, format IHE SVS)

| Fichier | OID | Usage |
|---|---|---|
| `jdv-code-document-imagerie-cisis.xml` | 1.2.250.1.213.1.1.5.687 | translations du `ClinicalDocument/code`, code de l'acte (~6 900 LOINC) |
| `jdv-modalite-acquisition-cisis.xml` | 1.2.250.1.213.1.1.5.618 | modalité (serviceEvent, methodCode, série) |
| `jdv-region-anatomique-cisis.xml` | 1.2.250.1.213.1.1.5.695 | région anatomique du serviceEvent |
| `jdv-lateralite-technique-imagerie-cisis.xml` | 1.2.250.1.213.1.1.5.617 | latéralité (modificateur topographique) |
| `jdv-quantite-exposition-rayonnements-cisis.xml` | 1.2.250.1.213.1.1.5.620 | FR-DICOM-Quantite |
| `jdv-contre-indication-cisis.xml` | 1.2.250.1.213.1.1.5.659 | contre-indications |
| `jdv-statut-grossesse-cisis.xml` | 1.2.250.1.213.1.1.5.671 | statut de grossesse |
| `jdv-imagerie-objectif-reference-cisis.xml` | 1.2.250.1.213.1.1.5.672 | objectifs de référence |

Structure SVS : `RetrieveValueSetResponse/ValueSet[@id]/ConceptList/Concept[@code,@codeSystem,@displayName]`.

## 6. Métadonnées XDS (STD §4.1, pour information — hors périmètre de la librairie)

| Métadonnée | Valeur |
|---|---|
| `classCode` | `31` Imagerie médicale |
| `typeCode` | `18748-4` CR d'imagerie médicale |
| `formatCode` | `urn:ihe:rad:CDA:ImagingReportStructuredHeadings:2013` (niveau 3) |
