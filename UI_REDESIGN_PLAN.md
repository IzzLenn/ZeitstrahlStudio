# UI-Redesignplan 0.3.0

Stand: 22.07.2026
Branch: ui/redesign-0.3.0
Auftragsgrenze: ausschließlich Phase 0 bis 3

## Leitbild und Referenzpriorität

Die Hauptansicht folgt vorrangig 04_SOLL_Hauptansicht_Hell.png: ruhige helle Arbeitsfläche, kompakte gruppierte Befehle, klare Projekt-/Ereignishierarchie, anpassbare linke Navigation und ein kontextbezogener rechter Detailinspektor. 05_SOLL_Projekteinstellungen_Hell.png wird nur als dokumentierte Vorgabe für einen späteren Dialogauftrag berücksichtigt. 06_SOLL_Vertikale_Timeline_Dunkel.png steuert in diesem Lauf ausschließlich die dunklen semantischen Kontraste; Karten- und Achsenlayout bleiben unverändert.

## Phase 0 - Bestandsaufnahme

| Sichtbares oder prüfbares Ergebnis | Datei/Komponente |
| --- | --- |
| IST-/SOLL-Abgleich aller sechs Anhänge, Ressourcen-, Größen-, Scroll-, Dialog-, Timeline- und Testaudit | UI_AUDIT_0.2.1.md |
| Umsetzungsmatrix für Phase 1–3 und klare Abgrenzung späterer Arbeiten | UI_REDESIGN_PLAN.md |
| Vollständige Spezifikation aller fehlenden Icons ohne Asseterzeugung | ICON_REQUIREMENTS.md |
| Jederzeit fortsetzbarer Branch-, Build-, Commit-, Push- und Restarbeitsstand | UI_REDESIGN_HANDOFF.md |

Abnahme: Dokumente vollständig, Debug-Build und relevante vorhandene UI-Tests erfolgreich, Arbeitsbaumänderung am Beispielarchiv unberührt.

## Phase 1 - Designsystem und Themes

| Sichtbare Änderung | Datei/Komponente | Prüfkriterium |
| --- | --- | --- |
| Semantische Pinsel für Navigation, Fenster, Arbeitsfläche, Karte, erhöhte Fläche, Eingabe, Text, Rahmen, Divider, Hover, Pressed, Selected, Focus, Read-only, Invalid, Error, Warning, Success, Timeline und Dialoge | Themes/Theme.Light.xaml, Themes/Theme.Dark.xaml | Alle Schlüssel in beiden Themes vorhanden; kontrastkritische Paare automatisiert geprüft. |
| Typografieskala auf lesbare 12-DIP-Metatexte, 13-DIP-Grundtext, 14-DIP-Abschnitte, 20-DIP-Seitenüberschrift und 24-DIP-Projekttitel ausrichten | Themes/Typography.xaml | Keine 10-DIP-Metatexte im neuen Hauptfenster. |
| Zustände normal, hover, pressed, focused, selected, disabled, read-only und invalid für Hauptsteuerelemente | Themes/ControlStyles.xaml | Zustandsressourcen werden über dynamische semantische Pinsel bezogen. |
| Lokale Light-Theme-Übersteuerung entfernen, sodass Hauptfenster und bestehende Dialoge das aktive Application-Theme erben | MainWindow.xaml und alle *Dialog.xaml | Hell/Dunkel/Windows wirken ohne Neustart konsistent; keine strukturelle Dialogüberarbeitung. |
| Theme-Vertrag und Kontrastprüfung ergänzen | neue/erweiterte Integrationstests | Erforderliche Keys und Zielkontraste werden geprüft. |

Technische Entscheidung: keine neue UI- oder Iconbibliothek. App.xaml bleibt einziger Ort für globale Theme-, Typografie- und Controlstyle-Dictionaries; ApplicationThemeService ersetzt ausschließlich das Theme-Dictionary.

## Phase 2 - Globale Navigation

| Sichtbare Änderung | Datei/Komponente | Prüfkriterium |
| --- | --- | --- |
| Normale Menüleiste mit Datei, Bearbeiten, Ansicht, Ereignis, Werkzeuge und Hilfe | MainWindow.xaml | Alle vorhandenen Commands bleiben erreichbar; Sekundärbefehle liegen im Menü. |
| Kompakte gruppierte Befehlsleiste für Projekt, Bearbeiten, Ansicht, Aktionen, Export und Einstellungen | MainWindow.xaml | Neu/Öffnen/Speichern, Undo/Redo, Horizontal/Vertikal, Suchen/Analysieren, PDF/HTML und Einstellungen sichtbar. |
| Textbasierte Bedienung bis zur Lieferung lizenzierter Icons | MainWindow.xaml, ICON_REQUIREMENTS.md | Keine SVG/PNG/XAML-Geometrie/Unicode-Ersatzicons werden neu erzeugt. |
| Menü-/Befehlsleistenstyles und deutliche aktive/deaktivierte Zustände | Themes/ControlStyles.xaml | Bedienbar bei 1280 Pixel ohne horizontalen Gesamt-Scrollbalken. |
| Hilfe-Information ohne neue Geschäftslogik | MainWindow.xaml.cs | Kleiner lokaler About-Hinweis, keine Netzwerkaktion. |
| Navigationstest | MainWindowAccessibilityTests oder neue MainWindowStructureTests | Menügruppen, Kernbefehle und Layoutbreite werden strukturell geprüft. |

