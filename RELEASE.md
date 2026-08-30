# Release-Runbook für Zeitstrahl Studio

Dieses Runbook beschreibt eine bewusste, wiederholbare Freigabe. Es automatisiert keine Veröffentlichung und setzt keine CI/CD-Pipeline voraus; im Repository ist keine solche Pipeline belegt. Jeder Tag-, Push- und GitHub-Schritt bleibt eine ausdrücklich freizugebende externe Aktion.

## Releaseziel 1.1.0 am 30.08.2026

- Branch: `ui/redesign-0.3.0`
- HEAD: wird nach Abschluss des Release-Commits festgelegt
- `Directory.Build.props`: Version `1.1.0`
- vorgesehener annotierter Tag `v1.1.0`: zeigt auf den geprüften Release-Commit
- Portable ZIP, Installer und Prüfsummen: werden aus dem sauberen Release-Arbeitsbaum erzeugt und geprüft
- öffentliche Remote-Tag- und GitHub-Release-Veröffentlichung: erfolgt erst nach erfolgreicher Artefaktprüfung
- manuelle Abnahme: nicht als vollständig belegt
- Drittanbieter-Lizenzbündel: laut [`THIRD_PARTY_LICENSES.md`](THIRD_PARTY_LICENSES.md) fehlen noch Original-Lizenz- oder Copyrighttexte
- Portable-Lizenzprüfung: `PackagePortable` kopiert die Root-`LICENSE.txt` für 1.1.0; der geschlossene ZIP-Inhalt wird vor Veröffentlichung geprüft
- Installer-Lizenzprüfung: Das Inno-Skript installiert `{app}\LICENSE.txt` für 1.1.0 ohne bedingten Check; die reale Installation bleibt vor Veröffentlichung zu prüfen

Dieser lokale Befund ist keine uneingeschränkte Releasefreigabe. Die offenen Gates und bestätigten Bugs stehen in [`STATUS.md`](STATUS.md).

## Rollen und Quellen

Vor einer Freigabe ist ein benannter Release-Verantwortlicher erforderlich. Maßgebliche Quellen sind:

| Quelle | Zweck |
| --- | --- |
| `Directory.Build.props` | Assembly-/Produktversion |
| `build.ps1 -Version` | Namen der Releaseartefakte und Installer-Define |
| [`CHANGELOG.md`](CHANGELOG.md) | releasebezogene Änderungen |
| [`STATUS.md`](STATUS.md) | aktueller Ist-, QA- und Bugstand |
| [`THIRD_PARTY_LICENSES.md`](THIRD_PARTY_LICENSES.md) und `licenses/` | Komponenten- und Lizenzbündel |
| [`PRIVACY.md`](PRIVACY.md) | Datenschutz- und Weitergabehinweise |
| [`MANUAL_RELEASE_CHECKLIST.md`](MANUAL_RELEASE_CHECKLIST.md) | vollständige manuelle Abnahme |

Versionswert, Release Notes, Tagziel und Artefaktnamen müssen auf denselben vorgesehenen Commit zeigen. `build.ps1` und das Installer-Skript besitzen Standardwerte, für Releases wird die Version dennoch immer explizit übergeben.

## 1. Preflight

### Commit und Arbeitsbaum festlegen

```powershell
$version = "1.1.0"
$tag = "v$version"
git branch --show-current
git rev-parse HEAD
git status --short
git log -1 --decorate --oneline
```

Vor der Paketierung muss `git status --short` leer sein. Der vorgesehene Commit muss reviewed und vollständig sein. Keine lokalen Arbeitsdaten, temporären Exporte oder uncommitteten Samples dürfen in den Releasebaum gelangen.

`PackagePortable` kopiert den aktuellen `samples`-Arbeitsbaum. Deshalb zusätzlich prüfen:

```powershell
git diff -- samples
git status --short -- samples
```

Die zu verteilenden Beispiele müssen exakt dem bewussten HEAD beziehungsweise einem ausdrücklich freigegebenen Stand entsprechen. Nicht mit `reset --hard` oder Force-Operationen bereinigen; lokale Arbeit vorher geordnet sichern oder in einen separaten Arbeitsbaum wechseln.

### Version, Dokumente und Lizenzen

- Version in `Directory.Build.props`, Changelog-Überschrift, Release Notes und `-Version` abgleichen.
- README, Datenschutz, Drittanbieterübersicht und bekannte Probleme auf den Zielcommit beziehen.
- Für jede ausgelieferte Produktionskomponente vollständige erforderliche Lizenz-/Copyrighttexte unter `licenses/` bereitstellen.
- Bestätigte BUG-001/BUG-002 und alle offenen Risiken besitzen eine dokumentierte Freigabeentscheidung.

### Werkzeuge

