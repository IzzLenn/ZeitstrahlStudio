# Zeitstrahl Studio

Zeitstrahl Studio ist eine deutschsprachige Desktopanwendung für chronologische Projekte. Sie läuft als .NET-8-/WPF-Anwendung auf Windows 10 und Windows 11 x64 und verarbeitet Projektdaten lokal: ohne Cloud-Synchronisation, Telemetrie oder automatische Datenübertragung.

## Funktionen

- Ereignisse mit unvollständigen oder genauen Datumsangaben, Zeiträumen, Fristen, Priorität, Status, Tags, Links, Quellen und Notizen verwalten
- Projekte horizontal oder vertikal als zoombaren Zeitstrahl darstellen, große Zeitlücken komprimieren und Karten manuell anordnen
- Dateien kollisionsfrei in das Projekt übernehmen, sicher prüfen und mit dem Windows-Standardprogramm öffnen
- PDF-, Bild-, DOCX- und XLSX-Anhänge lokal analysieren; Bilder können mit der lokalen Windows-OCR verarbeitet werden
- Projektweit im erkannten Text und in Ereignisdaten suchen und Filter kombinieren
- Projekte als prüfsummenvalidierte `.zeitprojekt`-Archive speichern und übertragen; Autosave, Wiederherstellung, Sicherungen, Auditprotokoll sowie Undo/Redo nutzen
- Zeitstrahlen als PDF oder als eigenständiges Offline-HTML exportieren; optional entsteht ein HTML-ZIP-Paket mit validierten Dokumentkopien

## Installieren und starten

Für Endnutzer sind zwei selbstenthaltende `win-x64`-Releaseausgaben vorgesehen; eine separate .NET-Installation ist dafür nicht nötig. Verwenden Sie ein vom Herausgeber bereitgestelltes Paket:

- Installer: `ZeitstrahlStudio-<Version>-win-x64-setup.exe` ausführen. Der Installer kann die Dateizuordnung für `.zeitprojekt` einrichten.
- Portable Version: `ZeitstrahlStudio-<Version>-win-x64-portable.zip` in einen neuen Ordner entpacken und `ZeitstrahlStudio.App.exe` starten.

Diese Dateien werden beim Release-Build erzeugt und gehören nicht zu einem frischen Quellcode-Checkout. Für Version 1.1.1 werden Portable-ZIP und Installer aus demselben geprüften Commit erzeugt. Das GitHub Release enthält ausschließlich diese beiden Binärartefakte; SHA-256-Werte und Checksummendateien bleiben lokale Prüfevidenz.

Start aus dem Quellcode:

```powershell
dotnet restore ZeitstrahlStudio.sln
dotnet run --project src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj
```

Für die Entwicklung wird das .NET SDK 8.x benötigt. Build, Tests, Publish und Paketierung beschreibt die [Build-Anleitung](BUILD.md).

## Schnellstart

1. `Datei > Neues Projekt` wählen, einen Projektnamen eingeben und das neue `.zeitprojekt`-Archiv speichern.
2. Über `Ereignis > Hinzufügen` ein Ereignis mit Titel und Datum anlegen.
3. Optional Anhänge hinzufügen und unterstützte Dokumente über `Werkzeuge > Dokumente analysieren` lokal auswerten.
4. Weitere Ereignisse erfassen, die Suche beziehungsweise Filter verwenden und zwischen horizontaler und vertikaler Ansicht wechseln.
5. Mit `Datei > Speichern` sichern. Das `.zeitprojekt`-Archiv ist bereits die vollständige übertragbare Projektdatei.
6. Bei Bedarf über `Werkzeuge` eine PDF- oder HTML-Ausgabe erzeugen.

Zum Ausprobieren kann [`samples/ZeitstrahlStudio-Beispiel.zeitprojekt`](samples/ZeitstrahlStudio-Beispiel.zeitprojekt) direkt geöffnet werden. Inhalt, Lizenz und reproduzierbare Erzeugung des frei erfundenen Beispiels stehen in [`samples/README.md`](samples/README.md).

## Projektstatus

Der aktuelle Quellstand ist Version 1.1.1 auf Branch `release/1.1.1`; der Release wird mit dem annotierten Tag `v1.1.1` aus dem geprüften Commit erstellt. Build- und Releaseevidenz ist dem unveränderlichen Tag und den lokal protokollierten Artefakthashes zuzuordnen.

Vor einer Distribution bleiben die [manuellen Releaseprüfungen](MANUAL_RELEASE_CHECKLIST.md) vollständig abzuschließen und das [Drittanbieter-Lizenzbündel](THIRD_PARTY_LICENSES.md) um noch fehlende Originaltexte zu ergänzen. Für 1.1.1 kopiert `PackagePortable` die Root-`LICENSE.txt` in die Portable-ZIP, und der Installer führt sie ohne bedingten Installations-Check mit. Beide geschlossenen Artefakte sind dennoch vor Distribution inhaltlich zu verifizieren. Zwei bestätigte Darstellungsfehler sind offen: Bei 1280×760 kann die Timeline nach dem Verkleinern leer erscheinen (BUG-001, mittel), und rote Frist-/Achsenbeschriftungen können überlappen (BUG-002, niedrig). Details und Umgehungen stehen in der [Fehlerbehebung](TROUBLESHOOTING.md); die datierte QA-Evidenz ist im [Projektstatus](STATUS.md) eingeordnet.

## Dokumentation

| Dokument | Inhalt |
| --- | --- |
| [Benutzerhandbuch](USER_GUIDE.md) | Bedienung, Projektabläufe, Anhänge, Suche, Sicherungen und Exporte |
| [Fehlerbehebung und bekannte Probleme](TROUBLESHOOTING.md) | Häufige Fehler, Diagnose und bestätigte Einschränkungen |
| [Build-Anleitung](BUILD.md) | Entwicklungsumgebung, Build, Tests, Publish und Paketierung |
| [Architektur](ARCHITECTURE.md) | Schichten, Komponenten, Datenflüsse und technische Grenzen |
| [Projektformat](PROJECT_FORMAT.md) | Struktur und Sicherheitsregeln von `.zeitprojekt` |
| [Projektstatus](STATUS.md) | Aktueller Snapshot und historisches Entwicklungsjournal |
| [Spezifikation](SPEC.md) | Normative Anforderungen; kein Implementierungsnachweis |
| [Architekturentscheidungen](DECISIONS.md) | Dauerhafte technische Entscheidungen |
| [Datenschutz](PRIVACY.md) | Lokale Verarbeitung, gespeicherte Daten und externe Übergaben |
| [Drittanbieter-Lizenzen](THIRD_PARTY_LICENSES.md) | Komponenten, Versionen und Lizenzstatus |
| [Release-Anleitung](RELEASE.md) | Kontrollierter lokaler und öffentlicher Releaseablauf |
| [Manuelle Release-Checkliste](MANUAL_RELEASE_CHECKLIST.md) | Noch auszuführende visuelle und plattformbezogene Prüfungen |
| [Änderungshistorie](CHANGELOG.md) | Versionsbezogene Änderungen |

## Lizenz

Der Projektquellcode steht unter der [MIT-Lizenz](LICENSE.txt). Für mitgelieferte Komponenten und Beispieldaten gelten zusätzlich die jeweils ausgewiesenen Lizenzhinweise.
