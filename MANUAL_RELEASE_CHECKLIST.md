# Manuelle Release-Checkliste

Diese Checkliste ist für eine konkrete Freigabe neu zu kopieren und vollständig auszufüllen. Leere Kästchen sind bewusst keine Bestätigung. Abweichungen, Blocker und akzeptierte Risiken gehören ins Ergebnisprotokoll.

## Freigabekopf

| Feld | Eintrag |
| --- | --- |
| Version | |
| vollständiger Commit | |
| Branch | |
| Tag | |
| Prüfzeitraum | |
| Release-Verantwortlicher | |
| Tester Windows 10 | |
| Tester Windows 11 | |
| Installer SHA-256 | |
| Portable ZIP SHA-256 | |
| Ergebnisprotokoll / Ablage | |

Referenzen: [`RELEASE.md`](RELEASE.md), [`STATUS.md`](STATUS.md), [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md).

## 1. Vorbedingungen und automatische Gates

- [ ] Vorgesehener Commit, Branch, Version und Releaseumfang sind schriftlich freigegeben.
- [ ] `git status --short` ist vor Paketierung vollständig leer.
- [ ] `samples/` entspricht exakt dem beabsichtigten, versionierten Releaseinhalt.
- [ ] Verwendetes Windows, PowerShell- und .NET-8-SDK aus `dotnet --info` protokolliert.
- [ ] Restore erfolgreich.
- [ ] Vor einem direkten Einzel-Task `Publish` wurde `dotnet restore src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -r win-x64` erfolgreich ausgeführt oder die Verfügbarkeit von `Microsoft.NETCore.App.Runtime.win-x64` und `Microsoft.WindowsDesktop.App.Runtime.win-x64` anderweitig belegt; `All` führt diesen RID-Restore seit 1.1.0 selbst aus.
- [ ] Debug-Build erfolgreich.
- [ ] Debug-Tests vollständig erfolgreich; Ergebnisdatei oder Konsolenausgabe archiviert.
- [ ] Release-Build erfolgreich.
- [ ] Release-Tests vollständig erfolgreich; Ergebnisdatei oder Konsolenausgabe archiviert.
- [ ] `dotnet format --verify-no-changes` erfolgreich.
- [ ] Frischer self-contained `win-x64`-Publish erfolgreich und ohne PDB-Dateien.
- [ ] Portable ZIP aus genau diesem Publish erfolgreich erzeugt.
- [ ] Installer aus genau diesem Publish/Paketierungsstand erfolgreich erzeugt.
- [ ] Portable- und Installer-SHA-256 frisch berechnet und in endgültiger Checksummenliste enthalten.
- [ ] Vollständige Original-Lizenz-/Copyrighttexte aller ausgelieferten Produktionskomponenten im Paket geprüft.
- [ ] README, `PRIVACY.md`, `THIRD_PARTY_LICENSES.md`, `CHANGELOG.md`, `licenses/` und freigegebene Samples in den Artefakten vorhanden.
- [ ] Kein älteres gleichnamiges Artefakt wurde versehentlich übernommen.

## 2. Prüfsysteme und Installation

### Windows 10 x64, sauber

- [ ] Betriebssystemversion, Hardware und Anzeige protokolliert.
- [ ] Keine separate .NET-Runtime vorinstalliert oder deren Einfluss durch sauberes System ausgeschlossen.
- [ ] Installer startet; Herausgeber-/Signaturstatus und Windows-Warnungen dokumentiert.
- [ ] Standardinstallation nach 64-Bit-Program Files mit erwartetem Admin-Dialog erfolgreich.
- [ ] Startmenüeintrag startet die Anwendung.
- [ ] Optionale Desktopverknüpfung abgewählt: keine Verknüpfung angelegt.
- [ ] Erneute Installation mit Desktopoption: Verknüpfung korrekt angelegt.
- [ ] `.zeitprojekt`-Dateizuordnung aktiviert und per Doppelklick geprüft.
- [ ] Zugeordneter Projektpfad mit Leerzeichen und Umlaut korrekt als einzelnes CLI-Argument geöffnet.
- [ ] Reparatur-/erneuter Installationslauf bewertet.
- [ ] Deinstallation erfolgreich; Anwendung, Startmenü-/Desktoplinks und Dateizuordnung entfernt.
- [ ] Lokale Nutzerdaten nur gemäß dokumentierter Datenschutz-/Deinstallationsentscheidung verblieben.

### Windows 11 x64, sauber

