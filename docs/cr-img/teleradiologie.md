# Volet Téléradiologie du CI-SIS : analyse pour CdaCrImg

Source : guide d'implémentation HL7 v2 **Volet Téléradiologie**, version **0.2.0** (publiée, remplace la
0.1.0-ballot) : <https://interop.esante.gouv.fr/ig/hl7v2/teleradiologie/0.2.0/> (URL permanente de la
version courante : <https://interop.esante.gouv.fr/ig/hl7v2/teleradiologie/>). Le guide reste en
concertation et peut évoluer.

Écarts 0.1.0-ballot → 0.2.0 (sans effet sur CdaCrImg) : groupe ORDER du flux 4 répétable, un par examen
(Study Instance UID, Accession Number, URL de visionneuse) ; composants ORC-2.3/2.4 (autorité
d'affectation du numéro de demande) ; code système SNOMED CT `SCT` au lieu de `SNT` ; validateurs EVS
publiés.

## 1. En résumé

- Le volet **ne transporte pas le compte rendu**. Il couvre les échanges **transactionnels** entre le
  RIS de la structure d'imagerie et le SI de téléradiologie, en HL7 v2.5.1 sur la base d'IHE RAD SWF :
  demande, annulation, réponse (avec le protocole), complément post-examen.
- Le CR CDA est rédigé **sur la plateforme de téléradiologie**, envoyé au RIS par le volet
  **« Transmission d'un document CDA-R2 en HL7v2 »**, puis publié dans le DMP par le RIS, qui **ne doit
  pas le modifier**. CdaCrImg est donc utilisée **côté plateforme de téléradiologie**, et le CR doit
  être complet en sortie (custodian compris).
- Les flux 1 et 4 apportent l'essentiel des données d'en-tête du CR : INS, Order Placer Number,
  Accession Number, Study Instance UID, code LOINC, date de l'examen, modalité, région, structure
  d'accueil. Plusieurs données restent sans source dans les flux (profession des PS, médecin
  prescripteur, CCAM, secteur d'activité) : voir les écarts.
- Le guide impose des **mentions DRIM-M** dans le CR : l'URL de la visionneuse DRIMbox et la durée de
  rétention des images. Au niveau 1, elles vont **dans le PDF** : CdaCrImg ne les porte pas.

## 2. Acteurs et flux

| Flux | Message HL7 v2.5.1 | Émetteur → récepteur | Oblig. | Contenu utile au CR |
|---|---|---|---|---|
| 1 Transmission de la demande | `ORM^O01` (ORC-1 `NW`), OBR-4 `TRANSMISSION_DEMANDE` | RIS → SI TLR | Oui | patient (PID), venue (PV1), n° de demande (ORC-2), date de la demande (ORC-9), PS responsable de la structure (ORC-12), structure d'imagerie (ORC-21), justification (OBR-13), modalité et région demandées (OBX) |
| 2 Annulation | `ORM^O01` (ORC-1 `CA`) | RIS → SI TLR | Non | — (interdite après le flux 4) |
| 3 Réponse | `ORU^R01` (ORC-1 `OK` / `OC`) | SI TLR → RIS | Oui | téléradiologue (ORC-12), plateforme (ORC-21) ; protocole (OBX `PROTOCOLE_IMAGERIE`) |
| 4 Complément post-examen | `OMI^O23` (ORC-1 `SR`), OBR-4 `COMPLEMENT_POST_ACTE` ; un groupe ORDER par examen | RIS → SI TLR | Oui | date de l'examen (TQ1-7), durée de rétention (TQ1-6, jours), Accession Number (IPC-1), LOINC (IPC-2/IPC-4 et OBX `CODE_ACTE_IMAGERIE`), Study Instance UID (IPC-3), URL partielle DRIMbox (OBX `URL_PARTIELLE_VIEWER`, base64), modalité, région, produits administrés, appareil (OBX) |
| CR (hors volet) | volet Transmission CDA-R2 en HL7v2 | SI TLR → RIS → DMP | — | le CR CDA lui-même |

Chaque message est acquitté (`ACK`). Les documents de la demande (demande numérisée, documents tiers)
passent aussi par le volet de transmission CDA-R2 en HL7v2.

Questions ouvertes du guide : le **mode de transmission du CR** (MSS, DMP) dont le téléradiologue est
informé, et le **séquencement des flux complémentaires** (ORU/MDM), dont le dernier message, ne sont pas
définis.

## 3. Correspondance flux → en-tête CDA (CdaCrImg)

| Donnée | Flux / champ HL7 v2 | Modèle CdaCrImg | Élément CDA | Remarque |
|---|---|---|---|---|
| INS et traits d'identité | PID-3 (`1.2.250.1.213.1.4.10`…, type `INS`), PID-5/7/8, PID-32 `VALI` | `Patient` | `recordTarget` | identité **qualifiée** (PID-32 = `VALI`) requise pour utiliser l'INS |
| Order Placer Number | flux 1/4 ORC-2 = OBR-2 (EI : 2.1 + 2.3 ISO) | `Demandes[].NumeroDemande` | `inFulfillmentOf/order/id` | root = ORC-2.3, extension = ORC-2.1 ; **attribué par le RIS** (écart E2) |
| Accession Number | flux 4 IPC-1 (EI : 1.1 + 1.3 ISO) | `Demandes[].AccessionNumber` | `order/ps3-20:accessionNumber` | **un par examen** (écart E3) |
| Study Instance UID | flux 4 IPC-3 (EI.1 seul) | `Actes[].StudyInstanceUid` | `serviceEvent/id/@root` | cohérent avec le STD (root seul) |
| Code de l'examen | flux 4 OBX `CODE_ACTE_IMAGERIE` (LN, JDV …5.687) ; IPC-2/IPC-4 | `Actes[].Code` | `serviceEvent/code` + `translation` du code du document | même JDV que le CR |
| Code CCAM | aucun champ | `Actes[].CodeCcam` | `translation` CCAM | écart E7 |
| Date de l'examen | flux 4 TQ1-7 | `Actes[].Debut` | `serviceEvent/effectiveTime/low` | pas de date de fin dans les flux |
| Modalité | flux 4 OBX `MODALITE_IMAGERIE` (optionnel ; à défaut flux 1) | `Actes[].Modalites` | `translation` DCM | **JDV différent** (écart E5) |
| Région anatomique | flux 4 OBX `REGION_ANATOMIQUE` sub-ID n.1 (optionnel ; à défaut flux 1) | `Actes[].RegionsAnatomiques` | `translation` SNOMED | même JDV (…5.695) ; la précision topographique (n.2, JDV …5.688) n'a pas de place dans l'en-tête |
| Téléradiologue | flux 3 ORC-12 / OBR-16 (PS_IdNat, nom, prénom) ; plateforme ORC-21 | `Auteurs[0]`, `Actes[].Executant` | `author`, `performer` | profession absente du flux (écart E1) |
| PS responsable de la structure d'accueil | flux 1 ORC-12 / OBR-16 | second `Auteur`, `SignataireLegal` | `author`, `legalAuthenticator` | peut être un manipulateur (écart E1) |
| Structure d'imagerie | flux 1 ORC-21 (nom, Struct_IdNat, FINEG/FINEJ) | `Custodian`, organisation du PS responsable | `custodian`, `representedOrganization` | écarts E8 et E9 |
| Date de la demande | flux 1 ORC-9 | `MedecinsDemandeurs[].DateDemande` | `participant REF/time` | |
| Médecin prescripteur | aucun champ | `MedecinsDemandeurs[]` | `participant typeCode=REF` | écart E4 |
| Venue / rendez-vous | PV1-2 (classe), PV1-19 (identifiant du RDV), PV1-44 | `PriseEnCharge` | `componentOf/encompassingEncounter` | PV1-2 `O`→`AMB`, `I`→`IMP`, `E`→`EMER` ; le modèle n'a pas d'identifiant de venue |

## 4. Données à faire figurer dans le CR (contenu du PDF au niveau 1)

Le guide (SF, classes `Images`, `AppareilImagerieUtilise`, `ProduitsAdministres`) et l'arrêté du
22/09/2006 imposent dans le CR des informations qui, au niveau 1, n'ont pas de place dans l'en-tête
CDA. Elles relèvent donc du PDF produit par le logiciel du téléradiologue :

- **URL d'accès aux images (DRIM-M)** : flux 4, OBX `URL_PARTIELLE_VIEWER`, URL partielle (FQDN, service,
  StudyInstanceUID, AccessionNumber) encodée en base64, **à compléter par l'identifiant du CR** (`idCDA`,
  spécification DRIMbox visionneuse §4.2.2). Conséquence pour l'API : l'`Id` du CR doit être connu
  **avant** de produire le PDF.
