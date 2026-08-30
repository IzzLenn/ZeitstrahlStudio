# Build-Anleitung für Zeitstrahl Studio

Diese Anleitung beschreibt Einrichtung, Build, Test und lokale Releaseausgaben des aktuellen Repository-Stands. Der eigentliche Freigabeprozess steht in [`RELEASE.md`](RELEASE.md).

## Voraussetzungen

- Windows 10 oder Windows 11 x64
- .NET SDK 8.x
- PowerShell 5.1 oder neuer
- Git für einen Quellcode-Checkout
- Inno Setup 6 ausschließlich für `BuildInstaller` und `All`
- optional: deutsches Windows-Sprachpaket einschließlich Texterkennung für reale OCR-Tests

Das Repository enthält kein `global.json`. Die .NET-CLI wählt daher nach ihrer allgemeinen SDK-Auswahllogik ein installiertes SDK und kann dabei auch ein neueres Major verwenden. Für einen freigegebenen Ablauf ist ein beabsichtigtes .NET-8.x-SDK bewusst bereitzustellen beziehungsweise zu verwenden und die tatsächliche Auswahl vor Diagnose und Release zu protokollieren:

```powershell
dotnet --info
dotnet --list-sdks
```

Die erzeugten Installer- und Portable-Pakete sind self-contained für `win-x64`; Endnutzer benötigen dafür keine separate .NET-Runtime.

## Checkout und Restore

Nach einem vorhandenen Checkout in das Repository wechseln. Bei einem neuen Checkout beispielsweise:

```powershell
git clone <Repository-URL> ZeitstrahlStudio
Set-Location ZeitstrahlStudio
git status --short
dotnet restore ZeitstrahlStudio.sln
```

Der erste Restore benötigt Zugriff auf die konfigurierten NuGet-Quellen. Paket- und Lizenzversionen sind in [`THIRD_PARTY_LICENSES.md`](THIRD_PARTY_LICENSES.md) dokumentiert.

Für einen direkten self-contained `win-x64`-Publish müssen zusätzlich die RID-spezifischen Runtimepacks verfügbar sein. Als robuster Preflight vor dem Einzel-Task `Publish` ist deshalb dieser explizite Restore auszuführen:

```powershell
dotnet restore src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -r win-x64
```

Diese Ergänzung ersetzt den normalen Solution-Restore nicht. Im Endvalidierungslauf scheiterte der direkte `--no-restore`-Publish nach erfolgreichem Solution-Restore mit `NETSDK1112`, weil `Microsoft.NETCore.App.Runtime.win-x64` und `Microsoft.WindowsDesktop.App.Runtime.win-x64` fehlten. Der explizite App-RID-Restore behob diesen konkreten Fall. Seit 1.1.0 führt `build.ps1` denselben RID-Restore in seinem `Restore`-Task aus; `All` deckt die Voraussetzung damit selbst ab.

## Anwendung im Debugmodus starten

Nach Restore und Debug-Build:

```powershell
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet run --project src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Debug --no-build
```

Ein Projektarchiv kann wie bei der installierten Dateizuordnung als erstes Anwendungsargument übergeben werden:

```powershell
dotnet run --project src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Debug --no-build -- "C:\Projekte mit Leerzeichen\Beispiel.zeitprojekt"
```

## Reproduzierbare Build- und Testmatrix

