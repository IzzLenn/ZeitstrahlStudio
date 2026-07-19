# Architektur von Zeitstrahl Studio

## Ziel und Randbedingungen

Zeitstrahl Studio ist eine deutschsprachige WPF-Desktopanwendung für einen lokalen Einzelbenutzer. Alle Projektdaten, Dokumentanalysen, Vorschaubilder, Suchindizes, Exporte und Protokolle bleiben auf dem Windows-Rechner. Es gibt keine Telemetrie, Cloud-Synchronisation oder Hintergrundzugriffe auf Webseiten.

Die Architektur ist auf Windows 10/11 x64, .NET 8, mehrere Tausend Ereignisse und potenziell große Projektarchive ausgelegt. Lange Datei-, OCR-, Datenbank- und Exportvorgänge laufen asynchron, sind über `CancellationToken` abbrechbar und melden Fortschritt.

## Solution-Struktur

```text
ZeitstrahlStudio.sln
├── src/
│   ├── ZeitstrahlStudio.App                 WPF, MVVM, Composition Root
│   ├── ZeitstrahlStudio.Application         Anwendungsfälle und Ports
│   ├── ZeitstrahlStudio.Domain              Fachmodell und Invarianten
│   ├── ZeitstrahlStudio.Infrastructure      SQLite, Archive, Backups, Logs
│   ├── ZeitstrahlStudio.DocumentProcessing  PDF/Bild/DOCX/XLSX/OCR
│   ├── ZeitstrahlStudio.Export              PDF- und Standalone-HTML-Export
│   └── ZeitstrahlStudio.Shared              kleine schichtübergreifende Ergebnistypen
└── tests/
    ├── ZeitstrahlStudio.UnitTests
    └── ZeitstrahlStudio.IntegrationTests
```

Abhängigkeiten zeigen nach innen: `Domain` besitzt keine Projektabhängigkeit; `Application` kennt `Domain` und `Shared`; Infrastruktur, Dokumentverarbeitung und Export implementieren Ports der Anwendungsschicht; WPF verdrahtet diese Implementierungen. Geschäftslogik gehört nicht in Code-behind.

## Fachmodell

`TimelineProject` ist die Aggregatwurzel für Projektinformationen, Einstellungen und Ereignisse. `TimelineEvent` enthält beliebig lange Texte, eine präzise `EventDate`, Klassifizierung, Tags, Anhänge, Links, eine optionale unabhängige Frist und eine optionale manuelle Sortierposition. `LayoutPosition` speichert visuelle Versätze getrennt vom Datum.

`EventDate` speichert Jahr, Monat, Tag und Uhrzeit als getrennte optionale Komponenten. Dadurch bleibt `2024` eine Jahresangabe und `Mai 2024` eine Monatsangabe. Nur der technische Sortierwert ergänzt intern fehlende Komponenten; er wird nie angezeigt oder persistiert, als wären die Komponenten eingegeben worden. Zeiträume werden als zwei exakte geschlossene Datumswerte gespeichert und vor der Übernahme validiert.

Technische Zeitstempel werden als UTC-`DateTimeOffset` gespeichert. Die Umrechnung in deutsche Ortszeit erfolgt ausschließlich an Anzeigegrenzen. IDs sind GUIDs. Eine manuelle Reihenfolge greift nur bei identischen fachlichen Datumswerten und verändert kein Datum.

## Implementiertes normalisiertes SQLite-Schema

Migrationen werden fortlaufend nummeriert und in `SchemaMigrations` transaktionssicher protokolliert. Migration 1 legt das folgende Schema an. Fremdschlüssel sind für jede Repository-Verbindung aktiviert; SQLite arbeitet im WAL-Modus.

