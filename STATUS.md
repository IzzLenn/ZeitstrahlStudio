# Projektstatus

Status: In Entwicklung - Dokumenttransfer und optionales HTML-Exportpaket für Version 1.0.0 umgesetzt

Letzte Aktualisierung: 13.08.2026

## Aktuelle Phase

Dokumentanhänge lassen sich jetzt aus der Ereignis-Detailansicht per Doppelklick sicher mit dem Windows-Standardprogramm öffnen. Anhänge werden weiterhin als projektinterne Kopien gespeichert; der Projektarchivexport prüft nun zusätzlich jede referenzierte Kopie gegen Größe und SHA-256. Der HTML-Export kann das Momentaufnahme-Warnbanner ausblenden und wahlweise ein geprüftes ZIP-Paket mit `index.html`, Anleitung und allen referenzierten Dokumentkopien erzeugen.

Implementierung, automatisierte Debug-/Release-Verifikation, HTML-Sichtprüfung und die Vorbereitung von Version 1.0.0 sind abgeschlossen. Die benutzereigenen Änderungen unter `samples/` bleiben unangetastet und außerhalb des Commits beziehungsweise der hierfür erzeugten Prüfpakete.
## Prüfung der Entwicklungsumgebung

- Windows 10 x64, Version 10.0.19045
- PowerShell 5.1.19041
- .NET SDK 8.0.423, MSBuild 17.11.48; zusätzlich SDK 6.0.100 installiert
- Git verfügbar; Ausgangszustand war ein Commit mit Spezifikationsdokumenten und ein unversioniertes WPF-Starttemplate
- Arbeitsbereich ist beschreibbar; durch Solution-Dateien, Builds und Tests praktisch bestätigt
- `dotnet`, Git und die WPF-Buildwerkzeuge sind verfügbar
- Inno Setup 6.7.3 ist lokal unter `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe` verfügbar; `build.ps1` erkennt neben `PATH` und Program Files nun auch diesen Installationsort
- Vor Arbeitsbeginn waren keine Build-/Testprotokolle vorhanden

## Abgeschlossene Arbeiten

### UI-Redesign 0.3.0 - Phase 2 Globale Navigation

- normales Hauptmenü mit `Datei`, `Bearbeiten`, `Ansicht`, `Ereignis`, `Werkzeuge` und `Hilfe` eingeführt
- bestehende Projekt-, Ereignis-, Timeline-, Anhangs-, Sicherungs-, Protokoll- und Exportbefehle vollständig den Menüs zugeordnet
- priorisierte textbasierte Befehlsleiste für Navigation, Neu/Öffnen/Speichern, Undo/Redo, Horizontal/Vertikal, Suchen/Analysieren, PDF/HTML und Einstellungen umgesetzt
- aktiver Orientierungszustand über semantische Selected-Ressourcen dargestellt; erneutes Ausführen der aktiven Orientierung bleibt ein vorhandener No-op
- Unicode-Hamburger aus der globalen Navigation entfernt; keine neuen Icon-Assets oder provisorischen Symbole eingeführt
- lokaler Info-Menüpunkt ohne Netzwerkzugriff ergänzt
- `MainWindowAccessibilityTests` um Menüstruktur, Kernbefehle, Symbolfreiheit, aktive Orientierung und 1280-DIP-Grenzen erweitert
- Debug-Build erfolgreich, 0 Warnungen/0 Fehler; 11 gezielte MainWindow-/Theme-Tests bestanden
- WPF-Anwendung erfolgreich gestartet; Screenshot `artifacts/ui-redesign/phase-2-navigation.png` erzeugt; UI-Automation bestätigt Hauptmenü und 13 vollständig sichtbare Befehlsleistenbuttons bei 1280×760

### UI-Redesign 0.3.0 - Phase 1 Designsystem und Themes

- 21 zusätzliche semantische Brush-Ressourcen identisch in hellem und dunklem Theme eingeführt
- Typografieskala auf 12 DIP für Meta-/Labeltexte, 13 DIP Grundtext, 14/16 DIP Abschnitte sowie 20/24 DIP Überschriften angehoben
- gemeinsame Zustände für Hover, Pressed, Fokus, Auswahl, Deaktiviert, Nur-Lesen und Validierungsfehler ergänzt
- themefähige Menü- und Tooltip-Stile vorbereitet
- lokale `Theme.Light.xaml`-Einbindung aus `MainWindow` und allen elf Dialogen entfernt; `App.xaml` ist die einzige globale Theme-Quelle
- neue `ThemeResourceTests` prüfen Schlüsselgleichheit, WCAG-Kontrast von sechs kritischen Farbpaaren, Typografieskala und fehlende lokale Theme-Festlegungen
- Debug-Build erfolgreich, 0 Warnungen/0 Fehler; 17 gezielte Theme-/Hauptfenster-/Dialogtests bestanden
- WPF-Anwendung aus dem Debug-Build mit dem Beispielprojekt erfolgreich gestartet; Screenshot unter `artifacts/ui-redesign/phase-1-theme.png` erzeugt, direkte Bildanzeige jedoch durch wiederholten Timeout des lokalen Sandbox-Bildhelpers blockiert

### UI-Redesign 0.3.0 - Phase 0 Bestandsaufnahme

- alle sechs Bildanhänge als drei IST- und drei SOLL-Referenzen eindeutig dokumentiert
- tatsächlichen XAML-/C#-Stand, Ressourcen, Typografie, Breiten, Scrollbereiche, Dialoge, Timelinekarten, Kollisionsmodell, Icons und UI-Tests geprüft
- kritische lokale Theme.Light-Übersteuerung in Hauptfenster und Dialogen als Ursache der Mischdarstellung belegt
- konkrete Umsetzung für Designsystem, Navigation und responsive Drei-Bereichs-Hauptansicht den Dateien zugeordnet
- vollständiges Icon-Gate für textbasierte Umsetzung ohne neu erzeugte Assets dokumentiert
- Baseline: Debug-Build erfolgreich, 0 Warnungen/0 Fehler; 17 gezielte Integrationstests bestanden

### Meilenstein 1 – Solution und Architektur

- Solution in App, Application, Domain, Infrastructure, DocumentProcessing, Export, Shared, UnitTests und IntegrationTests gegliedert
- zentrale Buildregeln: .NET 8/C# 12, Nullable, implizite Usings, deterministische Builds und Warnungen als Fehler
- WPF-Ziel auf `win-x64`, Per-Monitor-V2-DPI, Long Paths und Ausführung ohne Administratorrechte vorbereitet
- fachliches Grundmodell für Projekte, Ereignisse, unvollständige Datumsangaben, Zeiträume, Fristen, Anhänge, Webseitenlinks, Tags, Layoutpositionen, Einstellungen, Audit und Sicherungsmetadaten implementiert
- unvollständige Datumsangaben behalten ihre tatsächlich eingegebenen Komponenten
- manuelle Reihenfolge gleicher Datumswerte ist vom Datum getrennt
- erste Pfad-Traversal- und Prüfsummenvalidierung für Anhänge implementiert
- asynchrone Application-Ports für Repository, Arbeitsordner, Archive, Anhänge, Dokumentanalyse, Suche, PDF, HTML, Backups und Audit definiert
- Architektur, geplantes Datenbankschema, Projektformat, Risiken und aktuelle Drittanbieterlizenzen dokumentiert
- 20 Unit-Tests und 1 Architektur-Integrationstest implementiert

### Meilenstein 2 – Datenmodell und SQLite

- `Microsoft.Data.Sqlite` 8.0.29 als notwendige MIT-lizenzierte Produktionsabhängigkeit eingeführt und transitive SQLitePCLRaw-Lizenzen dokumentiert
- SQLite-Verbindungen mit Fremdschlüsseln, WAL, begrenzter Wartezeit und geeignetem Synchronitätsmodus konfiguriert
- transaktionale Schema-Migration 1 implementiert
- alle in `SPEC.md` geforderten Tabellen, Indizes und ein lokaler FTS5-Suchindex angelegt
- neueres unbekanntes Datenbankschema wird mit verständlicher Meldung abgelehnt
- transaktionales Repository zum Erstellen, Speichern und erneuten Öffnen vollständiger Projektaggregate implementiert
- alle fünf Datumsgenauigkeiten werden komponentengenau persistiert und wiederhergestellt
- Fristen, Tags, Anhänge, Webseitenlinks, Einstellungen und manuelle Layoutpositionen werden mit Fremdschlüsseln gespeichert
- Folge-Speicherungen erhalten vorhandene Anhangsmetadaten und extrahierte Texte und bauen den Suchindex daraus neu auf
- entfernte Ereignisse und abhängige Datensätze werden konsistent bereinigt
- Integritätstests für Schema, Idempotenz, Roundtrip, FTS, Kaskaden, Rollback und Versionsabwehr ergänzt

### Meilenstein 3A – Projektarchive und lokale Arbeitsordner

- versioniertes `.zeitprojekt`-Manifest mit Projektmetadaten, Dateilängen und SHA-256-Prüfsummen implementiert
- SQLite-WAL wird vor dem Export checkpointet; das neu geschriebene Archiv wird vor der Übernahme vollständig erneut geprüft
- bestehende Archive werden erst nach erfolgreicher Erstellung atomar ersetzt
- Import prüft Format/Version, eindeutiges Manifest, Dateianzahl, Einzel-/Gesamtgröße, freien Speicherplatz, Duplikate, Kompressionsverhältnis und Prüfsummen
- absolute, nicht normalisierte, reservierte und traversierende Archivpfade werden vor der Extraktion abgelehnt
- Extraktion erfolgt streamend und abbrechbar in einen neuen Staging-Ordner; vorhandene Ziele werden nicht überschrieben
- Arbeitsordnerdienst für neues Projekt, Öffnen, Speichern, „Speichern unter“, Duplizieren, Schließen und bestätigtes Löschen implementiert
- Duplikate erhalten eine neue Projekt-ID, behalten aber vollständige interne Ereignis-/Anhangsbeziehungen
- manipulierte Archive hinterlassen weder Zielordner noch außerhalb geschriebene Dateien
- Integrationstests für Transfer samt Anhang, Manifest, fehlendes Manifest, falsche Größe/Prüfsumme, ZIP-Traversal und den vollständigen Workspace-Ablauf ergänzt

### Meilenstein 3B – Autosave, Recovery, Recent Projects und lokale Logs

- maximal 20 zuletzt verwendete Projekte werden lokal, versioniert und atomar als JSON gespeichert
- fehlende Archive werden gekennzeichnet und können gezielt aus der Liste entfernt werden
- jeder aktive Workspace erhält einen nicht exportierten Recovery-Marker mit Projekt- und Prozessidentität
- aktive Prozesse werden von der Recovery-Suche ausgeschlossen; verwaiste gültige SQLite-Arbeitskopien können wiederhergestellt oder verworfen werden
- Workspace-Speicherungen sind gegen konkurrierende manuelle/automatische Aufrufe serialisiert
- abbrechbarer Autosave-Koordinator speichert ausschließlich als geändert markierte Projekte und meldet erwartbare Fehler, ohne die Schleife zu beenden
- größenbegrenzt rotierende technische JSON-Lines-Logs mit Lesen, Export und Löschen implementiert
- Logeinträge begrenzen Nachrichten/Fehlerdetails und enthalten keine automatisch übernommenen Dokumentinhalte
- Integrationstests für Recent Projects, Recovery, einen vollständigen Autosave-Zyklus und Logrotation/-export/-löschung ergänzt

### Meilenstein 3C – Dependency Injection und verbundene MVVM-Projektoberfläche

- Microsoft.Extensions.DependencyInjection 8.0.1 als produktiven Composition Root eingeführt und lizenzrechtlich dokumentiert
- Repository, Archiv, Workspace, Recovery, Recent Projects, Autosave, lokale Logs, Dialoge, Haupt-ViewModel und Hauptfenster zentral mit Lebenszyklusprüfung registriert
- WPF-Startablauf ohne StartupUri implementiert; .zeitprojekt-Kommandozeilenargumente werden nach erfolgreicher Initialisierung geöffnet
- deutschsprachigen MVVM-Startbildschirm mit Neu, Öffnen, zuletzt verwendeten Projekten und Recovery-Aktionen umgesetzt
- verbundene Projektansicht für Speichern, Speichern unter, Duplizieren, Schließen und chronologische Ereignisübersicht umgesetzt
- asynchrone Commands verhindern Doppelaufrufe und aktualisieren ihre Verfügbarkeit über den gebundenen Busy-/Projektzustand
- geordneter Fensterschluss fragt bei ungespeicherten Änderungen nach Speichern, Verwerfen oder Abbrechen
- globale WPF-Dispatcherfehler werden abgefangen, lokal strukturiert protokolliert und mit verständlicher deutscher Meldung angezeigt
- periodisches Autosave wird mit dem Anwendungslebenszyklus gestartet und beim Beenden abbrechbar heruntergefahren

### Meilenstein 4A – Ereignisse, Datumsgenauigkeiten, Fristen, Tags und Links

- vollständiges deutschsprachiges MVVM-Formular für Ereignisse mit getrennten Reitern für Inhalt/Datum und Frist/Einordnung/Links umgesetzt
- alle fünf Datumsgenauigkeiten werden ohne erfundene Komponenten erfasst und beim Bearbeiten wieder in ihre ursprünglichen Felder geladen
- unabhängige Frist mit optionaler Uhrzeit, Bezeichnung, Status und Erinnerungsnotiz angebunden
- Titel, Kurzinfo, Beschreibung, Quelle, Notizen, Priorität, Status und frei wählbare #RRGGBB-Farbe bearbeitbar
- Schlagwörter werden normalisiert und dedupliziert; Webseitenlinks akzeptieren ausschließlich absolute HTTP-/HTTPS-Adressen und optionale Bezeichnungen
- Erstellen, Bearbeiten und bestätigtes Löschen sind über die gebundene chronologische Ereignisliste verfügbar
- Bearbeitungen erzeugen zuerst ein vollständig validiertes Ersatzereignis; bestehende IDs, Anhänge, manuelle Reihenfolge und unveränderte Link-IDs bleiben erhalten
- fehlgeschlagene Validierung ersetzt das vorhandene Ereignis nicht und kann keinen halb geänderten Aggregatzustand hinterlassen
- Projekt wird nach jeder erfolgreichen Änderung als ungespeichert markiert und in chronologischer Reihenfolge neu dargestellt
- vier zusätzliche Unit-Tests decken vollständiges Erstellen, ID-/Anhangserhalt, atomare Fehlerabwehr und Löschen ab

### Meilenstein 4B – Undo/Redo, Audit und manuelle Reihenfolge

