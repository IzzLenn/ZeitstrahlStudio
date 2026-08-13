#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
    Reproduzierbarer Build, Test, Formatprüfung und Publish für Zeitstrahl Studio.

.DESCRIPTION
    Dieses Skript führt Restore, Debug-/Release-Builds, Tests, Formatprüfung und
    selbstenthaltenden win-x64-Publish aus. Es verwendet explizite Fehlercodes,
    damit CI/CD-Pipelines den Buildzustand zuverlässig erkennen können.

.PARAMETER Task
    Auszuführende Aufgabe:
    - Restore          NuGet-Pakete wiederherstellen
    - BuildDebug       Debug-Build
    - BuildRelease     Release-Build
    - TestDebug        Debug-Tests
    - TestRelease      Release-Tests
    - FormatCheck      Formatprüfung
    - Publish          win-x64-Publish erzeugen
    - BuildInstaller   Inno-Setup-Installer erzeugen (Inno Setup erforderlich)
    - PackagePortable  Portable ZIP erzeugen
    - All              Vollständige Release-Kette

.PARAMETER Configuration
    Build-Konfiguration für Build-/Testaufgaben (Debug oder Release).

.PARAMETER Version
    Versionsnummer für Release-Artefakte (z. B. "1.0.0").

.EXAMPLE
    .\build.ps1 -Task All -Version 1.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Restore", "BuildDebug", "BuildRelease", "TestDebug", "TestRelease",
                 "FormatCheck", "Publish", "BuildInstaller", "PackagePortable", "All")]
    [string]$Task = "All",

    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

# Fehlercodes
$ExitSuccess = 0
$ExitBuildError = 1
$ExitTestError = 2
$ExitFormatError = 3
$ExitPublishError = 4
$ExitInstallerError = 5
$ExitPackageError = 6
$ExitRestoreError = 7

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Solution = Join-Path $RepoRoot "ZeitstrahlStudio.sln"
$AppProject = Join-Path $RepoRoot "src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj"
$PublishDir = Join-Path $RepoRoot "artifacts\publish\win-x64"
$ReleaseDir = Join-Path $RepoRoot "artifacts\release"
$InstallerDir = Join-Path $RepoRoot "installer"
$LicenseDir = Join-Path $RepoRoot "licenses"

function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Invoke-Step {
    param(
        [string]$Description,
        [scriptblock]$Action,
        [int]$ErrorCode
    )
    Write-Step $Description
    try {
        & $Action
    }
    catch {
        Write-Host "FEHLER bei: $Description" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit $ErrorCode
    }
}

function Step-Restore {
    dotnet restore $Solution
    if ($LASTEXITCODE -ne 0) { throw "Restore fehlgeschlagen" }
}

function Step-BuildDebug {
    dotnet build $Solution -c Debug --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Debug-Build fehlgeschlagen" }
}

function Step-BuildRelease {
    dotnet build $Solution -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Release-Build fehlgeschlagen" }
}

function Step-TestDebug {
    dotnet test $Solution -c Debug --no-restore --no-build
    if ($LASTEXITCODE -ne 0) { throw "Debug-Tests fehlgeschlagen" }
}

function Step-TestRelease {
    dotnet test $Solution -c Release --no-restore --no-build
    if ($LASTEXITCODE -ne 0) { throw "Release-Tests fehlgeschlagen" }
}

function Step-FormatCheck {
    dotnet format $Solution --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Formatprüfung fehlgeschlagen" }
}

function Step-Publish {
    if (Test-Path $PublishDir) {
        Remove-Item -Recurse -Force $PublishDir
    }
    dotnet publish $AppProject -c Release -r win-x64 --self-contained true --no-restore -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "Publish fehlgeschlagen" }

    $exePath = Join-Path $PublishDir "ZeitstrahlStudio.App.exe"
    if (-not (Test-Path $exePath)) {
        throw "Veröffentlichte EXE nicht gefunden: $exePath"
    }

    Get-ChildItem -Path $PublishDir -Filter "*.pdb" -File -Recurse | Remove-Item -Force
}