```powershell
dotnet --info
git --version
Get-Command iscc -ErrorAction SilentlyContinue
```

Erforderlich sind Windows 10/11 x64, .NET SDK 8.x, PowerShell, Git und Inno Setup 6. Falls `iscc` nicht im PATH liegt, sucht `build.ps1` zusätzlich in den dokumentierten Standardpfaden. Für eine spätere GitHub-Veröffentlichung müssen Zugang und Berechtigungen separat geprüft werden; GitHub CLI ist nur nötig, wenn sie bewusst verwendet wird.

Für den direkten Einzel-Task `Publish` die RID-spezifischen Runtimepacks für den self-contained Publish explizit restaurieren:

```powershell
dotnet restore src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -r win-x64
```

Dieser robuste Preflight beruht auf dem Endvalidierungslauf: Nach erfolgreichem normalem Solution-Restore scheiterte der direkte `--no-restore`-Publish mit `NETSDK1112`, weil `Microsoft.NETCore.App.Runtime.win-x64` und `Microsoft.WindowsDesktop.App.Runtime.win-x64` fehlten; der explizite App-RID-Restore behob den Fall. Daraus wird keine allgemeine Aussage über den Solution-Restore abgeleitet. Seit 1.1.0 führt `build.ps1 -Task Restore` den App-Projekt-RID-Restore selbst aus.

## 2. Automatische Gates und Artefakterzeugung

Im sauberen Releasebaum führt die vollständige Kette aus:

```powershell
.\build.ps1 -Task All -Version $version
```

`All` führt RID-Restore, Debug-Build/-Tests, Release-Build/-Tests, Formatprüfung, self-contained `win-x64`-Publish, portable Paketierung und Installer-Build aus. Es benötigt Inno Setup und prüft selbst nicht, ob Git sauber ist. Sauberkeit bleibt daher eine verpflichtende Preflight-Bedingung.

Zur Diagnose kann dieselbe Reihenfolge explizit ausgeführt werden:

```powershell
.\build.ps1 -Task Restore
.\build.ps1 -Task BuildDebug
.\build.ps1 -Task TestDebug
.\build.ps1 -Task BuildRelease
.\build.ps1 -Task TestRelease
.\build.ps1 -Task FormatCheck
.\build.ps1 -Task Publish
.\build.ps1 -Task PackagePortable -Version $version
.\build.ps1 -Task BuildInstaller -Version $version
```

Einzelaufgaben führen ihre Voraussetzungen nicht automatisch aus. Die vollständigen CLI-Entsprechungen stehen in [`BUILD.md`](BUILD.md).

### Erwartete Ausgaben

```text
artifacts\publish\win-x64\
artifacts\release\ZeitstrahlStudio-<Version>-win-x64-portable.zip
artifacts\release\ZeitstrahlStudio-<Version>-win-x64-portable.zip.sha256
artifacts\release\checksums.txt
ZeitstrahlStudio-<Version>-win-x64-setup.exe
```

Die portable ZIP enthält die self-contained Anwendung, Laufzeitabhängigkeiten, README, Datenschutz, Changelog, Drittanbieterübersicht, `licenses/` und die freigegebenen Samples. PDB-Dateien werden entfernt. Diese generierten Ausgaben sind nicht Teil eines frischen Checkouts.

`build.ps1` hasht die portable ZIP. Der Installer muss anschließend separat gehasht und `checksums.txt` bewusst zu einer Gesamtliste erweitert werden, beispielsweise:

```powershell
$portable = "artifacts\release\ZeitstrahlStudio-$version-win-x64-portable.zip"
$installer = "ZeitstrahlStudio-$version-win-x64-setup.exe"
$portableHash = Get-FileHash -LiteralPath $portable -Algorithm SHA256
$installerHash = Get-FileHash -LiteralPath $installer -Algorithm SHA256
$portableHash
$installerHash
```

Die endgültige Checksummendatei muss Dateiname und SHA-256 beider exakt zu veröffentlichenden Dateien enthalten. Vor Freigabe die Hashes aus den geschlossenen Artefakten erneut berechnen, nicht aus einer früheren Ausgabe übernehmen.

## 3. Manuelle Gates

[`MANUAL_RELEASE_CHECKLIST.md`](MANUAL_RELEASE_CHECKLIST.md) wird auf sauberen Windows-10- und Windows-11-x64-Systemen vollständig ausgefüllt. Mindestens umfasst die Freigabe:

