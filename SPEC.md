# Entwicklungsauftrag: Lokale Windows-10-Anwendung zur Erstellung und Verwaltung von Zeitstrahlen

## 1. Rolle und Zielsetzung

Du bist ein erfahrener Senior-Softwareentwickler, Softwarearchitekt, UX-Designer und Qualitätssicherungsingenieur mit Schwerpunkt auf C#, .NET, WPF, lokaler Datenhaltung, Dokumentenverarbeitung und PDF-Erzeugung.

Entwickle eine vollständige, produktionsreife Desktop-Anwendung für Windows 10 und Windows 11 mit dem Arbeitstitel:

**Zeitstrahl Studio**

Die Anwendung dient dazu, chronologische Projekte mit Ereignissen, Fristen, Beschreibungen und zugehörigen Dokumenten zu erstellen. Die Ereignisse werden in einem vertikalen oder horizontalen Zeitstrahl dargestellt. Projekte müssen vollständig lokal gespeichert, gesichert, zwischen Computern übertragen und als PDF oder eigenständige HTML-Datei exportiert werden können.

Die Anwendung ist für einen einzelnen Benutzer pro Gerät vorgesehen. Es ist keine Benutzerverwaltung erforderlich. Ein Projekt muss jedoch auf einem Gerät exportiert und auf einem anderen Gerät vollständig importiert und weiterbearbeitet werden können.

Die Anwendung darf keine Cloud-Dienste, Online-Datenbanken, Telemetrie, Werbung, Analysewerkzeuge oder externe KI-Dienste verwenden.

Alle wesentlichen Funktionen müssen nach der Installation ohne Internetverbindung nutzbar sein.

---

# 2. Technische Grundanforderungen

## 2.1 Zielplattform

Entwickle eine native 64-Bit-Desktop-Anwendung für:

* Windows 10, 64 Bit
* Windows 11, 64 Bit

32-Bit-Systeme müssen nicht unterstützt werden.

## 2.2 Bevorzugter Technologie-Stack

Verwende nach Möglichkeit:

* C#
* .NET 8
* WPF
* MVVM-Architektur
* SQLite als lokale Projektdatenbank
* Dependency Injection
* asynchrone Datei- und Datenbankoperationen
* automatisierte Unit- und Integrationstests

Verwende ausschließlich stabile Bibliotheken, die lokal funktionieren und deren Lizenz eine Weitergabe der Anwendung erlaubt.

Bevorzuge Bibliotheken mit MIT-, BSD- oder Apache-2.0-Lizenz. Dokumentiere alle verwendeten Drittanbieterbibliotheken und deren Lizenz.

Falls eine der vorgeschlagenen Technologien technisch ungeeignet ist, darf eine besser geeignete Alternative verwendet werden. Begründe diese Entscheidung in der technischen Dokumentation.

## 2.3 Auslieferungsformen

Erstelle:

1. einen klassischen Windows-Installer,
2. eine portable 64-Bit-Version als ZIP-Datei,
3. eine selbstständig ausführbare, möglichst vollständig paketierte Anwendung,
4. eine vollständige Build-Anleitung,
5. den vollständigen Quellcode.

Die portable Version soll ohne reguläre Installation gestartet werden können.

Die Anwendung soll möglichst keine separat zu installierende .NET-Laufzeitumgebung voraussetzen. Erstelle deshalb eine selbstenthaltende Veröffentlichung für `win-x64`.

---

# 3. Datenschutz und Offline-Betrieb

Die Anwendung verarbeitet möglicherweise vertrauliche und personenbezogene Dokumente.

Daher gelten folgende Regeln:

* Sämtliche Daten werden ausschließlich lokal verarbeitet.
* Es gibt keine Telemetrie.
* Es gibt keine Nutzungsanalyse.
* Es gibt keine automatische Fehlerübertragung.
* Es gibt keine Cloud-Synchronisation.
* Es werden keine Daten an externe Server übertragen.
* OCR, Volltextindexierung und Dokumentenanalyse erfolgen lokal.
* Externe Inhalte werden nicht automatisch aus dem Internet nachgeladen.
* Webseitenlinks werden nur gespeichert und auf ausdrücklichen Wunsch des Benutzers im Standardbrowser geöffnet.
* Die Anwendung darf nicht selbstständig im Hintergrund Webseiten aufrufen.
* Temporäre Dateien müssen nach ihrer Verwendung zuverlässig gelöscht werden.
* Fehlerberichte und Protokolle bleiben lokal.
* Projekte müssen nicht verschlüsselt oder mit einem Passwort geschützt werden.

Die Anwendung muss auch dann vollständig funktionieren, wenn der Computer dauerhaft offline ist. Lediglich das Öffnen gespeicherter Webseitenlinks im Browser erfordert gegebenenfalls eine Internetverbindung.

---

# 4. Projektverwaltung

## 4.1 Projektfunktionen

Implementiere mindestens folgende Funktionen:

* Neues Projekt erstellen
* Projekt öffnen
* Projekt speichern
* Projekt unter neuem Namen speichern
* Projekt schließen
* Projekt duplizieren
* Projekt löschen
* Zuletzt verwendete Projekte anzeigen
* Projekt importieren
* Projekt exportieren
* Projekt als vollständiges Archiv herunterladen beziehungsweise speichern
* Projektarchiv auf einem anderen Computer hochladen beziehungsweise importieren
* Projektordner öffnen
* Projektinformationen bearbeiten
* Automatische Sicherung erstellen
* Manuelle Sicherung erstellen
* Sicherung wiederherstellen
* Wiederherstellung nach einem Programmabsturz

## 4.2 Projektinformationen

Ein Projekt soll mindestens enthalten:

* Projektname
* optionaler Untertitel
* Infotext beziehungsweise Kurztext
* ausführliche Projektbeschreibung
* Erstellungsdatum
* Datum der letzten Änderung
* optionaler Zeitraum des Gesamtprojekts
* bevorzugte Zeitstrahlansicht
* bevorzugtes Farbschema
* Einstellungen für den PDF-Export
* Einstellungen für den HTML-Export

## 4.3 Projektdateiformat

Erstelle ein versioniertes, transportables Projektformat mit einer eigenen Dateiendung, beispielsweise:

`.zeitprojekt`

Das Projektformat soll technisch ein ZIP-basiertes Archiv sein und mindestens enthalten:

* eine SQLite-Projektdatenbank,
* sämtliche importierten Originaldokumente,
* Vorschaubilder,
* extrahierte Textinhalte,
* Projekteinstellungen,
* Metadaten,
* Änderungsprotokolle,
* eine Manifestdatei,
* Prüfsummen für enthaltene Dateien.

Beispielstruktur:

```text
Projektname.zeitprojekt
├── manifest.json
├── project.db
├── attachments/
├── thumbnails/
├── extracted-text/
├── logs/
└── metadata/
```

Das Format muss versioniert werden, damit zukünftige Programmversionen ältere Projekte migrieren können.

Beim Import sind mindestens folgende Sicherheitsprüfungen erforderlich:

* Prüfung des Dateiformats
* Prüfung der Projektversion
* Prüfung der Manifestdatei
* Prüfung der Prüfsummen
* Schutz vor manipulierten relativen Pfaden
* Schutz vor ZIP-Path-Traversal
* Erkennung beschädigter oder unvollständiger Archive
* verständliche Fehlermeldungen
* keine Überschreibung bestehender Projekte ohne Bestätigung

## 4.4 Lokaler Arbeitsordner

Beim Öffnen eines Projektarchivs darf nicht permanent direkt innerhalb der ZIP-Datei gearbeitet werden.

Verwende stattdessen einen lokalen Projektarbeitsordner. Speichervorgänge müssen atomar erfolgen, damit ein Programmabsturz das Projekt möglichst nicht beschädigt.

Beim Export muss aus dem aktuellen Projektzustand ein neues, vollständiges Projektarchiv erstellt werden.

---

# 5. Automatisches Speichern und Sicherungen

Implementiere:

* automatisches Speichern nach relevanten Änderungen,
* transaktionssichere Datenbankoperationen,
* zusätzliches zeitgesteuertes Speichern,
* Wiederherstellung nicht gespeicherter Änderungen nach einem Absturz,
* manuelle Sicherungen,
* automatische rotierende Sicherungen,
* Wiederherstellung einer ausgewählten Sicherung,
* Anzeige von Datum und Uhrzeit jeder Sicherung.

Standardmäßig sollen automatische Sicherungen beispielsweise nach folgendem Schema aufbewahrt werden:

* mehrere aktuelle Sicherungen des laufenden Tages,
* tägliche Sicherungen der letzten sieben Tage,
* wöchentliche Sicherungen eines begrenzten Zeitraums.

Diese Werte müssen in den Einstellungen anpassbar sein.

Alte Sicherungen dürfen erst nach erfolgreicher Erstellung einer neuen Sicherung entfernt werden.

---

# 6. Datenmodell eines Ereignisses

Jedes Zeitstrahlereignis soll mindestens folgende Felder besitzen:

* eindeutige interne ID
* Datum beziehungsweise Datumsangabe
* optionale Uhrzeit
* Datumsgenauigkeit
* Titel
* Infotext beziehungsweise Kurzbeschreibung
* ausführliche Beschreibung
* optionale Frist
* Priorität
* Farbe
* Schlagwörter
* Quelle
* Notizen
* beliebig viele Anhänge
* beliebig viele Webseitenlinks
* Erstellungsdatum
* Änderungsdatum
* automatische chronologische Position
* optionale manuelle Sortierposition
* optionale manuelle Darstellungsposition
* Status des Eintrags
* Audit-Informationen zu Änderungen

Nicht benötigt werden separate Felder für:

* Kategorie
* Autor
* Aktenzeichen
* Ort

Titel, Infotext, Beschreibung und Notizen dürfen keine feste Zeichenbegrenzung besitzen. Die Oberfläche muss dennoch auch mit sehr langen Texten stabil und übersichtlich funktionieren.

## 6.1 Datumsarten

Unterstütze mindestens folgende Datumsvarianten:

1. Exaktes Datum
2. Exaktes Datum mit Uhrzeit
3. Nur Monat und Jahr
4. Nur Jahr
5. Start- und Enddatum als Zeitraum

Das Datenmodell darf unvollständige Datumsangaben nicht durch erfundene Tages- oder Monatswerte ersetzen.

Beispiel:

* „2024“ bleibt intern eine Jahresangabe.
* „Mai 2024“ bleibt intern eine Monatsangabe.
* Die Anwendung darf daraus nicht sichtbar „01.05.2024“ machen.

Für Sortierzwecke darf intern ein technischer Vergleichswert berechnet werden. Die ursprüngliche Genauigkeit muss jedoch erhalten bleiben und in Oberfläche und Export korrekt dargestellt werden.

Bei Zeiträumen müssen Start- und Enddatum angezeigt werden. Ungültige Zeiträume, bei denen das Enddatum vor dem Startdatum liegt, müssen verhindert werden.

## 6.2 Fristen

Ein Ereignis kann zusätzlich eine Frist besitzen.

Eine Frist soll enthalten können:

* Fristdatum
* optionale Uhrzeit
* optionale Bezeichnung
* optionaler Status, beispielsweise offen oder erledigt
* optionale Erinnerungsnotiz

Die Frist muss unabhängig vom eigentlichen Ereignisdatum gespeichert werden.

Auf dem Zeitstrahl muss eine Frist als eigener, eindeutig erkennbarer Marker dargestellt werden können. Das ursprüngliche Ereignis und die dazugehörige Frist müssen visuell miteinander verbunden sein.

Offene, bevorstehende und überschrittene Fristen sollen unterschiedlich gekennzeichnet werden können. Dabei darf die Anwendung keine externen Benachrichtigungsdienste benötigen.

## 6.3 Gleiche Datumswerte

Mehrere Ereignisse dürfen dasselbe Datum und dieselbe Uhrzeit besitzen.

Bei identischen Datumswerten soll zunächst die automatisch ermittelte Reihenfolge verwendet werden. Der Benutzer muss die Reihenfolge manuell ändern können.

Die manuelle Reihenfolge darf die gespeicherte Datumsangabe nicht verändern.

## 6.4 Bearbeitungsfunktionen

Für Ereignisse müssen mindestens möglich sein:

* neu anlegen,
* bearbeiten,
* kopieren,
* duplizieren,
* löschen,
* Wiederherstellen nach versehentlichem Löschen, soweit Undo verfügbar ist,
* per Drag-and-drop umsortieren,
* Anhänge hinzufügen und entfernen,
* Farbe ändern,
* Schlagwörter verwalten,
* Frist hinzufügen oder entfernen.

Vor einem endgültigen Löschvorgang ist eine Bestätigung erforderlich, sofern das Löschen nicht vollständig durch Undo rückgängig gemacht werden kann.

---

# 7. Dokumente und Anhänge

## 7.1 Unterstützte Dateitypen

Unterstütze mindestens:

* PDF
* Bilder, insbesondere PNG, JPG, JPEG, TIFF und BMP
* Word-Dateien, insbesondere DOCX
* Excel-Dateien, insbesondere XLSX
* Webseitenlinks

Ältere Binärformate wie DOC und XLS können optional über das jeweilige Windows-Standardprogramm geöffnet werden. Eine vollständige interne Analyse dieser alten Formate ist nicht zwingend erforderlich.

## 7.2 Mehrere Anhänge

Jedes Ereignis darf beliebig viele Anhänge besitzen.

Für jeden Anhang sollen mindestens gespeichert werden:

* interne ID
* ursprünglicher Dateiname
* internes Dateiformat
* Dateigröße
* Prüfsumme
* ursprünglicher Dateipfad als reine Metainformation
* Zeitpunkt des Imports
* lokaler Projektpfad
* extrahierter Text
* erkannte Metadaten
* Vorschaubild
* optional eine verknüpfte PDF-Seite
* Zustand des Dokuments

## 7.3 Speicherung im Projekt

Beim Hinzufügen einer Datei muss eine vollständige Kopie in das Projekt übernommen werden.

