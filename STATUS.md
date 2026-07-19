# Projektstatus

Status: In Entwicklung – Meilensteine 1 bis 4 sowie Anhangsimport und Dokumentanalyse 5A bis 5C1 abgeschlossen, noch kein Release

Letzte Aktualisierung: 19.07.2026

## Aktuelle Phase

Meilenstein 5C2 – lokale Bildvorschau und Öffnen von Projektkopien

## Prüfung der Entwicklungsumgebung

- Windows 10 x64, Version 10.0.19045
- PowerShell 5.1.19041
- .NET SDK 8.0.423, MSBuild 17.11.48; zusätzlich SDK 6.0.100 installiert
- Git verfügbar; Ausgangszustand war ein Commit mit Spezifikationsdokumenten und ein unversioniertes WPF-Starttemplate
- Arbeitsbereich ist beschreibbar; durch Solution-Dateien, Builds und Tests praktisch bestätigt
- `dotnet`, Git und die WPF-Buildwerkzeuge sind verfügbar
- Inno Setup (`iscc`) ist derzeit nicht im `PATH`; dies blockiert die laufende Implementierung nicht, muss aber vor dem Installer-Abnahmetest behoben werden
- Vor Arbeitsbeginn waren keine Build-/Testprotokolle vorhanden

## Abgeschlossene Arbeiten

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

## Erfolgreiche Build- und Testbefehle

Am 19.07.2026 erfolgreich ausgeführt:

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
```

Aktueller Stand nach Meilenstein 5C1: Debug und Release jeweils 0 Warnungen/0 Fehler; jeweils 29 Unit-Tests und 33 Integrationstests bestanden. Zusätzlich meldet `dotnet format ZeitstrahlStudio.sln --no-restore --verify-no-changes` keine Formatabweichung.

## Phasenweiser Implementierungsplan

1. **Solution und Architektur – abgeschlossen:** Schichten, Fachmodellbasis, Ports, Architektur- und Formatdokumentation.
2. **Datenmodell und SQLite – abgeschlossen:** vollständiges normalisiertes Schema, Migration 1, Repository, Transaktionen, FTS5 und Integrationstests.
3. **Projektverwaltung – abgeschlossen:** sichere Arbeitsordner, Archivtransfer, Neu/Öffnen/Speichern/Speichern unter/Duplizieren/Schließen, zuletzt verwendet, Autosave, Crash-Recovery, produktive DI und verbundene MVVM-Oberfläche.
4. **Ereignisse und Fristen – abgeschlossen:** vollständige MVVM-Bearbeitung, Datumsgenauigkeiten, Fristen, Tags, Links, mehrstufiges Undo/Redo, manuelle Reihenfolge gleicher Datumswerte und persistentes Audit.
5. **Anhänge und lokale Dokumentenanalyse – in Arbeit:** sicherer Import und Undo-fähige Zuordnung sind in 5A umgesetzt; DOCX-/XLSX-/PDF-Extraktion, transaktionale Persistenz, begrenzte Warteschlange und Ergebnisanzeige sind in 5B1 bis 5C1 implementiert; Bild-/PDF-Vorschau, Standardprogramm und OCR folgen.
6. **Zeitstrahldarstellung:** horizontale/vertikale virtualisierte Ansichten, Skala, Zoom/Pan, Lückenkompression, Fristmarker, manuelle Positionen.
7. **Suche und Filter:** inkrementeller Volltextindex, kombinierbare Filter, Trefferhervorhebung und Navigation.
8. **PDF-Export:** Vorschau, A4/A3/benutzerdefiniert, mehrseitig, große Einzelseite, Zeitraum, drucktaugliche Kennzeichnungen.
9. **Standalone-HTML-Export:** eine offlinefähige responsive Datei mit eingebetteten Daten, Suche, Filtern, Zoom und Druck-CSS.
10. **Projektarchiv, Sicherung und Wiederherstellung:** Manifest, SHA-256, sichere ZIP-Verarbeitung, Transfer, rotierende Sicherungen, Crash-Recovery.
11. **Tests und Beispielprojekt:** vollständige Unit-/Integrationstestmatrix, Fehlerfälle, freie PDF/Bild/DOCX/XLSX-Testdokumente und mindestens zehn Beispielereignisse.
12. **Installer, portable Veröffentlichung und Dokumentation:** Buildskripte, selbstenthaltendes Publish, ZIP, Inno-Setup-Dateizuordnung, Handbuch, Datenschutz, Release-Audit.

Nach jedem Meilenstein werden relevante Debug-/Release-Builds und Tests ausgeführt, Status/Entscheidungen aktualisiert und ein kleiner Git-Commit erstellt.

## Bekannte Probleme und Risiken

- Die manuelle Ereignisreihenfolge ist über tastaturzugängliche Früher-/Später-Aktionen verfügbar; direktes Drag-and-drop ist noch nicht implementiert.
- Nach Ablauf der Undo-Historie ist noch keine Bereinigung nicht mehr referenzierter physischer Anhangsdateien implementiert.
- Die Ergebnisanzeige für Office-Dokumente bietet noch keine Schaltfläche zum Öffnen im Windows-Standardprogramm.
- UI-Automation und visuelle Abnahmetests für 100/125/150/200 Prozent Skalierung stehen noch aus.
- PDF-Rendering und OCR benötigen später lokale native/verwaltete Komponenten; Lizenz, Größe und x64-Paketierung müssen vor Auswahl geprüft werden.
- Die Archivlimits sind implementiert, Lasttests mit realen mehrgigabytegroßen Archiven stehen noch aus.
- Inno Setup ist nicht im `PATH`; der Installer kann aktuell noch nicht gebaut werden.
- Selbstenthaltende Veröffentlichung wurde für diesen frühen Architekturstand bewusst noch nicht als Release-Gate gewertet.

## Nächster konkreter Arbeitsschritt

Sichere lokale Vorschau für Bild-Projektkopien sowie das explizite Öffnen unterstützter Anhänge im Windows-Standardprogramm implementieren und an die Anhangsauswahl anbinden.
