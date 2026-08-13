# Release-Anleitung Zeitstrahl Studio

Diese Anleitung beschreibt den reproduzierbaren Release-Prozess für Zeitstrahl Studio 1.0.0.

## Voraussetzungen

- Windows 10 oder 11, 64 Bit
- .NET SDK 8.x
- PowerShell 5.1 oder höher
- Inno Setup 6 für den Installer
- Git
- GitHub CLI (`gh`), bei `github.com` angemeldet
- Schreibrecht für `IzzLenn/ZeitstrahlStudio`

## Vorbedingungen

Der Arbeitsbaum muss vollständig sauber sein. Das gilt ausdrücklich auch für `samples/`, weil `build.ps1` diese Dateien in die portable ZIP und den Installer übernimmt. Lokale Beispieländerungen müssen vor einem Release bewusst committed, separat gesichert oder verworfen werden; der Release-Befehl verändert sie nicht selbst.

Die Versionsquellen müssen übereinstimmen:

- `Directory.Build.props`: `1.0.0`
- `build.ps1`: Standard `1.0.0`
- `installer/ZeitstrahlStudio.iss`: Standard `1.0.0`
- Git-Tag: `v1.0.0`

## Lokaler Release-Build

```powershell
.\build.ps1 -Task All -Version 1.0.0
```

Dieser Befehl führt Restore, Debug-/Release-Build und -Tests, Formatprüfung, selbstenthaltenden `win-x64`-Publish, portable ZIP, Prüfsummen und Installer aus. Falls Inno Setup nicht vorhanden ist, meldet das Skript eine Warnung; ein GitHub-Release darf dann erst nach separat erfolgreichem Installer-Build erstellt werden.

Erwartete Artefakte:

- `ZeitstrahlStudio-1.0.0-win-x64-setup.exe`
- `artifacts\release\ZeitstrahlStudio-1.0.0-win-x64-portable.zip`
- `artifacts\release\ZeitstrahlStudio-1.0.0-win-x64-portable.zip.sha256`
- `artifacts\release\checksums.txt`

## Manuelle Abnahme

Vor der Veröffentlichung ist `MANUAL_RELEASE_CHECKLIST.md` vollständig abzuarbeiten. Besonders relevant für 1.0.0 sind:

- Hell-/Dunkelmodus einschließlich nativer Titelleisten, Auswahlfelder, Register und Checkboxzustände
- Doppelklick auf Dokumentkopien sowie Blockade riskanter Dateitypen
- Projekttransfer mit erneutem Öffnen und Hashvergleich aller Dokumentkopien
- HTML-Einzeldatei mit sichtbarem und deaktiviertem Momentaufnahmehinweis
- vollständig entpacktes HTML-ZIP-Paket mit anklickbaren Dokumentnamen und Miniaturen
- Offline-Prüfung in Edge, Firefox und Chrome

## Einmaliger PowerShell-Befehl für GitHub

Der folgende Block prüft Arbeitsbaum, Version, Tag, Fast-Forward-Kompatibilität, GitHub-Anmeldung, Build und Artefakte. Danach aktualisiert er `master` ohne Force-Push, pusht `master` und `v1.0.0` atomar und erstellt das GitHub-Release.

