# Benutzerhandbuch Zeitstrahl Studio

Zeitstrahl Studio ist eine vollständig lokale Windows-Desktopanwendung zur Erstellung und Verwaltung chronologischer Projekte.

## Inhalt

1. [Erste Schritte](#erste-schritte)
2. [Projekte verwalten](#projekte-verwalten)
3. [Ereignisse erstellen und bearbeiten](#ereignisse-erstellen-und-bearbeiten)
4. [Dokumente und Anhänge](#dokumente-und-anhänge)
5. [Zeitstrahlansicht](#zeitstrahlansicht)
6. [Suche und Filter](#suche-und-filter)
7. [Export](#export)
8. [Sicherung und Wiederherstellung](#sicherung-und-wiederherstellung)
9. [Tastenkürzel](#tastenkürzel)
10. [Hinweise zum Datenschutz](#hinweise-zum-datenschutz)

## Erste Schritte

### Installation

#### Variante 1: Installer

Führen Sie `ZeitstrahlStudio-<Version>-win-x64-setup.exe` aus und folgen Sie den Anweisungen des Installationsassistenten. Der Installer legt einen Startmenüeintrag an und verknüpft optional `.zeitprojekt`-Dateien mit der Anwendung.

#### Variante 2: Portable Version

Entpacken Sie `ZeitstrahlStudio-<Version>-win-x64-portable.zip` in einen beliebigen Ordner. Starten Sie die Anwendung mit `ZeitstrahlStudio.App.exe`. Es ist keine Installation erforderlich.

### Startbildschirm

Beim ersten Start zeigt Zeitstrahl Studio einen Startbildschirm mit folgenden Optionen:

- **Neues Projekt**: Ein leeres Projekt erstellen
- **Projekt öffnen**: Ein vorhandenes `.zeitprojekt`-Archiv öffnen
- **Einstellungen in der oberen Befehlsleiste**: Hell, Dunkel oder die lokale Windows-Einstellung bereits vor dem Öffnen eines Projekts wählen
- **Zuletzt verwendet**: Schnellzugriff auf kürzlich geöffnete Projekte
- **Wiederherstellen**: Wiederherstellung nach einem Absturz

Das gewählte Farbschema wird ausschließlich lokal auf dem Gerät gespeichert. Es bleibt beim Erstellen, Öffnen und Schließen von Projekten sowie nach einem Programmneustart erhalten.

### Erscheinungsbild

Die globale Auswahl **Windows-Einstellung**, **Hell** oder **Dunkel** gilt projektübergreifend. Inhaltsflächen und native Titelleisten des Hauptfensters sowie aller anwendungseigenen Dialoge wechseln gemeinsam. Kontrollkästchen zeigen ungeprüfte, aktivierte, teilweise aktivierte und deaktivierte Zustände auch im Dunkelmodus dauerhaft an; Hover und Tastaturfokus verändern nur den Rahmen. Die Felder bleiben per Tabulator und Leertaste bedienbar.

## Projekte verwalten

### Neues Projekt erstellen

1. Klicken Sie auf **Neues Projekt** oder wählen Sie **Datei → Neu**.
2. Geben Sie einen Projektnamen und optional einen Untertitel ein.
3. Speichern Sie das Projekt mit `Strg + S`.

### Projekt öffnen

- Doppelklicken Sie auf eine `.zeitprojekt`-Datei (nach Installation).
- Oder wählen Sie in der Anwendung **Datei → Öffnen**.

### Projekt speichern

- `Strg + S`: Aktuelles Projekt speichern
- **Datei → Speichern unter**: Projekt unter neuem Namen speichern
- **Datei → Duplizieren**: Eine Kopie des Projekts erstellen

### Projekt schließen

Wählen Sie **Datei → Schließen**. Ungespeicherte Änderungen werden abgefragt.

## Ereignisse erstellen und bearbeiten

### Neues Ereignis

- Klicken Sie auf **Neuer Eintrag** oder drücken Sie `Strg + N`.
- Füllen Sie die Felder aus:
  - Titel (Pflichtfeld)
  - Infotext
  - Ausführliche Beschreibung
  - Datum mit Genauigkeit (exakt, Jahr/Monat, Zeitraum)
  - Optionale Uhrzeit
  - Frist
  - Priorität, Farbe, Schlagwörter, Quelle, Notizen
  - Webseitenlinks

Für die Ereignisfarbe steht eine direkt anklickbare und per Tastatur bedienbare Farbpalette zur Verfügung. Ein eigener Wert kann weiterhin im Feld daneben als `#RRGGBB` eingegeben werden.

### Ereignis bearbeiten

Wählen Sie ein Ereignis in der Liste oder im Zeitstrahl aus. Die Bearbeitungsmaske öffnet sich rechts.

### Ereignis löschen

Wählen Sie ein Ereignis aus und drücken Sie `Entf`. Eine Bestätigungsabfrage erscheint.

### Rückgängig und Wiederholen

- `Strg + Z`: Rückgängig
- `Strg + Y`: Wiederholen

Dies gilt für Ereignisanlage, -bearbeitung, -löschung, Friständerungen, Farbänderungen, manuelles Verschieben und Sortieränderungen.

## Dokumente und Anhänge

### Dateien hinzufügen

- Ziehen Sie Dateien per Drag-and-drop auf ein Ereignis, die Ereignisliste oder den Anhangsbereich.
- Oder klicken Sie in der Bearbeitungsmaske auf **Anhang hinzufügen**.

### Unterstützte Formate

- PDF
- Bilder: PNG, JPG, JPEG, TIFF, BMP
- Word: DOCX
- Excel: XLSX
- Webseitenlinks

### Dokumentvorschau

- PDF-Dateien werden direkt in der Anwendung angezeigt.
- Bilder werden direkt in der Vorschau dargestellt.
- DOCX- und XLSX-Dateien zeigen Metadaten und extrahierten Text.
- Mit **In Windows öffnen** wird die Projektkopie im Standardprogramm geöffnet.

### OCR

Bilder und bildbasierte PDFs werden lokal mit der Windows-OCR erkannt. OCR-Ergebnisse werden mit einem Warnhinweis versehen, da sie fehlerhaft sein können.

## Zeitstrahlansicht

### Ansichtsarten

- **Horizontal**: Ereignisse werden horizontal angeordnet.
- **Vertikal**: Ereignisse werden vertikal links und rechts der Achse angeordnet.

Wechseln Sie mit den Schaltflächen in der Werkzeugleiste.

### Navigation

- **Zoomen**: Mausrad oder Zoom-Schaltflächen (25% bis 800%)
- **Verschieben**: Klicken und Ziehen mit der Maus
- **Gesamtprojekt anzeigen**: Passt den sichtbaren Bereich an alle Ereignisse an
- **Ausgewähltes Ereignis zentrieren**: Springt zum markierten Ereignis
- **Zurücksetzen**: Stellt die Standardansicht wieder her

### Manuelle Anpassungen

Ereigniskarten können mit der Maus verschoben werden. Das Ereignisdatum bleibt dabei unverändert. Mit **Auto-Layout** werden alle manuellen Positionen zurückgesetzt.

### Große Zeitlücken

Sehr große leere Zeiträume können komprimiert dargestellt werden. Die Unterbrechung zeigt die übersprungene Zeitspanne an.

## Suche und Filter

### Volltextsuche

Drücken Sie `Strg + F` und geben Sie einen Suchbegriff ein. Die Suche durchsucht:

- Projekttitel und -beschreibung
- Ereignistitel, Infotext, Beschreibung, Notizen
- Schlagwörter, Quelle, Dateinamen
- Extrahierte Dokumenttexte (PDF, OCR, DOCX, XLSX)
- Webseitenadressen

### Filter

Sie können kombinieren nach:

- Zeitraum
- Datumsart
- Frist vorhanden / Friststatus
- Priorität
- Farbe
- Schlagwort
- Dateityp
- Ereignis mit/ohne Anhang
- Ereignis mit/ohne PDF
- Suchbegriff

Mit **Filter zurücksetzen** werden alle Filter aufgehoben.

## Export

### PDF-Export

1. Wählen Sie **Export → PDF-Export**.
2. Wählen Sie Papierformat, Ausrichtung und Exportmodus:
   - Mehrseitiger Export
   - Sehr große Einzelseite
   - Ausgewählter Zeitraum
3. Prüfen Sie die Vorschau.
4. Speichern Sie die PDF-Datei.

### Standalone-HTML-Export

1. Wählen Sie **Export → HTML-Export**.
2. Legen Sie die horizontale oder vertikale Startansicht fest und entscheiden Sie, ob Miniaturen und private Notizen enthalten sein dürfen.
3. Speichern Sie die HTML-Datei und öffnen Sie sie in einem aktuellen Browser.

Die erzeugte Einzeldatei ist eine vollständig lokale Momentaufnahme. Der Hinweis am oberen Rand macht kenntlich, dass Änderungen in der HTML-Datei nicht in das Projekt zurückgeschrieben werden. Es werden keine externen Bibliotheken oder Ressourcen nachgeladen.

Der Projektkopf zeigt Titel, Kurzbeschreibung, Ereigniszahl, Projektzeitraum und Exportzeitpunkt. Die Projektbeschreibung lässt sich aufklappen. Ereignisse erscheinen als Karten mit vollständigem Rahmen in der Ereignisfarbe; sehr helle oder dunkle Farben erhalten zusätzlich eine neutrale Außenkontur.

Bedienung im Browser:

- **Horizontal / Vertikal** wechselt die Zeitstrahlanordnung.
- **− / + / Zurücksetzen** steuert den Zoom; `Strg + Mausrad` zoomt ebenfalls.
- Ziehen auf der freien Arbeitsfläche verschiebt den sichtbaren Ausschnitt.
- Die Suche berücksichtigt Ereignis- und eingebettete Dokumenttexte; `/` setzt den Fokus direkt in das Suchfeld.
- Das aufklappbare Filterpanel kombiniert Zeitraum, Ereignisfarbe, Schlagwort und Friststatus. Ein Zähler zeigt aktive Kriterien; **Zurücksetzen** entfernt sie. `Esc` schließt das Panel.
- Ereignisdetails lassen sich einzeln sowie über **Alle öffnen / Alle schließen** steuern. Geöffnete Details bleiben bei Ansichts- und passenden Filterwechseln erhalten.
- **Design: Hell / Dunkel** wechselt nur die Darstellung der exportierten Datei. Die Wahl wird als reine Darstellungspräferenz lokal im Browserspeicher abgelegt; es werden keine Projektdaten gespeichert oder übertragen.
- **Drucken** öffnet die Browser-Druckfunktion. Für den Druck wechselt der Export vorübergehend auf eine kontrastreiche vertikale 100-%-Ansicht, öffnet Projektbeschreibung und Ereignisdetails und stellt anschließend Ausrichtung, Zoom, Scrollposition, Filterpanel und geöffnete Details wieder her.

Auf schmalen Fenstern ordnen sich Kennzahlen, Werkzeuggruppen und vertikale Karten untereinander an. Dokumente bleiben aus Sicherheits- und Größengründen als Namen beziehungsweise Miniaturen eingebettet; die Originaldateien werden nicht Teil der HTML-Datei. Externe HTTP(S)-Links werden erst nach einer Bestätigung geöffnet.

### Projektexport

Mit **Datei → Exportieren** wird das gesamte Projekt als `.zeitprojekt`-Archiv gespeichert. Dieses Archiv enthält alle Ereignisse, Dokumente, Analysen und Einstellungen.

## Sicherung und Wiederherstellung

### Automatische Sicherungen

Zeitstrahl Studio erstellt automatisch rotierende Sicherungen unter `%LocalAppData%\Zeitstrahl Studio\Backups\{Projekt-ID}`.

### Manuelle Sicherung

Wählen Sie **Datei → Sicherung erstellen**. Manuelle Sicherungen werden nie automatisch gelöscht.

### Wiederherstellung

1. Wählen Sie **Datei → Sicherung wiederherstellen**.
2. Wählen Sie eine Sicherung aus der Liste.
3. Bestätigen Sie die Wiederherstellung.

Vor der Wiederherstellung wird automatisch eine Sicherheitssicherung des aktuellen Stands erstellt.

## Tastenkürzel

| Tastenkürzel | Funktion |
|--------------|----------|
| `Strg + S` | Speichern |
| `Strg + F` | Suchen |
| `Strg + Z` | Rückgängig |
| `Strg + Y` | Wiederholen |
| `Strg + N` | Neuer Eintrag / Neues Projekt |
| `Entf` | Ausgewähltes Element löschen |
| `Esc` | Dialog/Vorgang abbrechen |

## Hinweise zum Datenschutz

- Alle Daten werden ausschließlich lokal verarbeitet.
- Es gibt keine Telemetrie, keine Nutzungsanalyse und keine Cloud-Synchronisation.
- Externe Links werden nur nach expliziter Bestätigung im Browser geöffnet.
- Technische Protokolle bleiben lokal und enthalten keine vollständigen Dokumentinhalte.

Weitere Informationen finden Sie in `PRIVACY.md`.