- projektbezogene Undo-/Redo-Historie mit maximal 100 vollständigen Vorher-/Nachher-Schritten implementiert
- Erstellen, Bearbeiten, Löschen und die gemeinsame Umsortierung mehrerer Ereignisse sind vollständig rückgängig machbar und wiederholbar
- neue Änderungen nach einem Undo verwerfen den nicht mehr gültigen Redo-Zweig; beim Schließen wird die Sitzungshistorie freigegeben
- Strg+Z/Strg+Y sowie gebundene Schaltflächen mit korrektem CanExecute-Zustand angebunden
- Ereignisse mit identischer fachlicher Datumsangabe können über zugängliche Früher-/Später-Aktionen manuell geordnet werden; andere Datumsgruppen bleiben unverändert
- produktiven SQLite-Auditdienst für Schreiben und chronologisch absteigendes Lesen der vorhandenen AuditLog-Tabelle implementiert
- erfolgreiche Create/Update/Delete/Undo/Redo/Reorder-Operationen werden lokal mit UTC-Zeitpunkt, Entität und Beschreibung protokolliert
- Auditfehler verändern eine bereits erfolgreiche Benutzeroperation nicht, sondern werden im rotierenden technischen Lokalprotokoll festgehalten
- Änderungsprotokoll über einen schreibgeschützten WPF-Dialog erreichbar
- vier zusätzliche Unit-Tests prüfen Undo/Redo für Anlage, Änderung und Löschung sowie gruppenbegrenzte Sortierung
- zwei zusätzliche Integrationstests prüfen Audit-Roundtrip, Sortierung und Erhaltung bei nachfolgenden Repository-Speicherungen

### Meilenstein 5A – Sicherer Mehrfachimport und Anhangsverwaltung

- externe Dateien werden asynchron und streamend vollständig in den Projektarbeitsordner kopiert
- jeder Anhang erhält eine GUID-basierte kollisionsfreie interne Datei unterhalb seines Ereignisses; gleiche Originaldateinamen überschreiben sich nicht
- SHA-256 wird während des Kopierens ohne vollständiges Laden in den Arbeitsspeicher berechnet
- Länge und Änderungszeit der Quelldatei werden nach dem Kopieren erneut geprüft; während des Imports veränderte Dateien werden verworfen
- Zielpfade werden kanonisch auf den Workspace begrenzt; projektinterne Reparse-Point-Anhangsordner werden abgelehnt
- PDF, PNG/JPEG/TIFF/BMP, DOCX und XLSX erhalten stabile Medientypen; andere Dateien bleiben als Binäranhang transportierbar
- Mehrfachauswahl und Drag-and-drop auf das Hauptfenster verarbeiten jede Datei einzeln und erhalten erfolgreiche Kopien bei Teilfehlern
- Dateiname, Fortschritt, Erfolgs-/Fehlerzähler und explizites Abbrechen werden in der Statusleiste angeboten
- ein Fehler einer einzelnen Datei beschädigt weder Projekt noch andere Importe; unvollständige Zieldateien werden bestmöglich entfernt
- hinzugefügte und entfernte Anhangszuordnungen sind als ein Schritt rückgängig/wiederholbar und werden im Audit protokolliert
- physische Projektkopien entfernter Anhänge bleiben für Undo erhalten
- vier Integrationstests prüfen kollisionsfreie Gleichnamigkeit, Teilfehler, Abbruchbereinigung und Unabhängigkeit von der Quelldatei
- ein Unit-Test prüft Undo für Hinzufügen und Entfernen einer Anhangszuordnung

### Meilenstein 5B1 – DOCX- und XLSX-Analyse

- DOCX-Haupttext wird lokal und ohne Office-Installation aus WordprocessingML extrahiert
- Absätze, Zeilenumbrüche und Tabulatoren werden in durchsuchbaren Klartext überführt
- XLSX-Shared-Strings, Inline-Strings und numerische Zellwerte werden arbeitsblattweise extrahiert
- Office-Kerneigenschaften wie Titel, Autor, Betreff und Erstellungs-/Änderungszeit werden als Metadaten gelesen
- deutsche und ISO-Datumsfundstellen werden dedupliziert und auf 200 Vorschläge begrenzt
- XML-DTDs und externe Resolver sind gesperrt; Zeichenmenge, Eintragszahl, Einzel-/Gesamtgröße und Kompressionsverhältnis sind begrenzt
- Analyzer liefern erwartbare Format-/Dateifehler als OperationResult statt das Projekt zu beschädigen
- drei Integrationstests prüfen DOCX-Text/Metadaten/Datum, XLSX-Zellauflösung/Datum und ein defektes Archiv

### Meilenstein 5B2 – Analyseergebnispersistenz

- extrahierter Text, Extraktionsmethode, Metadaten, Titel, Datumsfundstellen, Vorschaureferenz und Seitenzahl werden transaktional in SQLite gespeichert
- ein Analyseergebnis ersetzt atomar den vorherigen Stand desselben Anhangs und setzt dessen Datenbankzustand auf bereit
- gespeicherte Analyseergebnisse können vollständig über den Application-Port zurückgelesen werden
- FTS5 wird in derselben Transaktion aktualisiert; neuer Dokumenttext ist unmittelbar lokal durchsuchbar
- Projekt-/Anhangszugehörigkeit wird vor jedem Schreiben per Fremdschlüsselbeziehung geprüft
- ein Integrationstest prüft Roundtrip, Anhangszustand und unmittelbaren Volltexttreffer

### Meilenstein 5B3 – begrenzte Analysewarteschlange und UI-Anbindung

- DOCX- und XLSX-Analyzer sind im Composition Root registriert und werden nach einem erfolgreichen Import automatisch gestartet
- eine zentrale Warteschlange begrenzt die gleichzeitige lokale Analyse auf zwei Dokumente und bewahrt die Eingabereihenfolge der Einzelergebnisse
- Abbruch wird durch alle Analyzer und Speicherzugriffe weitergereicht; nicht unterstützte Formate erhalten ein explizites Einzelergebnis statt den Stapel abzubrechen
- neu zugeordnete Anhänge werden vor der Analyse durch einen serialisierten lokalen Workspace-Checkpoint in SQLite persistiert, ohne das gespeicherte Projektarchiv vorzeitig zu ersetzen
- erfolgreiche Analyseergebnisse und fehlgeschlagene Zustände werden zurück in das Projektaggregat synchronisiert und erneut als wiederherstellbare Arbeitskopie gesichert
- technische Analysezustände erzeugen keinen zusätzlichen fachlichen Undo-Schritt; Anhang hinzufügen und entfernen bleiben weiterhin die rückgängig machbaren Benutzeroperationen
- Statusleiste und Abbruchaktion zeigen Import- und Analysefortschritt, aktuelle Datei sowie Erfolgs-/Fehleranzahl
- eine manuelle Neuanalyse ist für die DOCX-/XLSX-Anhänge des ausgewählten Ereignisses verfügbar
- ein schreibgeschützter Anhangsdialog zeigt Dateityp, Analysezustand, Dokumenttitel, extrahierten Text, Datumsfundstellen und erkannte Metadaten
- drei Integrationstests prüfen Parallelitätsgrenze/Ergebnisreihenfolge, Abbruch und nicht unterstützte Typen; ein weiterer Test prüft den Checkpoint ohne Archivexport

### Meilenstein 5C1 – lokale PDF-Textextraktion

- PdfPig 0.1.15 als Apache-2.0-lizenzierte reine .NET-Produktionsabhängigkeit ohne transitive .NET-8-Pakete eingeführt und in der Drittanbieterübersicht dokumentiert
- eingebetteter PDF-Text wird vollständig lokal in Inhaltsreihenfolge extrahiert; Microsoft Office, Cloud-Dienste oder externe Prozesse sind nicht erforderlich
- Titel, Autor, Betreff, Schlagwörter, Erzeuger, Erstellungs-/Änderungsdatum, PDF-Version, Verschlüsselungszustand und Seitenzahl werden als Dokumentmetadaten übernommen
- Datumsfundstellen werden gemeinsam aus Text und Metadaten erkannt und über die bestehende transaktionale Analyseablage sofort durchsuchbar
- maximal 100.000 Seiten, 10 Millionen extrahierte Zeichen und eine Parser-Stacktiefe von 64 begrenzen die Verarbeitung; fehlende Fonts werden übersprungen
- die synchrone PDF-Verarbeitung läuft außerhalb des UI-Threads und prüft den CancellationToken vor dem Öffnen sowie vor jeder Seite
- der PDF-Analyzer ist in die vorhandene begrenzte Warteschlange, automatische Importanalyse, manuelle Neuanalyse und Ergebnisanzeige integriert
- drei Integrationstests prüfen Text/Metadaten/Datumsfundstelle/Seitenzahl, beschädigte PDFs und einen bereits ausgelösten Abbruch

### Meilenstein 5C2 – lokale Bildvorschau und Öffnen von Projektkopien

- zentraler Application-Port und lokaler Infrastructure-Dienst validieren Anhänge vor Vorschau oder externem Öffnen erneut
- interne Pfade werden kanonisch auf den Projektarbeitsordner begrenzt; Reparse Points in Arbeitsordner, Unterordnern oder Datei werden abgelehnt
- Dateilänge, Änderungsstabilität während der Prüfung und SHA-256 werden asynchron gegen die gespeicherten Anhangsmetadaten geprüft
- PNG, JPEG, TIFF und BMP können über eine eigene lokale WPF-Vorschau angezeigt werden
- die Bilddekodierung lädt ohne dauerhafte Dateisperre, begrenzt die Vorschau auf 2.400 Pixel Breite und verweist Bilder über 512 MiB auf das Standardprogramm
- jeder Anhang kann nach expliziter Auswahl und erfolgreicher Integritätsprüfung über Windows im zugeordneten Standardprogramm geöffnet werden
- Prozesshandles des über ShellExecute gestarteten Standardprogramms werden unmittelbar freigegeben; die Projektkopie selbst bleibt unverändert
- drei Integrationstests prüfen unveränderte Projektkopie, gleich lange Prüfsummenmanipulation und Abbruch

### Meilenstein 5C3 – integrierte lokale PDF-Vorschau

- PDFtoImage 5.2.1 (MIT), PDFium 147.0.7690 und SkiaSharp 3.119.2 wurden als lokale, `win-x64`-geeignete Renderingkette eingeführt und vollständig in der Drittanbieterübersicht dokumentiert
- ein zunächst geprüfter PdfPig-/Skia-Renderer wurde wegen einer reproduzierbaren ungefangenen Systemschrift-Parserausnahme bei einer gültigen Helvetica-Standardfont-PDF verworfen; der PDFium-Ersatz rendert denselben Fall stabil
- der Application-Port liefert einzelne PNG-Seiten; PDFium bleibt als natives Detail in der Dokumentverarbeitung und startet weder externe Prozesse noch Netzwerkzugriffe
- Seitenanzahl, Zielseite, Seitengröße und Ausgabe werden validiert; Limits von 100.000 Seiten, 24 Millionen Pixeln, 8.000 Pixeln je Kante und 100 MiB PNG begrenzen die Vorschau
- die geprüfte Projektkopie bleibt während der nativen Verarbeitung ohne Schreib- oder Löschfreigabe geöffnet; Abbruch wird vor, zwischen und nach den nicht unterbrechbaren nativen Einzelschritten geprüft
- das MVVM-Vorschaufenster bietet Vor/Zurück, Seitennummer, Zoom hinein/heraus, Fensterbreite, ganze Seite und Scrollen
- eine optional am Anhang verknüpfte PDF-Seite wird beim Öffnen bevorzugt; bei einer ungültigen Verknüpfung wird verständlich auf Seite 1 zurückgefallen
- die Schaltfläche „In Windows öffnen“ führt erneut die zentrale Pfad-, Reparse-Point-, Längen-, Stabilitäts- und SHA-256-Prüfung aus
- Render- und Öffnungsfehler bleiben im Dialog handlungsorientiert sichtbar und werden ohne Dokumentinhalt strukturiert lokal protokolliert
- sechs Integrationstests prüfen Standardfont-Rendering/Seitenauswahl, Bereichsabwehr, Zoom, kleine Anpassungsskala, defekte PDFs und Abbruch
- die selbstenthaltende `win-x64`-Veröffentlichung enthält ausschließlich x64-PDFium/Skia; ein transitives 83-MiB-Nativsymbol wird gezielt nicht ausgeliefert

### Meilenstein 5D – lokale OCR für Bilder und bildbasierte PDFs

- die lokale Windows-OCR (`Windows.Media.Ocr`) verarbeitet PNG, JPEG, TIFF und BMP ohne Cloud-Dienst, externe Prozesse oder separate OCR-Produktionsbibliothek
- eine installierte deutsche Windows-Texterkennungsressource wird explizit ausgewählt; fehlt sie, erhält der Benutzer eine verständliche Anleitung statt eines Startfehlers
- der Windows-SDK-Targeting-Pack 10.0.19041.56 und WinRT.Runtime 2.2.0.48161 sind als ausgelieferte Projektion dokumentiert; Anwendung, Dokumentverarbeitung und Integrationstests zielen auf `net8.0-windows10.0.19041.0` bei Mindestplattform Windows 10 Version 1507
- Bilddekodierung berücksichtigt EXIF-Ausrichtung und skaliert auf höchstens 24 Millionen Pixel sowie das dynamische Windows-OCR-Kantenlimit; Bilddateien sind auf 512 MiB und Mehrseitenbilder auf 250 Seiten begrenzt
- PDF-Seiten ohne eingebetteten Text werden bei dreifacher Basisskalierung über die vorhandene PDFium-Kette gerendert und anschließend lokal erkannt; eingebetteter Text bleibt vorrangig
- vollständig bildbasierte PDFs werden als OCR, gemischte PDFs als kombinierte eingebettete/OCR-Extraktion gekennzeichnet; höchstens 250 OCR-Seiten und zehn Millionen erkannte Zeichen schützen Ressourcen
- OCR-Ergebnisse tragen dauerhaft einen Warnstatus, verwendete Sprache, betroffene Seiten und einen sichtbaren Hinweis auf mögliche Erkennungsfehler
- OCR-Texte, Metadaten und Datumsfundstellen werden transaktional gespeichert und in derselben Transaktion in FTS5 aufgenommen; manuelle Neuanalyse steht nun auch für Bildanhänge zur Verfügung
- die Dokumentanalyse meldet Datei- und OCR-Seitenfortschritt, serialisiert die Windows-OCR zusätzlich zur zweifach begrenzten Analysewarteschlange und reicht Abbruch bis in WinRT-Async-Aufrufe weiter
- fünf neue Integrationstests prüfen reale deutsche OCR an einer gerenderten Testseite, defekte Bilder, Abbruch, bildbasierte PDFs und gemischte PDF-Extraktion; der bestehende Persistenztest prüft nun OCR-Warnstatus und FTS-Treffer

### Meilenstein 6A – Zeitachsen- und Kartenlayoutmodell