- **Durée de rétention des images** : flux 4, TQ1-6 (jours).
- **Appareil d'imagerie** pour les techniques irradiantes : support IUD et modèle (OBX `APPAREIL_IMAGERIE`).
- **Produits administrés** : code ATC V09/V10, numéro de lot, quantité (OBX `PRODUIT_ADMINISTRE`).

Au niveau 3 (hors périmètre), l'URL irait dans la sous-section Catalogue d'objets DICOM
(`IHEInvokeImageDisplay?…&idCDA=…`) et les produits dans les entrées dédiées.

## 5. Écarts et points d'attention

**E1. Auteurs, profession et signataire.** Le guide fait du médecin effecteur (téléradiologue)
l'auteur du CR. Le PS responsable de la structure d'accueil (ORC-12 du flux 1) peut être un médecin
**ou un manipulateur** (SF, « Professionnel de santé de proximité »). Or le STD 3.2 demande comme
second auteur et signataire légal le **médecin** responsable de la structure. Si ORC-12 désigne un
manipulateur, la plateforme doit obtenir le médecin responsable par un autre moyen (convention de
téléradiologie). Par ailleurs, ORC-12 (XCN) ne porte pas la **profession** : le guide prévoit que la
plateforme l'obtienne dans l'annuaire santé à partir du PS_IdNat. CdaCrImg exige `Profession` pour
chaque auteur : c'est donc l'appelant qui doit l'obtenir.

