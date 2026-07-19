# Zeitstrahl Studio

Zeitstrahl Studio wird als vollständig lokale, deutschsprachige Windows-Desktopanwendung für chronologische Projekte entwickelt. Zielplattform ist .NET 8/WPF auf Windows 10 und 11 x64. Die Anwendung wird keine Telemetrie, Cloud-Synchronisation oder automatische Datenübertragung enthalten.

## Aktueller Stand

Das Repository befindet sich nach Meilenstein 5B1 im Aufbau. Solution, Schichtengrenzen, fachliches Grundmodell, SQLite-Persistenz, lokaler FTS5-Index, sichere atomare `.zeitprojekt`-Archive, Autosave/Recovery, die verbundene WPF-Projekt-/Ereignisoberfläche, sicherer Anhangsimport und lokale DOCX-/XLSX-Extraktion sind implementiert und automatisiert getestet. PDF-/Bildanalyse, OCR, Vorschauen, Zeitstrahlansichten, PDF-/HTML-Exporte, Sicherungsrotation und Auslieferungsartefakte folgen in den in `STATUS.md` dokumentierten Meilensteinen. Dieser Stand ist noch kein Release.

## Lokaler Build

Voraussetzungen:

- Windows 10 oder 11 x64
- .NET SDK 8.x
- für den späteren Installer: Inno Setup 6

```powershell
dotnet restore
dotnet build ZeitstrahlStudio.sln -c Debug
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore
```

Die fachliche Spezifikation steht in `SPEC.md`, die Architektur in `ARCHITECTURE.md`, das geplante Archivformat in `PROJECT_FORMAT.md` und der tatsächliche Arbeitsstand in `STATUS.md`.