Das Projekt darf nicht ausschließlich vom ursprünglichen externen Dateipfad abhängig sein.

Wenn die ursprüngliche Datei später verschoben oder gelöscht wird:

* bleibt die im Projekt gespeicherte Kopie verfügbar,
* erscheint bei Bedarf eine nicht blockierende Warnung,
* wird der Vorgang im lokalen Protokoll dokumentiert,
* wird der Benutzer darüber informiert, dass die Projektkopie weiterhin vorhanden ist.

Es muss verhindert werden, dass zwei Anhänge aufgrund gleicher Dateinamen versehentlich überschrieben werden.

Vor dem atomaren Ersetzen eines vollständigen Projektarchivs muss jede in der Projektdatenbank referenzierte Dokumentkopie im Arbeitsordner vorhanden sein und weiterhin zu ihrer gespeicherten Größe und SHA-256-Prüfsumme passen. Eine fehlende oder veränderte Kopie bricht den Export verständlich ab; ein bereits vorhandenes gültiges Zielarchiv bleibt unverändert.

## 7.4 Drag-and-drop

Der Benutzer muss Dateien per Drag-and-drop:

* auf die Anwendung,
* auf ein Ereignis,
* in den Anhangsbereich,
* auf die Ereignisliste

ziehen können.

Werden mehrere Dateien gleichzeitig abgelegt, müssen alle Dateien verarbeitet werden.

Zeige bei längeren Importvorgängen:

* Fortschritt,
* aktuell verarbeitete Datei,
* Anzahl erfolgreicher Dateien,
* Anzahl fehlgeschlagener Dateien,
* Möglichkeit zum Abbrechen.

## 7.5 PDF-Vorschau

PDF-Dateien müssen innerhalb der Anwendung angezeigt werden können.

Zusätzlich muss eine Schaltfläche vorhanden sein, mit der die PDF-Datei im Windows-Standardprogramm geöffnet wird.

Die integrierte PDF-Vorschau soll mindestens ermöglichen:

* Seitenwechsel
* Seitennummernanzeige
* Zoom hinein und heraus
* An Fensterbreite anpassen
* Ganze Seite anzeigen
* Scrollen
* optional direkte Auswahl einer mit dem Ereignis verknüpften Seite

Die PDF-Vorschau muss vollständig lokal arbeiten.

## 7.6 Vorschau anderer Dateien

Für Bilder soll eine direkte Vorschau angezeigt werden.

Für DOCX- und XLSX-Dateien soll mindestens Folgendes angezeigt werden:

* Dateiname
* Dateityp
* Dateigröße
* erkannte Metadaten
* extrahierter Text oder eine kompakte Inhaltsvorschau
* Schaltfläche zum Öffnen im Windows-Standardprogramm

Ein Doppelklick auf einen sichtbaren dokumentartigen Anhang muss genau dessen geprüfte Projektkopie im Windows-Standardprogramm öffnen. Die Zugehörigkeit zum aktuell ausgewählten Ereignis sowie Pfad, Größe und Prüfsumme sind vorher erneut zu prüfen. Für ausführbare Dateien, Skripte und Verknüpfungen ist der direkte Doppelklick zu sperren; sie dürfen nur über die ausdrücklich gewählte Öffnen-Aktion übergeben werden.

Eine vollständige originalgetreue Word- oder Excel-Darstellung innerhalb der Anwendung ist nicht zwingend erforderlich.

---

# 8. Lokale Dokumentenanalyse

## 8.1 Allgemeine Anforderungen

Analysiere importierte Dateien automatisch und lokal.

Extrahiere, soweit technisch möglich:

* Dateiname
* Dokumenttitel
* Erstellungsdatum
* Änderungsdatum
* Autor aus Dokumentmetadaten, ohne ein separates Ereignisfeld „Autor“ anzulegen
* Seitenzahl
* Tabellenblattnamen bei Excel-Dateien
* Textinhalt
* im Dokument enthaltene Datumsangaben
* mögliche Überschriften
* weitere geeignete Metadaten

Die erkannten Daten dürfen nicht automatisch ungefragt bestehende Ereignisdaten überschreiben.

Zeige stattdessen Vorschläge an, die der Benutzer einzeln übernehmen oder ablehnen kann.

## 8.2 PDF-Texterkennung

Bei textbasierten PDF-Dateien soll der vorhandene Text direkt extrahiert werden.

Bei eingescannten PDF-Dateien und Bildern soll eine lokale OCR-Erkennung verwendet werden.

Vorgaben:

* deutsche OCR-Unterstützung,
* optional zusätzliche englische Erkennung,
* keine Online-OCR,
* Fortschrittsanzeige,
* Abbruchmöglichkeit,
* Kennzeichnung, ob Text direkt extrahiert oder per OCR erkannt wurde,
* Speichern des extrahierten Textes im Projekt,
* erneute OCR-Ausführung auf Wunsch.

OCR-Ergebnisse müssen als potenziell fehlerhaft gekennzeichnet werden.

## 8.3 Datumsanalyse

Erkannte Datumsangaben sollen als Vorschläge angezeigt werden.

Die Anwendung darf nicht automatisch davon ausgehen, dass jedes im Dokument erkannte Datum das Ereignisdatum ist.

Der Benutzer soll auswählen können:

* als Ereignisdatum übernehmen,
* als Frist übernehmen,
* ignorieren,
* als Notiz übernehmen.

---

# 9. Volltextsuche und Filter

Implementiere eine projektweite Volltextsuche.

Durchsuchbar sein müssen mindestens:

* Projekttitel
* Projektbeschreibung
* Ereignistitel
* Infotext
* ausführliche Beschreibung
* Notizen
* Schlagwörter
* Quelle
* Dateinamen
* extrahierte PDF-Texte
* OCR-Texte
* extrahierte Word-Texte
* extrahierte Excel-Inhalte
* Webseitenadressen

Die Suchergebnisse sollen:

* während der Eingabe aktualisiert werden,
* Fundstellen hervorheben,
* direkt zum Ereignis führen,
* nach Relevanz oder Datum sortierbar sein.

Implementiere Filter nach:

* Zeitraum
* Datumsart
* Frist vorhanden
* Friststatus
* Priorität
* Farbe
* Schlagwort
* Dateityp
* Ereignis mit oder ohne Anhang
* Ereignis mit oder ohne PDF
* Suchbegriff

Mehrere Filter müssen kombinierbar sein.

Eine Schaltfläche muss alle Filter zurücksetzen.

---

# 10. Zeitstrahldarstellung

## 10.1 Ansichtsarten

Der Zeitstrahl muss in zwei Ansichten verfügbar sein:

* horizontal
* vertikal

Der Wechsel erfolgt über eine deutlich sichtbare Schaltfläche in der Werkzeugleiste.

Der Wechsel darf keine Daten verändern.

Die zuletzt verwendete Ansicht soll pro Projekt gespeichert werden.

## 10.2 Darstellung der Ereignisse

Verwende eine professionelle Kartenansicht.

Eine Ereigniskarte soll abhängig vom verfügbaren Platz mindestens zeigen:

* Datum beziehungsweise Zeitraum
* Uhrzeit, sofern vorhanden
* Titel
* Infotext
* Kennzeichnung einer Frist
* Priorität
* Farbe
* vorhandene Anhänge
* kleines Vorschaubild eines primären PDFs oder Bildes
* Hinweis auf weitere Anhänge

In der vertikalen Ansicht sollen Karten bei ausreichendem Platz abwechselnd links und rechts der Achse angeordnet werden.

In der horizontalen Ansicht sollen Karten oberhalb und unterhalb der Achse verteilt werden, um Überlagerungen zu vermeiden.

Die Anwendung muss Überlappungen automatisch reduzieren.

## 10.3 Automatische Skalierung

Die Zeitskala soll automatisch abhängig vom Projektzeitraum gewählt werden.

Mögliche Einheiten:

* Stunden
* Tage
* Wochen
* Monate
* Jahre
* Jahrzehnte

Die Anwendung soll die passende Skala automatisch bestimmen. Der Benutzer darf zusätzlich hinein- und herauszoomen.

## 10.4 Zoom und Navigation

Implementiere:

* Hineinzoomen
* Herauszoomen
* Zoom über Mausrad
* Verschieben des sichtbaren Bereichs mit der Maus
* Scrollleisten
* Ansicht an Fenster anpassen
* gesamtes Projekt anzeigen
* ausgewählten Zeitraum anzeigen
* ausgewähltes Ereignis zentrieren
* Zurücksetzen der Ansicht

Zoom und Verschiebung müssen flüssig funktionieren.

## 10.5 Große Zeitlücken

Sehr große leere Zeiträume sollen automatisch komprimiert werden können.

Eine komprimierte Zeitlücke muss eindeutig dargestellt werden, beispielsweise durch:

* eine unterbrochene Achse,
* eine Zickzack-Markierung,
* ein beschriftetes Unterbrechungssymbol,
* einen Hinweis auf die übersprungene Zeitspanne.

Beispiel:

```text
Unterbrechung: 14 Jahre und 3 Monate
```

Die Unterbrechung darf nicht den Eindruck erzeugen, die Ereignisse lägen unmittelbar zeitlich nebeneinander.

Der Benutzer soll die Komprimierung großer Zeitlücken ein- und ausschalten können.

## 10.6 Manuelle Anpassungen

Der Zeitstrahl wird zunächst automatisch erzeugt.

Der Benutzer muss anschließend manuelle Anpassungen vornehmen können, beispielsweise:

* Ereigniskarten verschieben
* Reihenfolge gleicher Datumswerte verändern
* Kartenposition oberhalb oder unterhalb der Achse ändern
* Kartenposition links oder rechts der Achse ändern
* horizontale oder vertikale Versätze speichern
* automatische Anordnung wiederherstellen

Manuelle Layoutänderungen dürfen das Ereignisdatum nicht unbemerkt verändern.

Soll ein Drag-and-drop-Vorgang tatsächlich das Datum ändern, muss dies ausdrücklich bestätigt werden oder in einem klar getrennten Bearbeitungsmodus erfolgen.

---

# 11. Benutzeroberfläche

## 11.1 Hauptfenster

Gestalte die Hauptansicht professionell und klar strukturiert:

### Linker Bereich

* Projektinformationen
* Ereignisliste
* Sortierung
* Filter
* Suchergebnisse
* Schaltfläche für neues Ereignis

### Mittlerer Bereich

* horizontaler oder vertikaler Zeitstrahl
* Zoomsteuerung
* Navigation
* Auswahlrahmen
* Kontextmenüs

### Rechter Bereich

* Bearbeitungsmaske des ausgewählten Ereignisses
* Datumsfelder
* Fristfelder
* Titel
* Infotext
* Beschreibung
* Notizen
* Schlagwörter
* Priorität
* Farbe
* Anhänge
* PDF- beziehungsweise Dokumentenvorschau

### Oberer Bereich

Werkzeugleiste mit mindestens:

* Neues Projekt
* Projekt öffnen
* Speichern
* Importieren
* Exportieren
* Rückgängig
* Wiederholen
* Suchen
* Neuer Eintrag
* horizontale Ansicht
* vertikale Ansicht
* PDF-Export
* HTML-Export
* Einstellungen

### Unterer Bereich

Statusleiste mit mindestens:

* Projektstatus
* Speicherstatus
* Anzahl der Ereignisse
* Anzahl aktiver Filter
* Zoomstufe
* aktueller Zeitraum
* Hintergrundvorgänge
* Warnungen

## 11.2 Heller und dunkler Modus

Implementiere:

* helles Erscheinungsbild
* dunkles Erscheinungsbild
* optional automatische Übernahme der Windows-Einstellung

Der Modus muss ohne Neustart umschaltbar sein.

Die Auswahl soll gespeichert werden.

## 11.3 Farben und Schriftgrößen

Der Benutzer muss mindestens anpassen können:

* Farbe eines Ereignisses
* Standardfarbe neuer Ereignisse
* Schriftgröße der Zeitstrahlkarten
* Schriftgröße der Zeitachse
* Schriftgröße der Exportdarstellung

Die Lesbarkeit muss in hellem und dunklem Modus gewährleistet sein.

## 11.4 Tastenkürzel

Implementiere mindestens:

* `Strg + S`: Speichern
* `Strg + F`: Suchen
* `Strg + Z`: Rückgängig
* `Strg + Y`: Wiederholen
* `Strg + N`: neuer Eintrag oder neues Projekt, abhängig vom Kontext
* `Entf`: ausgewähltes Element löschen, mit Sicherheitsprüfung
* `Esc`: aktuellen Dialog oder Vorgang abbrechen

Zeige Tastenkürzel in Menüs und Tooltips an.

## 11.5 Rückgängig und Wiederholen

Unterstütze Undo und Redo mindestens für:

* Erstellen eines Ereignisses
* Bearbeiten eines Ereignisses
* Löschen eines Ereignisses
* Ändern einer Frist
* Ändern einer Farbe
* manuelles Verschieben
* Sortieränderungen
* Hinzufügen und Entfernen eines Anhangs, soweit technisch sicher möglich

Dateien sollen nicht endgültig gelöscht werden, solange der zugehörige Vorgang noch rückgängig gemacht werden kann.

---

# 12. Änderungsprotokoll und lokale Logs

## 12.1 Audit-Protokoll

Führe pro Projekt ein lokales Änderungsprotokoll.

Protokolliere mindestens:

* Erstellen eines Ereignisses
* Bearbeiten eines Ereignisses
* Löschen eines Ereignisses
* Wiederherstellen eines Ereignisses
* Hinzufügen oder Entfernen eines Anhangs
* Änderung einer Frist
* Änderung des Layouts
* Projektimport
* Projektexport
* Sicherung
* Wiederherstellung
* fehlende oder beschädigte Dateien
* fehlgeschlagene Dokumentenanalyse
* fehlgeschlagene OCR
* Fehler beim PDF- oder HTML-Export

Ein Eintrag soll enthalten:

* Datum
* Uhrzeit
* Vorgang
* betroffener Datensatz
* kurze Beschreibung
* Ergebnis
* gegebenenfalls technische Fehlerdetails

Da es nur einen Benutzer pro Gerät gibt, ist keine Benutzerkennung erforderlich. Optional darf der lokale Windows-Benutzername nur lokal protokolliert werden.

## 12.2 Technische Protokolle

Technische Logs müssen:

* lokal gespeichert werden,
* eine einstellbare maximale Größe besitzen,
* automatisch rotiert werden,
* keine unnötigen vollständigen Dokumentinhalte enthalten,
* über die Oberfläche einsehbar sein,
* manuell exportiert werden können,
* manuell gelöscht werden können.

---

# 13. PDF-Export des Zeitstrahls

## 13.1 Allgemeine Anforderungen

Der Zeitstrahl muss als druckoptimierte PDF-Datei exportiert werden können.

Vor dem Export muss eine Druck- beziehungsweise Exportvorschau angezeigt werden.

Der Export soll ausschließlich den Zeitstrahl mit den zugehörigen Ereignisdaten enthalten. Erstelle kein separates Inhaltsverzeichnis und kein umfangreiches Titelblatt.

Ein kompakter Projekttitel und der dargestellte Zeitraum dürfen im Kopfbereich erscheinen.

## 13.2 Inhalt einer Ereignisdarstellung im PDF-Export

Jeder sichtbare Ereigniseintrag soll möglichst vollständig enthalten:

* Datum oder Zeitraum
* Uhrzeit
* Titel
* Infotext
* ausführliche Beschreibung
* Frist
* Priorität
* Farbe beziehungsweise druckgeeignete Kennzeichnung
* Schlagwörter
* Quelle
* Notizen, sofern im Export aktiviert
* Dateinamen der zugehörigen Dokumente
* sehr kleine Vorschau der ersten Seite des primären PDFs
* alternativ eine kleine Bildvorschau, wenn kein PDF vorhanden ist

Anhänge selbst dürfen nicht in die Zeitstrahl-PDF eingebettet werden.

Es sollen lediglich Dateinamen beziehungsweise Dokumentverweise als Text dargestellt werden.

Diese Verweise müssen nicht anklickbar sein.

## 13.3 Papierformat

Unterstütze:

* A4
* A3
* weitere gebräuchliche Formate, sofern einfach umsetzbar
* benutzerdefinierte Papierbreite
* benutzerdefinierte Papierhöhe
* Hochformat
* Querformat

A4 soll die Standardeinstellung sein.

## 13.4 Exportmodi

Biete mindestens drei Exportmodi an:

### Modus 1: Mehrseitiger Export

Ein langer Zeitstrahl wird automatisch auf mehrere Seiten verteilt.

Dabei müssen:

* Karten möglichst nicht ungünstig geteilt werden,
* Seitenübergänge eindeutig sein,
* Achsen auf Folgeseiten sinnvoll fortgeführt werden,
* Zeiträume nachvollziehbar bleiben,
* ausreichende Seitenränder eingehalten werden.

### Modus 2: Sehr große Einzelseite

Der gesamte Zeitstrahl wird auf einer benutzerdefinierten großen PDF-Seite ausgegeben.

Die Seitengröße muss vor dem Export angezeigt werden.

Warne, wenn die erzeugte Seite von typischen PDF-Betrachtern oder Druckern möglicherweise nicht optimal verarbeitet werden kann.

### Modus 3: Ausgewählter Zeitraum

Der Benutzer bestimmt Start und Ende des zu exportierenden Zeitraums.

Nur Ereignisse und Fristen innerhalb dieses Bereichs werden exportiert.

Optional dürfen Ereignisse aufgenommen werden, deren Zeitraum den ausgewählten Bereich überschneidet.

## 13.5 Vorschau

Die Exportvorschau soll ermöglichen:

* Seiten durchblättern
* Zoom
* Papierformat ändern
* Ausrichtung ändern
* Exportbereich ändern
* Schriftgröße ändern
* Vorschau aktualisieren
* tatsächliche Seitenanzahl anzeigen
* Warnungen bei abgeschnittenem Inhalt anzeigen

## 13.6 Druckqualität

Der Export muss:

* vektorbasierte Texte verwenden,
* scharfe Linien erzeugen,
* Bilder sinnvoll komprimieren,
* eingebettete oder systemweit verfügbare Standardschriften verwenden,
* auch in Schwarz-Weiß verständlich sein,
* ausreichend hohe Druckqualität besitzen.

Farben dürfen nicht das einzige Merkmal zur Unterscheidung wichtiger Zustände sein. Verwende zusätzlich Symbole, Rahmenarten oder Beschriftungen.

---

# 14. Standalone-HTML-Export

Erstelle zusätzlich einen Export als einzelne, eigenständige HTML-Datei. Optional kann der Benutzer stattdessen ein transportables ZIP-Paket mit derselben HTML-Momentaufnahme und Kopien aller hinterlegten Dokumente erzeugen.

Die HTML-Datei muss:

* ohne Webserver geöffnet werden können,
* offline funktionieren,
* keine CDN-Abhängigkeiten besitzen,
* keine externen JavaScript- oder CSS-Dateien benötigen,
* sämtliche erforderlichen Styles und Skripte enthalten,
* alle Zeitstrahldaten direkt einbetten,
* eingebettete Vorschaubilder enthalten,
* auf modernen Browsern unter Windows funktionieren,
* druckfreundliche CSS-Regeln enthalten,
* responsive sein.

Der HTML-Export soll mindestens ermöglichen:

* Umschalten zwischen horizontaler und vertikaler Darstellung
* Hinein- und Herauszoomen
* Verschieben des Zeitstrahls
* Volltextsuche in den exportierten Ereignisdaten
* Filterung nach Zeitraum, Farbe, Schlagwort und Frist
* Auf- und Zuklappen langer Beschreibungen
* Anzeige der Ereignisdetails
* Anzeige kleiner Dokumentvorschauen
* Anzeige der Dokumentnamen als Verweise

Die Originaldokumente werden nicht in die einzelne HTML-Datei eingebettet. Ohne Dokumentoption bleiben Namen und eingebettete Miniaturen reine Hinweise ohne lokalen Dateipfad.

Bei aktivierter Dokumentoption muss der Export atomar ein ZIP-Paket mit mindestens `index.html`, `LESMICH.txt` und den validierten Kopien unter `Dokumente/{Anhang-ID}.{sichere Endung}` erzeugen. Originaldateinamen dürfen nicht als Archivpfad verwendet werden. Vor und während des Kopierens sind Projektpfad, Größe und SHA-256 erneut zu prüfen; bei einem Fehler darf kein unvollständiges Paket ein vorhandenes Ziel ersetzen. Nach vollständigem Entpacken verweisen Dokumentnamen und vorhandene Miniaturen relativ auf die jeweils mitgelieferte Kopie. Ob ein Browser die Datei direkt anzeigt, herunterlädt oder an ein Windows-Standardprogramm übergibt, richtet sich nach Dateityp und lokaler Browserkonfiguration.

