# Datenschutz und lokale Datenflüsse

Stand: 30.08.2026, Zeitstrahl Studio 1.1.0

Dieses Dokument beschreibt, welche Daten die Anwendung tatsächlich verarbeitet, wo sie liegen und an welchen Stellen die lokale Vertrauensgrenze endet. Es ist keine Zusage über das Verhalten externer Programme oder des Betriebssystems.

## Kurzfassung

Zeitstrahl Studio ist eine lokale Einzelbenutzer-Anwendung ohne Benutzerkonto, Anmeldung, Cloud-Synchronisation, Telemetrie, Werbung, eigenen Netzwerkclient oder automatische Fehlerübertragung. Der Anwendungskern benötigt für Projektarbeit, Analyse, Suche, Backup und Export keinen Onlinedienst.

Die Aussage „lokal“ gilt bis zur bewussten Übergabe: Öffnet der Benutzer eine Attachmentkopie in einem Windows-Standardprogramm oder bestätigt im HTML-Export einen externen HTTP(S)-Link, gelten zusätzlich die Netzwerk-, Cloud- und Datenschutzfunktionen dieses Programms beziehungsweise Browsers.

## Verarbeitete Datenarten

Zeitstrahl Studio kann folgende Daten speichern oder ableiten:

- Projektname, Untertitel, Beschreibung, Gesamtzeitraum und technische Projektzeitstempel
- Ereignistitel, Kurzinfo, Beschreibung, Quelle, interne Notizen und fachliche Datumsangaben
- Fristen einschließlich Uhrzeit, Status, Bezeichnung und Erinnerungsnotiz
- Priorität, Status, Farben, Schlagwörter und HTTP(S)-Weblinks
- visuelle Kartenpositionen, Orientierung, Lückenkompression, Schriftgrößen und weitere Projekteinstellungen
- vollständige importierte Attachmentkopien beliebiger Dateitypen
- Attachmentmetadaten: Originaldateiname, Medientyp, Größe, SHA-256, Zeitangaben, interner relativer Pfad und ursprünglicher absoluter Quellpfad
- lokal extrahierte Dokumenttexte, OCR-Ergebnisse, Datumsfundstellen, Dokumentmetadaten und Miniaturen
- fachliche Auditdaten zu Ereignisoperationen, Reihenfolge, Undo/Redo, Settings und Exporten
- Backupmetadaten wie Zeitpunkt, Typ, Größe, relativer Pfad und Prüfsumme
- Pfade und Zeitpunkte zuletzt verwendeter `.zeitprojekt`-Archive
- global gewähltes Farbschema
- technische JSONL-Logs mit Zeitpunkt, Kategorie, Meldung, Fehlerdetails und gegebenenfalls lokalen Pfaden

OCR und Analyse können fehlerhafte oder unvollständige Ergebnisse erzeugen. Sie sind als abgeleitete Such-/Anzeigeinformationen zu behandeln und am Original zu prüfen.

## Speicherorte

| Daten | Speicherort |
| --- | --- |
| gespeichertes Projekt | vom Benutzer gewählte `.zeitprojekt`-Datei |
| aktive extrahierte Arbeitskopien und Recovery | `%LocalAppData%\Zeitstrahl Studio\Workspaces` |
| Projektsicherungen | `%LocalAppData%\Zeitstrahl Studio\Backups` |
| technische Logs | `%LocalAppData%\Zeitstrahl Studio\Logs` |
| zuletzt verwendete Projektpfade | `%LocalAppData%\Zeitstrahl Studio\application-state.json` |
| globales Farbschema | `%LocalAppData%\Zeitstrahl Studio\appearance-settings.json` |
| PDF-, HTML- und HTML-ZIP-Exporte | jeweils im vom Benutzer gewählten Ziel |

Ein `.zeitprojekt` wird zum Bearbeiten in einen verwalteten Workspace extrahiert. Bei normalem Schließen versucht die Anwendung Laufzeitordner und temporäre Dateien bestmöglich zu bereinigen. Nach Absturz oder ungeordnetem Ende können gültige Workspaces bewusst für Recovery verbleiben. Backups und vom Benutzer gewählte Exporte bleiben absichtlich bestehen.

## Schutz und Grenzen der Dateiformate

