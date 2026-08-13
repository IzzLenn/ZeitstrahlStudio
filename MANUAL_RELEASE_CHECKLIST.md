# Manuelle Release-Checkliste

Diese Checkliste enthält Prüfungen, die in der Entwicklungsumgebung nicht vollständig automatisiert werden können. Sie müssen vor einem Release auf geeigneten Windows-10-/Windows-11-Systemen manuell durchgeführt werden.

## Vorbereitung

- [ ] Installer-Version auf einem sauberen Windows-10-System installieren
- [ ] Portable ZIP-Version auf einem sauberen Windows-11-System entpacken
- [ ] Beispielprojekt `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` zur Verfügung haben

## UI- und DPI-Abnahme

- [ ] Hauptfenster bei 100% Skalierung auf Lesbarkeit und Bedienbarkeit prüfen
- [ ] Hauptfenster bei 125% Skalierung auf Lesbarkeit und Bedienbarkeit prüfen
- [ ] Hauptfenster bei 150% Skalierung auf Lesbarkeit und Bedienbarkeit prüfen
- [ ] Hauptfenster bei 200% Skalierung auf Lesbarkeit und Bedienbarkeit prüfen
- [ ] Dialoge (Ereignis, Export, Sicherung, Vorschau) bei 150% Skalierung prüfen
- [ ] Zeitstrahl bei unterschiedlichen Skalierungen auf Überlappungen prüfen
- [ ] PDF-/Bildvorschau bei unterschiedlichen Skalierungen prüfen
- [ ] Exportvorschau bei unterschiedlichen Skalierungen prüfen

## Tastaturbedienung

- [ ] Vollständige Navigation durch den Startbildschirm ohne Maus
- [ ] Vollständige Navigation durch das Hauptfenster ohne Maus
- [ ] Ereignisdialog mit Tastatur bedienen
- [ ] Suchfeld mit `Strg + F` fokussieren und Suche mit Tastatur steuern
- [ ] Sicherungsdialog mit Tastatur bedienen
- [ ] PDF-/HTML-Exportdialog mit Tastatur bedienen
- [ ] Vorschaufenster mit Tastatur bedienen
- [ ] Fokusvisualisierung in allen Dialogen erkennbar

## Kontrast und visuelle DPI-Abnahme

- [ ] Hell-Thema auf realem Display prüfen
- [ ] Dunkel-Thema auf realem Display prüfen
- [ ] Zeitstrahlpalette im Hell-Thema auf Kontrast prüfen
- [ ] Zeitstrahlpalette im Dunkel-Thema auf Kontrast prüfen
- [ ] Kartenlesbarkeit bei kleinen Schriftgrößen prüfen
- [ ] Kartenlesbarkeit bei großen Schriftgrößen prüfen
- [ ] Fokusvisualisierung in Listen und Buttons prüfen
- [ ] Kontrollkästchen im Dunkelmodus ungeprüft, aktiviert, teilweise aktiviert und deaktiviert ohne Hover prüfen
- [ ] Bei Kontrollkästchen Hover und Tastaturfokus prüfen: keine helle Fläche, Haken beziehungsweise Teilmarkierung weiterhin sichtbar
- [ ] Native Titelleiste des Hauptfensters beim Start in Hell und Dunkel auf Windows 10 sowie Windows 11 prüfen
- [ ] Bereits geöffneten Dialog bei laufendem Wechsel Dunkel → Hell → Dunkel prüfen
- [ ] Nach dem Themewechsel neu geöffneten Dialog auf passende native Titelleiste prüfen

## Dokumentkopien und Öffnen

- [ ] PDF, Bild, DOCX und XLSX hinzufügen; externe Quelldateien anschließend verschieben oder löschen
- [ ] Im Detailreiter **Anhänge** jeden Eintrag doppelklicken und prüfen, dass genau die Projektkopie im jeweiligen Windows-Standardprogramm geöffnet wird
- [ ] Doppelklick auf freien Listenbereich ausführen und prüfen, dass keine Datei geöffnet wird
- [ ] Testanhang mit riskanter Skript- oder Verknüpfungsendung doppelklicken und die verständliche Blockade prüfen
- [ ] Denselben vertrauenswürdigen Testanhang nur bewusst über **Öffnen** auswählen; Sicherheitsauswirkung ausdrücklich beurteilen
- [ ] Projekt speichern, auf einen zweiten Rechner übertragen und dort alle Dokumentkopien erneut öffnen

## Standalone-HTML-Export

