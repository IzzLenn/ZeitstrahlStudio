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

## Standalone-HTML-Export

- [ ] HTML-Export in Microsoft Edge öffnen
- [ ] HTML-Export in Mozilla Firefox öffnen
- [ ] HTML-Export in Google Chrome öffnen
- [ ] Offline-Verhalten prüfen (Netzwerk deaktivieren)
- [ ] Volltextsuche im HTML-Export testen
- [ ] Zeitraumfilter im HTML-Export testen
- [ ] Farbfilter im HTML-Export testen
- [ ] Schlagwortfilter im HTML-Export testen
- [ ] Fristfilter im HTML-Export testen
- [ ] Zoom im HTML-Export testen
- [ ] Verschieben des Zeitstrahls im HTML-Export testen
- [ ] Druckvorschau im Browser testen
- [ ] Externe Linkwarnung anzeigen und bestätigen

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
- [ ] Reales mehrgigabytegroßes `.zeitprojekt`-Archiv importieren
- [ ] Großes Archiv speichern
- [ ] Großes Archiv sichern
- [ ] Sicherung eines großen Archivs wiederherstellen

## Fehlerszenarien

- [ ] Gesperrte Zieldatei beim Speichern simulieren und Fehlermeldung prüfen
- [ ] Volles Laufwerk beim Export simulieren und Fehlermeldung prüfen
- [ ] Nicht löschbare Sicherung simulieren und Fehlermeldung prüfen
- [ ] Beschädigtes `.zeitprojekt`-Archiv importieren und Fehlermeldung prüfen
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