`.zeitprojekt`-Archive, Backups, PDF-Dateien, HTML-Einzeldateien und HTML-ZIP-Pakete sind weder verschlüsselt noch kennwortgeschützt. Wer Dateisystemzugriff besitzt, kann den Inhalt abhängig vom Format lesen oder extrahieren.

SHA-256-Werte dienen der Integritätsprüfung. Da Hashes und Nutzdaten gemeinsam gespeichert beziehungsweise ausgeliefert werden, sind sie keine digitale Signatur und kein Beweis für Autor oder Vertrauenswürdigkeit.

Die Anwendung verwendet atomare Ersetzung und bestmögliche temporäre Bereinigung, garantiert aber keine sichere physische Löschung. Dateisystem, Backups, Schattenkopien, SSD-Verhalten und externe Sicherungssoftware können Daten länger erhalten. Entfernte Attachments werden zunächst fachlich entkoppelt; ihre physische Projektkopie kann für Undo bestehen bleiben und später weiterhin im Workspace oder Archiv enthalten sein.

Weitere Formatdetails stehen in [`PROJECT_FORMAT.md`](PROJECT_FORMAT.md).

## Attachmentimport und Öffnen

Beim Import wird eine vollständige, intern eindeutig benannte Kopie in den Projektworkspace geschrieben und gehasht. Das externe Original ist danach für die Projektarbeit nicht mehr erforderlich. Sein ursprünglicher absoluter Pfad bleibt jedoch als Attachmentmetadatum im Projekt und kann Benutzer-, Organisations- oder Ordnernamen offenlegen.

Bild-/PDF-Vorschau und Öffnen prüfen die Projektkopie unter anderem auf verwalteten Pfad, Reparse Points, Größe und SHA-256. Ein Doppelklick blockiert riskante ausführbare, Installer-, Skript- und Verknüpfungstypen. Die ausdrückliche Aktion `Öffnen` kann eine validierte riskante Datei dennoch an Windows übergeben. Integrität bedeutet dabei nicht Harmlosigkeit.

Nach der Übergabe verarbeitet ein externes Standardprogramm die Datei. Seine eigenen Einstellungen können Cloudspeicherung, Telemetrie, Onlineprüfung oder andere Netzwerkzugriffe aktivieren; Zeitstrahl Studio kontrolliert diese Vorgänge nicht.

## Webseitenlinks und Browser

Die WPF-Anwendung speichert absolute HTTP(S)-Links, besitzt im aktuellen Stand aber keinen belegten eigenen Befehl zum Öffnen eines gespeicherten Weblinks. Im Standalone-HTML-Export sind externe Links gekennzeichnet und werden erst nach Bestätigung an den Browser übergeben.

Die erzeugte HTML-Seite enthält CSS, JavaScript und Projektdaten lokal und verwendet eine restriktive Content Security Policy; sie lädt selbst keine externen Ressourcen nach. Der Themeumschalter speichert nur `zeitstrahl-studio-export-theme` in `localStorage`. Projektinhalte, Suchbegriffe und Filter werden dort nicht abgelegt.

Nach Bestätigung eines HTTP(S)-Links verlässt der Benutzer die Offline-Datei. Browser, Zielseite, DNS, Schutzsoftware oder Erweiterungen können dann Daten verarbeiten.

## Dokumentanalyse und OCR

PDF-, Bild-, DOCX- und XLSX-Analyse läuft lokal. PDF- und Bild-OCR verwendet die lokale deutsche Windows-Texterkennung; DOCX/XLSX werden ohne Office-Automation aus ihren lokalen Dateiinhalten gelesen. Dokumente werden durch Zeitstrahl Studio nicht zu einem Cloud- oder KI-Dienst hochgeladen.

Extrahierter Text, Metadaten und OCR-Ergebnisse werden im Projekt gespeichert und in den lokalen Suchindex aufgenommen. Sie können sensible Inhalte vervielfältigen und bleiben auch dann suchbar, wenn das externe Original später gelöscht wird. Eine automatische Übernahme von Analysefunden in Ereignisfelder findet nicht statt.

## Exporte

### PDF

Ein PDF-Export kann Projekt-/Ereignistexte, Fristen, interne Notizen nach Auswahl, Dokumentnamen und gegebenenfalls eine primäre validierte PDF- oder Bildminiatur enthalten. Attachmentdateien werden nicht als anklickbare Dateien in das PDF eingebettet. Dennoch kann bereits Text oder Miniatur vertraulich sein.