Webseitenlinks dürfen als anklickbare Links ausgegeben werden. Vor dem Öffnen eines externen Links soll eindeutig erkennbar sein, dass die lokale HTML-Datei verlassen wird.

Zeige standardmäßig einen deutlichen farbigen Hinweis, dass die HTML-Datei eine exportierte Momentaufnahme ist und Änderungen nicht zurück in das Projekt geschrieben werden. Der HTML-Exportdialog muss einen unabhängigen Toggle anbieten, mit dem der Benutzer diesen Hinweis für den konkreten Export deaktivieren kann.

HTML-Einzeldatei und Dokumentpaket dürfen keine Daten an externe Dienste senden.

---

# 15. Projektimport und vollständiger Projektexport

Der vollständige Projektexport unterscheidet sich vom PDF- und HTML-Export.

Beim vollständigen Projektexport müssen enthalten sein:

* sämtliche Ereignisse
* sämtliche Fristen
* sämtliche Beschreibungen
* sämtliche Metadaten
* sämtliche Originaldateien
* sämtliche Vorschaubilder
* sämtliche extrahierten Texte
* Projekteinstellungen
* manuelle Layoutpositionen
* Änderungsprotokolle
* Suchindex beziehungsweise die Möglichkeit, diesen beim Import neu aufzubauen

Beim Import auf einem anderen Computer muss das Projekt anschließend vollständig bearbeitbar sein.

Der Import darf keine absoluten Dateipfade des ursprünglichen Computers voraussetzen.

Nach dem Import müssen interne Dateiverknüpfungen weiterhin funktionieren.

Implementiere außerdem:

* Fortschrittsanzeige
* Abbruchmöglichkeit
* Speicherplatzprüfung
* Prüfung auf beschädigte Dateien
* Ergebnisbericht
* Protokollierung
* Behandlung neuerer oder älterer Projektversionen

---

# 16. Fehlerbehandlung

Alle Fehler müssen verständlich und handlungsorientiert angezeigt werden.

Schlechte Fehlermeldung:

```text
Fehler 0x80004005
```

Bessere Fehlermeldung:

```text
Die PDF-Datei konnte nicht in das Projekt kopiert werden.

Mögliche Ursache:
Der Zieldatenträger verfügt nicht über ausreichend freien Speicherplatz.

Die ursprüngliche Datei wurde nicht verändert.
```

Technische Details dürfen über eine aufklappbare Ansicht erreichbar sein.

Behandle mindestens:

* kein freier Speicherplatz
* fehlende Zugriffsrechte
* beschädigte PDF-Datei
* beschädigtes Projektarchiv
* nicht unterstütztes Dateiformat
* Datei während des Imports gelöscht
* Datei durch anderes Programm gesperrt
* Datenbankfehler
* Fehler bei OCR
* Fehler bei der Vorschaubilderstellung
* Fehler beim PDF-Export
* Fehler beim HTML-Export
* Fehler beim automatischen Speichern
* abgebrochener Import
* beschädigte Sicherung
* extrem lange Dateipfade
* ungültige Zeichen in Dateinamen

Ein Fehler bei einem einzelnen Anhang darf nach Möglichkeit nicht das gesamte Projekt unbrauchbar machen.

---

# 17. Leistung und Stabilität

Die Anwendung soll auch mit größeren Projekten stabil funktionieren.

Zielwerte:

* mindestens 5.000 Ereignisse pro Projekt
* mehrere Anhänge pro Ereignis
* mehrere Gigabyte große Projektarchive, abhängig vom freien Speicherplatz
* flüssiges Scrollen durch virtualisierte Listen
* verzögertes Laden großer Vorschaubilder
* Hintergrundverarbeitung für OCR und Dokumentenanalyse
* keine blockierte Benutzeroberfläche bei längeren Vorgängen

Verwende:

* Lazy Loading
* UI-Virtualisierung
* Caching von Vorschaubildern
* inkrementelle Volltextindexierung
* asynchrone Operationen
* CancellationToken für abbrechbare Vorgänge
* Fortschrittsmeldungen

OCR und Dateianalyse dürfen nicht gleichzeitig unbegrenzt viele Dateien verarbeiten. Verwende eine kontrollierte Warteschlange.

---

# 18. Barrierefreiheit und Bedienbarkeit

Berücksichtige:

* Skalierung bei 100 %, 125 %, 150 % und 200 %
* hochauflösende Monitore
* Tastaturbedienung
* sichtbare Fokusmarkierungen
* verständliche Tooltips
* ausreichende Kontraste
* keine ausschließliche Informationsvermittlung durch Farben
* Bestätigungsdialoge bei riskanten Aktionen
* konsistente deutsche Begriffe
* verständliche Datums- und Uhrzeitformate

Verwende deutsche Beschriftungen in der gesamten Anwendung.

Technische Begriffe sollen nur dort verwendet werden, wo sie notwendig sind.

---

# 19. Empfohlenes Datenbankschema

Entwirf ein normalisiertes SQLite-Datenbankschema.

Mindestens benötigte Tabellen:

* `Projects`
* `Events`
* `EventDates`
* `Deadlines`
* `Attachments`
* `AttachmentMetadata`
* `ExtractedTexts`
* `WebLinks`
* `Tags`
* `EventTags`
* `LayoutPositions`
* `ProjectSettings`
* `AuditLog`
* `ApplicationLogReferences`
* `Backups`
* `SchemaMigrations`

Verwende:

* Fremdschlüssel
* Transaktionen
* Indizes
* eindeutige IDs, beispielsweise GUIDs
* Schema-Versionierung
* Migrationen
* UTC-Zeitstempel für technische Speicherung
* lokale Darstellung in deutscher Ortszeit

Unvollständige Datumsangaben sollen nicht ausschließlich als normaler `DateTime`-Wert gespeichert werden. Speichere zusätzlich die Datumsgenauigkeit und die tatsächlich eingegebenen Komponenten.

---

# 20. Softwarearchitektur

Verwende eine klar getrennte Architektur.

Empfohlene Projekte innerhalb der Solution:

```text
ZeitstrahlStudio.sln

src/
├── ZeitstrahlStudio.App
├── ZeitstrahlStudio.Application
├── ZeitstrahlStudio.Domain
├── ZeitstrahlStudio.Infrastructure
├── ZeitstrahlStudio.DocumentProcessing
├── ZeitstrahlStudio.Export
└── ZeitstrahlStudio.Shared

tests/
├── ZeitstrahlStudio.UnitTests
├── ZeitstrahlStudio.IntegrationTests
└── ZeitstrahlStudio.UiTests
```

Trenne mindestens:

* Benutzeroberfläche
* Geschäftslogik
* Datenzugriff
* Projektarchivverwaltung
* Dokumentenanalyse
* OCR
* PDF-Vorschau
* PDF-Export
* HTML-Export
* Volltextsuche
* Sicherungen
* Protokollierung

Vermeide Geschäftslogik in Code-behind-Dateien.

Code-behind darf nur für rein UI-bezogene Aufgaben verwendet werden, die sich nicht sinnvoll über MVVM lösen lassen.

---

# 21. Tests

