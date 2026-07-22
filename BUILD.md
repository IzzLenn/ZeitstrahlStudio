# Build-Anleitung für Zeitstrahl Studio

Diese Anleitung beschreibt, wie Zeitstrahl Studio lokal gebaut, getestet und veröffentlicht wird.

## Voraussetzungen

- Windows 10 oder Windows 11 (64 Bit)
- .NET SDK 8.x
- PowerShell 5.1 oder höher
- Optional für den Installer: Inno Setup 6 (https://jrsoftware.org/isinfo.php)
- Für deutsche OCR: In Windows installiertes deutsches Sprachpaket inklusive Texterkennung

## Schnellstart

```powershell
.\build.ps1 -Task All -Version 0.2.1
```

Dieser Befehl führt aus:

1. `dotnet restore`
2. Debug-Build und -Tests
3. Release-Build und -Tests
4. Formatprüfung
5. Selbstenthaltenden win-x64-Publish
6. Portable ZIP mit Prüfsummen
7. Inno-Setup-Installer (falls verfügbar)

## Einzelne Build-Schritte

### NuGet-Pakete wiederherstellen

```powershell
.\build.ps1 -Task Restore
```

### Debug-Build

```powershell
.\build.ps1 -Task BuildDebug
```

### Debug-Tests

```powershell
.\build.ps1 -Task TestDebug
```

### Release-Build

```powershell
.\build.ps1 -Task BuildRelease
```

### Release-Tests

```powershell
.\build.ps1 -Task TestRelease
```

### Formatprüfung

```powershell
.\build.ps1 -Task FormatCheck
```

### Publish für win-x64

```powershell
.\build.ps1 -Task Publish
```

Die Ausgabe befindet sich in `artifacts\publish\win-x64`.

### Portable ZIP

```powershell
.\build.ps1 -Task PackagePortable -Version 0.1.0
```

Die ZIP-Datei wird in `artifacts\release` abgelegt.

### Installer

```powershell
.\build.ps1 -Task BuildInstaller -Version 0.1.0
```

Falls Inno Setup nicht installiert ist, wird der Schritt übersprungen und eine Hinweismeldung ausgegeben.

## Manuelle Build-Befehle

Falls das PowerShell-Skript nicht verwendet werden kann, können die folgenden Befehle direkt ausgeführt werden:

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64
```

## Fehlercodes

Das Buildskript verwendet folgende Fehlercodes:

| Code | Bedeutung |
|------|-----------|
| 0 | Erfolg |
| 1 | Build-Fehler |
| 2 | Test-Fehler |
| 3 | Format-Fehler |
| 4 | Publish-Fehler |
| 5 | Installer-Fehler |
| 6 | Paketierungs-Fehler |
| 7 | Restore-Fehler |

## Ausgabeverzeichnisse

| Verzeichnis | Inhalt |
|-------------|--------|
| `artifacts\publish\win-x64` | Selbstenthaltende win-x64-Anwendung |
| `artifacts\release` | Portable ZIP, Prüfsummen |
| Projekt-Hauptverzeichnis | Installer-EXE (`ZeitstrahlStudio-*-win-x64-setup.exe`) |

## Bekannte Einschränkungen

- Der Installer-Build erfordert Inno Setup 6.
- Die portable ZIP enthält alle Laufzeitabhängigkeiten und benötigt keine separate .NET-Installation.
- Die deutsche OCR setzt eine installierte Windows-Texterkennungsressource voraus.
