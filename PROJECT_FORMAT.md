# Projektformat `.zeitprojekt`

Diese Spezifikation beschreibt das implementierte Archivformat von Zeitstrahl Studio 1.1.0. Sie ist für Wartung, Diagnose und kompatible Leser gedacht; Projektdateien sollen dennoch ausschließlich durch die Anwendung erzeugt und geöffnet werden.

## Versionen und Kennung

| Ebene | Aktueller Wert | Bedeutung |
| --- | --- | --- |
| Formatkennung | `ZeitstrahlStudio.Project` | identifiziert ein Zeitstrahl-Studio-Archiv |
| Archivformat | `formatVersion: 1` | Struktur und Semantik des ZIP/Manifests |
| Mindestleser | `minimumReaderVersion: 1` | kleinste unterstützte Archivleserversion |
| Datenbankschema | `2` | interne SQLite-Migrationen in `project.db` |
| Anwendungsversion | zum Exportzeitpunkt, derzeit `1.1.0` | Information, unabhängig von Archiv- und DB-Version |

Eine andere `applicationVersion` ändert das Archivformat nicht. Insbesondere kann ein Archivformat-1-Projekt von einer älteren Anwendungsversion stammen und nach einer unterstützten Datenbankmigration weiterhin gültig sein.

## ZIP-Struktur

Ein Archiv ist eine ZIP-Datei mit Endung `.zeitprojekt`. Interne Pfade verwenden `/`.

```text
Projekt.zeitprojekt
├── manifest.json              Pflicht, exakt einmal und kleingeschrieben
├── project.db                 Pflicht
├── attachments/               optional
├── thumbnails/                optional
├── extracted-text/            optional/reserviert
├── logs/                      optional/reserviert
└── metadata/                  optional/reserviert
```

Leere Verzeichnisse werden nicht als eigene Einträge geschrieben. Der aktuelle Export sammelt neben `project.db` nur vorhandene reguläre Dateien unter den fünf genannten Wurzeln. Dabei gelten folgende Ist-Zustände:

- `attachments/` enthält projektinterne Dokumentkopien.
- `thumbnails/` kann erzeugte Vorschaubilder enthalten.
- Extrahierte Texte und Analysematadaten sind derzeit in SQLite maßgeblich; `extracted-text/` ist für kompatible Projektinhalte reserviert.
- Das fachliche Audit liegt in SQLite und technische JSONL-Logs liegen außerhalb des Projekts unter `%LocalAppData%\Zeitstrahl Studio\Logs`; `logs/` ist derzeit reserviert.
- `metadata/` ist für projektinterne Zusatzdateien reserviert. `metadata/session.json` und temporäre Varianten davon sind Recovery-Laufzeitdaten und werden ausdrücklich nicht exportiert.
- `project.db-wal` und `project.db-shm` werden nach dem WAL-Checkpoint nicht exportiert.

## Manifest

`manifest.json` ist UTF-8-JSON. Ein syntaktisch plausibles Beispiel:

```json
{
  "format": "ZeitstrahlStudio.Project",
  "formatVersion": 1,
  "minimumReaderVersion": 1,
  "applicationVersion": "1.1.0",
  "projectId": "11111111-2222-3333-4444-555555555555",
  "projectName": "Beispielprojekt",
  "createdAtUtc": "2026-08-01T08:00:00+00:00",
  "exportedAtUtc": "2026-08-29T12:00:00+00:00",
  "files": [
    {
      "path": "project.db",
      "length": 4096,
      "sha256": "0000000000000000000000000000000000000000000000000000000000000000"
    }
  ]
}
```

Pflichtfelder sind:

| Feld | Inhalt |
| --- | --- |
| `format` | exakt `ZeitstrahlStudio.Project` |
| `formatVersion` | Archivformatversion |
| `minimumReaderVersion` | benötigte Leserfähigkeit |
| `applicationVersion` | erzeugende Anwendungsversion |
| `projectId` | GUID der Projektdatenbank |
| `projectName` | Name der Projektdatenbank |
| `createdAtUtc` | fachlicher Erstellungszeitpunkt in UTC |
| `exportedAtUtc` | Exportzeitpunkt in UTC |
| `files` | genau ein Datensatz je regulärer Archivdatei außer `manifest.json` |

Jeder `files`-Eintrag besitzt einen normalisierten relativen `path`, die unkomprimierte `length` in Bytes und `sha256` als 64 hexadezimale Zeichen. Nach dem Laden von `project.db` müssen `projectId` und `projectName` mit dem Manifest übereinstimmen.

## Exakte Inhaltsübereinstimmung

Beim Import gilt die Menge aus `manifest.json` plus allen Pfaden in `files` als exakter Vertrag:

