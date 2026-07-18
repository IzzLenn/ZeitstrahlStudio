# Projektstatus

Status: In Entwicklung – Meilensteine 1 und 2 abgeschlossen, noch kein Release

Letzte Aktualisierung: 19.07.2026

## Aktuelle Phase

Meilenstein 3 – Projektverwaltung und lokale Arbeitsordner

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

## Erfolgreiche Build- und Testbefehle

Am 19.07.2026 erfolgreich ausgeführt:

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
```

Aktueller Stand nach Meilenstein 2: Debug und Release jeweils 0 Warnungen/0 Fehler; jeweils 20 Unit-Tests und 7 Integrationstests bestanden.

## Phasenweiser Implementierungsplan

1. **Solution und Architektur – abgeschlossen:** Schichten, Fachmodellbasis, Ports, Architektur- und Formatdokumentation.
2. **Datenmodell und SQLite – abgeschlossen:** vollständiges normalisiertes Schema, Migration 1, Repository, Transaktionen, FTS5 und Integrationstests.
3. **Projektverwaltung – als Nächstes:** Arbeitsordner, Neu/Öffnen/Speichern unter/Duplizieren/Löschen, zuletzt verwendet, atomarer Autosave.
4. **Ereignisse und Fristen:** verbundene MVVM-Bearbeitung, Tags, Links, Undo/Redo, Drag-Sortierung, Audit.
5. **Anhänge und lokale Dokumentenanalyse:** sichere Kopien, Mehrfach-Drop, PDF/Bild/DOCX/XLSX, Vorschau, lokale OCR, Warteschlange.
6. **Zeitstrahldarstellung:** horizontale/vertikale virtualisierte Ansichten, Skala, Zoom/Pan, Lückenkompression, Fristmarker, manuelle Positionen.
7. **Suche und Filter:** inkrementeller Volltextindex, kombinierbare Filter, Trefferhervorhebung und Navigation.
8. **PDF-Export:** Vorschau, A4/A3/benutzerdefiniert, mehrseitig, große Einzelseite, Zeitraum, drucktaugliche Kennzeichnungen.
9. **Standalone-HTML-Export:** eine offlinefähige responsive Datei mit eingebetteten Daten, Suche, Filtern, Zoom und Druck-CSS.
10. **Projektarchiv, Sicherung und Wiederherstellung:** Manifest, SHA-256, sichere ZIP-Verarbeitung, Transfer, rotierende Sicherungen, Crash-Recovery.
11. **Tests und Beispielprojekt:** vollständige Unit-/Integrationstestmatrix, Fehlerfälle, freie PDF/Bild/DOCX/XLSX-Testdokumente und mindestens zehn Beispielereignisse.
12. **Installer, portable Veröffentlichung und Dokumentation:** Buildskripte, selbstenthaltendes Publish, ZIP, Inno-Setup-Dateizuordnung, Handbuch, Datenschutz, Release-Audit.

Nach jedem Meilenstein werden relevante Debug-/Release-Builds und Tests ausgeführt, Status/Entscheidungen aktualisiert und ein kleiner Git-Commit erstellt.

## Bekannte Probleme und Risiken

- Produktive Dependency Injection und lokale strukturierte Protokollierung sind noch nicht implementiert.
- Die WPF-Oberfläche ist weiterhin das Starttemplate; sie ist noch nicht mit den Application-Ports verbunden.
- Dokumentanalyse, OCR und PDF-Vorschau benötigen später lokale native/verwaltete Komponenten; Lizenz, Größe und x64-Paketierung müssen vor Auswahl geprüft werden.
- Große Archive benötigen harte Extraktionslimits und Speicherplatzprüfung; bisher besteht nur Domain-Pfadvalidierung, noch kein ZIP-Importer.
- Inno Setup ist nicht im `PATH`; der Installer kann aktuell noch nicht gebaut werden.
- Selbstenthaltende Veröffentlichung wurde für diesen frühen Architekturstand bewusst noch nicht als Release-Gate gewertet.

## Nächster konkreter Arbeitsschritt

Sicheren lokalen Workspace-Dienst implementieren: neues Projekt in eindeutigem Arbeitsordner anlegen, vorhandene Arbeitskopie öffnen, atomar speichern/„Speichern unter“, duplizieren und schließen. Darauf aufbauend die ersten `.zeitprojekt`-Archive mit Manifest und SHA-256 erzeugen und per Integrationstest erneut öffnen.