Erstelle automatisierte Tests.

## 21.1 Unit-Tests

Teste mindestens:

* Datumssortierung
* Sortierung unvollständiger Datumsangaben
* Zeiträume
* Fristen
* manuelle Reihenfolge
* Erkennung großer Zeitlücken
* Berechnung von Achsenunterbrechungen
* Projektvalidierung
* Dateinamensbehandlung
* Prüfsummen
* Suchfilter
* HTML-Escaping
* PDF-Seitenaufteilung

## 21.2 Integrationstests

Teste mindestens:

* SQLite-Datenzugriff
* Speichern und erneutes Öffnen eines Projekts
* Projektexport und Projektimport
* Erhalt aller Anhänge
* Migration älterer Datenbankversionen
* Sicherung und Wiederherstellung
* PDF-Textextraktion
* OCR mit einem Testbild
* DOCX-Textextraktion
* XLSX-Textextraktion
* PDF-Export
* Standalone-HTML-Export

## 21.3 Fehlerfälle

Teste unter anderem:

* beschädigtes Projektarchiv
* fehlende Manifestdatei
* falsche Prüfsumme
* unzureichender Speicherplatz
* schreibgeschützter Ordner
* beschädigte PDF
* Datei mit gleichem Namen
* sehr lange Beschreibung
* mehrere Ereignisse mit identischem Datum
* Projekt mit nur Jahresangaben
* Projekt mit sehr großen Zeitlücken
* Abbruch während eines Imports

---

# 22. Dokumentation

Liefere mindestens:

* `README.md`
* `BUILD.md`
* `ARCHITECTURE.md`
* `USER_GUIDE.md`
* `PROJECT_FORMAT.md`
* `PRIVACY.md`
* `THIRD_PARTY_LICENSES.md`
* `CHANGELOG.md`
* Installationsanleitung
* Anleitung für die portable Version
* Anleitung zum Projekttransfer
* Anleitung zur Datensicherung
* Anleitung zur Wiederherstellung
* Beschreibung bekannter Einschränkungen

Die Benutzerdokumentation muss auf Deutsch sein.

Die technische Dokumentation darf bei Bedarf englische Fachbegriffe enthalten, soll aber möglichst ebenfalls auf Deutsch verfasst werden.

---

# 23. Installer und Build

Erstelle:

* reproduzierbare Build-Skripte
* Release-Build für `win-x64`
* selbstenthaltende Veröffentlichung
* portable ZIP-Version
* Installer-Skript
* Deinstallationsroutine
* Versionsinformationen
* Anwendungssymbol
* Dateizuordnung für `.zeitprojekt`

Nach der Installation soll ein Doppelklick auf eine `.zeitprojekt`-Datei die Anwendung öffnen und das Projekt importieren beziehungsweise laden.

Der Installer darf keine Online-Verbindung benötigen.

Er muss alle erforderlichen lokalen Komponenten enthalten oder verständlich erklären, falls eine Windows-Komponente bereits vorhanden sein muss.

---

# 24. Beispielprojekt

Erstelle ein kleines Beispielprojekt mit frei erfundenen Daten.

Das Beispiel soll enthalten:

* mindestens zehn Ereignisse
* exakte Datumsangaben
* Datumsangaben mit Uhrzeit
* Monatsangaben
* reine Jahresangaben
* mindestens einen Zeitraum
* mehrere Ereignisse am selben Datum
* mindestens drei Fristen
* unterschiedliche Farben
* unterschiedliche Prioritäten
* mehrere Schlagwörter
* mindestens eine PDF-Testdatei
* mindestens ein Bild
* mindestens eine DOCX-Testdatei
* mindestens eine XLSX-Testdatei
* mindestens einen Webseitenlink
* eine deutlich sichtbare große Zeitlücke

Verwende keine urheberrechtlich geschützten oder vertraulichen Beispieldokumente.

---

# 25. Abnahmekriterien

Die Anwendung gilt erst als vollständig, wenn mindestens folgende Szenarien funktionieren:

## Szenario 1: Projekt erstellen

1. Anwendung starten.
2. Neues Projekt erstellen.
3. Projektname und Beschreibung eingeben.
4. Projekt speichern.
5. Anwendung schließen.
6. Projekt erneut öffnen.
7. Alle Daten sind weiterhin vorhanden.

## Szenario 2: Ereignisse anlegen

1. Ereignis mit exaktem Datum anlegen.
2. Ereignis mit Datum und Uhrzeit anlegen.
3. Ereignis nur mit Monat und Jahr anlegen.
4. Ereignis nur mit Jahr anlegen.
5. Ereignis als Zeitraum anlegen.
6. Zwei Ereignisse mit demselben Datum anlegen.
7. Reihenfolge der beiden Ereignisse manuell verändern.

## Szenario 3: Frist

1. Ereignis auswählen.
2. Frist hinzufügen.
3. Frist auf dem Zeitstrahl anzeigen.
4. Frist bearbeiten.
5. Friststatus ändern.
6. Änderung im Protokoll prüfen.

## Szenario 4: Dokumente

1. Mehrere PDFs per Drag-and-drop hinzufügen.
2. Bild, DOCX und XLSX hinzufügen.
3. PDF innerhalb der Anwendung anzeigen.
4. Einen sichtbaren Anhang doppelklicken und genau diese geprüfte Projektkopie im Windows-Standardprogramm öffnen.
5. PDF zusätzlich über die ausdrückliche Öffnen-Aktion im Standardprogramm öffnen.
6. extrahierten Text durchsuchen.
7. Originaldatei außerhalb der Anwendung verschieben oder löschen.
8. Projektkopie bleibt weiterhin verfügbar.
9. Projekt speichern, auf einen zweiten Rechner übertragen und die dort enthaltene Dokumentkopie erneut öffnen.
10. Hinweis wird protokolliert.

## Szenario 5: Zeitstrahl

1. Horizontale Darstellung öffnen.
2. In den Zeitstrahl hineinzoomen.
3. Zeitstrahl verschieben.
4. Zur vertikalen Darstellung wechseln.
5. große Zeitlücke automatisch komprimieren.
6. Unterbrechung eindeutig anzeigen.
7. Ereigniskarte manuell verschieben.
8. automatische Anordnung wiederherstellen.

## Szenario 6: Suche und Filter

1. Begriff aus einer Beschreibung suchen.
2. Begriff aus einer PDF suchen.
3. nach Zeitraum filtern.
4. nach Farbe filtern.
5. nach Frist filtern.
6. alle Filter zurücksetzen.

## Szenario 7: PDF-Export

1. Exportvorschau öffnen.
2. A4 auswählen.
3. Hochformat auswählen.
4. mehrseitigen Export erzeugen.
5. Querformat auswählen.
6. ausgewählten Zeitraum exportieren.
7. große Einzelseite erzeugen.
8. Ergebnis in einem üblichen PDF-Betrachter prüfen.

## Szenario 8: HTML-Export