### Standalone-HTML und Dokument-ZIP

Die HTML-Einzeldatei kann Projektdaten, Dokumenttexte, Miniaturen und auf Wunsch interne Notizen enthalten. Mit `Alle hinterlegten Dokumente als Kopien mitgeben` entsteht zusätzlich ein ZIP mit vollständigen validierten Attachmentkopien. Das Paket muss vollständig entpackt werden und kann den vollständigen vertraulichen Inhalt dieser Dateien offenlegen.

Vor Weitergabe sind Optionen, Empfängerkreis, interne Notizen, Weblinks, Attachmentnamen und Dokumentkopien bewusst zu prüfen. Änderungen an HTML oder PDF werden nicht ins Projekt zurückgeschrieben.

## Audit, technische Logs und Sicherungen

Das fachliche Audit liegt in der Projektdatenbank und ist über `Werkzeuge > Protokoll` schreibgeschützt sichtbar. Es reist mit dem `.zeitprojekt`-Archiv und seinen Sicherungen.

Technische Logs sind davon getrennte rotierende JSONL-Dateien unter `%LocalAppData%\Zeitstrahl Studio\Logs`. Die Voreinstellung umfasst bis zu fünf Dateien von jeweils ungefähr 5 MiB. Meldungs- und Detailfelder sind begrenzt; vollständige Dokumentinhalte werden nicht automatisch protokolliert. Lokale Pfade und Fehlerdetails können trotzdem sensibel sein. In der aktuellen WPF-Oberfläche gibt es keine Anzeige-, Export- oder Löschfunktion für diese technischen Logs.

`Werkzeuge > Sicherungen` erstellt, listet und restauriert Projektbackups und verwaltet die automatische Retention. Manuelle Sicherungen werden nicht automatisch rotiert. Es gibt keine pauschale zentrale „alle Daten löschen“-Funktion.

## Löschen und Datenkontrolle

Für eine bewusste lokale Bereinigung:

1. Anwendung und externe Standardprogramme schließen.
2. Benötigte Projekte beziehungsweise Sicherungen zuerst an einen sicheren Zielort kopieren und prüfen.
3. Nicht mehr benötigte `.zeitprojekt`-Archive und Exporte an ihren selbst gewählten Speicherorten über das Windows-Dateisystem löschen.
4. Erkannte verwaiste Recovery-Kandidaten können am Startbildschirm nach Bestätigung mit `Verwerfen` entfernt werden. Nicht mehr benötigte technische Logs, Backups und sonstige verbleibende oder nicht mehr erkannte Workspaces müssen bei Bedarf bei geschlossener Anwendung über das Windows-Dateisystem unter `%LocalAppData%\Zeitstrahl Studio` bereinigt werden; für Logs und einzelne Backups gibt es keine Löschoberfläche.
5. Papierkorb, organisationsweite Backups, Dateiversionsverlauf, Synchronisationsordner und andere Sicherungssysteme separat berücksichtigen.

Zeitstrahl Studio besitzt keinen sichtbaren Projektlöschen-Befehl. Das Entfernen eines Ereignisses oder Attachments ist außerdem nicht gleichbedeutend mit sofortiger physischer Löschung aller Kopien.

## Checkliste vor Weitergabe

- [ ] Empfänger und Übertragungsweg sind vertrauenswürdig.
- [ ] Projektarchiv beziehungsweise Export ist die beabsichtigte aktuelle Fassung.
- [ ] Interne Notizen und Auditdaten dürfen mitgegeben werden.
- [ ] Attachments und Miniaturen dürfen vollständig weitergegeben werden.
- [ ] Ursprüngliche absolute Attachment-Quellpfade wurden berücksichtigt.
- [ ] Analyse-/OCR-Text enthält keine unerwarteten vertraulichen Inhalte.
- [ ] Externe Weblinks sind beabsichtigt.
- [ ] SHA-256 wurde als Integritätsprüfung verwendet, nicht als Herkunftsnachweis.
- [ ] Unverschlüsselte Speicherung und Transport wurden durch geeignete Betriebssystem-/Übertragungsmaßnahmen abgesichert.

Bedienung und Diagnose: [`USER_GUIDE.md`](USER_GUIDE.md) und [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md).