- ein UI-unabhängiger `TimelineLayoutEngine` projiziert Ereignisse, Zeiträume und Fristen deterministisch auf dieselbe Achse für horizontale und vertikale Ansichten
- die automatische Skala wählt abhängig vom Projektzeitraum Stunden, Tage, Wochen, Monate, Jahre oder Jahrzehnte; Zoomfaktoren von 25 bis 800 Prozent fließen direkt in die Projektion ein
- Ereigniskarten wechseln automatisch die Achsenseite und erhalten bei Kollisionen weitere Bahnen; gleiche Datumswerte bleiben in ihrer fachlich/manuell festgelegten Reihenfolge
- sehr große tatsächlich leere Zeitlücken werden auf eine feste sichtbare Länge komprimiert und mit einer exakten Jahre-/Monate-/Tage-Beschriftung ausgegeben
- Zeiträume eines Ereignisses gelten nicht als leer und werden daher niemals als Achsenunterbrechung komprimiert
- Achsenticks werden passend zur Skala erzeugt, innerhalb komprimierter Lücken ausgelassen und auf 2.000 Einträge begrenzt
- unabhängige Fristen erhalten eigene Achsenpositionen, Status und Verbindungskoordinaten zum zugehörigen Ereignis
- vorhandene horizontale/vertikale `LayoutPosition`-Versätze werden orientierungsrichtig angewendet, ohne Datumswerte zu verändern; extreme gespeicherte Versätze werden für eine stabile Darstellung begrenzt
- 13 neue Unit-Tests prüfen alle sechs Skalen, Lückenerkennung/-beschriftung, Zeitraumabwehr, Seiten-/Bahnverteilung, beide Versatzrichtungen, Fristprojektion und 5.000 Ereignisse

### Meilenstein 6B – virtualisierte WPF-Zeitstrahlansicht

- ein eigenes `FrameworkElement` mit `IScrollInfo` zeichnet Achse, Ticks, Unterbrechungen, Fristverbindungen und Ereigniskarten unmittelbar in den sichtbaren Viewport; auch bei großen Projekten entstehen keine tausenden WPF-Kartenelemente
- horizontale und vertikale Ansicht sind über deutlich sichtbare Werkzeugleistenaktionen umschaltbar; die bevorzugte Orientierung wird als Projekteinstellung gespeichert und im Audit protokolliert
- Karten zeigen Datum beziehungsweise Zeitraum, Titel, Kurzinfo, Priorität, Frist, Anhangsanzahl, Projektfarbe und den Zustand einer manuellen Position; ausgewählte Karten werden hervorgehoben und per Mausklick mit dem ViewModel synchronisiert
- Zoomen ist über Mausrad, Schaltflächen und Tastatur von 25 bis 800 Prozent möglich; der Mauszeiger bleibt beim Mausradzoom am selben Achseninhalt
- freie Mausverschiebung, horizontale/vertikale Scrollleisten, Gesamtprojektansicht, Zentrierung des ausgewählten Ereignisses und Zurücksetzen der Ansicht sind bedienbar
- die Lückenkompression kann in der Ansicht ein- und ausgeschaltet werden; die Einstellung wird im Projekt gespeichert, als ungespeicherte Änderung markiert und im Audit protokolliert
- die chronologische Ereignisliste bleibt als alternative, tastaturzugängliche Registerkarte erhalten
- ein neuer STA-WPF-Integrationstest rendert einen realen Zeitstrahl mit 100 Ereignissen und Frist in beiden Orientierungen in Bitmaps und prüft Navigation sowie Zoomgrenzen

### Meilenstein 6C – manuelle Kartenpositionen und Zeitraum-Navigation

- Ereigniskarten können in beiden Ansichten mit der linken Maustaste rein visuell gezogen werden; während der Bewegung wird die neue Karten- und Verbindungslage unmittelbar als Vorschau gezeichnet
- fachlicher Datumsanker und visuelle Kartenposition sind getrennt: auch ein Versatz entlang der Zeitachse lässt die Verbindungslinie am tatsächlichen Ereignisdatum beginnen und verändert kein Datumsfeld
- Karten können über die Achse hinweg auf die jeweils andere Seite verschoben werden; die Anschlusskante folgt der sichtbaren Kartenseite
- horizontale und vertikale Versätze werden getrennt, endlich und auf ±100.000 Pixel begrenzt im vorhandenen `LayoutPosition`-Modell gespeichert
- jede Kartenverschiebung ist ein eigener Undo-/Redo-Schritt, markiert das Projekt als ungespeichert und wird im lokalen Audit protokolliert
- „Auto-Layout“ entfernt alle manuellen Positionen in einem gemeinsamen Undo-/Redo-Schritt; das Rückgängigmachen eines Ereignislöschvorgangs stellt nun auch dessen manuelle Positionen wieder her
- ein frei gewähltes Start-/Enddatum kann in den Viewport eingepasst werden; die Navigation verwendet exakt dieselbe automatische Skala und Lückenkompression wie der gezeichnete Zeitstrahl
- vier neue Unit-Tests prüfen orientierungsbezogenes Layout-Undo/Redo, gemeinsames Zurücksetzen, Positionswiederherstellung nach Löschen und die Datumsprojektion; der WPF-Integrationstest prüft zusätzlich Move-Command und Zeitraumauftrag
- die Integrationsklassen laufen seriell, weil mehrere dateibasierte Tests prozessweite SQLite-Verbindungspools bereinigen; dadurch kann eine Klassenbereinigung nicht mehr mit dem Datenbanklebenszyklus einer anderen Klasse kollidieren

### Meilenstein 6D – Dokumentminiaturen und projektbezogene Darstellung

- sichtbare Ereigniskarten zeigen verzögert eine kleine Vorschau des ersten PDF-Anhangs oder, falls kein PDF vorhanden ist, des ersten unterstützten Bildanhangs; PDF-/HTML-Export und WPF verwenden dieselbe zentrale Primärauswahl
- der neue Thumbnail-Dienst lädt ausschließlich zentral geprüfte Projektkopien, begrenzt Bildquellen auf 50 MiB, 8.000 Pixel je Kante und 24 Millionen Pixel und übernimmt PDF-Seiten über den vorhandenen begrenzten PDFium-Dienst
- Miniaturen werden mit dem bereits vorhandenen SkiaSharp auf höchstens 360 × 240 Pixel reduziert, als JPEG atomar unter `thumbnails/timeline` abgelegt und über Anhangs-ID, verknüpfte Seite und vollständige SHA-256 eindeutig adressiert; es wurde keine neue Produktionsabhängigkeit eingeführt
- höchstens zwei Miniaturen werden gleichzeitig erzeugt; die WPF-Ansicht fordert nur tatsächlich gezeichnete Karten an, bricht überholte oder entladene Aufträge ab und hält höchstens 128 dekodierte Vorschaubilder nach LRU-Nutzung im Speicher
- ein MVVM-Dialog bearbeitet Farbschema, bevorzugte Orientierung, Lückenkompression, Standardfarbe neuer Ereignisse sowie Karten-, Achsen- und Exportschriftgröße; Änderungen werden validiert, im Projekt checkpointed und im Audit protokolliert
- Hell, Dunkel und die lokale Windows-App-Einstellung werden ohne Neustart über dynamische WPF-Ressourcen auf Hauptfenster und Standarddialoge angewendet; die direkt gezeichnete Zeitachse besitzt eine eigene kontrastgeprüfte Palette
- die Kartengröße wächst bei größeren Kartenschriften mit, Achsenbeschriftung und neue Ereignisse übernehmen unmittelbar die gespeicherten Vorgaben; der PDF-Export verwendet weiterhin denselben persistierten Exportfont
- drei neue Unit-Tests prüfen Primärauswahl und schriftgrößenabhängiges Layout; fünf neue Integrationstests prüfen Einstellungsvalidierung/Standardfarbe, sicheren lokalen Thumbnail-Cache und Abbruch, der erweiterte STA-WPF-Test prüft verzögertes Laden und beide Paletten

### Meilenstein 6E – Ereignissortierung und zielgenauer Datei-Drop

- Ereignisse lassen sich in der chronologischen Liste per Drag-and-drop vor oder nach ein Ziel derselben fachlichen Datumsgruppe einordnen; Drops auf andere Datumswerte und wirkungslose Positionen werden bereits im `CanExecute`-Pfad abgelehnt
- eine Sortieränderung normalisiert ausschließlich die manuellen Sortierpositionen der betroffenen Datumsgruppe und verändert weder Datum noch Uhrzeit; die sichtbaren Früher-/Später-Aktionen bleiben als tastaturzugängliche Alternative erhalten
- beliebige Sortier-Drops sind ein gemeinsamer Undo-/Redo-Schritt, halten das gezogene Ereignis ausgewählt, markieren das Projekt als ungespeichert und erzeugen einen lokalen `Reorder`-Audit-Eintrag
- externe Einzel- und Mehrfachdateien können auf das Hauptfenster, eine konkrete Zeitstrahlkarte, eine Ereigniszeile, die Ereignisliste oder den neuen sichtbaren Anhangsbereich abgelegt werden
- konkrete Karten und Zeilen bestimmen ihr Zielereignis ausdrücklich; freie Flächen und der Anhangsbereich verwenden das ausgewählte Ereignis, ohne dass ein während des asynchronen Imports wechselnder Auswahlzustand das Batch-Ziel ändern kann
- alle Drop-Pfade verwenden unverändert den vorhandenen sicheren Importdienst mit Projektkopie, kollisionsfreien Namen, Fortschritt, Teilfehlerbehandlung, Abbruch, gemeinsamem Anhangs-Undo, Checkpoint, Analyse und Audit
- WPF-Code-behind übersetzt ausschließlich Treffertest und geroutete Drag-Daten in typisierte Commands; Sortierung, Datumsgruppenregel, Historie, Projektänderung und Audit bleiben in Application-Schicht und ViewModel
- zwei neue Unit-Tests prüfen beliebige gruppeninterne Einordnung, No-op-/Gruppengrenzen sowie Undo/Redo; ein neuer Integrationstest prüft explizites Batch-Ziel, Selektion, Checkpoint und Audit beim Datei-Drop

### Meilenstein 7A – lokale Volltextsuche und kombinierbare Filter

- die Schema-Migration 2 ergänzt einen getrennten FTS5-Index ausschließlich für extrahierte PDF-, OCR-, DOCX- und XLSX-Inhalte; vorhandene Datenbanken der Version 1 werden transaktional aktualisiert und vorhandene Dokumenttexte übernommen
- aktuelle Projekt- und Ereignisfelder werden direkt aus dem geöffneten Aggregat durchsucht, sodass noch nicht ins Archiv geschriebene Änderungen sofort erscheinen und veraltete gespeicherte Titel keine falschen Treffer erzeugen
- Benutzereingaben werden auf höchstens 32 alphanumerische Präfixbegriffe mit je 64 Zeichen begrenzt, als parametrisierte FTS-Ausdrücke ausgeführt und liefern höchstens 5.000 Ereignistreffer
- Projekttitel/-beschreibung, Ereignistitel, Infotext, Beschreibung, Notizen, Quelle, Schlagwörter, Dateinamen und Webseiten werden gemeinsam mit extrahierten Dokumentinhalten durchsucht
- Zeitraum, Datumsart, Frist vorhanden, Friststatus, Priorität, Farbe, Schlagwort, Dateityp, Anhang vorhanden, PDF vorhanden und Suchbegriff sind frei kombinierbar
- die linke Such-/Filteroberfläche aktualisiert nach 250 Millisekunden Eingabepause, bricht überholte Suchen ab, meldet ungültige Zeiträume, sortiert nach Relevanz oder Datum und setzt alle Filter gemeinsam zurück
- FTS- und Feldfundstellen tragen eindeutige Marker und werden durch ein kleines WPF-Steuerelement visuell hervorgehoben
- ein Treffer wählt das Ereignis aus und zentriert es direkt im Zeitstrahl; aktive Filter liefern eine ID-Menge an dasselbe Layoutmodell, sodass auch die horizontale/vertikale Darstellung ausschließlich passende Karten zeigt
- `Strg+F` und die sichtbare Werkzeugleistenaktion fokussieren das Suchfeld; Statusleiste und Suchkopf zeigen die Anzahl aktiver Filter und Treffer
- ein zusätzlicher Unit-Test prüft das gefilterte Layout; vier neue Integrationstests prüfen Dokumentpräfix/Hervorhebung, sämtliche kombinierte Filter, ungespeicherten Text samt Alttextabwehr und die Schema-Aktualisierung 1→2

### Meilenstein 8A – druckoptimierter PDF-Export

- ein UI-unabhängiger Exportplaner filtert den gewählten Zeitraum, erhält überlappende Ereigniszeiträume optional und nimmt unabhängige Fristen innerhalb des Bereichs auf
- A4, A3, Letter und validierte benutzerdefinierte Maße von 50 bis 5.080 mm werden in Hoch- und Querformat unterstützt; A4 ist die Voreinstellung
- der mehrseitige Modus verschiebt gewöhnliche Karten vollständig auf die Folgeseite und teilt nur Inhalte, die höher als eine ganze Inhaltsseite sind, mit sichtbarer Fortsetzungskennzeichnung
- die große Einzelseite berechnet und zeigt ihre tatsächliche Höhe vor dem Speichern; ab 1.000 mm warnt die Anwendung vor möglichen Einschränkungen üblicher Betrachter und Drucker
- Ereigniskarten enthalten Datum beziehungsweise Zeitraum, Uhrzeit, Titel, Kurz- und Langtext, Frist samt Status, Priorität, Status, Farbe, Schlagwörter, Quelle, optionale Notizen und alle Dokumentnamen
- primäre PDF-Seiten und Bilder werden ausschließlich nach erneuter zentraler Pfad-, Reparse-Point-, Längen-, Stabilitäts- und SHA-256-Prüfung als kleine Vorschau geladen; fehlerhafte Miniaturen verlieren nicht den textuellen Dokumentverweis
- SkiaSharp 3.119.2 erzeugt Texte, Achsen, Linien, Rahmen und Kennzeichnungen vektorbasiert; Projektfarbe ist durch Prioritäts-/Status-/Fristtext auch in Schwarz-Weiß nicht das einzige Unterscheidungsmerkmal
- die Exportvorschau rendert die tatsächlich erzeugte temporäre PDF über den vorhandenen PDFium-Pfad und bietet Seitennavigation, Zoom, Fensterbreite, ganze Seite, Papierformat, Ausrichtung, Bereich, Schriftgröße, Warnungen und explizites Aktualisieren
- der endgültige Export wird zunächst vollständig im Zielordner erzeugt und danach atomar übernommen; Ziele innerhalb des aktiven Projektarbeitsordners und Zieldateien ohne `.pdf` werden abgelehnt
- sieben neue Unit-Tests prüfen Seitenaufteilung, verlustfreie Fortsetzungen, Zeitraum-/Fristfilter, große Einzelseite, Ausrichtung und Validierung; vier neue Integrationstests prüfen PDF-Text, PDFium-Darstellung, atomare Ersetzung, Abbruch, Miniaturen und den realen WPF-Dialog