- [ ] Betriebssystemversion, Hardware und Anzeige protokolliert.
- [ ] Portable ZIP ohne installierte .NET-Runtime vollständig in einen neuen Ordner entpackt.
- [ ] `ZeitstrahlStudio.App.exe` startet aus dem entpackten Ordner.
- [ ] Start von einem Pfad mit Leerzeichen und Umlaut erfolgreich.
- [ ] Beispielprojekt geöffnet, gespeichert, geschlossen und erneut geöffnet.
- [ ] Optionaler Start von einem geeigneten USB-/Wechseldatenträger bewertet.
- [ ] Installer-, Dateizuordnungs- und Deinstallationssmoke auch unter Windows 11 erfolgreich.

## 3. Hauptoberfläche, DPI und Barrierearmut

Jede Kombination ist mindestens mit Beispielprojekt, horizontaler und vertikaler Timeline sowie geöffnetem Ereignis-/Exportdialog zu prüfen.

- [ ] 1280×760 bei 100 % DPI geprüft.
- [ ] 1920×1080 bei 100 % DPI geprüft.
- [ ] 2560×1400 bei 100 % DPI geprüft.
- [ ] 125 % DPI geprüft.
- [ ] 150 % DPI geprüft.
- [ ] 200 % DPI geprüft.
- [ ] Wechsel zwischen Monitoren mit unterschiedlicher Skalierung geprüft.
- [ ] Hell, Dunkel und `Windows-Einstellung übernehmen` einschließlich nativer Titelleisten geprüft.
- [ ] Laufender Themewechsel mit bereits geöffnetem und anschließend neu geöffnetem Dialog geprüft.
- [ ] Menüs, globale Befehlsleiste, linke Navigation, Timeline/Liste und rechter Inspector vollständig erreichbar.
- [ ] Navigation und Details ein-/ausblenden; Layout bleibt nutzbar.
- [ ] Tab-Reihenfolge, sichtbarer Fokus, Access Keys und Screenreader-Namen stichprobenartig geprüft.
- [ ] Hauptabläufe ohne Maus bedienbar.
- [ ] Kontrollkästchen ungeprüft, aktiviert, teilweise aktiviert und deaktiviert in beiden Themes eindeutig.
- [ ] Schriftgrößen 8 und 48 pt auf Clipping und Bedienbarkeit geprüft.

### Bekannte Gate-Funde

- [ ] BUG-001 bei 1280×760 gezielt reproduziert oder mit belastbarer Gegenprobe widerlegt; Auswirkung und Releaseentscheidung dokumentiert. Kein erwarteter PASS.
- [ ] BUG-002 für rote Frist-/Achsenlabels bei 1920×1080 und 2560×1400 bewertet; Lesbarkeit, Exporte und Releaseentscheidung dokumentiert. Kein erwarteter PASS.
- [ ] Weitere visuelle Funde mit Schweregrad, Reproduktion und Screenshot ins Ergebnisprotokoll übernommen.

## 4. Projektlebenszyklus

- [ ] Neues Projekt fragt nur Name und Zielpfad; Archiv wird sofort erzeugt.
- [ ] Öffnen über Dialog, Recent-Liste und Dateizuordnung erfolgreich.
- [ ] Fehlender Recent-Pfad liefert verständliche Meldung und wird entfernt.
- [ ] Speichern aktualisiert das aktive Archiv und erhält bei Fehler ein vorheriges gültiges Ziel.
- [ ] Speichern unter wechselt auf das neue aktive Archiv.
- [ ] Vor Duplizieren gespeichert; Kopie besitzt neue Projekt-ID und wird zum aktiven Projekt.
- [ ] Projekt schließen mit Speichern, Verwerfen und Abbrechen jeweils geprüft.
- [ ] Geordnetes Beenden mit ungespeicherten Änderungen geprüft.
- [ ] `.zeitprojekt` als vollständige Transferdatei auf zweites System kopiert und geöffnet.
- [ ] Keine nicht dokumentierten Projekt-Export-, Projektlöschen- oder Projektordnerbefehle erwartet.
- [ ] Beschädigtes, neues/unbekanntes oder in der Prüfsumme verändertes Archiv wird verständlich und ohne Teilimport abgelehnt.

## 5. Ereignisse, Timeline und Suche

