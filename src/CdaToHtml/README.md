# CdaToHtml

Outil en ligne de commande **.NET 10** permettant de transformer un fichier **CDA R2 (XML HL7)** en **HTML** à l'aide d'une feuille de style **XSL**.

Compatible avec les feuilles de style du dépôt ANS (`FeuilleDeStyle/`).

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 (17.x ou supérieur) *ou* Visual Studio Code avec l'extension C#

## Ouvrir dans Visual Studio

Double-cliquer sur `src/CdaToHtml.slnx` pour ouvrir la solution dans Visual Studio.

## Utilisation

```
CdaToHtml <cda.xml> <feuille-de-style.xsl> <sortie.html>
```

### Arguments

| Argument             | Description                              |
|----------------------|------------------------------------------|
| `cda.xml`            | Fichier CDA source (XML HL7 CDA R2)      |
| `feuille-de-style.xsl` | Feuille de style XSL à appliquer      |
| `sortie.html`        | Chemin du fichier HTML à générer         |

### Options

| Option            | Description              |
|-------------------|--------------------------|
| `-h`, `--help`    | Affiche l'aide           |
| `-v`, `--version` | Affiche la version       |

## Exemples

Depuis la racine du dépôt :

```bash
# Avec la feuille de style biologie
dotnet run --project src/CdaToHtml -- \
  ExemplesCDA/BIO-CR-BIO_2024.01_Glycemie_mmol_par_Litre_1.xml \
  FeuilleDeStyle/cda_CRBIO.xsl \
  output/resultat.html

# Avec la feuille de style générique ANS
dotnet run --project src/CdaToHtml -- \
  ExemplesCDA/DOC_NON_STRUCTURE_CDA-R2-N1.xml \
  FeuilleDeStyle/CDA-FO.xsl \
  output/resultat.html
```

## Publier un exécutable autonome

```bash
dotnet publish src/CdaToHtml -c Release -r win-x64 --self-contained true
```

L'exécutable se trouve dans `src/CdaToHtml/bin/Release/net10.0/win-x64/publish/`.

## Notes techniques

| Point                    | Explication                                                                 |
|--------------------------|-----------------------------------------------------------------------------|
| `enableDocumentFunction` | Activé pour les XSL ANS qui utilisent `document('cda_l10n.xml')`           |
| `enableScript`           | Activé pour les XSL qui contiennent des blocs `msxsl:script`               |
| `XmlUrlResolver`         | Passé à `Load()` **et** à `Transform()` pour résoudre les URIs relatives   |
| `DtdProcessing.Ignore`   | Les fichiers CDA référencent parfois une DTD HL7 non accessible localement  |