Die Reihenfolge ist wichtig: `--no-restore` setzt einen erfolgreichen Restore voraus; Tests mit `--no-build` setzen den Build derselben Konfiguration voraus.

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
```

Testergebnisse und Anzahlen sind Momentaufnahmen des jeweiligen Commits und dürfen nicht als zeitlose Projektkennzahl dokumentiert werden. Die Integrationstest-Assembly deaktiviert xUnit-Testparallelisierung, weil viele Tests reale WPF-, SQLite-, native oder Dateisystemgrenzen prüfen.

### Gezielte Tests

Nach einem passenden Build können Projekte oder Testgruppen einzeln ausgeführt werden:

```powershell
dotnet test tests\ZeitstrahlStudio.UnitTests\ZeitstrahlStudio.UnitTests.csproj -c Debug --no-restore --no-build
dotnet test tests\ZeitstrahlStudio.IntegrationTests\ZeitstrahlStudio.IntegrationTests.csproj -c Debug --no-restore --no-build
dotnet test tests\ZeitstrahlStudio.IntegrationTests\ZeitstrahlStudio.IntegrationTests.csproj -c Debug --no-restore --no-build --filter "FullyQualifiedName~ProjectArchive"
```

Wenn noch kein Build der gewählten Konfiguration existiert, `--no-build` entfernen oder zuerst das Projekt beziehungsweise die Solution bauen. Ein Filter darf keinen unbeabsichtigt leeren Lauf erzeugen; die Testausgabe ist zu kontrollieren.

## SampleGenerator

`ZeitstrahlStudio.SampleGenerator` erzeugt das frei erfundene Beispiel samt Dokumenten und Exporten reproduzierbar. Für eine gefahrlose Prüfung einen separaten Zielordner verwenden:

```powershell
$sampleOutput = Join-Path $env:TEMP "ZeitstrahlStudio-SampleGenerator"
dotnet run --project tools\ZeitstrahlStudio.SampleGenerator\ZeitstrahlStudio.SampleGenerator.csproj -c Debug -- --output $sampleOutput
```

Ein Lauf mit `--output samples` verändert die Repository-Beispiele und gehört nur in einen ausdrücklich dafür vorgesehenen, überprüften Arbeitsbaum.

## `build.ps1`

Das Skript besitzt folgende Tasks:

| Task | Aktion | Erforderlicher Vorzustand bei Einzelaufruf |
| --- | --- | --- |
| `Restore` | `dotnet restore` für die Solution | keiner |
| `BuildDebug` | Debug-Build mit `--no-restore` | Restore |
| `TestDebug` | Debug-Tests mit `--no-restore --no-build` | Restore und Debug-Build |
| `BuildRelease` | Release-Build mit `--no-restore` | Restore |
| `TestRelease` | Release-Tests mit `--no-restore --no-build` | Restore und Release-Build |
| `FormatCheck` | `dotnet format --verify-no-changes --no-restore` | Restore |
| `Publish` | self-contained Release-Publish für `win-x64` | Solution-Restore sowie RID-Restore des App-Projekts beziehungsweise bereits verfügbare `win-x64`-Runtimepacks |
| `PackagePortable` | portable ZIP und SHA-256 erzeugen | frisches `artifacts\publish\win-x64` |
| `BuildInstaller` | Inno-Setup-Installer erzeugen | Publish plus paketierte Dokumente/Lizenzen/Samples, Inno Setup 6 |
| `All` | alle Schritte in korrekter Reihenfolge | bewusster Release-Arbeitsbaum, Inno Setup 6; der RID-Restore ist enthalten |

Beispiele:

```powershell
.\build.ps1 -Task Restore
.\build.ps1 -Task BuildDebug
.\build.ps1 -Task TestDebug
.\build.ps1 -Task BuildRelease
.\build.ps1 -Task TestRelease
.\build.ps1 -Task FormatCheck
.\build.ps1 -Task Publish
.\build.ps1 -Task PackagePortable -Version 1.1.0
.\build.ps1 -Task BuildInstaller -Version 1.1.0
```

Die Einzelaufgaben führen ihre Vorstufen nicht automatisch aus. Für den vollständigen Releaseablauf:

```powershell
.\build.ps1 -Task All -Version 1.1.0
```

`All` ist kein Mindestschritt für eine reine Dokumentations- oder normale Codevalidierung. Dafür genügt die Restore-/Debug-/Release-/Formatmatrix. `All` erzeugt und ersetzt lokale Paketartefakte und darf nur im bewusst vorbereiteten Releasebaum laufen.

Das Skript verwendet Fehlercodes 0 für Erfolg sowie 1 Build, 2 Test, 3 Format, 4 Publish, 5 Installer, 6 Paketierung und 7 Restore.

## Publish und portable ZIP

`Publish` entfernt ein vorhandenes `artifacts\publish\win-x64`, erzeugt dort einen self-contained Release-Publish für RID `win-x64`, prüft `ZeitstrahlStudio.App.exe` und entfernt rekursiv PDB-Dateien.

`PackagePortable` setzt einen frischen Publish voraus, prüft dessen Alter aber nicht. Es kopiert anschließend in den Publish-Baum:

- `README.md`, `PRIVACY.md`, `THIRD_PARTY_LICENSES.md`, `CHANGELOG.md`, `LICENSE.txt`
- den vollständigen Ordner `licenses`
- den vollständigen aktuellen Arbeitsbaum unter `samples`

Danach entstehen:

```text
artifacts\release\ZeitstrahlStudio-<Version>-win-x64-portable.zip
artifacts\release\ZeitstrahlStudio-<Version>-win-x64-portable.zip.sha256
artifacts\release\checksums.txt
```

Wichtig: `build.ps1` prüft den Git-Arbeitsbaum nicht auf Sauberkeit. Lokale oder uncommittete Änderungen unter `samples` würden unverändert in portable ZIP und anschließend Installer gelangen. `PackagePortable` und `All` deshalb ausschließlich aus einem überprüften sauberen Releasebaum ausführen. Der verbindliche Preflight steht in [`RELEASE.md`](RELEASE.md).

## Installer

`BuildInstaller` sucht `iscc` zunächst im `PATH`, danach unter `%ProgramFiles(x86)%\Inno Setup 6\iscc.exe` und `%LocalAppData%\Programs\Inno Setup 6\ISCC.exe`. Das Skript `installer\ZeitstrahlStudio.iss` erzeugt im Repository-Root:

```text
ZeitstrahlStudio-<Version>-win-x64-setup.exe
```

Der Installer:

- installiert standardmäßig nach 64-Bit-Program Files und fordert standardmäßig Administratorrechte an; der Inno-Dialog erlaubt eine Privilegienabweichung
- legt Startmenüeinträge an
- bietet eine standardmäßig nicht aktivierte Desktopverknüpfung
- bietet die `.zeitprojekt`-Dateizuordnung und übergibt auch Pfade mit Leerzeichen als zitiertes CLI-Argument
- verwendet derzeit kein fertiges projektspezifisches App-/Setup-Icon

Es ist keine Codesignierung konfiguriert oder durch das Buildskript belegt.

## Ausgaben und Prüfsummen

| Ort | Inhalt |
| --- | --- |
| `artifacts\publish\win-x64` | entpackter self-contained Publish |
| `artifacts\release` | portable ZIP, ZIP-Prüfsumme und `checksums.txt` |
| Repository-Root | Installer-EXE |

Diese Ausgaben sind generiert und per `.gitignore` ausgeschlossen; ein frischer Checkout enthält sie nicht. ZIP-Prüfsumme prüfen:

```powershell
$zip = "artifacts\release\ZeitstrahlStudio-1.1.0-win-x64-portable.zip"
Get-FileHash -LiteralPath $zip -Algorithm SHA256
Get-Content -LiteralPath "$zip.sha256"
```

`build.ps1` erzeugt keine Installer-Prüfsummendatei. Der Release-Verantwortliche muss den Installer separat hashen und die veröffentlichte Gesamtliste bewusst vervollständigen.

## Fehlerbehebung

### Restore oder NuGet schlägt fehl

Netzwerk, Proxy, Zertifikate und konfigurierte NuGet-Quellen prüfen. Danach `dotnet restore ZeitstrahlStudio.sln` ohne `--no-restore` wiederholen. Paketcache nicht pauschal löschen, solange keine Cachekorruption belegt ist.

### `NETSDK1112` beim self-contained `win-x64`-Publish

Wenn `Microsoft.NETCore.App.Runtime.win-x64` oder `Microsoft.WindowsDesktop.App.Runtime.win-x64` nicht heruntergeladen wurde, kann der direkte Publish trotz vorherigem erfolgreichem Solution-Restore mit `NETSDK1112` abbrechen. Dann den RID-spezifischen Restore ausführen und denselben Publish wiederholen:

```powershell
dotnet restore src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -r win-x64
```

In der Endvalidierung behob dieser Befehl den beobachteten Fall; der anschließende self-contained Publish war erfolgreich und enthielt `ZeitstrahlStudio.App.exe`. Ein danach erneut ausgeführter normaler Solution-Restore sowie ein weiterer identischer Publish blieben ebenfalls erfolgreich. Daraus folgt keine allgemeine Aussage über den Solution-Restore; der explizite RID-Restore ist hier als robuster Preflight für die Runtimepack-Verfügbarkeit dokumentiert.

### Falsches SDK oder Targeting-Pack

`dotnet --info` und `dotnet --list-sdks` prüfen. Ein .NET-8-SDK muss ausgewählt werden; ein zusätzlich installiertes neueres SDK ersetzt diese Anforderung nicht automatisch.

### Tests melden fehlende Assemblys

Die Konfiguration wurde wahrscheinlich nicht gebaut. Erst Restore und passenden Debug-/Release-Build ausführen, dann den Test mit `--no-build` starten.

### Dateien sind gesperrt

Laufende `ZeitstrahlStudio.App.exe`, Vorschau-, Installer- oder PDF-Prozesse geordnet schließen. Danach denselben Schritt wiederholen. Keine Buildausgabe löschen, solange ein Prozess sie noch verwendet.

### Native PDF-/OCR-Abhängigkeit fehlt

Für PDFium/Skia den `win-x64`-Publish und dessen `runtimes`-Unterordner vollständig verwenden. Für OCR die deutsche Windows-Texterkennungsressource installieren. Produktbezogene Symptome stehen in [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md).

### Inno Setup wird nicht gefunden

Inno Setup 6 installieren und `iscc.exe` über einen der oben genannten Suchpfade bereitstellen. `BuildInstaller` und `All` schlagen ohne einen frischen Installer bewusst fehl.

### Pfad- oder Längenfehler

Repository und temporäre Ausgabe in einen kürzeren lokalen Pfad verschieben und Windows-Long-Path-Unterstützung prüfen. Quell- und Sampledateien nicht mit Shellbefehlen umbenennen, wenn dadurch reproduzierbare Projektinhalte verändert würden.

Architekturhintergrund: [`ARCHITECTURE.md`](ARCHITECTURE.md). Releaseablauf: [`RELEASE.md`](RELEASE.md).
