# Release-Anleitung Zeitstrahl Studio

Diese Anleitung beschreibt den vollständigen Release-Prozess für Zeitstrahl Studio.

## Voraussetzungen

- Windows 10 oder 11 (64 Bit)
- .NET SDK 8.x
- PowerShell 5.1 oder höher
- Inno Setup 6 (für den Installer)
- Git

## Release-Prozess

### 1. Repository vorbereiten

```powershell
git status
git pull
```

Stellen Sie sicher, dass der Working Tree clean ist.

### 2. Vollständigen Build durchführen

```powershell
.\build.ps1 -Task All -Version 0.2.1
```

Dieser Befehl führt alle Build-, Test-, Publish- und Paketierungsschritte aus.

### 3. Ergebnisse prüfen

Nach erfolgreichem Build befinden sich folgende Artefakte im Projekt-Hauptverzeichnis und in `artifacts\release`:

- `ZeitstrahlStudio-0.2.1-win-x64-setup.exe` (im Projekt-Hauptverzeichnis, direkt auffindbar)
- `artifacts\release\ZeitstrahlStudio-0.2.1-win-x64-portable.zip`
- `artifacts\release\ZeitstrahlStudio-0.2.1-win-x64-portable.zip.sha256`
- `artifacts\release\checksums.txt`

### 4. Manuelle Release-Checkliste

Die folgenden Prüfungen können nicht vollständig automatisiert werden:

- [ ] UI-Abnahme bei 100/125/150/200 Prozent Skalierung
- [ ] Tastaturbedienung durch alle Dialoge
- [ ] Kontrast und visuelle DPI-Abnahme
- [ ] Standalone-HTML-Export in Edge, Firefox, Chrome
- [ ] Druckvorschau des PDF-Exports
- [ ] Reale mehrgigabytegroße Archive importieren/speichern/sichern
- [ ] Ausgewählte Dateisystemfehler simulieren

Details finden Sie in `MANUAL_RELEASE_CHECKLIST.md`.

### 5. Dokumentation aktualisieren

Stellen Sie sicher, dass folgende Dokumente aktuell sind:

- `README.md`
- `BUILD.md`
- `USER_GUIDE.md`
- `PRIVACY.md`
- `CHANGELOG.md`
- `THIRD_PARTY_LICENSES.md`
- `STATUS.md`

### 6. Git-Tag erstellen

```powershell
git tag -a v0.2.1 -m "Release Version 0.2.1"
```

### 7. Release-Artefakte verteilen

Die folgenden Dateien können verteilt werden:

- `ZeitstrahlStudio-0.2.1-win-x64-setup.exe` (im Projekt-Hauptverzeichnis)
- `artifacts\release\ZeitstrahlStudio-0.2.1-win-x64-portable.zip`
- `artifacts\release\checksums.txt`

## Fehlerbehebung

### Inno Setup nicht gefunden

Falls der Installer nicht erstellt werden kann, installieren Sie Inno Setup 6:

https://jrsoftware.org/isinfo.php

Stellen Sie sicher, dass `iscc.exe` im PATH liegt oder unter `C:\Program Files (x86)\Inno Setup 6\iscc.exe` erreichbar ist.

### Build-Fehler

Prüfen Sie die Fehlercodes in `BUILD.md`. Bei wiederkehrenden Fehlern dokumentieren Sie die Ursache in `STATUS.md`.

## Verantwortlichkeiten

- Der Release-Verantwortliche prüft alle automatisierten und manuellen Checklisten.
- Keine Release-Artefakte ohne erfolgreiche Abschlussprüfung verteilen.
- Alle bekannten Einschränkungen müssen in `STATUS.md` dokumentiert sein.