- `manifest.json` muss genau einmal unter exakt diesem kleingeschriebenen Pfad vorkommen.
- `project.db` muss genau einmal in `files` und im ZIP vorkommen.
- Jeder manifestierte Pfad muss genau einen ZIP-Dateieintrag mit passender Länge und SHA-256 besitzen.
- Eine nicht manifestierte zusätzliche Datei ist ein Fehler, keine Warnung.
- Eine zusätzliche sichere, normalisierte und manifestierte Datei wird vom aktuellen Import akzeptiert, sofern alle allgemeinen Regeln und Limits erfüllt sind. Der Import erzeugt dafür derzeit keine Warnung.

Der eigene Export beschränkt sich auf `project.db` und die aufgeführten Projektwurzeln. Die Importtoleranz für zusätzliche manifestierte Dateien ist keine Empfehlung, das Archiv manuell zu erweitern.

## Schutzgrenzen

Der aktuelle Leser erzwingt:

| Grenze | Wert |
| --- | --- |
| maximale Anzahl Dateien | 100.000 |
| maximale Größe von `manifest.json` | 4 MiB |
| maximale unkomprimierte Einzeldatei | 64 GiB |
| maximale unkomprimierte Gesamtsumme | 512 GiB |
| zusätzlich erforderliche freie Reserve | 64 MiB |
| Schutz vor extremer Kompression | bei Einträgen über 10 MiB wird ein Verhältnis über 1000:1 abgelehnt |

Die freie Zielkapazität wird vor der Extraktion gegen die deklarierte unkomprimierte Gesamtsumme zuzüglich Reserve geprüft. Die Limits verhindern Ressourcenmissbrauch, sind aber keine Aussage darüber, dass jede innerhalb der Grenzen liegende Projektgröße praktisch komfortabel bedienbar ist.

## Pfad- und Dateisystemregeln

Jeder Archivpfad muss relativ, eindeutig, mit `/` normalisiert und nach Windows-Regeln sicher sein. Abgelehnt werden insbesondere:

- absolute Pfade, Laufwerkspräfixe, UNC-Pfade und alternative Datenströme
- leere, `.`- oder `..`-Segmente, Traversal und nicht normalisierte Varianten
- Rückwärtsschrägstriche, ungültige oder Steuerzeichen sowie Segmente mit problematischen abschließenden Punkten/Leerzeichen
- reservierte Windows-Gerätenamen wie `CON`, `PRN`, `AUX`, `NUL`, `COM1` bis `COM9` und `LPT1` bis `LPT9`, auch mit Erweiterung
- doppelte Pfade unabhängig von Windows-Groß-/Kleinschreibung
- ein Ziel, dessen kanonischer Pfad den neu angelegten Staging-Ordner verlässt

Beim Export werden verwaltete Verzeichnisse und Dateien mit Reparse-Point-Attribut abgelehnt. Beim Import entstehen ausschließlich neue reguläre Dateien; vorhandene Ziele werden nicht verfolgt oder überschrieben.

## Exportablauf

`ProjectArchiveService` erzeugt ein Archiv in dieser Reihenfolge:

1. Das Projekt einschließlich der in SQLite gespeicherten Attachmentmetadaten laden.
2. SQLite-WAL vollständig checkpointen und `project.db` sowie vorhandene reguläre Dateien der erlaubten Projektwurzeln sammeln; Recovery-Marker sowie WAL/SHM ausschließen und Reparse Points ablehnen.
3. Mit `WriteArchiveAsync` die gesammelten Dateien streamend in ein temporäres ZIP im Zielverzeichnis schreiben, dabei Länge und SHA-256 ermitteln und `manifest.json` aus den tatsächlich geschriebenen Werten erzeugen.
4. Danach mit `ValidateReferencedAttachments` jede in SQLite referenzierte Attachmentkopie gegen Projekt und erzeugte Manifestdateiliste prüfen: interner Pfad, gespeicherte Länge und gespeichertes SHA-256 müssen übereinstimmen.
5. Anschließend das geschlossene ZIP mit `VerifyArchiveAsync` erneut auf Struktur, Manifest, Pfade, Längen und SHA-256 aller manifestierten Dateien prüfen. Der Datenbank↔Attachment-Bezug wird in dieser Abschlussprüfung nicht erneut verifiziert.
6. Erst nach beiden Prüfungen das Ziel atomar übernehmen. Bei bestehendem Ziel verwendet der interne Ersetzungsschritt vorübergehend eine `.previous`-Sicherung und entfernt sie nach Erfolg bestmöglich.

Bei Abbruch oder Fehler bleibt ein vorheriges gültiges Ziel erhalten; temporäre Ausgabe wird bestmöglich entfernt.

## Importablauf

Öffnen erfolgt niemals direkt aus dem ZIP:

1. Dateiendung, ZIP-Grundstruktur, einziges `manifest.json`, Manifestgröße und JSON prüfen.
2. Formatkennung, `formatVersion` und `minimumReaderVersion` prüfen.
3. Eintragsmenge exakt mit `files` abgleichen; Zähl-, Einzel-/Gesamtgrößen-, Kompressions- und Freispeicherlimits prüfen.
4. Alle Pfade vor dem Schreiben normalisieren und validieren.
5. Den Staging-Ordner als Geschwisterpfad `<Zielworkspace>.importing-<GUID>` unter der verwalteten Workspace-Wurzel anlegen.
6. Jede Datei abbrechbar und streamend in ein neues Ziel extrahieren; Länge und SHA-256 unmittelbar prüfen.
7. `project.db` öffnen, unterstützte Migrationen transaktional anwenden und das Aggregat laden.
8. Das geladene Projekt fachlich validieren und Projekt-ID sowie Projektname aus SQLite mit dem Manifest vergleichen.
9. Erst nach vollständigem Erfolg den Staging-Ordner an seinen aktiven Workspace-Namen verschieben und einen separaten Recovery-Marker anlegen.

Fehler verwerfen den neuen Staging-Ordner bestmöglich. Ein bestehender Workspace oder ein Archivziel wird nicht halb überschrieben.

### Aktuelle Querverifikationsgrenze

Der Import prüft jede extrahierte Datei gegen Manifestlänge und Manifest-SHA-256 und lädt anschließend Datenbank, Domainaggregat, Projekt-ID und Projektname. Er vergleicht beim Import jedoch nicht zusätzlich die in SQLite gespeicherten Attachmentlängen und -hashes mit den extrahierten Attachmentdateien. Der nächste Export beziehungsweise Speichervorgang schreibt zunächst das temporäre ZIP und führt danach vor dessen Übernahme die DB↔Attachment-Querverifikation aus; bei einer Abweichung wird das temporäre Ergebnis nicht übernommen.

## SQLite-Schema und Migrationen

`project.db` enthält `SchemaMigrations`; das aktuelle Schema ist Version 2. Migrationen laufen in einer SQLite-Transaktion und werden mit Version, Name und UTC-Zeitpunkt protokolliert.

### Migration 1

Migration 1 erstellt vollständig:

- `Projects`
- `Events`
- `EventDates`
- `Deadlines`
- `Attachments`
- `AttachmentMetadata`
- `ExtractedTexts`
- `WebLinks`
- `Tags`
- `EventTags`
- `LayoutPositions`
- `ProjectSettings`
- `AuditLog`
- `ApplicationLogReferences`
- `Backups`
- den FTS5-Index `SearchIndex`

`SchemaMigrations` wird als Migrationsverwaltung vor den fachlichen Schritten angelegt. Fremdschlüssel und Indizes sichern Zuordnungen und häufige Abfragen. `ProjectSettings` verwendet typisierte Spalten für Orientierung, Theme, Standardfarbe, Schriftgrößen, Lückenkompression, Autosave und Backupretention; es ist kein JSON-Dokument. `SearchIndex` ist der ältere Projekt-/Ereignisindex.

### Migration 2

Migration 2 erstellt den FTS5-Index `DocumentSearchIndex` und übernimmt vorhandenen Text aus `ExtractedTexts`. Dieser Index ist für extrahierte Dokumenttexte maßgeblich; Analyseablage und Suche halten ihn aktuell.

## Kompatibilität

- Implementiert und lesbar ist ausschließlich Archivformat 1.
- Datenbankschema 1 wird beim Öffnen der extrahierten Arbeitskopie transaktional auf Schema 2 migriert.
- Eine unbekannte neuere Archiv- oder Datenbankschemaversion wird unverändert abgelehnt.
- Es gibt keine belegte separate Archivformatmigration und keinen gesonderten Vor-Migrationsbackup-Schritt. Die Migration betrifft die neue Arbeitskopie; das ursprüngliche `.zeitprojekt`-Archiv wird beim Öffnen nicht verändert.
- Erst ein späteres erfolgreiches Speichern erzeugt ein neues Archiv mit der migrierten Datenbank.

## Integrität, Vertraulichkeit und Betrieb

Die SHA-256-Werte stehen im selben Archiv wie die Nutzdaten. Sie erkennen Inkonsistenz oder nachträgliche Veränderung, sind aber keine Signatur, kein Herkunftsnachweis und kein Schutz gegen einen Angreifer, der Daten und Manifest gemeinsam ersetzt.

`.zeitprojekt` ist nicht verschlüsselt und nicht kennwortgeschützt. Neben Dokumenten und Notizen kann die Datenbank den ursprünglichen absoluten Quellpfad eines Attachments enthalten. Archive nur über vertrauenswürdige Wege kopieren, vor dem Ersetzen sichern und vor Weitergabe wie die enthaltenen Originaldaten behandeln. Keine Dateien im ZIP manuell hinzufügen, löschen oder ersetzen; Änderungen immer in Zeitstrahl Studio öffnen und in ein neues beziehungsweise geprüftes Ziel speichern.

Weiterführend: [`ARCHITECTURE.md`](ARCHITECTURE.md), [`USER_GUIDE.md`](USER_GUIDE.md) und [`PRIVACY.md`](PRIVACY.md).