**E2. Numéro de demande.** Pour le guide, l'Order Placer Number est **attribué par le RIS** de la
structure d'imagerie (ORC-2, avec son autorité d'affectation), alors que le référentiel des champs
indique « Médecin demandeur : numéro attribué à sa demande ». En téléradiologie, `NumeroDemande` =
ORC-2 du flux 1. La source de la donnée est corrigée dans le référentiel.

**E3. Accession Number par examen.** Le flux 4 associe un Accession Number à **chaque examen** (un
groupe ORDER par Study Instance UID), tous rattachés au même Order Placer Number. Dans CdaCrImg,
l'Accession Number est porté par la demande (`inFulfillmentOf/order`). Pour N examens, il faut donc N
`Demandes` de même `NumeroDemande` et d'Accession Numbers différents. Le modèle ne relie pas un acte à
sa demande : c'est acceptable pour le CDA, mais à documenter.

**E4. Médecin prescripteur.** Aucun flux ne transporte le médecin prescripteur initial (par exemple le
médecin traitant du cas d'usage 1). Le « PS demandeur » du guide est le PS responsable de la structure
d'accueil. Le `participant REF` (`MedecinsDemandeurs`) doit venir du document de demande (transmis par
le volet CDA-R2 en HL7v2) ou du RIS, hors flux.

**E5. Modalité.** Les flux utilisent le JDV **modalité de demande d'acte** (`1.2.250.1.213.1.1.5.660`),
qui contient des codes non DICOM : `MED-1126/1127` (radiographie), `MED-1128/1129` (échographie),
SNOMED `258174001` (imagerie interventionnelle), `OT`. Le CR exige des codes DICOM du JDV **modalité
d'acquisition** (`…5.618`). La modalité du CR doit donc venir de l'étude DICOM (attribut Modality
0008,0060) ou d'une table de correspondance à définir (radiographie → `CR`/`DX`, échographie → `US`,
interventionnel → `XA`/`RF`…). Seules `CT`, `MR`, `MG`, `NM`, `PT` passent telles quelles.

**E6. Région anatomique.** Elle est optionnelle dans le flux 4 et obligatoire dans le CR (STD 1..*).
À défaut, il faut reprendre celle du flux 1, qui est la région **demandée** et pas forcément celle
**examinée**.

**E7. Code CCAM.** Le SF le cite (« peut être présent ») mais aucun champ HL7 ne le porte (OBX
`CODE_ACTE_IMAGERIE` limité à LOINC). `CodeCcam` reste à la charge du logiciel du téléradiologue ou
d'un échange hors volet.

**E8. Custodian.** Le SF désigne le RIS, et donc la **structure d'imagerie**, comme entité responsable
du CR et de son cycle de vie (publication DMP, sans modification). Cela répond au point ouvert de la
roadmap : `Custodian` = structure d'imagerie qui accueille le patient (ORC-21 du flux 1). Le
référentiel indique encore « selon la convention entre structures ».

**E9. Exécutant et DRIM-Box.** Le guide rattache la DRIMbox (visionneuse) **au RIS de la structure
d'imagerie**. Dans `SampleReports.Teleradiologie`, l'exécutant (`performer`) est le téléradiologue,
rattaché au groupement de téléradiologie. Or le STD fait porter à `performer/representedOrganization`
l'identifiant de la structure (extension pour la DRIM-Box). L'organisation de l'exécutant devrait donc
être la structure d'accueil, et pas la plateforme. À trancher en relisant le STD 3.2 et 3.3.4.

**E10. Secteur d'activité.** `Organisation.SecteurActivite` (obligatoire pour l'organisation de
l'exécutant) n'est dans aucun flux : il est à déduire du FINESS ou de la convention.

**E11. Versions du CR.** Le guide ne traite ni le remplacement d'un CR (`relatedDocument RPLC`) ni
l'addendum. La question des flux complémentaires est explicitement ouverte.

## 6. Suites proposées

1. *(Fait.)* Référentiel des champs (`ReferentielChamps.SourceDonnee`, puis `champs.xlsx`) : sources de
   `NumeroDemande` (RIS, ORC-2), `AccessionNumber` (IPC-1, un par examen), `StudyInstanceUid` (IPC-3),
   `Actes[].Code` (OBX `CODE_ACTE_IMAGERIE`), `Actes[].Debut` (TQ1-7), `Custodian` (structure
   d'imagerie, ORC-21), `MedecinsDemandeurs` (hors flux).
2. Trancher E1 (médecin ou manipulateur), E5 (correspondance des modalités) et E9 (organisation de
   l'exécutant) avec les utilisateurs. Selon le cas, corriger `SampleReports.Teleradiologie`.
3. API : permettre de fixer l'`Id` du CR avant la génération du PDF, pour l'URL DRIM-M (déjà possible
   en renseignant `Id` ; à documenter).
4. Option hors périmètre actuel : un utilitaire de lecture des messages ORM/OMI qui pré-remplit
   `CompteRenduImagerie`. Il n'en faut pas tant que l'appelant dispose déjà d'un moteur HL7 v2.
