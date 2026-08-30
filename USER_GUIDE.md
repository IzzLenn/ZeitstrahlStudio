# Benutzerhandbuch Zeitstrahl Studio 1.1.0

Zeitstrahl Studio ist eine lokale Windows-Desktopanwendung zum Erfassen, Darstellen und Exportieren chronologischer Projekte. Dieses Handbuch beschreibt den tatsächlich implementierten Stand von Version 1.1.0.

## Inhalt

1. [Voraussetzungen und Start](#voraussetzungen-und-start)
2. [Hauptoberfläche und Menüs](#hauptoberfläche-und-menüs)
3. [Projekte verwalten](#projekte-verwalten)
4. [Ereignisse verwalten](#ereignisse-verwalten)
5. [Zeitstrahl und Reihenfolge](#zeitstrahl-und-reihenfolge)
6. [Suche und Filter](#suche-und-filter)
7. [Anhänge und Dokumentanalyse](#anhänge-und-dokumentanalyse)
8. [Einstellungen und Autosave](#einstellungen-und-autosave)
9. [Recovery, Sicherungen und Protokolle](#recovery-sicherungen-und-protokolle)
10. [PDF exportieren](#pdf-exportieren)
11. [HTML exportieren](#html-exportieren)
12. [Tastenkürzel](#tastenkürzel)
13. [Datenschutz und Sicherheit](#datenschutz-und-sicherheit)

## Voraussetzungen und Start

Die Anwendung ist für Windows 10 oder Windows 11 x64 vorgesehen. Installer und portable ZIP sind selbstenthaltende Releasepakete; dafür ist keine separate .NET-Installation nötig. Für deutsche OCR muss die deutsche Windows-Sprach- und OCR-Ressource installiert sein.

- Installer: Ein bereitgestelltes `ZeitstrahlStudio-<Version>-win-x64-setup.exe` ausführen. Die optionale Dateizuordnung ermöglicht das Öffnen einer `.zeitprojekt`-Datei per Doppelklick.
- Portable Ausgabe: Ein bereitgestelltes `ZeitstrahlStudio-<Version>-win-x64-portable.zip` vollständig in einen neuen Ordner entpacken und dort `ZeitstrahlStudio.App.exe` starten.

Releasepakete werden beim Build erzeugt und sind nicht Bestandteil eines frischen Quellcode-Checkouts. Version 1.1.0 wird als Portable-ZIP und Installer aus demselben geprüften Commit bereitgestellt. Vergleichen Sie ein bereitgestelltes Paket vor dem Start mit seiner SHA-256-Prüfsumme.

Zum Kennenlernen kann das frei erfundene [`samples/ZeitstrahlStudio-Beispiel.zeitprojekt`](samples/ZeitstrahlStudio-Beispiel.zeitprojekt) über `Datei > Projekt öffnen` geladen werden. Hinweise dazu stehen in [`samples/README.md`](samples/README.md).

## Hauptoberfläche und Menüs

Ohne geöffnetes Projekt zeigt die Anwendung die zuletzt verwendeten Projekte und gegebenenfalls wiederherstellbare Arbeitskopien. Bei geöffnetem Projekt gliedert sich das Hauptfenster in drei Bereiche:

- Links liegen Projekt- und Ereignisnavigation, Volltextsuche, Filter und Suchergebnisse. `Ansicht > Navigation ein-/ausblenden` schaltet diesen Bereich um.
- In der Mitte liegen die interaktive Timeline und die chronologische Ereignisliste als Register.
- Rechts zeigt der Detailinspektor das ausgewählte Ereignis und seine Anhänge schreibgeschützt an. `Ansicht > Details ein-/ausblenden` schaltet ihn um. Bearbeitet wird nicht im Inspektor, sondern in einem modalen Ereignisdialog.

Das Hauptmenü enthält die real verfügbaren Befehle:

| Menü | Befehle |
| --- | --- |
| `Datei` | Neues Projekt, Projekt öffnen, Speichern, Speichern unter, Duplizieren, Projekt schließen |
| `Bearbeiten` | Rückgängig, Wiederholen |
| `Ansicht` | Horizontal, Vertikal, Lücken komprimieren, Gesamtprojekt anzeigen, Auswahl zentrieren, Timeline-Ansicht zurücksetzen, Navigation, Details, Einstellungen |
| `Ereignis` | Hinzufügen, Bearbeiten, früher/später verschieben, Löschen, Anhänge hinzufügen/öffnen/entfernen |
| `Werkzeuge` | Suchen, Dokumente analysieren, Analyse anzeigen, Bildvorschau, PDF-Vorschau, Sicherungen, Protokoll, Als PDF exportieren, Als HTML exportieren |
| `Hilfe` | Info zu Zeitstrahl Studio |

Häufige Aktionen sind zusätzlich in der Befehlsleiste erreichbar.

## Projekte verwalten

### Neues Projekt

1. `Datei > Neues Projekt` wählen.
2. Nur den Projektnamen eingeben.
3. Im folgenden Speicherdialog den Zielpfad der `.zeitprojekt`-Datei festlegen.

Das Projekt wird sofort erstellt und gespeichert. Untertitel, Beschreibung und übergreifende Projektdaten sind im aktuellen Dialog nicht bearbeitbar.

### Öffnen

Projekte lassen sich über `Datei > Projekt öffnen`, die Liste der zuletzt verwendeten Projekte oder eine zugeordnete `.zeitprojekt`-Datei öffnen. Beim Öffnen prüft die Anwendung Format, Pfade, Größen und SHA-256-Prüfsummen, bevor sie den Inhalt in einen verwalteten Arbeitsordner extrahiert.

### Speichern, Speichern unter und Duplizieren

- `Datei > Speichern` aktualisiert das aktive Archiv.
- `Datei > Speichern unter` speichert an einem neuen Ziel; dieses Ziel wird zum aktiven Projektarchiv.
- `Datei > Duplizieren` erstellt eine Kopie mit neuer Projekt-ID und wechselt anschließend zur geöffneten Kopie.

Speichern Sie unmittelbar vor dem Duplizieren manuell. So ist sichergestellt, dass auch der zuletzt beabsichtigte Arbeitsstand im Ausgangsarchiv liegt; nur im Speicher befindliche Änderungen können andernfalls fehlen.

### Schließen und Übertragen

`Datei > Projekt schließen` fragt bei ungespeicherten Änderungen nach Speichern, Verwerfen oder Abbrechen. Dasselbe gilt beim geordneten Beenden der Anwendung.

Die `.zeitprojekt`-Datei ist bereits das vollständige, übertragbare Projektarchiv einschließlich Ereignissen, Einstellungen, referenzierten Dokumentkopien und Analyseergebnissen. Zum Übertragen zuerst speichern, dann diese Datei kopieren und auf dem Zielgerät öffnen. Es gibt keinen separaten Projekt-Export oder -Import und keinen sichtbaren Befehl zum Löschen eines Projektarchivs oder Öffnen seines internen Arbeitsordners.

Technische Details und Sicherheitsgrenzen stehen in [`PROJECT_FORMAT.md`](PROJECT_FORMAT.md).

## Ereignisse verwalten

`Ereignis > Hinzufügen` öffnet einen modalen Dialog. Ein vorhandenes Ereignis wird ausgewählt und mit `Ereignis > Bearbeiten` ebenfalls modal geöffnet. Der rechte Inspektor bleibt eine reine Anzeige.

Verfügbare Felder sind:

- Titel als Pflichtfeld, Kurzinfo, ausführliche Beschreibung, Quelle und interne Notizen
- Priorität, Status, Farbe im Format `#RRGGBB` und Schlagwörter
- Webseitenlinks als `Bezeichnung | https://adresse.example`, je ein Link pro Zeile
- eine unabhängige Frist mit Datum, optionaler Uhrzeit, Status, Bezeichnung und Erinnerungsnotiz

Für die fachliche Datumsangabe stehen fünf Genauigkeiten zur Auswahl:

- Exaktes Datum
- Datum und Uhrzeit
- Monat und Jahr
- Nur Jahr
- Zeitraum mit Start- und Enddatum

Die Anwendung ergänzt fehlende Datumsbestandteile nicht. Ereignisse können mit `Ereignis > Löschen` nach Bestätigung entfernt werden.

### Rückgängig und Wiederholen

`Bearbeiten > Rückgängig` und `Bearbeiten > Wiederholen` gelten unter anderem für Ereignisänderungen, Anlagenzuordnungen, Reihenfolge und visuelle Kartenpositionen. Die Historie ist auf 100 Schritte begrenzt, gehört nur zur aktuellen Sitzung und wird beim Schließen des Projekts verworfen. Sie ersetzt kein Speichern oder Backup.

## Zeitstrahl und Reihenfolge

`Ansicht > Horizontal` und `Ansicht > Vertikal` wechseln die Orientierung. `Ansicht > Lücken komprimieren` verkürzt große leere Zeiträume visuell. Die tatsächlichen Datumswerte bleiben dabei unverändert.

- Mausrad oder die Schaltflächen `-` und `+` zoomen zwischen 25 % und 800 %.
- Ziehen auf freier Fläche verschiebt den sichtbaren Ausschnitt.
- `Gesamtprojekt anzeigen` passt Zoom und Ausschnitt an alle Ereignisse an.
- `Auswahl zentrieren` bringt das markierte Ereignis in die Mitte.
- `Timeline-Ansicht zurücksetzen` setzt Zoom und sichtbaren Bereich zurück.
- `Auto-Layout` entfernt gespeicherte manuelle Kartenversätze.

Eine Ereigniskarte kann in der Timeline visuell gezogen werden. Das speichert nur ihren Darstellungsversatz für die jeweilige Orientierung und ändert niemals das Datum.

Die chronologische Ereignisliste unterstützt Drag-and-drop sowie `Nach früher verschieben` und `Nach später verschieben`. Eine manuelle Reihenfolge ist ausschließlich zwischen Ereignissen mit vollständig identischer fachlicher Datumsangabe möglich, einschließlich Genauigkeit, Uhrzeit oder Zeitraum. Ereignisse verschiedener Datumsgruppen lassen sich so nicht umdatieren.

## Suche und Filter

`Werkzeuge > Suchen` oder `Strg+F` fokussiert die lokale Projektsuche. Sie berücksichtigt Ereignisinhalte, Tags, Quellen, Linkadressen, Anhangsnamen und bereits extrahierten Dokumenttext. Eine Dokumentanalyse muss abgeschlossen sein, bevor deren Text auffindbar ist.

Kombinierbar sind Suchtext, Zeitraum, Datumsart, Frist und Friststatus, Priorität, Farbe, Schlagwort, Dateityp, vorhandene Anhänge und vorhandene PDF-Dateien. Die Sortierung ist auswählbar; ein Ergebnis wählt das zugehörige Ereignis aus. `Filter zurücksetzen` entfernt alle Kriterien. Aus Schutz vor ungebremsten Ergebnismengen liefert die Suche höchstens 5.000 Treffer.

## Anhänge und Dokumentanalyse

### Dateien hinzufügen

Zuerst das Zielereignis auswählen, dann `Ereignis > Anhänge hinzufügen` beziehungsweise `Dateien hinzufügen` verwenden. Mehrere Dateien können gewählt werden. Dateien lassen sich außerdem auf das ausgewählte Ereignis, die Ereignisliste, die Timeline oder den Anhangsbereich ziehen.

Jede erfolgreiche Datei wird als intern eindeutig benannte Kopie in den Projektarbeitsordner übernommen. Das Original wird danach für die weitere Projektarbeit nicht benötigt. Beliebige Dateitypen sind als transportierbare Anhänge zulässig; Analyse und Vorschau sind jedoch auf folgende Formate optimiert:

| Format | Analyse/Vorschau |
| --- | --- |
| PDF | eingebetteter Text, bei Bedarf lokale OCR, Metadaten und PDF-Vorschau |
| PNG, JPEG/JPG, TIFF, BMP | lokale OCR und Bildvorschau |
| DOCX | lokaler Text und Metadaten |
| XLSX | lokaler Zelltext und Metadaten |

`Werkzeuge > Dokumente analysieren` verarbeitet analysierbare Anhänge des ausgewählten Ereignisses lokal. `Analyse anzeigen` zeigt extrahierten Text, Datumsfundstellen und Metadaten schreibgeschützt. Diese Ergebnisse fließen in die Suche ein, werden aber nicht automatisch in Ereignisfelder übernommen. OCR-Ergebnisse sind potenziell fehlerhaft und müssen am Original geprüft werden. Fehlt die deutsche Windows-OCR-Ressource, hilft [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md).

### Öffnen und Integrität

Vor jeder Übergabe an Windows prüft Zeitstrahl Studio internen Pfad, Reparse Points, Größe, Dateistabilität und SHA-256-Prüfsumme. Bei Abweichung wird die Datei nicht geöffnet; auch Speichern oder Export kann dann abbrechen, ohne ein gültiges vorhandenes Ziel zu überschreiben.

Ausführbare Dateien, Skripte, Installer und Verknüpfungen sind beim Doppelklick blockiert. Die ausdrückliche Aktion `Ereignis > Anhang öffnen` beziehungsweise `Öffnen` kann eine solche Datei nach erfolgreicher Integritätsprüfung dennoch an das konfigurierte Windows-Standardprogramm übergeben. Verwenden Sie sie nur für vertrauenswürdige Inhalte.

HTTP-/HTTPS-Weblinks können im Ereignis gespeichert und im HTML-Export ausgegeben werden. Für das direkte Öffnen eines gespeicherten Weblinks ist in der WPF-Anwendung kein eigener Befehl belegt.

## Einstellungen und Autosave

`Ansicht > Einstellungen` zeigt ohne geöffnetes Projekt nur das globale Farbschema `Windows-Einstellung übernehmen`, `Hell` oder `Dunkel`. Mit geöffnetem Projekt kommen bevorzugte Orientierung, Lückenkompression, Standardfarbe neuer Ereignisse sowie Karten-, Achsen- und Exportschriftgrößen hinzu. Das Farbschema gilt global auf dem Gerät; die übrigen Werte werden im Projekt gespeichert. Aufbewahrungswerte für Backups liegen im Sicherungsdialog.

Autosave läuft fest alle 60 Sekunden, sofern ein Projekt geändert und die Anwendung nicht beschäftigt ist. Das Intervall ist in der Oberfläche nicht konfigurierbar. Einige Aktionen checkpointen den Arbeitsstand sofort; andere markieren das Projekt zunächst nur als ungespeichert und gelangen erst durch manuelles Speichern oder den nächsten 60-Sekunden-Autosave in den Workspace beziehungsweise ins Archiv. Speichern Sie deshalb regelmäßig manuell, besonders vor Kopieren, Duplizieren oder Beenden.

## Recovery, Sicherungen und Protokolle

### Recovery

Nach einem ungeordneten Ende kann der Startbildschirm verwaiste Arbeitskopien anbieten. `Wiederherstellen` öffnet die gefundene Kopie; speichern Sie das Ergebnis anschließend. `Verwerfen` löscht die angebotene Arbeitskopie nach Bestätigung. Aktive Sitzungen werden nicht als Recovery-Kandidaten angeboten.

### Sicherungen

`Werkzeuge > Sicherungen` öffnet die Sicherungsverwaltung. `Jetzt sichern` erzeugt eine manuelle Sicherung. Automatische Sicherungen werden bei Speichervorgängen nach Fälligkeit erstellt und standardmäßig in drei Stufen aufbewahrt:

- 6 Sicherungen des aktuellen Tages
- 7 tägliche Sicherungen
- 8 wöchentliche Sicherungen

Diese Werte können im Sicherungsdialog geändert werden. Manuelle Sicherungen werden nicht automatisch rotiert. Vor jeder Wiederherstellung erstellt die Anwendung eine manuelle Sicherheitssicherung des aktuellen Stands. Nach erfolgreicher Wiederherstellung ist das Ergebnis ausdrücklich zu speichern.

### Protokolle

`Werkzeuge > Protokoll` zeigt das fachliche Auditprotokoll des Projekts, darunter das Erstellen, Bearbeiten und Löschen von Ereignissen, Reihenfolge, Undo/Redo und Exporte. Davon getrennt schreibt die Anwendung technische Fehler- und Diagnoseeinträge als rotierende JSONL-Dateien. Dafür gibt es in der aktuellen Oberfläche keine Anzeige-, Export- oder Löschfunktion.

Hilfreiche lokale Pfade:

| Daten | Pfad unter `%LocalAppData%\Zeitstrahl Studio` |
| --- | --- |
| Zuletzt verwendete Projekte | `application-state.json` |
| Globales Farbschema | `appearance-settings.json` |
| Verwaltete Arbeitskopien und Recovery | `Workspaces` |
| Projektsicherungen | `Backups` |
| Technische JSONL-Protokolle | `Logs` |

Die selbst gewählte `.zeitprojekt`-Datei liegt unabhängig davon am im Speicherdialog gewählten Ort.

## PDF exportieren

`Werkzeuge > Als PDF exportieren` öffnet eine echte Vorschau der zu erzeugenden PDF-Datei. Einstellbar sind:

- A4, A3, Letter oder benutzerdefinierte Maße von 50 bis 5.080 mm
- Hoch- oder Querformat
- mehrseitiger Export, große Einzelseite oder ausgewählter Zeitraum
- Einbeziehen überschneidender Zeiträume, Exportschriftgröße und interne Notizen

Die Vorschau bietet Seitenwechsel, Zoom, Fensterbreite, ganze Seite und eine Prüfung im externen Standardprogramm. Sehr große Einzelseiten über 1.000 mm erzeugen eine Kompatibilitätswarnung.

Der PDF-Export verwendet ein eigenes druckorientiertes Layout. Er enthält Ereignistexte und Dokumentnamen sowie gegebenenfalls eine primäre validierte PDF- oder Bildminiatur, bettet die Anlagen aber nicht als anklickbare Dateien ein. Er bildet die manuelle Kartenanordnung, den aktuellen Zoom und die WPF-Lückenkompression nicht pixelgenau ab. Entscheidend ist die PDF-Vorschau, nicht die aktuelle Hauptansicht.

## HTML exportieren

`Werkzeuge > Als HTML exportieren` erzeugt eine eigenständige responsive Momentaufnahme. Optionen sind horizontale oder vertikale Anfangsdarstellung, eingebettete kleine Dokumentvorschauen, interne Notizen und der standardmäßig aktive orange Momentaufnahmehinweis.

- Ohne Dokumentkopien entsteht eine einzelne Offline-HTML-Datei mit eingebettetem CSS, JavaScript und Daten.
- Mit `Alle hinterlegten Dokumente als Kopien mitgeben` entsteht ein ZIP-Paket mit `index.html`, `LESMICH.txt` und dem Ordner `Dokumente`.

Ein ZIP-Paket muss vollständig entpackt werden, bevor `index.html` geöffnet wird. Nur validierte Projektkopien werden aufgenommen. Externe HTTP(S)-Links sind gekennzeichnet und verlangen in der HTML-Seite vor dem Öffnen eine Bestätigung. Interne Notizen und Dokumentkopien nur einbeziehen, wenn die Empfänger sie sehen dürfen.

Die HTML-Datei besitzt eine eigene interaktive Darstellung mit Suche, Filtern, Zoom, Horizontal-/Vertikalwechsel und Druckansicht. Sie übernimmt weder die WPF-Kartenpositionen noch deren sichtbaren Ausschnitt exakt und schreibt Änderungen niemals in das Projekt zurück.

## Tastenkürzel

| Kürzel | Wirkung |
| --- | --- |
| `Strg+S` | Projekt speichern |
| `Strg+F` | Suche fokussieren |
| `Strg+N` | Ohne Projekt: neues Projekt; mit Projekt: neues Ereignis |
| `Strg+Z` | Rückgängig |
| `Strg+Y` | Wiederholen |
| `Entf` | Ausgewähltes Ereignis löschen |
| `Esc` | Laufenden Anhangsimport abbrechen |

## Datenschutz und Sicherheit

Die Kernverarbeitung erfolgt lokal; es gibt keine Telemetrie, Cloud-Synchronisation oder automatische Datenübertragung. Beim bewussten Öffnen eines Anhangs wird seine geprüfte Projektkopie jedoch an ein externes Windows-Standardprogramm übergeben. Externe Links im HTML-Export öffnen nach Bestätigung den Browser.

`.zeitprojekt`-Archive, PDF-/HTML-Exporte und Sicherungen sind nicht verschlüsselt oder kennwortgeschützt. Behandeln Sie sie wie die enthaltenen Originaldokumente. Anhangsmetadaten speichern außerdem den ursprünglichen absoluten Quellpfad im Projekt. Prüfen Sie vor einer Weitergabe insbesondere diese Pfadangabe, interne Notizen, Anhänge und Analyseinhalte.

Weitere Hinweise: [`PRIVACY.md`](PRIVACY.md), [`PROJECT_FORMAT.md`](PROJECT_FORMAT.md) und [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md).
