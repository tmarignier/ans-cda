# Modèle métier (à construire)

Objets C# (POCO) décrivant un CR d'imagerie **indépendamment du XML** : patient, auteur,
acte(s) documenté(s), sections, entrées. Ils sont transformés en `XDocument` par le
sérialiseur (`System.Xml.Linq`). Voir `docs/cr-img/architecture.md`.
