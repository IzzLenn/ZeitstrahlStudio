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

Dieser Befehl führt Restore, Debug-/Release-Build und -Tests, Formatprüfung, selbstenthaltenden `win-x64`-Publish, portable ZIP, Prüfsummen und Installer aus. Fehlt Inno Setup oder kann kein frischer Installer erzeugt werden, schlägt der Build bewusst fehl.

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

Der folgende wiederanlaufbare Block prüft Arbeitsbaum, Version, Inno Setup, Tag, Fast-Forward-Kompatibilität, GitHub-Anmeldung, Build und frisch erzeugte Artefakte. Danach aktualisiert er `master` ohne Force-Push, pusht `master` und `v1.0.0` atomar und erstellt oder vervollständigt das GitHub-Release.

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

$iscc = Get-Command "iscc" -ErrorAction SilentlyContinue
if (-not $iscc) {
    $iscc = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe"
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $iscc) {
    throw "Inno Setup 6 wurde nicht gefunden. Der Release-Build wird nicht ohne frischen Installer fortgesetzt."
}

git fetch origin --prune --tags
if ($LASTEXITCODE -ne 0) { throw "git fetch ist fehlgeschlagen." }

git rev-parse --verify "$sourceBranch^{commit}" *> $null
if ($LASTEXITCODE -ne 0) { throw "Der Quellbranch $sourceBranch wurde nicht gefunden." }
$sourceCommit = (git rev-parse "$sourceBranch^{commit}").Trim()

git merge-base --is-ancestor origin/master $sourceCommit
if ($LASTEXITCODE -ne 0) {
    throw "origin/master ist nicht Fast-Forward-kompatibel. Den Branch zuerst bewusst aktualisieren."
}

git show-ref --verify --quiet "refs/tags/$tag"
$tagExists = $LASTEXITCODE -eq 0
if ($tagExists) {
    $tagCommit = (git rev-list -n 1 $tag).Trim()
    if ($tagCommit -ne $sourceCommit) {
        throw "Der vorhandene Tag $tag zeigt nicht auf den freizugebenden Commit $sourceCommit."
    }
}

gh auth status --hostname github.com
if ($LASTEXITCODE -ne 0) { throw "GitHub CLI ist nicht bei github.com angemeldet." }

git switch master
if ($LASTEXITCODE -ne 0) { throw "Wechsel auf master fehlgeschlagen." }
git pull --ff-only origin master
if ($LASTEXITCODE -ne 0) { throw "master konnte nicht per Fast-Forward aktualisiert werden." }
git merge --ff-only $sourceBranch
if ($LASTEXITCODE -ne 0) { throw "Der Quellbranch konnte nicht per Fast-Forward übernommen werden." }
$releaseCommit = (git rev-parse "HEAD^{commit}").Trim()
if ($releaseCommit -ne $sourceCommit) {
    throw "master zeigt nach dem Merge nicht exakt auf den geprüften Quellcommit."
}

$installer = ".\ZeitstrahlStudio-$version-win-x64-setup.exe"
$portable = ".\artifacts\release\ZeitstrahlStudio-$version-win-x64-portable.zip"
$portableHashFile = "$portable.sha256"
$installerHashFile = ".\artifacts\release\ZeitstrahlStudio-$version-win-x64-setup.exe.sha256"
$checksums = ".\artifacts\release\checksums.txt"
foreach ($oldArtifact in @($installer, $portable, $portableHashFile, $installerHashFile, $checksums)) {
    if (Test-Path -LiteralPath $oldArtifact -PathType Leaf) {
        Remove-Item -LiteralPath $oldArtifact -Force
    }
}
$buildStartedUtc = [DateTime]::UtcNow

.\build.ps1 -Task All -Version $version
if ($LASTEXITCODE -ne 0) { throw "Der vollständige Release-Build ist fehlgeschlagen." }