- [ ] HTML-Export in Microsoft Edge öffnen
- [ ] HTML-Export in Mozilla Firefox öffnen
- [ ] HTML-Export in Google Chrome öffnen
- [ ] Offline-Verhalten mit deaktiviertem Netzwerk prüfen
- [ ] Desktoplayout und schmale Ansicht einschließlich 200-%-Browserzoom prüfen
- [ ] Horizontale und vertikale Ausrichtung sowie vollständige farbige Kartenrahmen prüfen
- [ ] In der vertikalen Desktopansicht prüfen, dass die Zeitachse bei breitem und schmalem Browserfenster mittig im sichtbaren Arbeitsbereich liegt
- [ ] Systemdesign beim ersten Start, manuelle Hell-/Dunkel-Umschaltung und lokale Persistenz nach Neuladen prüfen
- [ ] Volltextsuche und `/`-Tastenkürzel testen
- [ ] Filterpanel, Aktivzähler, `Esc`, Zurücksetzen und kombinierte Zeitraum-/Farbe-/Schlagwort-/Fristfilter testen
- [ ] Ereignisdetails einzeln sowie über **Alle öffnen / Alle schließen** testen
- [ ] Detailzustand bei Ansichts- und Filterwechsel prüfen
- [ ] Zoom über Schaltflächen und `Strg + Mausrad` testen
- [ ] Verschieben des Zeitstrahls per Ziehen testen
- [ ] Projektkennzahlen, Projektbeschreibung und optionale Miniaturen prüfen
- [ ] Export mit aktiviertem orangefarbenem Momentaufnahmehinweis öffnen und Banner prüfen
- [ ] Hinweis im Exportdialog deaktivieren; zweite Datei öffnen und prüfen, dass keine helle oder leere Bannerfläche verbleibt
- [ ] Dokumentoption deaktivieren und prüfen, dass eine einzelne `.html` ohne lokale Dokumentpfade entsteht
- [ ] Dokumentoption aktivieren und vollständiges ZIP mit `index.html`, `LESMICH.txt` und allen Dateien unter `Dokumente` prüfen
- [ ] ZIP vollständig entpacken; Dokumentnamen und vorhandene Vorschaubilder anklicken und die jeweils richtige lokale Kopie prüfen
- [ ] Gleichnamige Anhänge prüfen: beide müssen unterschiedliche GUID-Pfade besitzen und byteidentisch zum Projektbestand sein
- [ ] HTML-Dokumentpaket offline in Edge, Firefox und Chrome testen; Browserunterschiede bei Anzeige, Download oder Übergabe an Windows dokumentieren
- [ ] Druckbutton und Browser-Druckvorschau im Hell- sowie Dunkeldesign prüfen
- [ ] Im Druck prüfen: Werkzeugleiste verborgen, vertikale 100-%-Ansicht, Projektbeschreibung und alle Ereignisdetails sichtbar, sinnvolle Seitenumbrüche
- [ ] Nach Abbruch beziehungsweise Ende des Druckens Wiederherstellung von Ausrichtung, Zoom, Scrollposition, Filterpanel, Projektbeschreibung und Ereignisdetails prüfen
- [ ] Externe Linkwarnung anzeigen, einmal abbrechen und einmal bestätigen

## PDF-Export

- [ ] PDF-Export im Standard-PDF-Betrachter öffnen
- [ ] Druckvorschau des PDF-Exports prüfen
- [ ] Mehrseitiger Export auf korrekte Seitenumbrüche prüfen
- [ ] Große Einzelseite auf Warnhinweis prüfen
- [ ] Zeitraumexport auf korrekten Bereich prüfen
- [ ] Texte vektorbasiert und scharf darstellbar
- [ ] Miniaturen korrekt eingebettet oder als Textverweis vorhanden

## Projekttransfer und große Archive

- [ ] Beispielprojekt importieren
- [ ] Beispielprojekt speichern
- [ ] Beispielprojekt exportieren
- [ ] Exportiertes Projekt auf einem zweiten Rechner importieren
- [ ] Nach dem Transfer jede hinterlegte Dokumentkopie öffnen und mit Dateigröße sowie SHA-256 des Ausgangsprojekts vergleichen
- [ ] Reales mehrgigabytegroßes `.zeitprojekt`-Archiv importieren
- [ ] Großes Archiv speichern
- [ ] Großes Archiv sichern
- [ ] Sicherung eines großen Archivs wiederherstellen

## Fehlerszenarien

- [ ] Gesperrte Zieldatei beim Speichern simulieren und Fehlermeldung prüfen
- [ ] Volles Laufwerk beim Export simulieren und Fehlermeldung prüfen
- [ ] Nicht löschbare Sicherung simulieren und Fehlermeldung prüfen
- [ ] Beschädigtes `.zeitprojekt`-Archiv importieren und Fehlermeldung prüfen
- [ ] Referenzierte Projektkopie löschen sowie bei gleicher Länge manipulieren; Speichern muss jeweils abbrechen und das vorhandene `.zeitprojekt` byteidentisch erhalten
- [ ] Für das HTML-Dokumentpaket eine Projektkopie manipulieren; Export muss abbrechen und ein vorhandenes Ziel-ZIP byteidentisch erhalten
- [ ] Projektordner während der Bearbeitung extern löschen und Verhalten prüfen

## Installer und Deinstallation

- [ ] Installer auf sauberem System ausführen
- [ ] Startmenüeintrag prüfen
- [ ] Optionale Desktopverknüpfung prüfen
- [ ] `.zeitprojekt`-Datei per Doppelklick öffnen
- [ ] Deinstallation über Systemsteuerung ausführen
- [ ] Nach Deinstallation keine `.zeitprojekt`-Zuordnung mehr vorhanden

## Portable Version

- [ ] ZIP-Datei entpacken
- [ ] Anwendung ohne Installation starten
- [ ] Beispielprojekt aus dem entpackten Ordner öffnen
- [ ] Projekt speichern und erneut öffnen
- [ ] Anwendung von einem USB-Laufwerk starten

## Abschluss

- [ ] Alle manuellen Prüfungen dokumentiert
- [ ] Gefundene Probleme in `STATUS.md` eingetragen
- [ ] Release-Artefakte mit Prüfsummen verifiziert
- [ ] Freigabe durch Release-Verantwortlichen