function Step-BuildInstaller {
    $iscc = Get-Command "iscc" -ErrorAction SilentlyContinue
    if (-not $iscc) {
        $isccPath = @(
            "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe"
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
        if ($isccPath) {
            $iscc = $isccPath
        }
    }

    if (-not $iscc) {
        throw "Inno Setup 6 (iscc.exe) wurde nicht gefunden. Ein vollständiger Release-Build benötigt einen erfolgreich erzeugten Installer."
    }

    $issScript = Join-Path $InstallerDir "ZeitstrahlStudio.iss"
    if (-not (Test-Path $issScript)) {
        throw "Installer-Skript nicht gefunden: $issScript"
    }

    & $iscc /DMyAppVersion=$Version $issScript
    if ($LASTEXITCODE -ne 0) { throw "Installer-Build fehlgeschlagen" }
}

function Step-PackagePortable {
    if (-not (Test-Path $PublishDir)) {
        throw "Publish-Verzeichnis nicht gefunden. Führen Sie zuerst 'Publish' aus."
    }

    if (-not (Test-Path $ReleaseDir)) {
        New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null
    }

    $zipName = "ZeitstrahlStudio-$Version-win-x64-portable.zip"
    $zipPath = Join-Path $ReleaseDir $zipName

    if (Test-Path $zipPath) {
        Remove-Item -Force $zipPath
    }

    # Dokumente in Publish-Verzeichnis kopieren
    $docsToInclude = @(
        "README.md",
        "PRIVACY.md",
        "THIRD_PARTY_LICENSES.md",
        "CHANGELOG.md"
    )

    foreach ($doc in $docsToInclude) {
        $source = Join-Path $RepoRoot $doc
        if (Test-Path $source) {
            Copy-Item $source $PublishDir -Force
        }
    }

    $licenseTarget = Join-Path $PublishDir "licenses"
    if (Test-Path $licenseTarget) { Remove-Item -Recurse -Force $licenseTarget }
    if (-not (Test-Path $LicenseDir)) { throw "Lizenzbündel fehlt: $LicenseDir" }
    Copy-Item -Recurse $LicenseDir $licenseTarget -Force
    Get-ChildItem -Path $PublishDir -Filter "*.pdb" -File -Recurse | Remove-Item -Force

    # Beispielprojekt in Unterverzeichnis kopieren
    $sampleSource = Join-Path $RepoRoot "samples"
    $sampleTarget = Join-Path $PublishDir "samples"
    if (Test-Path $sampleSource) {
        if (Test-Path $sampleTarget) {
            Remove-Item -Recurse -Force $sampleTarget
        }
        Copy-Item -Recurse $sampleSource $sampleTarget -Force
    }

    # ZIP-Datei erzeugen
    Compress-Archive -Path "$PublishDir\*" -DestinationPath $zipPath -Force
    if (-not (Test-Path $zipPath)) {
        throw "ZIP-Datei konnte nicht erzeugt werden"
    }

    # SHA-256-Prüfsummen
    $hash = Get-FileHash -Path $zipPath -Algorithm SHA256
    $hashFile = "$zipPath.sha256"
    "$($hash.Hash)  $zipName" | Out-File -FilePath $hashFile -Encoding utf8

    $checksumsFile = Join-Path $ReleaseDir "checksums.txt"
    "$($hash.Hash)  $zipName" | Out-File -FilePath $checksumsFile -Encoding utf8

    Write-Host "Portable ZIP erstellt: $zipPath" -ForegroundColor Green
    Write-Host "SHA-256: $($hash.Hash)" -ForegroundColor Green
}

# Hauptausführung
switch ($Task) {
    "Restore" { Invoke-Step "Restore" { Step-Restore } $ExitRestoreError }
    "BuildDebug" { Invoke-Step "Debug-Build" { Step-BuildDebug } $ExitBuildError }
    "BuildRelease" { Invoke-Step "Release-Build" { Step-BuildRelease } $ExitBuildError }
    "TestDebug" { Invoke-Step "Debug-Tests" { Step-TestDebug } $ExitTestError }
    "TestRelease" { Invoke-Step "Release-Tests" { Step-TestRelease } $ExitTestError }
    "FormatCheck" { Invoke-Step "Formatprüfung" { Step-FormatCheck } $ExitFormatError }
    "Publish" { Invoke-Step "Publish" { Step-Publish } $ExitPublishError }
    "BuildInstaller" { Invoke-Step "Installer" { Step-BuildInstaller } $ExitInstallerError }
    "PackagePortable" { Invoke-Step "Portable ZIP" { Step-PackagePortable } $ExitPackageError }
    "All" {
        Invoke-Step "Restore" { Step-Restore } $ExitRestoreError
        Invoke-Step "Debug-Build" { Step-BuildDebug } $ExitBuildError
        Invoke-Step "Debug-Tests" { Step-TestDebug } $ExitTestError
        Invoke-Step "Release-Build" { Step-BuildRelease } $ExitBuildError
        Invoke-Step "Release-Tests" { Step-TestRelease } $ExitTestError
        Invoke-Step "Formatprüfung" { Step-FormatCheck } $ExitFormatError
        Invoke-Step "Publish" { Step-Publish } $ExitPublishError
        Invoke-Step "Portable ZIP" { Step-PackagePortable } $ExitPackageError
        Invoke-Step "Installer" { Step-BuildInstaller } $ExitInstallerError
    }
}

Write-Host "`nBuild erfolgreich abgeschlossen." -ForegroundColor Green
exit $ExitSuccess