| Tabelle | Wesentliche Inhalte |
| --- | --- |
| `Projects` | ID, Name, Untertitel, Texte, Gesamtzeitraum, UTC-Zeitstempel |
| `Events` | ID, Projekt-ID, Texte, Priorität, Farbe, Quelle, Status, manuelle Reihenfolge, UTC-Zeitstempel |
| `EventDates` | Ereignis-ID, Genauigkeit, Startjahr/-monat/-tag/-zeit, Endkomponenten |
| `Deadlines` | Ereignis-ID, Fälligkeitsdatum/-zeit, Bezeichnung, Status, Notiz |
| `Attachments` | ID, Ereignis-ID, Originalname, Typ, Größe, SHA-256, Quellmetadatum, relativer Projektpfad, Zustand |
| `AttachmentMetadata` | Anhang-ID, Schlüssel, Wert |
| `ExtractedTexts` | Anhang-ID, Text, Extraktionsart, Sprache, UTC-Zeitstempel |
| `WebLinks` | ID, Ereignis-ID, Adresse, Bezeichnung |
| `Tags` / `EventTags` | normalisierte Schlagwörter und n:m-Zuordnung |
| `LayoutPositions` | Ereignis-ID, Ausrichtung, X-/Y-Versatz |
| `ProjectSettings` | versionierte JSON- oder typisierte Einstellungswerte |
| `AuditLog` | Zeitpunkt, Vorgang, Datensatz, Beschreibung, Ergebnis, technische Details |
| `ApplicationLogReferences` | Verweise auf rotierte lokale technische Logs |
| `Backups` | Zeitpunkt, relativer Pfad, Größe, Prüfsumme, Sicherungsart |
| `SchemaMigrations` | Versionsnummer, Bezeichnung, UTC-Anwendungszeitpunkt |

Indizes bestehen für Event-/Projektzuordnung, Fristen, Anhangstypen, Tags, Audit und Sicherungen. Die mit `Microsoft.Data.Sqlite` ausgelieferte lokale e_sqlite3-Distribution stellt FTS5 bereit; `SearchIndex` wird nach Aggregatspeicherungen einschließlich vorhandener extrahierter Texte neu aufgebaut. Ein Test prüft diese Funktion mit einer realen Datenbank.

## Arbeitsordner und Speicherung

Eine `.zeitprojekt`-Datei wird nie direkt bearbeitet. Beim Öffnen wird sie nach vollständiger Manifest-, Pfad-, Größen- und Prüfsummenvalidierung in einen eindeutigen Staging-Ordner importiert und erst nach bestandener Datenbankprüfung zum lokalen Arbeitsordner verschoben. SQLite arbeitet dort im WAL-Modus. Speichern checkpointet SQLite und erzeugt im Zielverzeichnis zunächst ein neues vollständiges Archiv, validiert es und ersetzt dann atomar die vorherige Datei. Ein vorheriger gültiger Stand bleibt bis zum erfolgreichen Abschluss erhalten.

Die Verzeichnisse `attachments`, `thumbnails`, `extracted-text`, `logs` und `metadata` sind ausschließlich über normalisierte relative Pfade adressierbar. Das detaillierte Format steht in `PROJECT_FORMAT.md`.

## Anwendungsabläufe

Die Anwendungsschicht definiert Ports für Repository, Workspace, Archiv, Anhangsimport, Dokumentanalyse, Suche, PDF, HTML, Sicherung und Audit. Implementierungen liefern erwartbare Datei- und Validierungsfehler als handlungsorientierte `OperationResult`-Werte; Programmierfehler und verletzte fachliche Invarianten bleiben Ausnahmen.

Zusammengehörige Datenbankänderungen laufen in einer Transaktion. Anhangsdateien werden erst unter kollisionsfreien internen Namen kopiert und geprüft, bevor ihre Datenbankzuordnung bestätigt wird. Undo hält entfernte Dateien in einem projektinternen Papierkorb, solange die Operation wiederherstellbar ist.

## Oberfläche und Nebenläufigkeit

Der WPF-Start wird über einen validierten Microsoft.Extensions.DependencyInjection-Container aufgebaut. Das Haupt-ViewModel bindet Projektanlage, Archivöffnung, Recent Projects, Recovery, Speichern, Duplizieren, Schließen, Autosave und lokale Fehlerprotokollierung an die Oberfläche. Code-behind behandelt ausschließlich Fenster- und Dialoglebenszyklen. Asynchrone Commands verhindern Doppelaufrufe und machen laufende Vorgänge sichtbar.

