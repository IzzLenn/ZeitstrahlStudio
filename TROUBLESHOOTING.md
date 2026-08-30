# Fehlerbehebung und bekannte Probleme

Dieses Dokument ist der kanonische Ort für Fehlerhilfe und bekannte Grenzen von Zeitstrahl Studio 1.0.0. Der datierte QA-Stand ist in [`STATUS.md`](STATUS.md) eingeordnet.

## Vor jeder Diagnose

1. Das betroffene `.zeitprojekt`-Archiv nicht manuell verändern oder neu packen.
2. Wenn möglich eine Dateikopie und über `Werkzeuge > Sicherungen > Jetzt sichern` eine manuelle Sicherung erstellen.
3. Genaue Fehlermeldung, ausgeführte Aktion, Version aus `Hilfe > Info zu Zeitstrahl Studio` und betroffenen Dateinamen notieren.
4. Einen Fehler zuerst mit einer Kopie oder dem frei erfundenen Beispielprojekt eingrenzen. Vertrauliche Projektarchive oder Logs nicht ungeprüft weitergeben.

## Start und Projekt öffnen

### Anwendung startet nicht

- Unterstützt sind Windows 10 und 11 x64.
- Die portable ZIP vollständig entpacken; `ZeitstrahlStudio.App.exe` nicht innerhalb des ZIP starten.
- Fehlen EXE oder Laufzeitdateien, ein vollständiges selbstenthaltendes Releasepaket erneut bereitstellen beziehungsweise entpacken.
- Bei einem Entwicklerstart die Schritte in [`BUILD.md`](BUILD.md) verwenden.

### Projekt wird nicht geöffnet

Zeitstrahl Studio prüft Manifest, Formatversion, sichere relative Pfade, Größen, Kompressionsverhältnis, freien Speicher und SHA-256-Prüfsummen. Eine konkrete Meldung wie „Prüfsumme stimmt nicht“, „Datei fehlt“, „Format wird nicht unterstützt“ oder „neueres Schema“ bezeichnet einen Integritäts- oder Kompatibilitätsfehler und ist kein Anlass, das ZIP manuell zu reparieren.

- Mit einer unveränderten Kopie des letzten gültigen Archivs erneut versuchen.
- Eine Sicherung über `Werkzeuge > Sicherungen` wiederherstellen, falls das Projekt noch geöffnet werden kann.
- Bei einem fehlenden Eintrag unter „Zuletzt verwendet“ den tatsächlichen Pfad über `Datei > Projekt öffnen` wählen. Ein nicht mehr vorhandener Recent-Pfad wird beim Öffnungsversuch entfernt.
- Ein beschädigtes Archiv nicht über ein vorhandenes gültiges Ziel speichern.

### Recovery nach Absturz

Der Startbildschirm zeigt nur verwaiste, prüfbare Arbeitskopien. `Wiederherstellen` öffnet die Kopie; danach sofort speichern und das Ergebnis kontrollieren. `Verwerfen` entfernt sie nach Bestätigung. Erscheint kein Kandidat, wurde keine gültige verwaiste Arbeitskopie gefunden. Recovery ersetzt keine Sicherung.

## Bestätigte Darstellungsfehler

### BUG-001: Timeline wird bei kleinerem Fenster leer

Bestätigt, Schweregrad mittel. Nach dem Verkleinern von einem großen Fenster auf 1280×760 kann die horizontale wie vertikale Timeline vollständig leer erscheinen, obwohl Ereigniszahl und Liste geladen bleiben.

Umgehung: Fenster wieder vergrößern. Die Timeline erscheint dabei ohne erneutes Laden wieder. Bis zur Behebung die Ereignisliste zur Auswahl verwenden und die Timeline nicht in dieser Fenstergröße beurteilen.

### BUG-002: Frist- und Achsenbeschriftungen überlappen