### Meilenstein 8B – eigenständiger Standalone-HTML-Export

- die WPF-Werkzeugleiste bietet einen abbrechbaren HTML-Export mit Zielauswahl, horizontaler oder vertikaler Startansicht sowie getrennten Optionen für Vorschaubilder und private Notizen
- der Export erzeugt genau eine UTF-8-HTML-Datei mit eingebettetem CSS, JavaScript, JSON und optionalen JPEG-Miniaturen; es werden weder CDN-, Schrift-, Skript- noch Stylesheet-Ressourcen nachgeladen
- eine restriktive Content Security Policy sperrt Netzwerk-, Objekt-, Formular- und Basis-URI-Zugriffe; alle benutzerkontrollierten Daten werden mit dem sicheren `System.Text.Json`-Encoder eingebettet und im Browser ausschließlich über DOM-Textknoten ausgegeben
- Projektinformationen einschließlich Gesamtzeitraum und Ereignisse mit ihrer tatsächlichen Datumspräzision, Frist, Status, Priorität, Farbe, Schlagwörtern, Quellen, Weblinks und Dokumentverweisen werden als unveränderliche Momentaufnahme exportiert
- extrahierte lokale Dokumenttexte fließen in den Suchindex der Datei ein, ohne im Detailbereich offengelegt zu werden; Notizen werden nur nach ausdrücklicher Auswahl übernommen
- primäre Bild- und PDF-Vorschaubilder werden ausschließlich über die zentral validierte Projektkopie geladen; Bildquellen sind vor dem Decoding auf 8.000 Pixel je Kante und 24 Millionen Pixel begrenzt, Miniaturen auf höchstens 360 × 240 Pixel reduziert und als komprimiertes JPEG eingebettet; ein Vorschaufehler lässt die textuellen Dokumentverweise bestehen
- die responsive Datei unterstützt horizontale und vertikale Darstellung, Zoom von 50 bis 250 Prozent, Maus-/Stiftverschiebung, Volltextsuche, Zeitraum-, Farb-, Schlagwort- und Fristfilter, Zurücksetzen, aufklappbare Details und druckfreundliches CSS
- größere unbelegte Zeitabstände werden sichtbar als Unterbrechung markiert; externe HTTP-/HTTPS-Links tragen eine sichtbare Warnung und werden erst nach Bestätigung in einem neuen Fenster geöffnet
- der Export schreibt zunächst eine eindeutige temporäre Datei im Zielordner und übernimmt sie erst nach vollständigem Erfolg atomar; Ziele im aktiven Projektarbeitsordner sowie andere Dateiendungen als `.html`/`.htm` werden abgelehnt
- ein Unit-Test prüft JSON-Roundtrip und die Abwehr eines `</script>`-Injektionsversuchs; drei Integrationstests prüfen Offline-Vertrag, Optionen, Datumsgrenzen/-präzision, Dokumentvolltext, Miniatur, atomare Ersetzung, Workspace-Schutz und Abbruch

### Meilenstein 10A – lokale Sicherung, Rotation und Wiederherstellung

- der vollständige lokale `IBackupService` erzeugt manuelle und automatische `.zeitprojekt`-Sicherungen unter `%LocalAppData%\Zeitstrahl Studio\Backups\{Projekt-ID}`; Sicherungsdateien enthalten UTC-Zeitpunkt, Art und unveränderliche ID im verwalteten Namen
- jeder Snapshot speichert zuerst den aktuellen Aggregatzustand serialisiert in die Workspace-SQLite-Datenbank und exportiert anschließend über den vorhandenen atomaren Archivdienst, ohne Archivpfad oder Ungespeichert-Zustand des geöffneten Projekts zu verändern
- Metadaten mit relativem Pfad, Dateigröße, SHA-256, UTC-Zeitpunkt und Sicherungsart werden erst nach vollständigem Archiv und stabiler Prüfsummenberechnung in SQLite eingetragen
- vollständig geschriebene Sicherungsarchive ohne Metadatensatz werden beim Auflisten anhand des streng validierten Dateinamens wieder in SQLite aufgenommen; verwaiste Metadatensätze ohne Datei werden bereinigt
- automatische Sicherungen werden nach erfolgreichen Projekterstellungen, manuellen Speicherungen und Autosaves fälligkeitsgesteuert angestoßen; ein Sicherungsfehler macht eine bereits erfolgreiche Projektspeicherung nicht rückgängig und wird lokal strukturiert protokolliert
- die konfigurierbare Rotation behält mehrere Sicherungen des aktuellen lokalen Tages, je eine tägliche Sicherung der letzten konfigurierten Tage und je eine wöchentliche Sicherung für den älteren Zeitraum; manuelle Sicherungen werden niemals automatisch entfernt
- alte automatische Sicherungen werden ausschließlich nach einer vollständig erfolgreichen neuen automatischen Sicherung rotiert; nicht löschbare Altdateien bleiben erhalten und erzeugen eine lokale Warnung
- die WPF-Werkzeugleiste öffnet einen MVVM-gesteuerten Sicherungsdialog mit lokaler Zeit, Art, Größe und SHA-256, manueller Sofortsicherung sowie validierten Aufbewahrungswerten
- vor jeder bestätigten Wiederherstellung wird eine manuelle Sicherheitssicherung des aktuellen Zustands erzeugt; die Auswahl wird vor und nach dem Import gegen Größe und SHA-256 geprüft, in einen neuen verwalteten Workspace geladen, mit dem bisherigen Archivpfad als ungespeichert markiert und im Audit protokolliert
- zwei neue Unit-Tests prüfen Aufbewahrungsstufen und Fälligkeit; acht neue Integrationstests prüfen Snapshot/Wiederherstellung, Manipulationsabwehr, Rotation, Metadatenrekonstruktion, Autosave-Anbindung, Einstellungs-Checkpoint, Wiederherstellungsbefehl und den realen WPF-Dialog

### Meilenstein 11A – freies Beispielprojekt und große integrierte Szenarien

- ein weitergebbares `.zeitprojekt`-Beispielarchiv enthält zehn vollständig frei erfundene Ereignisse mit allen fünf Datumsgenauigkeiten, zwei manuell geordneten Ereignissen am selben Datum, vier Fristen in allen Zuständen, unterschiedlichen Farben/Prioritäten/Schlagwörtern, einem reservierten `example.com`-Link, großen Zeitlücken und manuellen Positionen für beide Ausrichtungen
- fünf frei lizenzierte Quelldokumente werden mit fest definierten Inhalten erzeugt: zwei textbasierte PDFs, eine PNG-Planungstafel, eine DOCX-Werkstattnotiz und ein XLSX-Meilensteinplan; `samples/README.md` und `samples/LICENSE.txt` dokumentieren Inhalt, Nutzung, Neuerzeugung und MIT-Lizenz
- das neue Generatorwerkzeug verwendet ausschließlich vorhandene Produktionsdienste für sicheren Anhangsimport, PDF-/Office-Analyse, SQLite-Persistenz, Thumbnail-Erzeugung und Archivexport; es wurden keine neuen Produktionsabhängigkeiten eingeführt
- das Archiv enthält fünf validierte lokale Projektkopien ohne maschinenspezifische Ursprungsdateipfade, extrahierte und per FTS auffindbare Prüfterme, eine erzeugte Bildminiatur sowie drei nachvollziehbare Audit-Einträge
- zwei End-to-End-Integrationstests prüfen das eingecheckte und das neu erzeugte Archiv über realen Import, Anhangsintegrität, PDFium-Vorschau, Dokumentvolltextsuche, PDF-/Standalone-HTML-Export, semantische Reproduzierbarkeit, nachträgliche Ereignisbearbeitung und erneuten Projekttransfer mit unveränderten SHA-256-Prüfsummen
- ein weiterer Integrationstest persistiert und lädt 5.000 fachlich gemischte Ereignisse mit 40 Anhangsmetadaten, findet einen eindeutigen Begriff im letzten Ereignis und berechnet horizontales sowie vertikales Layout mit endlichen Koordinaten und höchstens 2.000 Achsenticks innerhalb einer großzügigen Zwei-Minuten-Grenze
- drei neue Integrationstests erhöhen den vollständigen Integrationsstand von 73 auf 76 Tests

### Meilenstein 11B – UI-/DPI-/Tastaturabnahme und negative End-to-End-Szenarien

- `MainWindow.xaml` und `MainWindow.xaml.cs` um Barrierefreiheitsattribute, Tastaturfokus-Visualisierung und AutomationProperties erweitert
- `TimelineView.cs` um zusätzliche Prüf- und Hilfseigenschaften für Barrierefreiheit und Testbarkeit ergänzt
- `MainWindowAccessibilityTests.cs` als neue Integrationstestklasse hinzugefügt; prüft Tastaturnavigation, Fokusreihenfolge, Automation-Labels und grundlegende Screenreader-Sichtbarkeit der Hauptfenstersteuerelemente
- `ProjectArchiveAndWorkspaceTests.cs` um negative End-to-End-Szenarien erweitert: ungültige Manifeste, fehlende Dateien, Größenabweichungen, Prüfsummenfehler und Workspace-Konflikte
- `TimelineViewTests.cs` um zusätzliche Layout- und Navigationsprüfungen ergänzt
- `ProjectArchiveService.cs` und `AssemblyInfo.cs` um Testunterstützung (`InternalsVisibleTo`) erweitert, ohne öffentliche Schnittstellen zu ändern
- zwölf neue Integrationstests erhöhen den vollständigen Integrationsstand von 76 auf 88 Tests

## Erfolgreiche Build- und Testbefehle

Am 21.07.2026 erfolgreich ausgeführt:

```powershell
.\build.ps1 -Task All -Version 0.2.1
```

Ergebnis: 0 Warnungen, 0 Fehler; 62 Unit-Tests bestanden; 88 Integrationstests bestanden; Formatprüfung bestanden; Publish und Installer erfolgreich.

Artefakte:
- `ZeitstrahlStudio-0.2.1-win-x64-setup.exe` (62.815.324 Bytes)
- `artifacts\release\ZeitstrahlStudio-0.2.1-win-x64-portable.zip` (87.695.616 Bytes)