1. eigenständige HTML-Datei mit aktiviertem Momentaufnahmehinweis erzeugen.
2. Hinweis im Exportdialog deaktivieren und zweite HTML-Datei ohne sichtbares Warnbanner erzeugen.
3. Computer vom Internet trennen.
4. beide HTML-Dateien lokal im Browser öffnen.
5. zwischen horizontaler und vertikaler Ansicht wechseln.
6. Ereignisse durchsuchen.
7. Beschreibungen aufklappen.
8. Druckvorschau des Browsers öffnen.
9. HTML-Exportpaket mit allen Dokumentkopien als ZIP erzeugen.
10. ZIP vollständig entpacken und `index.html` öffnen.
11. Dokumentname sowie vorhandene Dokumentvorschau anklicken und die zugehörige mitgelieferte Datei öffnen beziehungsweise vom Browser lokal übergeben lassen.
12. Größe und SHA-256 der mitgelieferten Kopien mit dem Projektbestand vergleichen.
13. sicherstellen, dass keine externen Ressourcen geladen und keine Daten übertragen werden.

## Szenario 9: Projekttransfer

1. vollständiges Projekt exportieren.
2. Projektarchiv auf einen anderen Windows-Computer kopieren.
3. Anwendung dort starten.
4. Projekt importieren.
5. alle Ereignisse, Dokumente, Vorschaubilder und Einstellungen prüfen.
6. Projekt bearbeiten.
7. erneut exportieren.

## Szenario 10: Sicherung

1. automatische Sicherung erstellen lassen.
2. Ereignis löschen.
3. ältere Sicherung wiederherstellen.
4. gelöschtes Ereignis ist wieder vorhanden.
5. Wiederherstellung ist protokolliert.

---

# 26. Vorgehensweise bei der Umsetzung

Arbeite in klaren Phasen.

## Phase 1: Architektur

Erstelle zunächst:

* Anforderungszusammenfassung
* Architekturentscheidung
* Projektstruktur
* Datenmodell
* Datenbankschema
* Beschreibung des Projektarchivformats
* Beschreibung der verwendeten Bibliotheken
* Risikoanalyse

## Phase 2: Grundprojekt

Erstelle:

* vollständige Solution
* Dependency Injection
* Logging
* Konfiguration
* SQLite-Datenzugriff
* Migrationen
* Hauptfenster
* helles und dunkles Erscheinungsbild

## Phase 3: Projekt- und Ereignisverwaltung

Implementiere:

* Projekte
* Ereignisse
* Datumsarten
* Fristen
* Tags
* Farben
* Prioritäten
* Undo und Redo
* Autosave

## Phase 4: Anhänge und Dokumentenanalyse

Implementiere:

* Dateiverwaltung
* Drag-and-drop
* PDF-Vorschau
* Bildvorschau
* Textauslesung
* OCR
* DOCX- und XLSX-Auslesung
* Vorschaubilder

## Phase 5: Zeitstrahl

Implementiere:

* horizontale Ansicht
* vertikale Ansicht
* automatische Skalierung
* Zoom
* Verschieben
* große Zeitlücken
* manuelle Positionen
* Fristmarker

## Phase 6: Suche und Filter

Implementiere:

* Volltextindex
* Suchoberfläche
* kombinierbare Filter
* Hervorhebung der Treffer

## Phase 7: Export

Implementiere:

* PDF-Vorschau
* mehrseitige PDF
* große Einzelseite
* Zeitraumexport
* Standalone-HTML
* Druck-CSS

## Phase 8: Import, Export und Sicherungen

Implementiere:

* Projektarchiv
* Validierung
* Prüfsummen
* Migration
* Sicherung
* Wiederherstellung
* Absturzwiederherstellung

## Phase 9: Tests und Auslieferung

Erstelle:

* Unit-Tests
* Integrationstests
* Installer
* portable Version
* Dokumentation
* Beispielprojekt
* Release-Build

---

# 27. Vorgaben für die Codeausgabe

Erzeuge echten, kompilierbaren und vollständigen Code.

Verwende keine Platzhalter wie:

```text
TODO
Hier weitere Logik ergänzen
Restlicher Code ausgelassen
...
```

Gib keine nur beispielhaften Fragmente aus, wenn eine vollständige Datei erforderlich ist.

Für jede Datei:

1. vollständigen relativen Dateipfad nennen,
2. vollständigen Dateiinhalt ausgeben,
3. notwendige Abhängigkeiten angeben,
4. erklären, wie die Datei in das Projekt eingebunden wird.

Achte auf:

* Nullable Reference Types
* saubere Fehlerbehandlung
* XML-Dokumentation bei öffentlichen Schnittstellen
* sinnvolle Kommentare
* keine hart codierten absoluten Dateipfade
* keine fest eingebauten Benutzerdaten
* keine externen API-Schlüssel
* keine versteckten Netzwerkzugriffe
* sichere Verarbeitung von Dateipfaden
* Ressourcenfreigabe über `using` beziehungsweise `IDisposable`
* CancellationToken bei langen Operationen
* thread-sichere Aktualisierung der Benutzeroberfläche

Führe nach jeder größeren Phase gedanklich oder tatsächlich folgende Prüfungen durch:

* Kompiliert die Solution?
* Sind alle Namespaces vorhanden?
* Sind alle NuGet-Pakete eingetragen?
* Passen Interfaces und Implementierungen zusammen?
* Sind Datenbankmigrationen vollständig?
* Sind XAML-Bindings korrekt?
* Sind Befehle und ViewModels verbunden?
* Sind alle Ressourcen eingebunden?
* Funktioniert der Release-Build für `win-x64`?

Behebe gefundene Fehler, bevor du die nächste Phase beginnst.

---

# 28. Nicht gewünschte Funktionen

Implementiere ohne ausdrückliche spätere Anforderung keine:

* Cloud-Synchronisation
* Mehrbenutzerverwaltung
* Benutzerkonten
* Online-KI
* externe OCR-Dienste
* Telemetrie
* Werbung
* automatische Datenübertragung
* verpflichtende Online-Registrierung
* Abonnementfunktionen
* Import aus Excel oder CSV als Ereignisliste
* direkte Einbettung der Originaldokumente in die Zeitstrahl-PDF
* anklickbare Dokumentlinks innerhalb der PDF
* Projektverschlüsselung
* Passwortschutz

---

# 29. Erwartetes Endergebnis

Das Endergebnis muss ein vollständiges Repository sein, das ein Entwickler auf einem Windows-Computer klonen oder entpacken und anhand der Dokumentation bauen kann.

Es muss enthalten:

* vollständigen Quellcode
* kompilierbare Visual-Studio-Solution
* Datenbankmigrationen
* Tests
* Installer-Konfiguration
* portable Veröffentlichung
* Beispielprojekt
* Benutzerhandbuch
* technische Dokumentation
* Lizenzübersicht
* Release-Anleitung

Das Programm muss nach dem Build als professionelle deutschsprachige Windows-Anwendung nutzbar sein und darf für seine Kernfunktionen keine Internetverbindung benötigen.

Beginne mit der Architektur- und Umsetzungsplanung. Erstelle danach die vollständige Projektstruktur und implementiere die Anwendung schrittweise. Überspringe keine für den Build erforderlichen Dateien.