Sekundäre Zuordnung:

- Datei: Neu, Öffnen, Speichern, Speichern unter, Duplizieren, Schließen.
- Bearbeiten: Rückgängig, Wiederholen.
- Ansicht: Horizontal, Vertikal, Lückenkompression, Gesamtprojekt, Auswahl zentrieren, Zurücksetzen, Einstellungen.
- Ereignis: Hinzufügen, Bearbeiten, Früher, Später, Löschen, Anhänge hinzufügen/öffnen/entfernen.
- Werkzeuge: Suchen, Analysieren, Analyse anzeigen, Bild/PDF-Vorschau, Sicherungen, Protokoll, PDF-/HTML-Export.
- Hilfe: lokale Produktinformation.

## Phase 3 - Responsive Hauptansicht

| Sichtbare Änderung | Datei/Komponente | Prüfkriterium |
| --- | --- | --- |
| Adaptive Fünfspalten-Geometrie: linke Navigation, Splitter, zentrale Timeline, Splitter, rechter Inspektor | MainWindow.xaml | Standard 310 / flexibel / 300 DIP; Min/Max und beide Splitter vorhanden. |
| Linke Projektseitenleiste mit Projektkopf, Suchfeld, aktiven Filtern, progressiven Schnell-/erweiterten Filtern und mitwachsender Treffer-/Ereignisliste | MainWindow.xaml | Kein äußerer Sidebar-ScrollViewer, keine feste 330-DIP-Ergebnislistenhöhe. |
| Recent Projects und Recovery nur im bestehenden Kein-Projekt-Kontext, nicht in der dauerhaften Projektansicht | MainWindow.xaml | Bei geöffnetem Projekt ausgeblendet; Startansicht wird nicht neu gestaltet. |
| Rechte, einklappbare Ereignisinspektion mit Allgemein, Anhänge und Notizen | MainWindow.xaml | Bindet ausschließlich SelectedEvent und vorhandene Commands; Speichern/Validierung bleiben im bestehenden Bearbeitungsdialog/ViewModel. |
| Deutsche Präsentation ausgewählter Datums-, Prioritäts-, Status-, Tag- und Fristwerte | MainWindowViewModel.cs | Reine Presentation-Properties, keine Domain-/Persistenzänderung. |
| Linke und rechte Breite ein-/ausblendbar | MainWindow.xaml.cs | Wiederherstellung definierter Standardbreiten; keine Geschäftslogik. |
| Projektkopf, lokale Ereignisaktionen, Tabs und Timelinewerkzeuge erhalten mehr nutzbare Höhe und klare Hierarchie | MainWindow.xaml | Zentrale Fläche wächst mit Fenster; Statusleiste bleibt sichtbar. |
| Responsive Strukturtests | MainWindowAccessibilityTests / neue MainWindowLayoutTests | 1280×720, 1366×768, 1600×900, 1920×1080, 2560×1440; endliche positive zentrale Fläche und keine Gesamt-Horizontalrolle. |

## Nicht in diesem Auftrag

- Startansicht neu gestalten oder Recent-/Recovery-Karten überarbeiten (Phase 4).
- Dialoge anhand von 05_SOLL_Projekteinstellungen_Hell.png strukturell überarbeiten (Phase 4).
- Timelinewerkzeuge oder Ereigniskarten neu gestalten (Phase 5).
- Kollisionen, Lane-Modell, Achse oder Viewportkarten anhand von 06_SOLL_Vertikale_Timeline_Dunkel.png ändern (Phase 6).
- Accessibility-/DPI-Gesamtprüfung (Phase 7).
- Vollständige visuelle Abnahme, Publish, Installer, Versionsänderung oder Releasevorbereitung (Phase 8/Release).

## Verifikation je Phase

1. Betroffenen Build in Debug ausführen.
2. Nur relevante Theme-/WPF-/Layouttests ausführen.
3. Laufende Anwendung aus genau diesem Build starten und sichtbare Änderung prüfen, sofern die lokale Sitzung GUI und Screenshot zulässt.
4. STATUS.md und UI_REDESIGN_HANDOFF.md aktualisieren.
5. Funktionierenden Stand committen.
6. Ausschließlich ui/redesign-0.3.0 ohne Force-Push zu origin pushen.
7. Die Benutzeränderung an samples/ZeitstrahlStudio-Beispiel.zeitprojekt nie stagen oder committen.

Nach Phase 3: Debug-Build, relevante UI-/Theme-/Layouttests, git diff --check, Status/Handoff, Commit, Push und Gleichheit von lokalem/Remote-HEAD. Danach endet dieser Auftrag.