Das Ereignisformular erzeugt einen vollständigen Application-Request. Bei Änderungen wird daraus zunächst ein neues validiertes Domain-Ereignis aufgebaut und anschließend atomar im Projekt ersetzt. Dadurch bleiben IDs und Anhänge erhalten, während Validierungsfehler niemals einen teilweise veränderten Eintrag hinterlassen. Erstellen und Löschen verwenden dieselbe Application-Fassade; die WPF-Dialogklasse beschränkt sich auf Bestätigen und Anzeigen von Validierungsmeldungen.

ViewModels stellen Commands und bindbare Zustände bereit. Der UI-Thread übernimmt nur kleine Zustandsänderungen. Listen werden virtualisiert; große Vorschaubilder und Dokumenttexte werden verzögert geladen. Dokumentanalyse und OCR verwenden eine begrenzte Warteschlange. Autosave serialisiert Speichervorgänge, damit niemals zwei Archivgenerationen konkurrieren.

Helles und dunkles Theme sind Resource Dictionaries. Zeitstrahl-Layouts werden aus einem testbaren Layoutmodell erzeugt; horizontale und vertikale WPF-Ansichten konsumieren dasselbe Modell. Farben werden immer durch Text, Symbole oder Rahmen ergänzt.

## Sicherheit und Datenschutz

- Keine Netzwerk- oder Telemetriekomponente wird registriert.
- Externe Links werden nur nach expliziter Benutzeraktion über Windows geöffnet.
- Archivpfade werden vor dem Extrahieren kanonisiert und auf das Arbeitsverzeichnis begrenzt.
- Anzahl, Einzelgröße, Gesamtextraktionsgröße und Kompressionsverhältnis von ZIP-Einträgen werden begrenzt.
- SHA-256-Prüfsummen werden während des Streamens berechnet und beim Import vollständig verglichen.
- Temporäre Dateien liegen in eindeutigen Anwendungsverzeichnissen und werden in `finally`-Pfaden entfernt.
- Technische Logs enthalten keine vollständigen Dokumenttexte und rotieren nach konfigurierbarer Größe.

Technische Logs sind als lokale JSON-Lines-Dateien mit Größenrotation, begrenzten Textfeldern, manueller Anzeige, Export und Löschung implementiert. Der lokale Anwendungszustand für zuletzt verwendete Projekte ist getrennt davon versioniert. Workspace-Sitzungsmarker werden bewusst nicht in `.zeitprojekt`-Archive aufgenommen.

## Wesentliche technische Risiken

| Risiko | Gegenmaßnahme |
| --- | --- |
| Manipulierte oder extrem große Archive | Streaming, harte Extraktionsgrenzen, kanonische Pfadprüfung, SHA-256 und atomare Zielübernahme |
| Große Dateien blockieren UI oder Speicher | asynchrone Streams, begrenzte Puffer, Fortschritt, CancellationToken, Hintergrundwarteschlange |
| PDF-Vorschau und OCR erhöhen native Abhängigkeiten | lokal weitergabefähige Engines, lizenzierte Binärdateien, feste x64-Pakete und Integrationstests auf sauberem Windows |
| Unterschiedliche PDF-Betrachter bei sehr großen Seiten | Grenzwerte und Warnung in Vorschau, mehrseitiger Standardmodus |
| WPF-Layout bei 5.000 Ereignissen | Virtualisierung, viewportbezogene Karten, gecachte Geometrie und Lasttests |
| Defekter Zustand nach Absturz | SQLite-Transaktionen/WAL, Save-Journal, atomare Archivablage, rotierende geprüfte Sicherungen |
| Inno Setup derzeit nicht im PATH | Installer-Skript unabhängig erstellen; Build-Skript erkennt das Werkzeug und gibt eine klare Installationsanweisung |

## Qualitätsstrategie

Domain- und Layoutlogik werden als schnelle Unit-Tests abgedeckt. Integrationstests arbeiten in isolierten temporären Verzeichnissen und prüfen SQLite, Archive, Dokumentformate, Exporte und Sicherungen. Release-Gates sind Restore, Debug-Build/-Tests, Release-Build/-Tests, selbstenthaltendes `win-x64`-Publish, portable ZIP-Prüfung und – sobald Inno Setup verfügbar ist – der Installer-Build.