Bestätigt, Schweregrad niedrig. Rote Frist- und Achsenunterbrechungsbeschriftungen können in der WPF-Timeline ineinanderlaufen. Zoom, Orientierung, Lückenkompression oder ein größerer Ausschnitt können die Lesbarkeit verbessern, sind aber keine garantierte Behebung. Im geprüften PDF trat der Fehler nicht auf; für den Export ist die jeweilige Vorschau maßgeblich.

Eine zeitweise vermutete Verzögerung beim Beenden ist nicht bestätigt und wird daher nicht als Bug geführt.

## OCR und Dokumentanalyse

### Deutsche OCR ist nicht verfügbar

Die OCR verwendet ausschließlich die lokale deutsche Windows-Texterkennung. In den Windows-Spracheinstellungen das deutsche Sprachpaket einschließlich Texterkennung installieren und die Anwendung neu starten. Ohne diese Ressource können PDF-Text, DOCX und XLSX je nach Inhalt weiterhin analysierbar sein; bildbasierter Text wird nicht zuverlässig erkannt.

OCR-Ergebnisse sind potenziell fehlerhaft. Sie lassen sich im Analysedialog prüfen, aber nicht automatisch in Ereignisfelder übernehmen.

### Analyse fehlt in der Suche

Das Ereignis auswählen, `Werkzeuge > Dokumente analysieren` ausführen und den Abschluss abwarten. Nur gespeicherter extrahierter Text wird durchsucht. Unterstützt sind PDF, PNG/JPEG/TIFF/BMP, DOCX und XLSX; andere Dateien bleiben transportierbare Anhänge ohne optimierte Analyse.

## Anhänge und Integritätsfehler

Bei „Datei fehlt“, Größenabweichung, Prüfsummenfehler oder unsicherem Pfad wird die Projektkopie nicht geöffnet oder exportiert. Eine nachträglich veränderte interne Datei darf nicht durch manuelles Neuberechnen einer Prüfsumme „repariert“ werden. Wenn die vertrauenswürdige Originaldatei noch existiert, den fehlerhaften Anhang aus dem Ereignis entfernen, erneut importieren, Ergebnis prüfen und unter einem neuen Archivnamen speichern.

Der Doppelklick blockiert die Erweiterungen `.appref-ms`, `.application`, `.appx`, `.bat`, `.cmd`, `.com`, `.cpl`, `.exe`, `.hta`, `.jse`, `.js`, `.lnk`, `.msi`, `.msix`, `.msp`, `.pif`, `.ps1`, `.psd1`, `.psm1`, `.reg`, `.scf`, `.scr`, `.url`, `.vbe`, `.vbs`, `.wsf` und `.wsh`. Das ist beabsichtigt. Die bewusste Schaltfläche `Öffnen` kann die Datei nach Integritätsprüfung dennoch an Windows übergeben; nur bei eindeutig vertrauenswürdigem Inhalt verwenden.

## PDF-Vorschau und PDF-Export

- `Werkzeuge > Als PDF exportieren` öffnen und nach jeder Optionsänderung `Vorschau aktualisieren` wählen.
- Für benutzerdefinierte Seiten müssen Breite und Höhe zwischen 50 und 5.080 mm liegen; bei einem Zeitraum müssen Anfang und Ende vollständig und korrekt geordnet sein.
- Bei Problemen mit einer großen Einzelseite auf A4/A3 und `Mehrseitiger Export` wechseln. Seiten über 1.000 mm werden nicht von jedem Betrachter oder Drucker zuverlässig verarbeitet.
- `Extern prüfen` öffnet die erzeugte Vorschau im Windows-Standardprogramm. Schlägt das fehl, zunächst Speicherort, Berechtigungen und Standard-PDF-Anwendung prüfen.
- Manuelle WPF-Kartenpositionen, aktueller Zoom und Lückenkompression werden nicht pixelgenau übernommen. Die PDF-Vorschau zeigt das maßgebliche Exportlayout.

## HTML und HTML-ZIP

