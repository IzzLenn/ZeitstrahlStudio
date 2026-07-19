# Zeitstrahl Studio

Zeitstrahl Studio wird als vollständig lokale, deutschsprachige Windows-Desktopanwendung für chronologische Projekte entwickelt. Zielplattform ist .NET 8/WPF auf Windows 10 und 11 x64. Die Anwendung wird keine Telemetrie, Cloud-Synchronisation oder automatische Datenübertragung enthalten.

## Aktueller Stand

Das Repository befindet sich nach Meilenstein 8B im Aufbau. Solution, Schichtengrenzen, fachliches Grundmodell, SQLite-Persistenz, lokale Volltextsuche und kombinierbare Filter, sichere atomare `.zeitprojekt`-Archive, Autosave/Recovery, die verbundene WPF-Projekt-/Ereignisoberfläche, Dokumentimport und -analyse, lokale OCR, die virtualisierte horizontale/vertikale Zeitstrahlansicht sowie PDF- und eigenständiger Offline-HTML-Export sind implementiert und automatisiert getestet. Sicherungsrotation, Beispielprojekt, vollständige Abnahme, portable ZIP-Datei, Installer und Endbenutzerdokumentation folgen in den in `STATUS.md` dokumentierten Meilensteinen. Dieser Stand ist noch kein Release.

## Lokaler Build

Voraussetzungen:

- Windows 10 oder 11 x64
- .NET SDK 8.x
- für deutsche OCR: in Windows installiertes deutsches Sprachpaket einschließlich Texterkennung; die Anwendung erkennt eine fehlende Ressource und zeigt eine lokale Handlungsanweisung
- für den späteren Installer: Inno Setup 6

```powershell
dotnet restore
dotnet build ZeitstrahlStudio.sln -c Debug
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore
```

OCR verwendet ausschließlich die lokale Windows-Texterkennung. Dokumente, Bilder und erkannte Texte werden weder hochgeladen noch an externe Prozesse übergeben. OCR-Ergebnisse werden im Projekt ausdrücklich als potenziell fehlerhaft gekennzeichnet.

Die fachliche Spezifikation steht in `SPEC.md`, die Architektur in `ARCHITECTURE.md`, das geplante Archivformat in `PROJECT_FORMAT.md` und der tatsächliche Arbeitsstand in `STATUS.md`.
