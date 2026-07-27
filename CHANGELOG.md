# Changelog

Alle wichtigen Änderungen an Zeitstrahl Studio werden in dieser Datei dokumentiert.

## [Unveröffentlicht]

### Geändert

- ComboBoxen und ihre Popup-Einträge verwenden im Dunkelmodus durchgehend dunkle Flächen und lesbare Labels; `DisplayMemberPath`, Auswahlbindungen und Tastaturbedienung bleiben erhalten
- Ausgewählte Registerkarten in Hauptansicht, Ereigniseditor und Detailinspektor sowie DatePicker und Kalender verwenden konsistente themefähige Flächen
- Projekteinstellungen verwenden für die Standardereignisfarbe dieselbe visuelle Palette und freie `#RRGGBB`-Eingabe wie der Ereigniseditor
- Einstellungen bereits im Startbildschirm zugänglich gemacht und das globale Farbschema lokal sowie atomar gespeichert
- Projektwechsel und Projekterstellung überschreiben das gewählte globale Hell-/Dunkel-Schema nicht mehr
- Ereigniseditor um eine visuelle, tastaturzugängliche Farbpalette mit Live-Vorschau ergänzt; freie `#RRGGBB`-Eingaben bleiben möglich

## [0.2.1] - 2026-07-22

### Geändert

- Beispielprojekt und Akzeptanzvertrag auf zehn Ereignisse synchronisiert
- PDB-Dateien aus Endnutzerpaketen ausgeschlossen und Lizenzbündelung verbessert
- Releaseartefakte für den korrigierten Patchstand vorbereitet

## [0.2.0] - 2026-07-21

### Geändert

- UI-Redesign Phase 5: semantische Theme-Ressourcen, globale Control-Styles, überarbeitete Hauptnavigation mit kollabierbarer Seitenleiste, konsistente Dialoge
- Installer-Version auf 0.2.0 erhöht, um korrektes Upgrade-Verhalten sicherzustellen

## [0.1.0] - 2026-07-19

### Hinzugefügt

- Vollständige Windows-Desktopanwendung für chronologische Projekte
- Projekte als versionierte `.zeitprojekt`-ZIP-Archive
- SQLite-basierte lokale Projektdatenbank mit WAL-Modus
- Fünf Datumsgenauigkeiten: exakt, exakt mit Uhrzeit, Monat/Jahr, Jahr, Zeitraum
- Ereignisse mit Titel, Beschreibung, Fristen, Prioritäten, Farben, Schlagwörtern, Quellen, Notizen
- Unabhängige Fristen mit Status und Erinnerungsnotiz
- Anhangsverwaltung für PDF, Bilder, DOCX, XLSX und Webseitenlinks
- Sicherer Import mit SHA-256-Prüfsummen und Pfadvalidierung
- Lokale Dokumentenanalyse für DOCX, XLSX und PDF
- Lokale OCR für Bilder und bildbasierte PDFs über Windows.Media.Ocr
- Integrierte PDF-Vorschau über PDFium
- Bildvorschau für PNG, JPEG, TIFF und BMP
- Volltextsuche über Projekt-, Ereignis- und Dokumentinhalte
- Kombinierbare Filter nach Zeitraum, Priorität, Farbe, Schlagwort, Dateityp und mehr
- Horizontale und vertikale Zeitstrahlansicht mit Zoom, Mausverschiebung und Navigation
- Automatische Skalierung und Komprimierung großer Zeitlücken
- Manuelle Kartenpositionen mit Undo/Redo-Unterstützung
- PDF-Export mit Vorschau, mehrseitigem Modus, großer Einzelseite und Zeitraumauswahl
- Standalone-HTML-Export als einzelne Offline-Datei
- Automatische und manuelle lokale Sicherungen mit Rotation
- Wiederherstellung aus Sicherungen
- Undo/Redo für Ereignisoperationen
- Lokales Audit-Protokoll
- Hell-/Dunkel-Thema mit anpassbaren Schriftgrößen
- Tastenkürzel für alle wichtigen Funktionen
- Beispielprojekt mit lokalen Testdokumenten
- Selbstenthaltende win-x64-Veröffentlichung
- Portable ZIP-Version
- Windows-Installer mit `.zeitprojekt`-Dateizuordnung

### Technisch

- .NET 8, WPF, MVVM, Dependency Injection
- Geschichtete Solution: App, Application, Domain, Infrastructure, DocumentProcessing, Export, Shared
- 62 Unit-Tests und 88 Integrationstests
- Nullable Reference Types, Warnungen als Fehler
- Asynchrone Datei- und Datenbankzugriffe mit CancellationToken
- Transaktionssichere SQLite-Operationen
- Lokale strukturierte JSON-Lines-Protokollierung