- Installer und Portable ohne vorinstallierte .NET-Runtime, Install/Uninstall, Desktopoption und `.zeitprojekt`-Zuordnung
- reale UI-/DPI-/Theme-/Tastaturabnahme bei den festgelegten Auflösungen und Skalierungen
- Projekt-, Ereignis-, Timeline-, Suche-, Attachment-, OCR-, Backup-, Recovery- und Auditabläufe
- alle PDF-Modi und echte Vorschau
- Offline-HTML als Einzeldatei und vollständig entpacktes Dokument-ZIP in Edge, Firefox und Chrome
- Sicherheitsprüfung riskanter Attachments, manipulierte Archive/Projektkopien und atomarer Zielerhalt
- Datenschutz-, Netzwerk-, Pfad-/Log- und Lizenzprüfung
- Portable ZIP erst nach korrigierter Aufnahme der Root-`LICENSE.txt`; beim finalen Installer durch reale Installation verifizieren, dass `{app}\LICENSE.txt` vorhanden und lesbar ist
- 5.000-Ereignisse-Akzeptanz, Abbruchverhalten und getrennte Beobachtung des geordneten Beendens

BUG-001 und BUG-002 werden als Gate-Funde bewertet, nicht still als erwarteter PASS behandelt. Entscheidung, Schweregrad, akzeptierte Umgehung, Verantwortlicher und gegebenenfalls Zielversion sind im Ergebnisprotokoll festzuhalten.

Erst wenn automatische und manuelle Gates, Lizenzbündel, Datenschutzprüfung und Artefakt-Smokes vollständig freigegeben sind, darf der Stand als veröffentlichungsfähig bezeichnet werden.

## 4. Release Notes und Tag

Release Notes sollen mindestens enthalten:

- Version und vollständigen Commit
- Nutzeränderungen seit der Vorversion
- unterstützte Plattform und Paketarten
- bekannte Probleme beziehungsweise deren bewusste Disposition
- SHA-256-Verweis und Datenschutz-/Lizenzhinweise

Vorhandenen Tag prüfen:

```powershell
git show-ref --verify --quiet "refs/tags/$tag"
if ($LASTEXITCODE -eq 0) {
    git show --no-patch --decorate $tag
    git rev-list -n 1 $tag
}
```

Existiert noch kein Tag und sind alle Gates freigegeben, kann der vorgesehene Commit annotiert getaggt werden:

```powershell
git tag -a $tag -m "Zeitstrahl Studio $version"
git rev-list -n 1 $tag
git rev-parse HEAD
```

Tagziel und freigegebener Commit müssen exakt übereinstimmen. Einen bestehenden abweichenden Tag niemals still löschen, verschieben oder per Force ersetzen. Stattdessen Veröffentlichung stoppen und Version beziehungsweise Freigabeverfahren klären.

## 5. Bewusste Veröffentlichung

Branch, Tag und Artefakte werden erst nach gesonderter Freigabe veröffentlicht. Mögliche manuelle Git-Schritte sind:

```powershell
$releaseBranch = git branch --show-current
git push origin $releaseBranch
git push origin $tag
```

Optional kann ein GitHub Release über die Weboberfläche oder bewusst mit `gh release create` angelegt werden. Dabei ausschließlich die geprüften Artefakte, deren endgültige Checksummen und freigegebene Release Notes verwenden. Dieses Runbook behauptet nicht, dass ein Push oder GitHub Release bereits erfolgt ist.

Nach der Veröffentlichung unabhängig prüfen:

- Remote-Tag existiert und zeigt auf den freigegebenen Commit.
- Release-Seite ist öffentlich beziehungsweise im vorgesehenen Sichtbarkeitsbereich erreichbar.
- Installer, portable ZIP und Checksummen sind vollständig herunterladbar.
- Heruntergeladene Dateien stimmen mit den freigegebenen SHA-256-Werten überein.
- Installations- und Portable-Smoke aus den heruntergeladenen, nicht lokalen Dateien bestehen.

## 6. Abschluss und Rücknahme

Nach erfolgreicher Veröffentlichung Version, Commit, Tag, Zeitpunkt, Verantwortliche, Artefakthashes, Checklistenprotokoll und öffentliche Releaseadresse dauerhaft festhalten. [`STATUS.md`](STATUS.md) und [`CHANGELOG.md`](CHANGELOG.md) erst durch eine bewusste nachfolgende Änderung als veröffentlicht kennzeichnen.

Bei einem Fehler nach Veröffentlichung:

1. Verteilung stoppen oder Release sichtbar als problematisch markieren.
2. Betroffene Artefakte und Hashes sichern, damit der Vorfall nachvollziehbar bleibt.
3. Vorhandenen veröffentlichten Tag nicht verschieben.
4. Fehler in einem neuen Commit beheben und für eine neue Versionsnummer sämtliche Gates wiederholen.
5. Nutzer klar über Auswirkungen, sichere Umgehung und Ersatzversion informieren.

Eine lokale fehlgeschlagene Paketierung darf durch erneuten sauberen Build derselben noch unveröffentlichten Version wiederholt werden. Nach einer öffentlichen Freigabe werden gleichnamige Artefakte nicht still ersetzt.