Vorherige vollständige Matrix am 19.07.2026 erfolgreich ausgeführt:

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet restore src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -r win-x64
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
```

Aktueller Stand nach Meilenstein 11B und Installer-Korrekturen: Debug und Release jeweils 0 Warnungen/0 Fehler; jeweils 62 Unit-Tests und 88 Integrationstests bestanden. Die Formatprüfung meldet keine Abweichung. Der Installer-Build mit Inno Setup 6 verläuft erfolgreich ohne Warnungen und erzeugt `ZeitstrahlStudio-0.1.0-win-x64-setup.exe` direkt im Projekt-Hauptverzeichnis. Die Setup-EXE fordert durch `PrivilegesRequired=admin` Administratorrechte an, sodass die Installation unter `C:\Program Files` zuverlässig funktioniert.

## Phasenweiser Implementierungsplan

1. **Solution und Architektur – abgeschlossen:** Schichten, Fachmodellbasis, Ports, Architektur- und Formatdokumentation.
2. **Datenmodell und SQLite – abgeschlossen:** vollständiges normalisiertes Schema, Migration 1, Repository, Transaktionen, FTS5 und Integrationstests.
3. **Projektverwaltung – abgeschlossen:** sichere Arbeitsordner, Archivtransfer, Neu/Öffnen/Speichern/Speichern unter/Duplizieren/Schließen, zuletzt verwendet, Autosave, Crash-Recovery, produktive DI und verbundene MVVM-Oberfläche.
4. **Ereignisse und Fristen – abgeschlossen:** vollständige MVVM-Bearbeitung, Datumsgenauigkeiten, Fristen, Tags, Links, mehrstufiges Undo/Redo, manuelle Reihenfolge gleicher Datumswerte und persistentes Audit.
5. **Anhänge und lokale Dokumentenanalyse – abgeschlossen:** sicherer Import und Undo-fähige Zuordnung, DOCX-/XLSX-/PDF-Extraktion, transaktionale Persistenz, begrenzte Warteschlange, Bild- und PDF-Vorschau, Integritätsprüfung, Standardprogramm und lokale OCR für Bilder sowie bildbasierte PDF-Seiten sind in 5A bis 5D umgesetzt.
6. **Zeitstrahldarstellung – abgeschlossen:** gemeinsames Layoutmodell, automatische Skala, Lückenkompression, Kollisionsbahnen und Fristprojektion sind in 6A umgesetzt; die virtualisierte horizontale/vertikale WPF-Ansicht samt Zoom, Mausverschiebung, Scrollleisten und Navigation ist in 6B aktiv. Manuelle, persistente und Undo-fähige Kartenpositionen sowie ausgewählte Zeiträume sind in 6C umgesetzt. Kleine verzögert geladene Dokumentvorschaubilder, lokale Caches und projektbezogene Darstellungs-/Exportvorgaben sind in 6D umgesetzt. Datumsneutrale Ereignissortierung und zielgenauer Datei-Drop auf Anwendung, Ereignisse, Liste und Anhangsbereich sind in 6E umgesetzt.
7. **Suche und Filter – abgeschlossen:** getrennte lokale Dokument-FTS, Suche in aktuellen Aggregatfeldern, kombinierbare Filter, Eingabedebounce/Abbruch, Relevanz-/Datumssortierung, hervorgehobene Fundstellen, direkte Navigation und gefilterte Zeitstrahldarstellung sind in 7A umgesetzt.
8. **PDF-Export – abgeschlossen:** tatsächliche PDF-Vorschau, A4/A3/Letter/benutzerdefiniert, Hoch-/Querformat, mehrseitig, große Einzelseite, Zeitraum, Fortsetzungen, Miniaturen und drucktaugliche Kennzeichnungen sind in 8A umgesetzt.
9. **Standalone-HTML-Export – abgeschlossen:** eine einzelne responsive Offlinedatei mit eingebetteten Daten und Miniaturen, horizontaler/vertikaler Darstellung, Zoom, Verschieben, Suche, kombinierten Filtern, Detailkarten, externen Linkwarnungen und Druck-CSS ist in 8B umgesetzt.
10. **Projektarchiv, Sicherung und Wiederherstellung – abgeschlossen:** Manifest, SHA-256, sichere ZIP-Verarbeitung, Projekttransfer, Crash-Recovery, manuelle und rotierende Sicherungen, validierte Wiederherstellung, Audit und Sicherungsverwaltung sind umgesetzt.
11. **Tests und Beispielprojekt – abgeschlossen:** das freie Beispielprojekt, seine PDF-/Bild-/DOCX-/XLSX-Dokumente, integrierte Transfer-/Exportabnahme, der 5.000-Ereignisse-Lasttest, die automatisierte Barrierefreiheits-/Tastaturabnahme des Hauptfensters und die priorisierten negativen Archiv-/Workspace-End-to-End-Szenarien sind in 11A und 11B abgeschlossen.
12. **Installer, portable Veröffentlichung und Dokumentation:** Buildskripte, selbstenthaltendes Publish, ZIP, Inno-Setup-Dateizuordnung, Handbuch, Datenschutz, Release-Audit.

Nach jedem Meilenstein werden relevante Debug-/Release-Builds und Tests ausgeführt, Status/Entscheidungen aktualisiert und ein kleiner Git-Commit erstellt.

## Noch offene Anforderungen

- reproduzierbare Buildskripte, portable ZIP-Datei, Installer mit `.zeitprojekt`-Zuordnung, Benutzerhandbuch, Datenschutz-/Lizenzbündelung und Release-Anleitung fertigstellen

## Manuelle Release-Checkliste (nicht automatisierbar)

Die folgenden Prüfungen können in dieser Entwicklungsumgebung nicht vollständig automatisiert werden und müssen vor einem Release manuell auf geeigneten Windows-10-/Windows-11-Systemen durchgeführt werden:

- [ ] UI-Abnahme bei 100/125/150/200 Prozent Skalierung: Hauptfenster, Dialoge, Zeitstrahl, PDF-/Bildvorschau und Exportvorschau auf Lesbarkeit und Bedienbarkeit prüfen
- [ ] Tastaturbedienung: vollständige Navigation durch Startbildschirm, Hauptfenster, Ereignisdialog, Suche, Sicherungsdialog, PDF-/HTML-Export und Vorschau ohne Maus
- [ ] Kontrast und visuelle DPI-Abnahme: Hell-/Dunkel-Thema, Zeitstrahlpalette, Kartenlesbarkeit und Fokusvisualisierung auf realen Displays prüfen
- [ ] Standalone-HTML-Export in üblichen Windows-Browsern (Edge, Firefox, Chrome) öffnen, Offline-Verhalten, Volltextsuche, Filter, Zoom, Druckvorschau und externe Linkwarnung prüfen
- [ ] Druckvorschau des PDF-Exports in einem Standard-PDF-Betrachter prüfen
- [ ] Reale mehrgigabytegroße `.zeitprojekt`-Archive importieren, speichern und sichern, um Archivlimits und Speicherplatzreserven zu validieren
- [ ] Ausgewählte Dateisystemfehler (z. B. gesperrte Zieldatei, volles Laufwerk, nicht löschbare Sicherung) manuell simulieren und Fehlermeldungen prüfen

## Bekannte Probleme und Risiken

- Nach Ablauf der Undo-Historie ist noch keine Bereinigung nicht mehr referenzierter physischer Anhangsdateien implementiert.
- Deutsche OCR setzt die entsprechende lokale Windows-Sprachressource voraus; fehlt sie, bleibt die übrige Anwendung funktionsfähig und die Analyse zeigt eine Installationsanleitung.
- Ein bereits laufender nativer PDFium-Einzelseitenaufruf kann nicht hart abgebrochen werden; die Anwendung prüft Cancellation davor und unmittelbar danach und begrenzt die Ausgabe strikt.
- Die Setup-EXE wird direkt ins Projekt-Hauptverzeichnis geschrieben, damit Endbenutzer sie sofort finden.
- Installer-Setup wird mit `PrivilegesRequired=admin` erzeugt, damit die Installation unter `C:\Program Files` zuverlässig funktioniert.
- Inno Setup 6 ist verfügbar; der Installer-Build funktioniert ohne Warnungen.
- Die selbstenthaltende Veröffentlichung ist technisch erfolgreich; portable ZIP-Erzeugung und Lizenztext-Bündelung bleiben spätere Release-Gates.

## Nächster konkreter Arbeitsschritt

Neue Version 0.2.1 installieren und visuelle Abnahme durchführen. Anschließend Phase 6 des UI-Redesigns beginnen: Ereigniskarten und Zeitstrahllayout überarbeiten.

## Release-Audit 22.07.2026

- Branch: `master`; geprüfter Ausgangscommit: `2ad58d0`; aktuelle Version: `0.2.1`.
- Beispielprojekt: Generator und Referenzarchiv sind wieder semantisch synchron; 10 Ereignisse. Das versehentlich eingecheckte zusätzliche Ereignis „Test“ wurde durch Regeneration entfernt.
- Verifikation: Debug und Release jeweils 0 Warnungen/0 Fehler, 62 Unit-Tests und 88 Integrationstests bestanden; Formatprüfung bestanden; self-contained win-x64-Publish erfolgreich.
- PDB-Strategie: PDB-Dateien werden vor ZIP-Erzeugung entfernt und nicht vom Installer übernommen.
- Lizenzstatus: drei lokal verfügbare Originaltexte werden unter `licenses/` gebündelt. Vollständige Originaltexte weiterer ausgelieferter Produktionskomponenten fehlen lokal und bleiben ein Release-Gate.
- Offene manuelle Prüfungen: vollständige Checkliste, Installer-Build und portable ZIP aus dem aktuellen Publish.
- Nächster Schritt: lokale Original-Lizenztexte vervollständigen, dann Portable ZIP und Installer erzeugen und manuell abnehmen.


## Releaseartefakte 22.07.2026

- Portable ZIP: `artifacts\\release\\ZeitstrahlStudio-0.2.1-win-x64-portable.zip`, 90.388.403 Bytes, SHA-256 `0DEAB8B522B3C13D60630F781263328BF3791FCB9715353413BB9CDDEED90A95`.
- Installer: `ZeitstrahlStudio-0.2.1-win-x64-setup.exe`, 62.718.761 Bytes, SHA-256 `47AD63677C702EC47D241861D145E63D770D12996CED9B49AD66FAB1208C384E`; Inno Setup 6.7.3 erfolgreich kompiliert.
- Nach dem finalen Installer-Fix wurde der vollständige Verifikationsbefehl gestartet, aber durch das Ausführungszeitlimit abgebrochen; keine neue vollständige Ergebnisangabe daraus ableiten.
- Der verbleibende zwingende Release-Gate ist die Vervollständigung der Original-Lizenztexte für alle ausgelieferten Komponenten.

## UI-Redesign 0.3.0 – Phasen 0 bis 3 (22.07.2026)

### Aktueller Stand

- Phase 0 abgeschlossen: sechs Bildanhänge eindeutig als drei IST- und drei SOLL-Referenzen dokumentiert; `04_SOLL_Hauptansicht_Hell.png` ist die primäre Referenz, Bild 05 und 06 sind analysiert und für spätere Arbeiten zurückgestellt.
- Phase 1 abgeschlossen: globales semantisches Hell-/Dunkel-Designsystem, lesbare Typografieskala und gemeinsame Interaktionszustände eingeführt; lokale Light-Theme-Übersteuerungen entfernt.
- Phase 2 abgeschlossen: Hauptmenü und gruppierte textbasierte Befehlsleiste mit klarer Orientierungsauswahl umgesetzt.
- Phase 3 abgeschlossen: adaptive Drei-Spalten-Hauptansicht mit Projekt-/Filterleiste, zentraler Arbeitsfläche und rechtem Detailinspektor umgesetzt. Beide Seitenbereiche lassen sich ein- und ausblenden; der Inspektor verwendet ausschließlich vorhandene MVVM-Befehle und verändert keine Dialog- oder Timeline-Logik.
- Die Ereignisliste nutzt die zentrale Arbeitsfläche; Anhangsansicht und -aktionen sind ohne Funktionsverlust in den kontextbezogenen Inspektor verlagert.
- Fünf automatisierte Referenzgrößen (1280×720, 1366×768, 1600×900, 1920×1080 und 2560×1440) prüfen sichtbare Seitenbereiche, nutzbare Timeline und fehlenden horizontalen Überlauf.
- Laufzeitprüfung bei 1280×720: Timeline sichtbar, 45 Schaltflächen erkannt, keine Schaltfläche außerhalb des Fensterrechtecks. Screenshot: `artifacts/ui-redesign/phase-3-main-window.png` (ignoriertes Prüfartefakt).

### Verifikation

```powershell
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test tests\ZeitstrahlStudio.IntegrationTests\ZeitstrahlStudio.IntegrationTests.csproj -c Debug --no-restore --no-build --filter "FullyQualifiedName~MainWindowAccessibilityTests|FullyQualifiedName~ThemeResourceTests"
```

Ergebnis: Debug-Build mit 0 Warnungen und 0 Fehlern; 17/17 gezielte MainWindow-/Theme-Integrationstests bestanden. Die fünf neuen Größenfälle bestanden zusätzlich einzeln mit 5/5 Tests.

### Umfangsgrenze und nächster Schritt

Dieser Auftrag ist nach Phase 3 beendet. Phasen 4 bis 8, vollständige Endverifikation, vollständiger Release-Prüfzyklus, Publish, Vorschauinstaller, Versionsänderungen und Releasevorbereitung wurden nicht begonnen. Ein späterer Auftrag beginnt frühestens mit Phase 4 und liest zuvor `UI_REDESIGN_HANDOFF.md`.

## UI-Redesign 0.3.0 – Phase 4, Teilmeilenstein Startansicht (25.07.2026)

- Die Startansicht verwendet nun eine klare Textmarke anstelle eines nicht lizenzierten Unicode-Pseudo-Icons.
- Der Bereich „Wiederherstellung“ erscheint nur noch, wenn tatsächlich Wiederherstellungskandidaten verfügbar sind.
- Verifikation: `dotnet build ZeitstrahlStudio.sln -c Debug --no-restore` erfolgreich (0 Warnungen, 0 Fehler); 19 gezielte Hauptfenster- und Dialogintegrationstests bestanden.
- Noch offen: übrige Phase-4-Dialogabnahme sowie Phasen 5 bis 7 und das verbindliche Screenshot-Review-Gate.

## UI-Redesign 0.3.0 – Phase 4, Teilmeilenstein Ereignisdialog (25.07.2026)

- Der Ereignisdialog verwendet jetzt einen klaren, themefähigen Kopfbereich mit dauerhaftem Bearbeitungskontext; der Tab-Inhalt bleibt scrollbar, die Aktionsleiste ist getrennt und behält Enter/Escape.
- Verifikation: Debug-Build erfolgreich (0 Warnungen, 0 Fehler); 19 gezielte Hauptfenster- und Dialogintegrationstests bestanden.

## UI-Redesign 0.3.0 – Phase 5, Teilmeilenstein Timeline-Werkzeuge (25.07.2026)

- Die Timeline-Werkzeuge sind in umbruchfähige Gruppen für Ansicht, Zoom, Orientierung und Layout gegliedert. Alle bestehenden Befehle bleiben erhalten; die Zoom-Pseudo-Icons wurden durch verständliche Textbefehle ersetzt.
- Neuer automatisierter Vertrag: bei 1280×720 bleiben alle Werkzeugbuttons innerhalb der Werkzeugfläche; Unicode-Zoominhalte sind ausgeschlossen.
- Verifikation: Debug-Build erfolgreich (0 Warnungen, 0 Fehler); 18 gezielte Hauptfenster- und Timeline-Integrationstests bestanden.

## UI-Redesign 0.3.0 – Phase 5/6, Teilmeilenstein Karten- und Konfliktzustände (25.07.2026)

- Ereigniskarten zeigen neben der Priorität nun immer den textualen Bearbeitungsstatus. Frist, Anhang, manueller Zustand und ein möglicher Layoutkonflikt werden ebenfalls textlich ausgewiesen.
- Manuelle Kartenüberschneidungen werden layoutseitig erkannt und im Renderer durch verstärkten Warnrahmen sowie den Text „Konflikt“ sichtbar gemacht; fachliche Daten bleiben unverändert.
- Verifikation: Debug-Build 0 Warnungen/0 Fehler; 17 Layoutengine-Unit-Tests und 5 Timeline-Renderer-Integrationstests bestanden.

## UI-Redesign 0.3.0 – Phase 6, Teilmeilenstein Achsenbeschriftungen (25.07.2026)

- Tickbeschriftungen respektieren jetzt einen Mindestabstand und werden nahe einer Lückenbeschriftung unterdrückt; alle Achsenmarken bleiben sichtbar.
- Verifikation: Debug-Build erfolgreich (0 Warnungen, 0 Fehler); Timeline-Renderer-Tests 5/5 bestanden.

## UI-Redesign 0.3.0 – Phase 4, Teilmeilenstein Dokumentanalyse-Dialog (25.07.2026)

- Der Dokumentanalyse-Dialog folgt nun dem semantischen Kopf-, Inhalts- und Aktionsleistenvertrag; alle bestehenden Bindings und Tastaturaktionen bleiben erhalten.
- Verifikation: Debug-Build erfolgreich (0 Warnungen, 0 Fehler); 7 gezielte Analyse-, Export- und Sicherungsintegrationstests bestanden.

## UI-Redesign 0.3.0 – Phase 4, Teilmeilenstein Bildvorschau und Änderungsprotokoll (25.07.2026)

- Bildvorschau und Änderungsprotokoll folgen jetzt dem vorhandenen themefähigen Dialogvertrag aus Kopfbereich, abgegrenztem scrollbaren Inhalt und fester Aktionsleiste.
- Die Bildvorschau behält Dekodierung, Zoomdarstellung und Scrollen; das Änderungsprotokoll behält Datenquelle, Spalten, Sortierung und Schließen, ergänzt einen beschreibenden Leerzustand und lesbare Langtextbehandlung.
- Neue STA-basierte Dialogtests prüfen beide Fenster mit temporären Daten auf Titel, erreichbare primäre Controls, AutomationProperties, Leerzustand, Mindestlayout und Standard-/Abbrechenaktion. Eine separate externe UI-Automation-Bibliothek ist nicht vorhanden.
- Verifikation: `dotnet build ZeitstrahlStudio.sln -c Debug --no-restore` erfolgreich (0 Warnungen, 0 Fehler); gezielte Dialog-, Audit- und Themevertragstests 9/9 bestanden.
- Keine neuen Iconanforderungen. Nächster Schritt nach erneuter Freigabe: verbleibende Phase-4-Dialogmarkups für PDF-Vorschau, Export und Sicherungsverwaltung gegen denselben Vertrag prüfen.

## UI-Redesign 0.3.0 – Phase 4, Teilmeilenstein PDF-Vorschau und Exportdialoge (25.07.2026)

- PDF-Vorschau, PDF-Export und Standalone-HTML-Export folgen nun dem gemeinsamen themefähigen Kopf-, Inhalts- und Aktionsleistenvertrag. Seitennavigation und Zoom sind durch Klartext statt Unicode-Pseudo-Icons zugänglich; alle vorhandenen Commands und Exportabläufe bleiben erhalten.
- Die PDF-Optionen und HTML-Optionen bleiben bei geringer Fensterhöhe scrollbar. Die HTML-Optionen erklären eingebettete Ressourcen und den anschließenden nativen Zielpfaddialog; dieser vorhandene Pfad bleibt unverändert.
- Verifikation: `dotnet build ZeitstrahlStudio.sln -c Debug --no-restore` erfolgreich (0 Warnungen, 0 Fehler). Gezielte PDF-Vorschau-, PDF-/HTML-Export-, Dialogautomations- und Themevertragstests: 20/20 bestanden.
- Keine neuen Iconanforderungen. Noch offen in Phase 4: nur die Sicherungsverwaltung; danach sind die ausstehenden Phase-7-DPI-/Accessibility- und Review-Gates separat zu behandeln.

## UI-Redesign – Phase 4 abgeschlossen (25.07.2026)

- Sicherungsverwaltung und Wiederherstellungskarten gegen den gemeinsamen Dialogvertrag vereinheitlicht; keine Sicherungs- oder Persistenzlogik geändert.
- Debug-Build: erfolgreich, 0 Warnungen/0 Fehler. Gezielte Sicherungs-, Wiederherstellungs-, Theme- und Accessibility-Integrationstests: 31/31 erfolgreich.
- Nächster Schritt erst nach Freigabe: kleinsten noch offenen Layout-/Viewport-Schritt aus Phase 5/6 neu bestimmen.

## UI-Korrekturmeilenstein Theme, Start-Einstellungen und Ereignisfarbe (27.07.2026)

- Der zunächst eingeführte vollständige ComboBox-Template-Ersatz wurde nach der visuellen Abnahme in der unten dokumentierten Nachkorrektur vollständig zurückgenommen; Dropdowns verwenden wieder ihr Standardverhalten.
- DatePicker-/Kalender-, Kontextmenü-, Tabellenkopf/-zellen- und Registerflächen erhielten ergänzende globale Darkmode-Stile; weiße WPF-Systemflächen werden dadurch nicht mehr in die dunkle Oberfläche übernommen.
- Das globale Farbschema wird versioniert und atomar in `%LocalAppData%\Zeitstrahl Studio\appearance-settings.json` gespeichert und vor dem Hauptfenster geladen.
- Projekt öffnen, Projekt erstellen und Projekt schließen wenden kein projektspezifisches Theme mehr auf die laufende Oberfläche an. Die globale Auswahl bleibt aktiv.
- Der Einstellungsbefehl ist ohne geöffnetes Projekt verfügbar; der bereits vorhandene Button in der oberen Befehlsleiste und das Ansichtsmenü bieten den direkten Zugang.
- Der Ereigniseditor zeigt zwölf beschriftete, tastaturbedienbare Farbfelder und eine Live-Vorschau. Ein beliebiger validierter `#RRGGBB`-Wert kann weiterhin eingegeben werden.
- Neue Regressionstests prüfen atomare Persistenz und Wiederherstellung, toleranten Start bei beschädigter Einstellungsdatei, Theme-Stabilität beim Projektwechsel, Startansicht-Zugang, tatsächliche dunkle Popupfarbe, Palettenzugänglichkeit und freie Hexwerte.
- Ein bereits vorhandener Timeline-Konflikttest wurde deterministisch an die tatsächlich sortierte zweite Karte gebunden; die vorherige zufällige GUID-Zuordnung konnte den beabsichtigten Überlappungsfall verfehlen.

