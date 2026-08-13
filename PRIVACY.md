# Datenschutzerklärung für Zeitstrahl Studio

**Stand:** 13. August 2026

## Zusammenfassung

Zeitstrahl Studio ist eine vollständig lokale Windows-Desktopanwendung. Alle von Ihnen eingegebenen Daten, importierten Dokumente, erzeugten Exporte und Protokolle verbleiben auf Ihrem eigenen Rechner.

## Welche Daten verarbeitet werden

Zeitstrahl Studio verarbeitet ausschließlich Daten, die Sie selbst eingeben oder importieren:

- Projektdaten (Name, Beschreibung, Zeitraum, Einstellungen)
- Ereignisdaten (Titel, Texte, Datum, Fristen, Farben, Schlagwörter)
- Anhänge (PDF, Bilder, DOCX, XLSX und andere Dateien)
- Extrahierte Texte und OCR-Ergebnisse
- Webseitenlinks
- Lokale Anwendungseinstellungen
- Technische Protokolle zur Fehleranalyse

## Wo die Daten gespeichert werden

Alle Daten werden lokal auf Ihrem Rechner gespeichert:

- Projektarchive (`.zeitprojekt`) an einem von Ihnen gewählten Ort
- Lokaler Arbeitsordner unter `%LocalAppData%\Zeitstrahl Studio`
- Automatische Sicherungen unter `%LocalAppData%\Zeitstrahl Studio\Backups`
- Technische Protokolle unter `%LocalAppData%\Zeitstrahl Studio\Logs`

## Was nicht geschieht

Zeitstrahl Studio überträgt keine Daten an externe Server oder Dienste:

- Keine Telemetrie
- Keine Nutzungsanalyse
- Keine automatische Fehlerübertragung
- Keine Cloud-Synchronisation
- Keine automatische Datenübertragung
- Keine automatischen Hintergrundzugriffe auf Webseiten

## Standalone-HTML-Export

Eine exportierte HTML-Einzeldatei enthält ausschließlich die beim Export gewählten Projektdaten und optional verkleinerte Miniaturen beziehungsweise private Notizen. Sie arbeitet offline, lädt keine externen Ressourcen und sendet keine Daten an Dienste oder Server.

Der Hell-/Dunkel-Umschalter des Exports speichert nur den Wert `zeitstrahl-studio-export-theme` als lokale Darstellungspräferenz im Browserspeicher (`localStorage`). Dort werden keine Projektinhalte, Suchbegriffe oder Filterwerte abgelegt. Externe HTTP(S)-Links verlassen die lokale Datei erst nach einer ausdrücklichen Bestätigung.

## Externe Links

Webseitenlinks werden nur gespeichert und auf Ihren ausdrücklichen Wunsch hin im Standardbrowser geöffnet. Beim Öffnen verlassen Sie die Anwendung; dies erfordert gegebenenfalls eine Internetverbindung.

## OCR und Dokumentenanalyse

Die Texterkennung (OCR) und Dokumentenanalyse erfolgen ausschließlich lokal auf Ihrem Rechner:

- Bilder und bildbasierte PDFs werden mit der in Windows enthaltenen OCR verarbeitet.
- DOCX- und XLSX-Dateien werden mit lokalen .NET-Bordmitteln analysiert.
- Es werden keine Dokumente an Cloud-Dienste oder externe Prozesse übergeben.

## Temporäre Dateien

Temporäre Dateien werden während des Betriebs in lokalen Anwendungsverzeichnissen erzeugt und nach ihrer Verwendung zuverlässig gelöscht.

## Ihre Rechte

Da alle Daten lokal auf Ihrem Rechner verbleiben, haben Sie die volle Kontrolle:

- Sie können Projekte jederzeit exportieren, kopieren oder löschen.
- Sie können technische Protokolle über die Anwendung anzeigen, exportieren oder löschen.
- Sie können Sicherungen verwalten und wiederherstellen.

## Kontakt

Diese Anwendung wird lokal von Ihnen betrieben. Bei Fragen zum Datenschutz wenden Sie sich an den Betreuer des Projekts.