- [ ] Ereignisse für exaktes Datum, Datum/Uhrzeit, Monat/Jahr, nur Jahr und Zeitraum angelegt und roundtripgeprüft.
- [ ] Texte, Quelle, Notizen, Priorität, Status, Farbe, Tags und HTTP(S)-Links bearbeitet.
- [ ] Unabhängige Frist mit Uhrzeit, Status, Bezeichnung und Erinnerungsnotiz geprüft.
- [ ] Validierungsfehler hinterlassen kein teilweise geändertes Ereignis.
- [ ] Erstellen, Bearbeiten und Löschen mit Undo/Redo geprüft.
- [ ] 100-Schritte-Grenze und sitzungsbezogenes Verwerfen der Historie bewertet.
- [ ] Früher/später und Listendrag nur bei vollständig identischer Datumsgruppe möglich.
- [ ] Kartenverschieben ändert nur visuelle Position, niemals Datum.
- [ ] Auto-Layout entfernt manuelle Versätze.
- [ ] Horizontal/Vertikal, Lückenkompression, 25–800 % Zoom, Pan, Gesamtprojekt, Auswahl zentrieren und Reset geprüft.
- [ ] Volltextsuche findet Ereignisdaten, Attachmentnamen und analysierten Dokumenttext.
- [ ] Zeitraum-, Datumsart-, Frist-, Prioritäts-, Farb-, Tag-, Dateityp-, Attachment- und PDF-Filter kombiniert.
- [ ] Filterreset, Sortierung, Ergebnisnavigation und 5.000-Treffergrenze bewertet.

## 6. Attachments, Vorschau und OCR

- [ ] Mehrfachimport per Dateidialog erfolgreich.
- [ ] Drag-and-drop auf ausgewähltes Ereignis, Ereignisliste, Timeline und Anhangsbereich erfolgreich.
- [ ] Gleichnamige Dateien erhalten kollisionsfreie interne Pfade und bleiben byteidentisch.
- [ ] Quelldatei nach Import verschoben/gelöscht; Projektkopie bleibt verwendbar.
- [ ] Teilfehler und Abbruch erhalten erfolgreiche Dateien und entfernen unvollständige Kopien.
- [ ] PDF-, PNG-, JPEG-, TIFF-, BMP-, DOCX- und XLSX-Analyse geprüft.
- [ ] Bild- und PDF-Vorschau einschließlich Seitenwechsel/Zoom geprüft.
- [ ] Deutsche Windows-OCR mit echter Ressource und bildbasierter PDF geprüft.
- [ ] Fehlende OCR-Ressource liefert verständliche lokale Handlungsanleitung.
- [ ] OCR-Ergebnis sichtbar als potenziell fehlerhaft behandelt und gegen Original kontrolliert.
- [ ] Analyseergebnisse/Datumsfundstellen bleiben schreibgeschützt und werden nicht automatisch in Ereignisse übernommen.
- [ ] Dokumenttext erscheint nach Analyse in der Suche.
- [ ] Doppelklick öffnet eine normale validierte Projektkopie im richtigen Windows-Standardprogramm.
- [ ] Riskante Erweiterung wird beim Doppelklick blockiert.
- [ ] Bewusstes `Öffnen` einer kontrollierten riskanten Testdatei und Sicherheitsauswirkung bewertet.
- [ ] Fehlende, verkürzte, verlängerte und bei gleicher Länge manipulierte Projektkopie wird durch Integritätsprüfung abgelehnt.
- [ ] Reparse-Point-/Traversal-Versuch wird abgelehnt.
- [ ] Ursprünglicher absoluter Quellpfad in Attachmentmetadaten vor Weitergabe als sensibler Inhalt bewertet.
- [ ] Entfernen eines Attachments nicht fälschlich als sichere physische Löschung zugesichert.
- [ ] Gespeicherter Weblink besitzt in WPF keinen erwarteten direkten Öffnen-Befehl; HTML-Verhalten separat geprüft.

## 7. Autosave, Recovery, Sicherungen und Audit

- [ ] Manueller Save und fester 60-Sekunden-Autosave mit Dirty-Projekt geprüft.
- [ ] Autosave pausiert während Busy-Vorgang und läuft danach weiter.
- [ ] Ungeordnetes Ende in kontrollierter Kopie simuliert; verwaiste Recovery-Arbeitskopie angeboten.
- [ ] Recovery wiederhergestellt, geprüft und anschließend ausdrücklich gespeichert.
- [ ] Recovery-Verwerfen mit Bestätigung geprüft.
- [ ] Aktive Sitzung wird nicht als Recovery-Kandidat angeboten.
- [ ] `Werkzeuge > Sicherungen` lädt und aktualisiert die Liste.
- [ ] Manuelle Sicherung erzeugt und validiert.
- [ ] Retention 6 aktuell/7 täglich/8 wöchentlich mit kontrollierten Testdaten bewertet.
- [ ] Manuelle Sicherung wird nicht automatisch rotiert.
- [ ] Restore erzeugt vorher eine Sicherheitssicherung und Ergebnis wird danach gespeichert.
- [ ] Manipulierte Sicherung wird abgelehnt.
- [ ] `Werkzeuge > Protokoll` zeigt fachliche Ereignis-/Undo-/Reorder-/Export-Auditeinträge schreibgeschützt.
- [ ] Technische JSONL-Logs liegen unter `%LocalAppData%\Zeitstrahl Studio\Logs` und sind nicht mit Audit verwechselt.