### Erfolgreiche Abschlussverifikation

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
```

Ergebnis: Debug und Release jeweils 0 Warnungen/0 Fehler; jeweils 63/63 Unit-Tests und 113/113 Integrationstests bestanden; Formatprüfung und selbstenthaltender Publish erfolgreich.

## Nächster konkreter Arbeitsschritt (27.07.2026)

Manuelle visuelle Nachprüfung des korrigierten oberen Einstellungszugangs, der WPF-Standard-Dropdowns, aller Darkmode-Auswahlzustände, des Theme-Wechsels und der vollständig farbigen Ereigniskartenrahmen; anschließend den nächsten noch offenen UI-Redesign-Schritt neu bestimmen.

## UI-Nachkorrektur nach visueller Abnahme (27.07.2026)

### Umgesetzte Änderungen

- Den ausschließlich im vorherigen Korrekturcommit ergänzten Einstellungsbutton innerhalb der Startseiten-Aktionsgruppe entfernt.
- Den bereits vorhandenen Einstellungsbutton rechts in der oberen Befehlsleiste beibehalten; `SettingsCommand` bleibt ohne geöffnetes Projekt ausführbar und öffnet dort die globalen Anwendungseinstellungen.
- Das im vorherigen Korrekturcommit eingeführte vollständige `ComboBox`- und `ComboBoxItem`-Template vollständig auf den vorherigen Standardstil zurückgeführt. Damit funktionieren `DisplayMemberPath`, `SelectedValuePath`, Tastatursteuerung und bestehende Dropdown-Bindings wieder unverändert.
- Helle native WPF-Auswahlflächen im Dunkelmodus über Theme-Ressourcen für Window, Control, Highlight, inaktive Auswahl, Menu, Rahmen und Infobereiche korrigiert. Dieselben Ressourcenschlüssel sind mit passenden Farben auch im hellen Theme vorhanden.
- Fehler im Projekt-Einstellungsablauf behoben: Das Ergebnis wird gegen `settingsForDialog` statt gegen den davon abweichenden gespeicherten Projektzustand verglichen. Dadurch wird die Auswahl „Hell“ nicht mehr verworfen, wenn das Projekt bereits Hell gespeichert hatte, global aber Dunkel aktiv ist.
- Timeline-Renderer geändert: Der vollständige innere Kartenrahmen verwendet immer `TimelineEvent.ColorHex`. Auswahl beziehungsweise Datei-Drop erhalten weiterhin eine separate blaue äußere gestrichelte Markierung; ein Layoutkonflikt erhält eine separate rote äußere gestrichelte Markierung.
- Benutzerhandbuch, Änderungsprotokoll und ADR-038 an den korrigierten Bedien- und Themevertrag angepasst.

### Automatisierte Regressionstests

- Hauptfenstertest prüft, dass vor dem Öffnen eines Projekts genau der obere Einstellungsbutton sichtbar und aktiv ist und kein zusätzlicher Startseiten-Button existiert.
- ViewModel-Test reproduziert den zuvor fehlerhaften Wechsel von global Dunkel zu im Projekt bereits gespeichertem Hell und prüft Anwendung, Projektzustand und Statusmeldung.
- Theme-Test verhindert ein erneutes globales eigenes ComboBox-Template und prüft die dunklen nativen Window-/Control-/Highlight-/Menu-Flächen.
- Laufzeittest prüft am echten Standard-ComboBox-Visual-Tree die sichtbare Beschriftung „Dunkel“ sowie eine nicht weiße Popupfläche.
- Renderpixel-Test prüft bei einer ausgewählten Ereigniskarte die benutzerdefinierte Ereignisfarbe am oberen Kartenrahmen und damit außerhalb des linken Farbbalkens.

### Erfolgreiche Abschlussverifikation

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
.\build.ps1 -Task BuildInstaller -Version 0.3.0
```

Ergebnis: Debug und Release jeweils 0 Warnungen und 0 Fehler; jeweils 63/63 Unit-Tests und 116/116 Integrationstests bestanden. Formatprüfung und selbstenthaltender `win-x64`-Publish erfolgreich. Inno Setup 6.7.3 erzeugte den Installer `ZeitstrahlStudio-0.3.0-win-x64-setup.exe` mit 62.730.065 Bytes und SHA-256 `2A9D607B6A8FB50A5973A8BDD06D063888B37A6A1F18FCAD5E8A42806ECD1C95`.

## UI-Nachkorrektur Auswahlfelder nach zweiter visueller Abnahme (27.07.2026)

### Ursache und enger Änderungsumfang

- Der neue Screenshot belegt, dass die vorherige Rückkehr zum nativen WPF-Standardtemplate nicht genügte: geschlossene `ComboBox`-Felder wurden im Dunkelmodus weiterhin mit einer weißen Windows-Systemfläche gezeichnet, während die ausgewählten Labels dadurch praktisch unsichtbar waren.
- Die zuvor ergänzten System-Brush-Schlüssel bleiben erhalten, sind für die geschlossene native ComboBox-Fläche aber nicht zuverlässig wirksam.
- Ausschließlich die gemeinsame visuelle `ComboBox`-/`ComboBoxItem`-Hülle wurde korrigiert. Datenquellen, Auswahlwerte, Befehle, Filterlogik, Dialogabläufe und andere UI-Bereiche wurden nicht verändert.
- Die vorhandenen Benutzeränderungen in `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` wurden nicht berührt und bleiben außerhalb dieses Meilensteins.

### Umsetzung

- Geschlossene Auswahlfelder zeichnen `InputBackgroundBrush`, Theme-Rahmen, Fokuszustand und Pfeil nun selbst; geöffnete Dropdowns verwenden `ElevatedSurfaceBrush` und themefähige Hover-/Auswahlzustände. Im Dunkelmodus existiert damit weder im geschlossenen Feld noch in der Popupfläche eine weiße Systemfläche.
- Die ausgewählte Beschriftung wird direkt über `SelectionBoxItem`, `SelectionBoxItemTemplate`, `ItemTemplateSelector` und `SelectionBoxItemStringFormat` dargestellt. Popup-Einträge verwenden `ContentSource="Content"`. Dadurch bleiben `DisplayMemberPath`-Labels wie „Dunkel“, „Horizontal“ oder „Priorität: alle“ sichtbar und es erscheinen keine internen `SelectionOption`-/`SearchChoice`-Typtexte.
- `ItemsSource`, `SelectedItem`, `SelectedValue`, `SelectedValuePath`, `DisplayMemberPath`, Öffnen/Schließen und Tastaturbedienung bleiben beim WPF-`ComboBox`-Steuerelement; es wurde keine fachliche Auswahl- oder Filterlogik ersetzt.
- Die beiden Projekteinstellungsfelder erhielten ausschließlich stabile Namen und Automation-Namen für gezielte Laufzeittests; Position und Bedienablauf des Dialogs blieben unverändert.
- ADR-038 und `CHANGELOG.md` wurden an den tatsächlich abgenommenen ComboBox-Vertrag angepasst.
- Der normale Patch-Helfer scheiterte vor Dateizugriff am bekannten Windows-Sandbox-Startfehler. Die Änderungen wurden deshalb als exakt gezählte 1:1-Blockersetzungen ausgeführt und anschließend mit `git diff --check`, Diff-Review und Build kontrolliert.

### Neue und erweiterte Regressionstests

- Pixelbasierter Darkmode-Test prüft globale Einstellungen sowie beide Projekteinstellungsfelder auf überwiegend dunkle Renderfläche, sichtbare helle Schriftpixel und die Labels „Dunkel“ beziehungsweise „Horizontal“.
- Popup-Laufzeittest prüft dunkle Hintergrundfläche und die tatsächlich erzeugten sichtbaren Einträge „Windows-Einstellung übernehmen“, „Hell“ und „Dunkel“.
- Hauptfenster-Laufzeittest prüft die drei im Screenshot sichtbaren Filter „Priorität: alle“, „Schlagwort: alle“ und „Anhänge: alle“ auf dunkle Fläche und helle Beschriftung.
- Derselbe Hauptfenstertest wählt „Niedrig“ und „Mit Anhang“ aus und prüft sichtbaren Text sowie Rückfluss in `SearchPriority` und `SearchHasAttachment`.
- Der statische Themevertrag fordert die Template-Bindungen, `ContentSource="Content"`, das echte `PART_Popup` und weiterhin das Fehlen fest codierter weißer Flächen.

### Erfolgreiche Abschlussverifikation

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
.\build.ps1 -Task BuildInstaller -Version 0.3.0
```

Ergebnis: Debug und Release jeweils 0 Warnungen und 0 Fehler; jeweils 63/63 Unit-Tests und 117/117 Integrationstests bestanden. Formatprüfung und selbstenthaltender `win-x64`-Publish erfolgreich. Inno Setup 6.7.3 erzeugte den aktualisierten Installer `ZeitstrahlStudio-0.3.0-win-x64-setup.exe` mit 62.720.035 Bytes und SHA-256 `9F0369589596AB9B4CF9EF870B939BE054FA845948E40E75E3A787C0A7505677`.

## Nächster konkreter Arbeitsschritt (27.07.2026, Auswahlfeldkorrektur)

Aktualisierten Installer installieren und die geschlossenen sowie geöffneten Auswahlfelder in globalen Einstellungen, Projekteinstellungen, Hauptfiltern und Ereignisdialog visuell prüfen. Andere UI- oder Funktionsbereiche bleiben bis zu einer neuen ausdrücklichen Beauftragung unverändert.

## UI-Nachkorrektur Registerkarten, Fälligkeitsdatum und Standardfarbe (27.07.2026)

### Ursache und enger Änderungsumfang

- Die erneute visuelle Abnahme zeigte, dass ausgewählte native WPF-`TabItem`-Header trotz gesetzter Theme-Brushes weiterhin eine fast weiße Systemfläche zeichneten. Betroffen waren „Zeitstrahl/Ereignisliste“, „Inhalt und Datum/Frist, Einordnung und Links“ sowie „Allgemein/Anhänge/Notizen“ im Detailinspektor.
- Der native `DatePicker` zeichnete internes Textfeld und Kalenderbutton abweichend zu den übrigen Eingabefeldern; der intern erzeugte Kalender übernahm ohne explizite Styleübergabe sogar einen nativen Verlaufspinsel.
- Ausschließlich die gemeinsamen Präsentationstemplates für `TabItem`, `DatePicker` und `DatePickerTextBox`, die stabile Testadressierung des Fälligkeitsfelds sowie der vorhandene Standardfarbenbereich der Projekteinstellungen wurden geändert. Registerinhalte, Ereignis-/Datumslogik, Filter, Befehle und andere UI-Bereiche blieben unverändert.
- Die vorhandenen Benutzeränderungen in `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` wurden erneut nicht berührt.

### Umsetzung

- `TabItem` rendert Headerfläche, Rahmen, Hover, Auswahl und Deaktivierung nun ausschließlich über semantische Theme-Ressourcen. Ausgewählte Register verwenden im Dunkelmodus `NavigationSelectedBackgroundBrush` (`#1E3A8A`) und `NavigationSelectedForegroundBrush` (`#BFDBFE`) statt einer weißen nativen Fläche.
- `DatePicker` verwendet dieselbe Eingabehintergrund-, Rahmen-, Fokus-, Fehler- und Deaktiviertdarstellung wie Text- und Auswahlfelder. `PART_TextBox`, `PART_Button`, `PART_Popup`, `SelectedDate` und `IsDropDownOpen` bleiben erhalten; das Kalendericon ist ein lokaler Vektorpfad und kein neues Asset oder Unicode-Ersatzsymbol.
- `DatePickerTextBox` rendert Texteingabe beziehungsweise „Datum auswählen“ ohne native weiße Innenfläche. Der intern erzeugte Kalender erhält `ThemedCalendarStyle` ausdrücklich über `CalendarStyle`, damit auch die aufgeklappte Fläche dunkel bleibt.
- Die Projekteinstellungen verwenden jetzt dieselben zwölf `EventColorPalette.Options`, dieselbe Live-Vorschau und dieselbe freie `#RRGGBB`-Eingabe wie der Ereigniseditor. `SelectedPaletteColorHex` synchronisiert die Palette mit `DefaultEventColorHex`; freie gültige Hexwerte bleiben möglich und werden unverändert gespeichert.
- ADR-038 und `CHANGELOG.md` wurden an diesen Theme- und Palettenvertrag angepasst.