- Eine einzelne `.html`-Datei kann direkt lokal geöffnet werden. Sie enthält keine vollständigen Dokumentkopien.
- Ein ZIP mit Dokumentkopien vollständig in einen Ordner entpacken und erst dort `index.html` öffnen. Innerhalb des ZIP funktionieren relative Dokumentlinks nicht zuverlässig.
- Fehlen Dokumentlinks, beim Export `Alle hinterlegten Dokumente als Kopien mitgeben` aktivieren und Integritätsfehler der Anhänge beheben.
- Externe HTTP(S)-Links fragen vor dem Öffnen nach. Browser- oder Windows-Sicherheitsregeln können das Öffnen lokaler Dokumente zusätzlich einschränken.
- HTML ist eine eigenständige Momentaufnahme mit eigenem Layout. Änderungen darin werden nicht ins Projekt zurückgeschrieben.

## Sicherungen und Wiederherstellung

Sicherungen liegen unter `%LocalAppData%\Zeitstrahl Studio\Backups`. Im Dialog `Werkzeuge > Sicherungen` zunächst `Aktualisieren` wählen. Standardmäßig bleiben 6 Sicherungen des aktuellen Tages, 7 tägliche und 8 wöchentliche automatische Sicherungen erhalten; manuelle Sicherungen werden nicht rotiert.

Vor einer Wiederherstellung erstellt die Anwendung eine manuelle Sicherheitssicherung. Nach der Wiederherstellung das Projekt prüfen und ausdrücklich speichern. Bei Prüfsummen- oder Dateifehlern eine andere Sicherung verwenden; Sicherungsdateien nicht manuell verändern.

## Technische Protokolle

Technische Logs liegen ausschließlich im Dateisystem unter `%LocalAppData%\Zeitstrahl Studio\Logs`, beginnend mit `application.log.jsonl` und rotierenden Vorgängern. Die Voreinstellung begrenzt sie auf fünf Dateien zu je etwa 5 MiB. In der WPF-Oberfläche gibt es dafür keine Anzeige-, Export- oder Löschfunktion; `Werkzeuge > Protokoll` zeigt stattdessen das fachliche Auditprotokoll des geöffneten Projekts.

JSONL-Dateien können Fehlermeldungen, technische Details und lokale Pfade enthalten. Vor Weitergabe kopieren, auf sensible Angaben prüfen und nur den benötigten Ausschnitt bereitstellen. Dokumentinhalte werden nicht automatisch vollständig protokolliert, dennoch sind Logs als potenziell vertraulich zu behandeln.

## Belegte Produktgrenzen

Die folgenden Punkte sind aktuelle Funktionsgrenzen, keine neu entdeckten Fehler:

- Nach dem Erstellen gibt es keine Oberfläche zum Bearbeiten von Projektuntertitel, Projektbeschreibung oder übergreifenden Projektdaten.
- Analyseergebnisse und Datumsfundstellen sind nur lesbar und durchsuchbar; sie lassen sich nicht automatisch in Ereignisfelder übernehmen.
- Autosave läuft fest alle 60 Sekunden und ist in der Oberfläche nicht konfigurierbar.
- Technische JSONL-Logs besitzen keine WPF-Anzeige-, Export- oder Löschfunktion.
- Gespeicherte Webseitenlinks besitzen in der WPF-Anwendung keinen belegten direkten Öffnen-Befehl.
- `.zeitprojekt` ist bereits die Transferdatei; es gibt keinen separaten Projekt-Export/-Import und keinen sichtbaren Projektlöschen- oder Projektordner-öffnen-Befehl.
- Beim Entfernen eines Anhangs wird zunächst seine Zuordnung gelöst. Die physische Projektkopie kann für Undo und Sitzungshistorie im Arbeitsstand und später im Archiv verbleiben; Entfernen garantiert daher keine sofortige Verkleinerung des Archivs.

## Entwicklerprobleme

Restore-, Compiler-, Test-, Publish- und Inno-Setup-Probleme gehören in die [`BUILD.md`](BUILD.md). Produktfehler und Releasegates sind im [`STATUS.md`](STATUS.md) eingeordnet.