## 8. PDF-Export

- [ ] A4, A3 und Letter in Hoch- und Querformat geprüft.
- [ ] Benutzerdefinierte gültige Mindest-/Maximalmaße und verständliche Ablehnung ungültiger Maße geprüft.
- [ ] Mehrseitiger Export mit sinnvollen Seitenumbrüchen geprüft.
- [ ] Große Einzelseite einschließlich Warnung über 1.000 mm in mehreren Betrachtern geprüft.
- [ ] Zeitraumexport mit und ohne überschneidende Zeiträume geprüft.
- [ ] Interne Notizen ein- und ausgeschaltet.
- [ ] Echte PDF-Vorschau: Aktualisieren, Seitenwechsel, Zoom, Fensterbreite, ganze Seite und extern prüfen.
- [ ] Texte, Farben, Fristen, Dokumentnamen und gegebenenfalls primäre Miniatur korrekt.
- [ ] Keine anklickbar eingebetteten Attachmentdateien erwartet.
- [ ] Unterschied zwischen druckorientiertem PDF-Layout und manueller WPF-Anordnung/Gaps dokumentiert.
- [ ] PDF aus Standardbetrachter geöffnet und reale Druckvorschau geprüft.

## 9. Offline-HTML und Dokument-ZIP

Die folgenden Prüfungen jeweils in Microsoft Edge, Mozilla Firefox und Google Chrome ausführen.

- [ ] HTML-Einzeldatei offline mit deaktiviertem Netzwerk geöffnet.
- [ ] HTML-ZIP vollständig entpackt und erst danach `index.html` geöffnet.
- [ ] `index.html`, `LESMICH.txt` und erwartete GUID-Dokumentpfade vorhanden.
- [ ] Option ohne Dokumentkopien enthält keine vollständigen lokalen Dokumentdateien.
- [ ] Option mit Dokumentkopien enthält ausschließlich validierte referenzierte Kopien.
- [ ] Dokumentnamen und Miniaturen öffnen jeweils die richtige Paketkopie; Browserunterschiede dokumentiert.
- [ ] Horizontal/Vertikal, Zoom, Pan und Reset geprüft.
- [ ] Suche, `/`, Filterpanel, aktive Kriterien, `Esc` und Filterreset geprüft.
- [ ] Details einzeln sowie alle öffnen/schließen; Zustand bei Filter-/Ansichtswechsel geprüft.
- [ ] Hell/Dunkel, Browserpersistenz, schmales Layout und 200-%-Browserzoom geprüft.
- [ ] Orange Momentaufnahmehinweis ein- und ausgeschaltet; keine leere Restfläche.
- [ ] Interne Notizen ein- und ausgeschaltet.
- [ ] Externer HTTP(S)-Link: Warnung abbrechen und bestätigen.
- [ ] Druckansicht aus Hell und Dunkel geprüft; Werkzeuge verborgen, vertikale 100-%-Ansicht und Details vollständig.
- [ ] Zustand nach Abbruch/Ende des Druckdialogs wiederhergestellt.
- [ ] Kein unerwarteter Netzwerkzugriff oder extern nachgeladene Ressource beobachtet.

## 10. Datenschutz, Sicherheit und lokale Pfade

- [ ] Anwendungskern offline nutzbar; kein unerwarteter Netzwerkzugriff bei Start, Bearbeitung, Analyse, Suche, Save, Backup und Export.
- [ ] Übergabe an Standardprogramme und bestätigte externe Browserlinks als bewusste Prozessgrenze dokumentiert.
- [ ] Archive, Backups, PDF und HTML als unverschlüsselt/ohne Passwortschutz bewertet.
- [ ] Interne Notizen, Dokumentkopien, Analyseergebnisse und absolute Attachment-Quellpfade vor Weitergabe geprüft.
- [ ] `%LocalAppData%\Zeitstrahl Studio\application-state.json` enthält erwartete Recent-Pfade.
- [ ] `appearance-settings.json`, `Workspaces`, `Backups` und `Logs` enthalten nur erwartete lokale Daten.
- [ ] Technische JSONL-Logs auf Pfade und sensible Fehlerdetails geprüft; keine vollständigen Dokumentinhalte automatisch protokolliert.
- [ ] Portable ZIP und Installer enthalten vollständige Datenschutz-/Lizenzdokumente und keine PDB-/Test-/temporären Dateien.
- [ ] SHA-256 als Integritäts-, nicht Signatur-/Authentizitätsnachweis korrekt kommuniziert.