```powershell
$ErrorActionPreference = "Stop"
$version = "1.0.0"
$tag = "v$version"
$sourceBranch = "ui/redesign-0.3.0"
$repository = "IzzLenn/ZeitstrahlStudio"

if (@(git status --porcelain).Count -ne 0) {
    throw "Der Arbeitsbaum ist nicht sauber. Änderungen zuerst bewusst committen oder separat sichern."
}

if (-not (Select-String -Path "Directory.Build.props" -SimpleMatch "<Version>1.0.0</Version>" -Quiet)) {
    throw "Directory.Build.props ist nicht auf Version 1.0.0 gesetzt."
}

if (-not (Select-String -Path "build.ps1" -SimpleMatch '[string]$Version = "1.0.0"' -Quiet)) {
    throw "build.ps1 ist nicht auf Version 1.0.0 gesetzt."
}

if (-not (Select-String -Path "installer\ZeitstrahlStudio.iss" -SimpleMatch '#define MyAppVersion "1.0.0"' -Quiet)) {
    throw "Das Installer-Skript ist nicht auf Version 1.0.0 gesetzt."
}

git fetch origin --prune --tags
if ($LASTEXITCODE -ne 0) { throw "git fetch ist fehlgeschlagen." }

git show-ref --verify --quiet "refs/tags/$tag"
if ($LASTEXITCODE -eq 0) { throw "Der Tag $tag existiert bereits." }

git rev-parse --verify $sourceBranch *> $null
if ($LASTEXITCODE -ne 0) { throw "Der Quellbranch $sourceBranch wurde nicht gefunden." }

git merge-base --is-ancestor origin/master $sourceBranch
if ($LASTEXITCODE -ne 0) {
    throw "origin/master ist nicht Fast-Forward-kompatibel. Den Branch zuerst bewusst aktualisieren."
}

gh auth status --hostname github.com
if ($LASTEXITCODE -ne 0) { throw "GitHub CLI ist nicht bei github.com angemeldet." }

git switch master
if ($LASTEXITCODE -ne 0) { throw "Wechsel auf master fehlgeschlagen." }

git pull --ff-only origin master
if ($LASTEXITCODE -ne 0) { throw "master konnte nicht per Fast-Forward aktualisiert werden." }

git merge --ff-only $sourceBranch
if ($LASTEXITCODE -ne 0) { throw "Der Quellbranch konnte nicht per Fast-Forward übernommen werden." }

.\build.ps1 -Task All -Version $version
if ($LASTEXITCODE -ne 0) { throw "Der vollständige Release-Build ist fehlgeschlagen." }

$installer = ".\ZeitstrahlStudio-$version-win-x64-setup.exe"
$portable = ".\artifacts\release\ZeitstrahlStudio-$version-win-x64-portable.zip"
$portableHashFile = "$portable.sha256"
$checksums = ".\artifacts\release\checksums.txt"
foreach ($artifact in @($installer, $portable, $portableHashFile, $checksums)) {
    if (-not (Test-Path -LiteralPath $artifact -PathType Leaf)) {
        throw "Release-Artefakt fehlt: $artifact"
    }
}

$installerHashFile = ".\artifacts\release\ZeitstrahlStudio-$version-win-x64-setup.exe.sha256"
$installerHash = Get-FileHash -LiteralPath $installer -Algorithm SHA256
$portableHash = Get-FileHash -LiteralPath $portable -Algorithm SHA256
"$($installerHash.Hash)  $(Split-Path -Leaf $installer)" | Set-Content -LiteralPath $installerHashFile -Encoding utf8
@(
    "$($installerHash.Hash)  $(Split-Path -Leaf $installer)"
    "$($portableHash.Hash)  $(Split-Path -Leaf $portable)"
) | Set-Content -LiteralPath $checksums -Encoding utf8

git tag -a $tag -m "Release Version $version"
if ($LASTEXITCODE -ne 0) { throw "Der Release-Tag konnte nicht erstellt werden." }

git push --atomic origin master $tag
if ($LASTEXITCODE -ne 0) { throw "Der atomare Push von master und Tag ist fehlgeschlagen." }

gh release create $tag `
    $installer `
    $installerHashFile `
    $portable `
    $portableHashFile `
    $checksums `
    --repo $repository `
    --verify-tag `
    --title "Zeitstrahl Studio $version" `
    --generate-notes `
    --latest
if ($LASTEXITCODE -ne 0) { throw "Das GitHub-Release konnte nicht erstellt werden." }
```

## Fehlerbehebung

### Inno Setup nicht gefunden

Installieren Sie Inno Setup 6 und stellen Sie sicher, dass `iscc.exe` im `PATH` oder unter `C:\Program Files (x86)\Inno Setup 6\iscc.exe` erreichbar ist. Wiederholen Sie danach den vollständigen Build.

### Release-Befehl stoppt wegen Änderungen unter `samples/`

Das ist beabsichtigt: Diese Dateien werden verteilt. Prüfen Sie die lokalen Änderungen und entscheiden Sie selbst, ob sie Teil von 1.0.0 werden, in einen eigenen Commit gehören oder außerhalb des Release-Arbeitsbaums gesichert werden sollen. Der Release-Ablauf verwendet weder `reset --hard` noch Force-Push.

### Build oder Test fehlgeschlagen

Beheben Sie die Ursache und wiederholen Sie denselben fehlgeschlagenen Schritt erst nach einer relevanten Änderung. Release-Artefakte dürfen nur nach vollständig erfolgreicher automatisierter und manueller Abnahme veröffentlicht werden.