### Bearbeitungs- und Fehlerprotokoll

- Der normale Patch-Helfer scheiterte erneut vor Dateizugriff am Windows-Sandbox-Startfehler. Alle Ersetzungen wurden deshalb über eindeutig gezählte Blöcke beziehungsweise Start-/Endmarken vorgenommen und anschließend mit Diff-Prüfung und Build kontrolliert.
- Eine erste Bereichsersetzung traf wegen verschobener Indizes Teile des CheckBox-/ViewModel-Umfelds; der unmittelbar folgende Debug-Build schlug dadurch fehl. Die betroffenen eng begrenzten Bereiche wurden vollständig aus dem unveränderten Vertrag rekonstruiert. Derselbe Debug-Build bestand danach mit 0 Warnungen und 0 Fehlern.
- Der erste DatePicker-Laufzeittest erwartete ein dauerhaft geöffnetes Popup in einem nur angeordneten, nicht tatsächlich angezeigten Testfenster. WPF schließt dieses Popup in dieser Umgebung sofort. Der Test prüft daher den real erzeugten `Calendar` und seine Themefläche; die `IsDropDownOpen`-Bindung wird zusätzlich statisch vertraglich geprüft.
- Der dabei sichtbare native Kalender-Verlauf wurde nicht ignoriert: `ThemedCalendarStyle` wird nun explizit an den DatePicker übergeben; danach bestanden alle vier neuen gezielten Verträge.

### Neue Regressionstests

- Hauptfenster-Laufzeittest schaltet „Zeitstrahl“ und „Ereignisliste“ sowie „Allgemein“, „Anhänge“ und „Notizen“ durch und prüft bei jedem ausgewählten Header dunkle Fläche und helle Schrift.
- Dialog-Laufzeittest schaltet beide Ereigniseditor-Register durch und prüft denselben Themevertrag.
- DatePicker-Laufzeittest prüft deaktivierte und aktive Hintergrundfläche, nicht weißen Kalenderbutton, gesetzten Datumswert, sichtbaren Datumstext und den dunkel gestylten internen Kalender.
- Paletten-Laufzeit- und ViewModel-Tests prüfen dieselbe Optionsinstanz wie im Ereigniseditor, zwölf Farbfelder, Auswahlrückfluss, Live-Vorschau und weiterhin gültige freie Hexwerte.
- Statischer Themevertrag prüft die erforderlichen TabItem-/DatePicker-Parts, Popupbindung, expliziten CalendarStyle und das Fehlen fest codierter weißer Flächen.

### Erfolgreiche Abschlussverifikation

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
.\build.ps1 -Task BuildInstaller -Version 0.3.0
```

Ergebnis: Debug und Release jeweils 0 Warnungen und 0 Fehler; jeweils 63/63 Unit-Tests und 121/121 Integrationstests bestanden. Formatprüfung und selbstenthaltender `win-x64`-Publish erfolgreich. Inno Setup 6.7.3 erzeugte den aktualisierten Installer `ZeitstrahlStudio-0.3.0-win-x64-setup.exe` mit 62.737.699 Bytes und SHA-256 `E755C1F968CDCC919CD99D546B1693E9E920E154D147210304B8875DA8A27389`.

## Nächster konkreter Arbeitsschritt (27.07.2026, Register-/DatePicker-Korrektur)

Aktualisierten Installer installieren und die ausgewählten Registerkarten in Hauptansicht, Ereigniseditor und Detailinspektor, das deaktivierte/aktive Fälligkeitsdatum einschließlich Kalender sowie die Standardfarbpalette visuell prüfen. Andere UI- oder Funktionsbereiche bleiben bis zu einer neuen ausdrücklichen Beauftragung unverändert.

## UI-Nachkorrektur Hauptmenü-Markierung im Darkmode (27.07.2026)

### Ursache und enger Änderungsumfang

- Der neue Screenshot zeigte im geöffneten oberen Menü „Ansicht“ eine helle native WPF-Hervorhebungsfläche. Die helle Darkmode-Schrift des Eintrags war darauf nicht lesbar.
- Ausschließlich die Darstellung der `MenuItem`-Instanzen im vorhandenen `MainMenu` wurde geändert. Menübefehle, Eingabegesten, Untermenüs, Kontextmenüs, ComboBoxen, Dialoge und fachliche Abläufe blieben unverändert.
- Die vorhandenen Benutzeränderungen in `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` wurden nicht bearbeitet und bleiben außerhalb dieses Meilensteins.

### Umsetzung und Tests

- Das lokale `MainMenu.Resources`-Template zeichnet Grundfläche und Popup mit `MenuBackgroundBrush`.
- Hervorgehobene und geöffnete Einträge verwenden `NavigationSelectedBackgroundBrush` und `NavigationSelectedForegroundBrush`; angehakte Einträge verwenden die korrespondierenden Auswahlressourcen. Im dunklen Theme entspricht dies einer Fläche `#1E3A8A` mit Schrift `#BFDBFE`.
- Header, Zugriffstasten, Befehlskürzel, Untermenüpfeil, Häkchen, Rollen und Tastaturnavigation bleiben Bestandteil des WPF-`MenuItem`-Vertrags.
- Ein Laufzeittest prüft am realen Hauptmenü die dunkle aktive Eintragsfläche, lesbare helle Schrift und die Popupfläche `#111827`. Ein zusätzlicher XAML-Vertragstest fordert den lokal begrenzten `IsHighlighted`-Zustand und verhindert eine versehentliche Rückkehr zur nativen hellen Markierung.
- Alle 27 Hauptfenster-/Theme-Tests bestanden. Der vollständige Abschlusslauf bestand mit 63/63 Unit- und 123/123 Integrationstests in Debug und Release.
- Der normale Patch-Helfer scheiterte bei allen Änderungsversuchen vor Dateizugriff am Windows-Sandbox-Startfehler. Daher wurden ausschließlich exakt gezählte Einmal-Ersetzungen verwendet und anschließend mit `git diff --check`, Diff-Review, Build und Tests kontrolliert.
- Der erste Lauf des neuen Laufzeittests erwartete einen geöffnet bleibenden Popupzustand in einem nur angeordneten, nicht eingeblendeten WPF-Testfenster. WPF schloss diesen Zustand sofort. Die instabile Erwartung wurde entfernt; der aktive Eintrag wird zur Laufzeit geprüft, während der konkrete Hover-Trigger zusätzlich statisch abgesichert ist.

### Erfolgreiche Abschlussverifikation

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
.\build.ps1 -Task BuildInstaller -Version 0.3.0
```

Ergebnis: Debug und Release jeweils 0 Warnungen und 0 Fehler; jeweils 63/63 Unit-Tests und 123/123 Integrationstests bestanden. Formatprüfung und selbstenthaltender `win-x64`-Publish erfolgreich. Inno Setup 6.7.3 erzeugte den aktualisierten Installer `ZeitstrahlStudio-0.3.0-win-x64-setup.exe` mit 62.739.836 Bytes und SHA-256 `ABECB136D2447BAF350E2F0FE359C704C37B4D3F0FD69628CDF48B63A3EF30F5`.

## Nächster konkreter Arbeitsschritt (27.07.2026, Hauptmenü-Markierung)

Aktualisierten Installer installieren, im Dunkelmodus das Menü „Ansicht“ öffnen und die Lesbarkeit beim Überfahren aller Einträge mit Maus sowie Tastatur visuell prüfen. Andere UI- oder Funktionsbereiche bleiben bis zu einer neuen ausdrücklichen Beauftragung unverändert.

## UI-Nachkorrektur Checkboxen, native Fenstertitelleisten und HTML-Export (13.08.2026)

### Enger Änderungsumfang

- Ausschließlich der gemeinsame Checkbox-Stil, die Synchronisierung nativer WPF-Titelleisten und die Darstellung beziehungsweise Bedienung des Standalone-HTML-Exports wurden geändert.
- Projektformat, Datenbank, Ereignislogik, Suche, PDF-Export und vorhandene Dropdown-/Register-/DatePicker-Verträge bleiben unverändert.
- Die benutzereigenen Änderungen in `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` wurden nicht bearbeitet und bleiben ungestaged sowie uncommitted.
- Es wurde keine neue Produktionsabhängigkeit eingeführt.

### Kontrollkästchen im Dunkelmodus

- Das globale WPF-`CheckBox`-Template zeichnet eine dauerhaft dunkle Grundfläche, einen klaren Akzentzustand für geprüft beziehungsweise unbestimmt und kontrastierte Haken-/Teilglyphen.
- Hover ändert ausschließlich den Rahmen; Tastaturfokus ändert Rahmen und Fokusdicke. Keine dieser Interaktionen ersetzt die Fläche durch einen hellen nativen Zustand.
- Ungeprüft, geprüft, unbestimmt und deaktiviert werden ohne Mauszeiger geprüft. `IsChecked`, `IsThreeState`, Two-Way-Binding, Tabstopp, Leertaste und UI-Automation bleiben erhalten.

### Native Fenstertitelleisten

- `ApplicationThemeService` synchronisiert nach jedem effektiven Themewechsel die native Titelleiste aller bereits erzeugten anwendungseigenen WPF-Fenster.
- Ein globaler `Window.Loaded`-Handler übernimmt denselben Zustand für später geöffnete Dialoge.
- Die DWM-Anbindung verwendet das aktuelle Immersive-Dark-Mode-Attribut mit Legacy-Fallback. Windows 11 erhält zusätzlich Caption- und Textfarben aus den Theme-Ressourcen; Hell setzt die Windows-Standardfarben zurück.
- Fehlende oder nicht unterstützte DWM-Funktionen werden als reine Darstellungserweiterung behandelt und dürfen kein Fenster verhindern.

### Vollständige Neugestaltung des Standalone-HTML-Exports

- Responsive Vollfenster-Shell mit Offline-/Momentaufnahmehinweis, Projektkopf, aufklappbarer Projektbeschreibung und Kennzahlen für Ereigniszahl, Zeitraum und Exportzeitpunkt.
- Gruppierte Werkzeugleiste für horizontale/vertikale Ausrichtung, Zoom, Zurücksetzen, Detailsteuerung, Hell/Dunkel und Drucken. Auf Mobilbreite brechen die Gruppen vollständig sichtbar um.
- Kompakte Volltextsuche und ein aufklappbares, am Fenster begrenztes Filterpanel für Zeitraum, Farbe, Schlagwort und Friststatus mit Aktivzähler und Rücksetzen.
- Neu gezeichneter horizontaler beziehungsweise vertikaler Zeitstrahl mit alternierenden Karten, Zeitlückenmarkern, vollständigem farbigem Ereignisrahmen, neutraler Kontrastkontur, Badges, Miniaturen und Detailbereichen.
- Maus-/Pointer-Ziehen, Schaltflächen- und `Strg + Mausrad`-Zoom, `/` für die Suche, `Esc` für das Filterpanel, ARIA-Zustände, sichtbarer Fokus und reduzierte Bewegungen. Passende geöffnete Details bleiben bei Ansichts- und Filterwechseln erhalten.
- Lokaler Hell-/Dunkel-Umschalter mit Systempräferenz als Startwert und ausschließlich `zeitstrahl-studio-export-theme` im Browserspeicher.
- Reversibler Druckmodus: vertikal, 100 %, papierkontrastreiche Variablen sowie geöffnete Projektbeschreibung und Ereignisdetails; danach Wiederherstellung von Ausrichtung, Zoom, Scrollposition, Filterpanel, Projektbeschreibung und offenen Details.
- Verschärfte CSP und weiterhin keine externen Ressourcen, keine Netzwerk-APIs und keine unsichere HTML-Injektion; Projektdaten werden nur als Textknoten beziehungsweise validierte DOM-Attribute eingesetzt.

### Regressionstests und Browser-QA

- `CheckBoxThemeTests` prüfen statisch und zur Laufzeit sämtliche Checkboxzustände, fehlende helle Hover-/Fokusfläche, sichtbare kontrastierte Glyphen, Two-Way-Binding und UI-Automation.
- `NativeWindowTitleBarThemeTests` prüfen Attributfolge, Legacy-Fallback, Windows-11-Farben, Light-Reset, COLORREF-Konvertierung und gemerkten Themezustand.
- Drei `StandaloneHtmlExportServiceTests` prüfen Einzeldatei, Offline-/CSP-/DOM-Sicherheitsvertrag, neue Shell-/Filter-/Theme-/Druckelemente, vollständigen Ereignisfarbrahmen sowie vorhandene Exportoptionen und atomare Dateibehandlung.
- Kombinierter gezielter Lauf für Checkboxen, Titelleisten, Anwendungstheme, Theme-Ressourcen und HTML-Export: 21/21 Tests bestanden.
- Der integrierte Browserdienst war in dieser Sitzung nicht verfügbar. Die produktiv erzeugte QA-Datei wurde daher mit lokalem Chromium über Playwright 1.60.0 geprüft: 1440×900, 1024×600 und 390×844; Suche, Filter, Ausrichtung, Zoom, Detailzustand, Hell/Dunkel, Kartenkontur, Druckzustand und Zustandswiederherstellung bestanden. Es traten keine JavaScript-Fehler und keine HTTP(S)-Anfragen auf.
- Sichtgeprüfte Artefakte: `artifacts/html-export-desktop-dark.png`, `artifacts/html-export-desktop-light.png`, `artifacts/html-export-1024x600.png`, `artifacts/html-export-mobile-390.png` und `artifacts/html-export-print-preview.png`.

### Dokumentation und Bearbeitungsprotokoll

- ADR-031 und ADR-038, `CHANGELOG.md`, `USER_GUIDE.md`, `PRIVACY.md` und `MANUAL_RELEASE_CHECKLIST.md` wurden an den umgesetzten Vertrag angepasst.
- Der normale Patch-Helfer scheiterte wiederholt vor jedem Dateizugriff am bekannten Windows-Sandbox-Timeout. Danach wurden ausschließlich exakt gezählte Einmal-Ersetzungen beziehungsweise eindeutig begrenzte Abschnitte verwendet und durch Diff, Build und Tests kontrolliert.

### Erfolgreiche Abschlussverifikation

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
.\build.ps1 -Task BuildInstaller -Version 0.3.0
git diff --check
```

