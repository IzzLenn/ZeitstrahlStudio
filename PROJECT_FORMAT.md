# Projektformat `.zeitprojekt`

## Überblick

Ein Projektarchiv ist eine versionierte ZIP-Datei mit der Endung `.zeitprojekt`. Die aktuelle Formatversion ist `1`. Innerhalb des Archivs werden ausschließlich `/` als Trennzeichen und UTF-8 für JSON/Text verwendet.

```text
Beispiel.zeitprojekt
├── manifest.json
├── project.db
├── attachments/
├── thumbnails/
├── extracted-text/
├── logs/
└── metadata/
```

## Manifest

`manifest.json` wird zuletzt erzeugt und enthält mindestens:

```json
{
  "format": "ZeitstrahlStudio.Project",
  "formatVersion": 1,
  "minimumReaderVersion": 1,
  "applicationVersion": "0.1.0",
  "projectId": "00000000-0000-0000-0000-000000000000",
  "projectName": "Beispielprojekt",
  "createdAtUtc": "2026-07-19T08:00:00+00:00",
  "exportedAtUtc": "2026-07-19T09:00:00+00:00",
  "files": [
    {
      "path": "project.db",
      "length": 4096,
      "sha256": "64 hexadezimale Zeichen"
    }
  ]
}
```

Jede reguläre Datei außer dem Manifest selbst steht genau einmal in `files`. Pfad, Länge und SHA-256 müssen beim Import übereinstimmen. Unbekannte optionale Manifestfelder werden toleriert; unbekannte Dateien führen zu einer Warnung und werden nur übernommen, wenn ihre Prüfsumme im Manifest steht.

## Datenbank und Verzeichnisse

`project.db` ist eine vollständige SQLite-Datenbank einschließlich `SchemaMigrations`. Vor dem Archivieren wird ein WAL-Checkpoint durchgeführt; `-wal`- und `-shm`-Dateien gehören nicht ins Archiv.

- `attachments/`: unveränderte Kopien der Originaldokumente unter kollisionsfreien IDs.
- `thumbnails/`: lokal erzeugte PNG- oder JPEG-Vorschauen.
- `extracted-text/`: optionale UTF-8-Kopien extrahierter Texte; maßgebliche Metadaten stehen in SQLite.
- `logs/`: projektbezogene, rotierte Audit-/Diagnosedateien ohne vollständige Dokumentinhalte.
- `metadata/`: erweiterbare versionierte JSON-Metadaten und Importberichte.

Alle internen Verweise sind relativ. Absolute Quellpfade dürfen nur als nicht benötigte Metainformation in der Datenbank stehen.

## Sichere Importreihenfolge

1. Dateiendung, ZIP-Struktur und lesbares `manifest.json` prüfen.
2. Formatkennung sowie unterstützte Minimal-/Formatversion prüfen.
3. Eintragsanzahl, deklarierte Größen, freie Zielkapazität und Extraktionsgrenzen prüfen.
4. Jeden Pfad normalisieren, absolute Pfade und `.`/`..`-Segmente ablehnen und den kanonischen Zielpfad auf den neuen Arbeitsordner begrenzen.
5. Dateien streamend in einen neuen temporären Arbeitsordner extrahieren; symbolische Links und alternative Dateiströme nicht übernehmen.
6. Länge und SHA-256 jedes Eintrags gegen das Manifest prüfen.
7. SQLite-Integritätsprüfung, Schema-Version und notwendige Migrationen prüfen.
8. Erst danach den Arbeitsordner als geöffnet markieren. Bei Fehlern wird er vollständig verworfen; bestehende Projekte bleiben unverändert.

Aktuelle Schutzgrenzen sind 100.000 Dateien, 64 GiB je Datei, 512 GiB dekomprimierte Gesamtgröße, 4 MiB Manifestgröße und für Dateien über 10 MiB ein maximales Kompressionsverhältnis von 1000:1. Vor dem Import wird außerdem die deklarierte Gesamtgröße zuzüglich einer lokalen Reserve von 64 MiB gegen den freien Speicherplatz geprüft.

Importe überschreiben nie ohne bestätigte Benutzerentscheidung einen vorhandenen Zielnamen. Neuere, nicht unterstützte Formatversionen werden nicht verändert und mit einer verständlichen Meldung abgelehnt. Ältere unterstützte Versionen werden nach einer Sicherung in einer Datenbanktransaktion migriert.

## Atomarer Export

Der Export führt zuerst einen SQLite-WAL-Checkpoint aus und arbeitet dann aus dem konsistenten Arbeitsordner. Er schreibt ein neues Archiv im Zielverzeichnis und berechnet alle Prüfsummen während des Schreibens. Jede in SQLite referenzierte Anhangsdatei muss dabei im Archiv enthalten sein und zu ihrer gespeicherten Größe sowie SHA-256-Prüfsumme passen. Anschließend wird das Archiv erneut vollständig validiert und die bestehende Zieldatei erst dann atomar ersetzt. Bei Abbruch, fehlender Dokumentkopie, Integritätsabweichung oder anderem Fehler wird nur die unvollständige temporäre Datei entfernt; ein vorhandenes Ziel bleibt unverändert.

## Kompatibilitätsregeln

- `formatVersion` ändert sich nur bei inkompatiblen Archivänderungen.
- Datenbankänderungen besitzen eine unabhängige fortlaufende Schema-Version.
- Leser müssen `minimumReaderVersion` beachten.
- Neue optionale Felder sind rückwärtskompatibel.
- Entfernte oder semantisch geänderte Pflichtfelder erfordern eine neue Formatversion.