foreach ($artifact in @($installer, $portable, $portableHashFile, $checksums)) {
    if (-not (Test-Path -LiteralPath $artifact -PathType Leaf)) {
        throw "Frisch erwartetes Release-Artefakt fehlt: $artifact"
    }
    if ((Get-Item -LiteralPath $artifact).LastWriteTimeUtc -lt $buildStartedUtc.AddSeconds(-2)) {
        throw "Release-Artefakt ist nicht frisch erzeugt worden: $artifact"
    }
}

$installerHash = Get-FileHash -LiteralPath $installer -Algorithm SHA256
$portableHash = Get-FileHash -LiteralPath $portable -Algorithm SHA256
"$($installerHash.Hash)  $(Split-Path -Leaf $installer)" | Set-Content -LiteralPath $installerHashFile -Encoding utf8
"$($portableHash.Hash)  $(Split-Path -Leaf $portable)" | Set-Content -LiteralPath $portableHashFile -Encoding utf8
@(
    "$($installerHash.Hash)  $(Split-Path -Leaf $installer)"
    "$($portableHash.Hash)  $(Split-Path -Leaf $portable)"
) | Set-Content -LiteralPath $checksums -Encoding utf8

if (-not $tagExists) {
    git tag -a $tag -m "Release Version $version"
    if ($LASTEXITCODE -ne 0) { throw "Der Release-Tag konnte nicht erstellt werden." }
}

git push --atomic origin master $tag
if ($LASTEXITCODE -ne 0) {
    throw "Der atomare Push ist fehlgeschlagen. Der korrekte lokale Tag bleibt für einen sicheren Wiederholungsversuch erhalten."
}

$releaseAssets = @($installer, $installerHashFile, $portable, $portableHashFile, $checksums)
gh release view $tag --repo $repository *> $null
$releaseExists = $LASTEXITCODE -eq 0
if ($releaseExists) {
    gh release upload $tag @releaseAssets --repo $repository --clobber
    if ($LASTEXITCODE -ne 0) { throw "Die Release-Artefakte konnten nicht vervollständigt werden." }
    gh release edit $tag --repo $repository --verify-tag --title "Zeitstrahl Studio $version" --latest
    if ($LASTEXITCODE -ne 0) { throw "Das vorhandene GitHub-Release konnte nicht aktualisiert werden." }
}
else {
    gh release create $tag @releaseAssets --repo $repository --verify-tag --title "Zeitstrahl Studio $version" --generate-notes --latest
    if ($LASTEXITCODE -ne 0) { throw "Das GitHub-Release konnte nicht erstellt werden. Derselbe Block kann erneut ausgeführt werden." }
}

```

## Fehlerbehebung

### Inno Setup nicht gefunden

Installieren Sie Inno Setup 6 und stellen Sie sicher, dass `iscc.exe` im `PATH`, unter `C:\Program Files (x86)\Inno Setup 6\iscc.exe` oder unter `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe` erreichbar ist. `BuildInstaller` und `All` schlagen ohne Compiler bewusst fehl.

### Release-Befehl stoppt wegen Änderungen unter `samples/`

Das ist beabsichtigt: Diese Dateien werden verteilt. Prüfen Sie die lokalen Änderungen und entscheiden Sie selbst, ob sie Teil von 1.0.0 werden, in einen eigenen Commit gehören oder außerhalb des Release-Arbeitsbaums gesichert werden sollen. Der Release-Ablauf verwendet weder `reset --hard` noch Force-Push.

### Push oder GitHub-Release wurde nur teilweise abgeschlossen

Derselbe Block darf erneut ausgeführt werden. Ein bereits vorhandener lokaler Tag wird nur akzeptiert, wenn er auf exakt denselben geprüften Quellcommit zeigt; ein bereits veröffentlichtes Release wird mit den frisch erzeugten Artefakten vervollständigt. Abweichende Tags werden nicht überschrieben und Force-Push wird nicht verwendet.

### Build oder Test fehlgeschlagen

Beheben Sie die Ursache und wiederholen Sie denselben fehlgeschlagenen Schritt erst nach einer relevanten Änderung. Release-Artefakte dürfen nur nach vollständig erfolgreicher automatisierter und manueller Abnahme veröffentlicht werden.