Ergebnis:

- Restore erfolgreich; alle Projekte waren aktuell.
- Debug- und Release-Build jeweils mit 0 Warnungen und 0 Fehlern.
- Debug und Release: jeweils 63/63 Unit-Tests und 130/130 Integrationstests bestanden.
- Gezielter Theme-/Checkbox-/Titelleisten-/HTML-Lauf: 21/21 Tests bestanden; die drei HTML-Exporttests bestanden nach der finalen Responsive-Korrektur erneut separat.
- `dotnet format --verify-no-changes` und `git diff --check` erfolgreich.
- Selbstenthaltender `win-x64`-Publish erfolgreich.
- Inno Setup 6.7.3 erzeugte `ZeitstrahlStudio-0.3.0-win-x64-setup.exe` mit 62.746.009 Bytes und SHA-256 `5A8F01AAA1E91D5AF2052EBF6C7A4D86E78CFC33AB7834ABA6DE30F42AE287E8`.
- Portable, selbstenthaltende Anwendung: `artifacts/publish/win-x64/ZeitstrahlStudio.App.exe`, 152.576 Bytes, SHA-256 `C6E822086732D6CA3B4AF486A7EC5D0890883FB8B11A8783EA3FC3FA3D214302`.

### Offenes manuelles Release-Gate

- Die Headless-Browser-QA des HTML-Exports ist abgeschlossen; die herstellerspezifische Sichtprüfung in Edge, Firefox und Chrome bleibt zusätzlich in `MANUAL_RELEASE_CHECKLIST.md` offen.
- Kontrollkästchenzustände und native Titelleisten müssen mit der neuen EXE auf realem Windows 10 und Windows 11 bei Start sowie laufendem Dunkel → Hell → Dunkel geprüft werden. Dies ist keine offene Implementierungsaufgabe, sondern die ausdrücklich dokumentierte visuelle Releaseabnahme.

## Nächster konkreter Arbeitsschritt (13.08.2026)

Den aktualisierten Installer beziehungsweise die portable EXE auf Windows 10 und Windows 11 starten und gemäß `MANUAL_RELEASE_CHECKLIST.md` die Checkboxzustände ohne Hover, Hover/Fokus ohne helle Fläche sowie Hauptfenster-, bereits geöffnete und später geöffnete Dialogtitelleisten bei Dunkel → Hell → Dunkel visuell abnehmen.

## HTML-Nachkorrektur: vertikale Zeitachse mittig ausgerichtet (13.08.2026)

### Umsetzung

- Ausschließlich die Positionierung des vertikalen Standalone-HTML-Zeitstrahls wurde angepasst.
- Bei Desktopbreiten ab 761 Pixeln wird die skalierte Breite des vertikalen Zeitstrahls gegen die tatsächliche Breite des sichtbaren Arbeitsbereichs gerechnet. Solange der Zeitstrahl hineinpasst, erhält er links und rechts denselben freien Raum; die Achse liegt damit exakt in der Mitte.
- Die horizontale Ansicht bleibt bei `left: 0`. Die einspaltige Mobilansicht bis 760 Pixel behält bewusst ihre links geführte Achse, damit die Kartenbreite vollständig nutzbar bleibt.
- Zoom, Filter, Detailzustände, Druckmodus und Drag-Navigation bleiben unverändert. Ein zusätzlicher HTML-Vertragstest verhindert den Verlust der Zentrierungsberechnung.
- Die benutzereigenen Änderungen in `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` wurden nicht bearbeitet und bleiben außerhalb des Commits.

### Visuelle Browserprüfung

- Die produktiv erzeugte Datei `artifacts/html-export-centered-qa.html` wurde lokal in Chromium geprüft.
- Gemessene Abweichung zwischen Achsenmitte und Mitte des sichtbaren Arbeitsbereichs: bei 2048, 1440 und 1024 Pixel Fensterbreite jeweils `0,00 px`.
- Die horizontale Ansicht blieb unverändert bei `left: 0`; es traten keine JavaScript-Fehler und keine HTTP(S)-Anfragen auf.
- Sichtgeprüfter Screenshot: `artifacts/html-export-centered-vertical.png`.
- Der integrierte Browserdienst stellte erneut keine Browserinstanz bereit; deshalb wurde die lokale Chromium-Prüfung über Playwright 1.60.0 ausgeführt.

### Erfolgreiche Verifikation und neue Artefakte

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
.\build.ps1 -Task BuildInstaller -Version 0.3.0
```

Ergebnis: Debug und Release jeweils 0 Warnungen und 0 Fehler; jeweils 63/63 Unit-Tests und 130/130 Integrationstests bestanden. Die drei gezielten HTML-Exporttests und die Formatprüfung bestanden ebenfalls.

- Installer: `ZeitstrahlStudio-0.3.0-win-x64-setup.exe`, 62.730.937 Bytes, SHA-256 `E8C564E9112318E5BE28033F5D0E82360063011901B27D6531DC032245D79808`.
- Portable App: `artifacts/publish/win-x64/ZeitstrahlStudio.App.exe`, 152.576 Bytes, SHA-256 `F26D91C1BD2BF936A035BA1A6FE0C33A2A7568695CAF4EA19A6D57DAD7ABA69D`.

## Nächster konkreter Arbeitsschritt (13.08.2026, HTML-Zentrierung)

Den neuen Installer oder die portable EXE verwenden, ein Projekt als HTML exportieren und die mittige vertikale Achse mit dem vollständigen Beispielprojekt visuell abnehmen.

## Dokumenttransfer und HTML-Exportpaket für Version 1.0.0 (13.08.2026)

### Umgesetzte Bedienung

- Im HTML-Exportdialog kann das orange Momentaufnahme-Warnbanner unabhängig von den übrigen Exportoptionen ein- oder ausgeschaltet werden. Die bisherige Einzeldatei bleibt bei deaktivierten Dokumentkopien vollständig abwärtskompatibel.
- Ein zweiter Export-Schalter erzeugt statt einer einzelnen HTML-Datei ein ZIP-Paket mit `index.html`, `LESMICH.txt` und den benötigten Projektkopien unter `Dokumente/`.
- Dokumentname und vorhandene Dokumentvorschau im Paketexport sind relative Links auf die mitgelieferte Datei. Das Paket muss vor dem Öffnen vollständig entpackt werden; dies wird im Dialog und in `LESMICH.txt` erklärt.
- Ein Doppelklick auf einen konkreten Anhang in der Ereignis-Detailansicht öffnet die validierte Projektkopie über das konfigurierte Windows-Standardprogramm. Ein Doppelklick auf freien Listenraum löst nichts aus.
- Potenziell ausführbare, skriptfähige oder verknüpfende Formate werden beim direkten Doppelklick bewusst nicht gestartet. Der bereits vorhandene explizite Öffnen-Dialog bleibt für eine bewusste Benutzerentscheidung erhalten.

### Projektkopien und Integrität

- Der vorhandene Importpfad kopiert jeden Anhang weiterhin sofort streamend und mit GUID-basiertem Namen nach `attachments/{eventId}/{attachmentId}.{ext}` im Projektarbeitsordner. Die Funktion hängt nach dem Import nicht mehr von der ursprünglichen Quelldatei ab.
- Vor dem atomaren Export eines `.zeitprojekt`-Archivs wird nun zusätzlich geprüft, ob jede in SQLite referenzierte Anhangsdatei im Manifest vorhanden ist und ob gespeicherte Länge und SHA-256 mit der tatsächlich geschriebenen Kopie übereinstimmen.
- Fehlende, verkürzte oder bei gleicher Länge manipulierte Projektkopien verhindern den Export; ein vorhandenes Zielarchiv bleibt dabei byteidentisch erhalten.
- Der HTML-Paketexport verwendet ausschließlich GUID-basierte relative Zielpfade, validiert jede Projektkopie über den zentralen Anhangsdienst, prüft Länge und SHA-256 während des Kopierens und öffnet das fertig geschlossene ZIP erneut zur Inhalts- und Prüfsummenprüfung.
- `index.html`, `LESMICH.txt` und alle Dokumente werden zunächst in einer temporären Datei erzeugt. Erst nach vollständiger Prüfung wird das Ziel atomar ersetzt; bei Fehler oder Abbruch werden temporäre Dateien entfernt.
- Es wurden keine neuen Produktionsabhängigkeiten, Netzwerkzugriffe oder Cloud-Funktionen eingeführt.

### Automatisierte Prüfung und visuelle QA

- 46/46 gezielte Integrationstests für HTML-Export, Dialog-/Hauptfenster-Barrierefreiheit, direktes Dokumentöffnen und Projektarchivschutz bestanden.
- Vollständiger Abschlusslauf in Debug und Release: jeweils 63/63 Unit-Tests und 137/137 Integrationstests bestanden; beide Builds meldeten 0 Warnungen und 0 Fehler.
- `dotnet restore ZeitstrahlStudio.sln` und `dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore` waren erfolgreich.
- Selbstenthaltender `win-x64`-Publish, portables ZIP und Inno-Setup-Installer für Version 1.0.0 wurden erfolgreich erzeugt.
- Reale QA-Pakete mit und ohne Warnbanner wurden erzeugt. Der integrierte Browserdienst stellte keine Browserinstanz bereit; lokales Chromium zeigte den Paketexport auf Desktopbreite mit Banner und den responsiven Einzeldateiexport auf Mobilbreite ohne Banner und ohne helle Leerstelle korrekt an.
- Die lokale Browser-Automationssitzung lief nach den erzeugten Screenshots beim Schließen beziehungsweise erneuten Öffnen in einen Timeout. Deshalb wird keine nicht beobachtete End-to-End-Linknavigation behauptet; relative Dokumentpfade, sichere Linkattribute, Paketinhalt und Bytegleichheit sind stattdessen automatisiert geprüft.

Erfolgreich ausgeführte Abschlussbefehle:

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
.\build.ps1 -Task Publish -Version 1.0.0
git archive --format=zip --output=artifacts\committed-samples-1.0.0.zip HEAD samples
Expand-Archive -LiteralPath artifacts\committed-samples-1.0.0.zip -DestinationPath artifacts\committed-samples-1.0.0
Copy-Item -LiteralPath artifacts\committed-samples-1.0.0\samples -Destination artifacts\publish\win-x64\samples -Recurse
Compress-Archive -Path artifacts\publish\win-x64\* -DestinationPath artifacts\release\ZeitstrahlStudio-1.0.0-win-x64-portable.zip -Force
.\build.ps1 -Task BuildInstaller -Version 1.0.0
git diff --check
```

### Versions- und GitHub-Release-Vorbereitung

- `Directory.Build.props`, `build.ps1` und das Inno-Setup-Skript verwenden jetzt Version 1.0.0 beziehungsweise 1.0.0.0.
- `CHANGELOG.md` enthält einen datierten Abschnitt 1.0.0. `RELEASE.md` enthält einen wiederanlaufbaren PowerShell-Ablauf für Prüfung, Fast-Forward-Merge, erzwungen frisch erzeugte Artefakte, Tag `v1.0.0`, atomaren Push und Erstellen beziehungsweise Vervollständigen des GitHub-Releases.
- Der Vergleich mit dem zuletzt abgerufenen Remote-Stand `origin/ui/redesign-0.3.0` (`cc2e4c7`) zeigt vor diesem Meilenstein sieben lokale Commits ohne Remote-Rückstand. Seitdem kamen insbesondere global persistente Themeeinstellungen, die Darkmode-Nachkorrekturen für Auswahl-/Menü-/Checkboxzustände und native Titelleisten, visuelle Ereignis- und Standardfarbenwahl, vollständige farbige Ereignisrahmen sowie der neu gestaltete responsive Offline-HTML-Export mit mittiger vertikaler Desktopachse hinzu. Dieser Meilenstein ergänzt Dokument-Doppelklick, Archivintegrität, Warnbanner-Schalter und Dokumentpaketexport.
- `SPEC.md`, `USER_GUIDE.md`, `PRIVACY.md`, `ARCHITECTURE.md`, `PROJECT_FORMAT.md`, `DECISIONS.md`, `CHANGELOG.md`, `MANUAL_RELEASE_CHECKLIST.md`, `README.md`, `BUILD.md` und `RELEASE.md` wurden aktualisiert.
- Die bereits vor Arbeitsbeginn veränderten Dateien `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` wurden weder bearbeitet noch gestaged. Für die lokalen Prüfpakete wurden die committed Fassungen aus Git verwendet, damit keine nicht autorisierten Benutzeränderungen in Release-Artefakte gelangen.
- Ein unabhängiger Abschlussaudit deckte zwei Release-Risiken auf: einen möglichen Rückgriff auf einen alten Installer bei fehlendem Inno Setup sowie einen nicht wiederanlaufbaren Teilfehler nach Tag/Push. `build.ps1` verlangt jetzt einen erfolgreichen Installer-Compiler; der Releaseblock löscht vorab ausschließlich die exakt benannten Zielartefakte, prüft deren frische Erzeugung und kann einen korrekten vorhandenen Tag beziehungsweise ein teilweise angelegtes GitHub-Release sicher fortsetzen. Abweichende Tags und Force-Push bleiben ausgeschlossen.
- Der vorgeschriebene Patch-Helfer scheiterte erneut vor Dateizugriff am Windows-Sandbox-`spawn_ready`-Timeout. Dokumentänderungen erfolgten deshalb ausschließlich über eindeutig gezählte Einmal-Ersetzungen und wurden unmittelbar mit Diff-, Build- und Testprüfungen kontrolliert.

### Verbleibende manuelle Release-Gates

- Auf einem realen Windows-System einen ungefährlichen Anhang per Doppelklick öffnen und bestätigen, dass das registrierte Standardprogramm verwendet wird; zusätzlich einen blockierten ausführbaren beziehungsweise skriptfähigen Typ prüfen.
- Ein Dokumentpaket vollständig entpacken und die Links in Edge, Firefox und Chrome anklicken. Außerdem den Installer auf Windows 10 und Windows 11 installieren und die Prüfliste in `MANUAL_RELEASE_CHECKLIST.md` abarbeiten.
- Vor dem GitHub-Release müssen die zwei unabhängigen Änderungen unter `samples/` bewusst committed oder anderweitig durch den Benutzer aufgelöst werden. Der dokumentierte Release-Befehl bricht bei einem nicht sauberen Arbeitsbaum absichtlich ab.

## Nächster konkreter Arbeitsschritt (13.08.2026, Version 1.0.0)

Die zwei bestehenden Beispieländerungen bewusst behandeln, danach den in `RELEASE.md` dokumentierten v1.0.0-Ablauf ausführen und die verbleibenden Windows-/Browser-Sichtprüfungen protokollieren.