## 11. Last, Abbruch und Beenden

- [ ] Reproduzierbares Projekt mit 5.000 Ereignissen erstellt oder freigegebenes Lastprojekt verwendet.
- [ ] Öffnen, Timeline horizontal/vertikal, Zoom/Pan, Liste, Suche/Filter und Auswahl ohne unvertretbare Blockade geprüft.
- [ ] Speicher- und CPU-Beobachtung mit Systemdaten und Dauer protokolliert.
- [ ] Große Attachment-/Analysequeue gestartet und kontrolliert abgebrochen; erfolgreiche Teilergebnisse konsistent.
- [ ] PDF-/HTML-Exportabbruch an unterstützten Grenzen geprüft; gültiges vorhandenes Ziel bleibt erhalten.
- [ ] Große Archivspeicherung, Backup und Restore mit ausreichend freiem Speicher geprüft.
- [ ] Normales Schließen, Fensterschließen und Alt+F4 getrennt beobachtet; Dauer protokolliert.
- [ ] Eine mögliche Beendigungsverzögerung nur bei reproduzierbarer Evidenz als neuer Bug klassifiziert; nicht mit BUG-001/002 vermischt.

## 12. Artefakt-Smoke und Abschluss

- [ ] Installer und portable ZIP direkt aus dem vorgesehenen Veröffentlichungsordner erneut SHA-256-verifiziert.
- [ ] Beide Artefakte in neue Verzeichnisse kopiert/heruntergeladen und Hash erneut geprüft.
- [ ] Portable ZIP vollständig entpackbar; keine unerwarteten oder fehlenden Dateien.
- [ ] Installer vollständig startbar; Version in Datei-/Infoanzeige plausibel.
- [ ] README-Quickstart aus dem Paket nachvollziehbar.
- [ ] `PRIVACY.md`, `THIRD_PARTY_LICENSES.md`, `CHANGELOG.md` und alle mitgelieferten Original-Lizenztexte lesbar.
- [ ] Portable ZIP enthält die Root-`LICENSE.txt`; der derzeitige `PackagePortable`-Fehler ist behoben oder ausdrücklich als blockierend bewertet.
- [ ] Nach realer Installation aus dem finalen geschlossenen Installer existiert `{app}\LICENSE.txt` und ist lesbar.
- [ ] Freigegebene Samples öffnen; keine lokalen oder vertraulichen Arbeitskopien enthalten.
- [ ] BUG-001, BUG-002 und alle neuen Funde besitzen dokumentierte Disposition.
- [ ] Release Notes, Checksummen, Tagziel und Artefakte beziehen sich auf denselben Commit.
- [ ] Öffentliche Veröffentlichung noch nicht behauptet, solange Remote-Tag, Release-Seite und Downloads nicht unabhängig verifiziert sind.

## Ergebnisprotokoll

| Bereich | Ergebnis (PASS/FAIL/BLOCKIERT/N. A.) | Evidenz / Abweichung | Verantwortlich | Datum |
| --- | --- | --- | --- | --- |
| Automatische Gates | | | | |
| Installer Windows 10 | | | | |
| Portable Windows 11 | | | | |
| UI/DPI/Accessibility | | | | |
| Projekte/Ereignisse/Timeline | | | | |
| Attachments/OCR/Suche | | | | |
| Backup/Recovery/Audit | | | | |
| PDF | | | | |
| HTML/Browser | | | | |
| Sicherheit/Datenschutz/Lizenzen | | | | |
| Last/Abbruch/Shutdown | | | | |
| Artefakte/Checksummen | | | | |

## Finale Freigabe

| Entscheidung | Name | Datum | Unterschrift/Referenz |
| --- | --- | --- | --- |
| Technische Freigabe | | | |
| QA-Freigabe | | | |
| Datenschutz-/Lizenzfreigabe | | | |
| Release-Verantwortlicher | | | |

- [ ] Alle Pflichtzeilen besitzen PASS oder eine ausdrücklich genehmigte, dokumentierte Abweichung.
- [ ] Keine offene FAIL-/BLOCKIERT-Zeile wird verschwiegen.
- [ ] Veröffentlichung gemäß [`RELEASE.md`](RELEASE.md) ausdrücklich freigegeben.
