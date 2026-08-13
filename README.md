# Zeitstrahl Studio

Zeitstrahl Studio ist eine vollständig lokale, deutschsprachige Windows-Desktopanwendung für chronologische Projekte. Zielplattform ist .NET 8/WPF auf Windows 10 und 11 x64. Die Anwendung enthält keine Telemetrie, Cloud-Synchronisation oder automatische Datenübertragung.

## Aktueller Stand

Version 1.0.0 ist für die lokale Release-Erstellung vorbereitet. Solution, Schichtengrenzen, fachliches Grundmodell, SQLite-Persistenz, lokale Volltextsuche und kombinierbare Filter, sichere atomare `.zeitprojekt`-Archive, Autosave/Recovery, lokale Sicherungen, die WPF-Projekt-/Ereignisoberfläche, Dokumentimport und -analyse, lokale OCR, die virtualisierte horizontale/vertikale Zeitstrahlansicht sowie PDF-Export und responsiver Offline-HTML-Export sind implementiert und automatisiert getestet. Importierte Dokumente werden vollständig und kollisionsfrei in das Projekt übernommen, beim Projekttransfer erneut auf Größe und SHA-256 geprüft und lassen sich per Doppelklick über das Windows-Standardprogramm öffnen. Der HTML-Export kann den Momentaufnahmehinweis ausblenden und optional ein transportables ZIP mit `index.html` und allen validierten Dokumentkopien erzeugen. Ein frei erfundenes Beispielprojekt mit lokalen PDF-, Bild-, DOCX- und XLSX-Testdokumenten sowie ein 5.000-Ereignisse-Lasttest sind enthalten. Buildskript, selbstenthaltende portable Version, Installer mit `.zeitprojekt`-Dateizuordnung, Benutzerhandbuch, Datenschutzhinweis und Release-Dokumentation sind verfügbar.

## Beispielprojekt

Das Archiv [`samples/ZeitstrahlStudio-Beispiel.zeitprojekt`](samples/ZeitstrahlStudio-Beispiel.zeitprojekt) kann direkt in Zeitstrahl Studio geöffnet werden. Es enthält zehn vollständig frei erfundene Ereignisse, alle Datumsgenauigkeiten, Fristen, große Zeitlücken, manuelle Layoutpositionen sowie lokale Testdokumente. Herkunft, Lizenz und erneute Erzeugung sind in [`samples/README.md`](samples/README.md) beschrieben.

```powershell
dotnet run --project tools/ZeitstrahlStudio.SampleGenerator/ZeitstrahlStudio.SampleGenerator.csproj -- --output samples
```

## Lokaler Build

Voraussetzungen:

- Windows 10 oder 11 x64
- .NET SDK 8.x
- PowerShell 5.1 oder höher
- für deutsche OCR: in Windows installiertes deutsches Sprachpaket einschließlich Texterkennung; die Anwendung erkennt eine fehlende Ressource und zeigt eine lokale Handlungsanleitung
- für den Installer: Inno Setup 6

Schnellstart für den vollständigen Release-Build:

```powershell
.\build.ps1 -Task All -Version 1.0.0
```

Einzelne Schritte:

```powershell
dotnet restore
dotnet build ZeitstrahlStudio.sln -c Debug
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
```

OCR verwendet ausschließlich die lokale Windows-Texterkennung. Dokumente, Bilder und erkannte Texte werden weder hochgeladen noch an externe Prozesse übergeben. OCR-Ergebnisse werden im Projekt ausdrücklich als potenziell fehlerhaft gekennzeichnet.

## Dokumentation

- `SPEC.md` – Fachliche Spezifikation
- `ARCHITECTURE.md` – Architekturübersicht
- `PROJECT_FORMAT.md` – Projektdateiformat `.zeitprojekt`
- `BUILD.md` – Build- und Release-Anleitung
- `USER_GUIDE.md` – Benutzerhandbuch
- `PRIVACY.md` – Datenschutzerklärung
- `THIRD_PARTY_LICENSES.md` – Drittanbieterlizenzen
- `CHANGELOG.md` – Änderungshistorie
- `RELEASE.md` – Release-Prozess
- `MANUAL_RELEASE_CHECKLIST.md` – Manuelle Abnahmeprüfungen
- `STATUS.md` – Aktueller Projektstatus
