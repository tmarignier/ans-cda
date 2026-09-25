#!/usr/bin/env bash
# Valide un document CDA avec l'outillage du kit ANS (Java requis) :
#   1. XSD      : infrastructure/cda/CDA_extended.xsd (xsdvalidator-1.3.jar)
#   2. Schématron du volet (par défaut CI-SIS_IMG-CR-IMG_2024.01), compilé en XSLT2 via Saxon HE
# Usage : tools/validate-cda.sh <document.xml> [schematron]
#   schematron = chemin relatif à schematrons/, sans extension. Exemples :
#     CI-SIS_IMG-CR-IMG_2024.01                              (défaut, volet CR imagerie)
#     profils/CI-SIS_ModelesDeContenusCDA                    (modèles de contenus CI-SIS)
#     profils/structurationMinimale/ASIP-STRUCT-MIN-StrucMin (structuration minimale / en-tête)
# Code retour : 0 si aucune erreur XSD ni failed-assert, 1 sinon.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOC="$(realpath "${1:?usage: $0 <document.xml> [schematron]}")"
SCH_NAME="${2:-CI-SIS_IMG-CR-IMG_2024.01}"
MOTEUR="$ROOT/schematrons/moteur"
SAXON="java -cp $MOTEUR/saxon9he.jar net.sf.saxon.Transform"
OUT="${VALIDATE_OUT:-${TMPDIR:-/tmp}/cda-validation}"
mkdir -p "$OUT"
BASE="$(basename "$DOC" .xml)"
status=0

echo "== XSD : $BASE"
java -jar "$MOTEUR/xsdvalidator-1.3.jar" "$DOC" "$ROOT/infrastructure/cda/CDA_extended.xsd" >"$OUT/${BASE}_xsd.xml" 2>/dev/null || true
if grep -q 'result="OK"' "$OUT/${BASE}_xsd.xml"; then
  echo "   OK"
else
  echo "   ÉCHEC (voir $OUT/${BASE}_xsd.xml)"; status=1
fi

# Compilation du schématron (mise en cache ; les chemins relatifs des JDV sont résolus
# depuis schematrons/, d'où la génération du XSL à cet endroit).
SCH="$ROOT/schematrons/${SCH_NAME}.sch"
SCH_ID="$(basename "$SCH_NAME")"
XSL="$(dirname "$SCH")/.compiled_${SCH_ID}.xsl"
if [[ ! -f "$XSL" || "$SCH" -nt "$XSL" ]]; then
  $SAXON -s:"$SCH" -xsl:"$MOTEUR/iso_dsdl_include.xsl" -o:"$OUT/${SCH_ID}_1.sch"
  $SAXON -s:"$OUT/${SCH_ID}_1.sch" -xsl:"$MOTEUR/iso_abstract_expand.xsl" -o:"$OUT/${SCH_ID}_2.sch"
  $SAXON -s:"$OUT/${SCH_ID}_2.sch" -xsl:"$MOTEUR/iso_svrl_for_xslt2.xsl" -o:"$XSL"
fi

echo "== Schématron $SCH_NAME : $BASE"
SVRL="$OUT/${BASE}_${SCH_ID}.svrl"
if ! $SAXON -s:"$DOC" -xsl:"$XSL" -o:"$SVRL" 2>"$OUT/${BASE}_saxon.log"; then
  echo "   ÉCHEC d'exécution (document mal formé ?) : voir $OUT/${BASE}_saxon.log"; exit 1
fi
fails=$(grep -c "<svrl:failed-assert" "$SVRL" || true)
if [[ "$fails" -eq 0 ]]; then
  echo "   OK"
else
  echo "   $fails failed-assert (voir $SVRL)"
  python3 - "$SVRL" <<'PY'
import sys, xml.etree.ElementTree as ET
ns = {"svrl": "http://purl.oclc.org/dsdl/svrl"}
for fa in list(ET.parse(sys.argv[1]).getroot().iterfind(".//svrl:failed-assert", ns))[:20]:
    print("   -", " ".join("".join(fa.find("svrl:text", ns).itertext()).split()))
    print("     @", fa.get("location"))
PY
  status=1
fi
exit $status
