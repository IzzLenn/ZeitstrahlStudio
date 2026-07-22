# Iconanforderungen für UI-Redesign 0.3.0

Stand: 22.07.2026
Status: Spezifikation erstellt, keine neuen Iconassets vorhanden oder erzeugt

## Technischer Befund und verbindliches Gate

Das WPF-App-Projekt enthält derzeit keine lizenzierten Icon- oder Bildressourcen. Ohne neue Abhängigkeit unterstützt WPF PNG, ICO, BMP, JPEG und GIF zuverlässig über BitmapImage/BitmapFrame und Pack-URIs. SVG wird nicht nativ unterstützt. Neue XAML-Path-Geometrien, heruntergeladene Grafiken, generierte PNG/SVG und Emoji-/Unicode-Ersatzicons sind für diesen Auftrag ausgeschlossen.

Für Befehlsleisten werden transparente PNG-Dateien gewählt, weil sie ohne Bibliothek zuverlässig funktionieren. Für das Anwendungs-/Fenstersymbol wird ein mehrrahmiges ICO vorgesehen. Bis der Benutzer diese Dateien mit geklärter Lizenz liefert, bleiben alle Funktionen eindeutig textbeschriftet. Es werden in Phase 1–3 keine provisorischen endgültigen Icons und keine Assetdateien angelegt.

## Gemeinsamer visueller Vertrag für Befehlsicons

Sofern die Einzelanforderung nichts anderes sagt, gelten für jedes Icon:

- Perspektive und Form: orthografische Frontalansicht, auf einer quadratischen Arbeitsfläche optisch zentriert, keine räumliche Perspektive.
- Visueller Stil: ruhiger professioneller Windows-Desktopstil, geometrisch reduziert, konsistent über die gesamte Serie.
- Füllung: Outline; kleine semantisch notwendige Flächen dürfen gefüllt sein.
- Strichstärke: 1,5 Pixel auf der 16×16-Arbeitsfläche, proportional für höhere Rastergrößen.
- Eckenform: leicht gerundet; runde Linienenden und Linienverbindungen.
- ViewBox/Arbeitsfläche: 16×16 logische Einheiten mit mindestens 1 Einheit Sicherheitsrand.
- Darstellungsgröße: 16 DIP; Trefferfläche des Buttons mindestens 36×36 DIP.
- Erforderliche Pixelgrößen: 16×16 bei 100 %, 20×20 bei 125 %, 24×24 bei 150 %, 32×32 bei 200 %.
- Farbigkeit: monochrom, transparenter Hintergrund.
- Farbe hell: normal #334155, hover #2563EB, pressed #1D4ED8, selected #2563EB, disabled #94A3B8.
- Farbe dunkel: normal #CBD5E1, hover #60A5FA, pressed #3B82F6, selected #60A5FA, disabled #64748B.
- Zustände: Hover und Pressed müssen zusätzlich über den Buttonhintergrund erkennbar sein; Selected erhält Akzenthintergrund plus Akzent-/Kontrasticon; Disabled bleibt lesbar, aber klar reduziert. Fokus wird durch den Buttonfokusrahmen, nicht durch eine eigene Grafik vermittelt.
- Dateiformat: transparente PNG-Datei, ohne eingebettetes Farbprofil und ohne Animation.
- Zielordner: src/ZeitstrahlStudio.App/Assets/Icons/Light beziehungsweise src/ZeitstrahlStudio.App/Assets/Icons/Dark.
- Dateinamen: pro in der Tabelle genanntem Slug exakt slug.light.16.png, slug.light.20.png, slug.light.24.png, slug.light.32.png sowie slug.dark.16.png, slug.dark.20.png, slug.dark.24.png, slug.dark.32.png.
- Hell-/Dunkel-Asset: ja, je vier Rastergrößen; keine Laufzeitfärbung erforderlich.

## Funktions- und Motivanforderungen

| Icon-ID | Deutscher Name | Fachliche Bedeutung | Zugehöriger Befehl | Zielposition | Gewünschtes Motiv | AutomationProperties.Name | Tooltip |
| --- | --- | --- | --- | --- | --- | --- | --- |
| APP-BRAND-001 | Anwendungssymbol | Wiedererkennung von Zeitstrahl Studio | Anwendungsstart/Fenster, kein Command | Fenstertitel, Taskleiste, spätere Installer-/Dateizuordnung | Dokumentblatt mit klarer Zeitachse und zwei Ereignismarkern; keine Buchstaben | Zeitstrahl Studio | Zeitstrahl Studio |
| NAV-PROJECT-NEW-001 | Neues Projekt | Neues lokales Projekt anlegen | NewProjectCommand | Befehlsgruppe Projekt, Menü Datei | Leeres Dokument mit kleinem Plus rechts unten | Neues Projekt | Neues Projekt erstellen (Strg+N ohne geöffnetes Projekt) |
| NAV-PROJECT-OPEN-001 | Projekt öffnen | Vorhandenes Projektarchiv öffnen | OpenProjectCommand | Befehlsgruppe Projekt, Menü Datei | Geöffneter Ordner mit nach innen zeigendem Dokumentblatt | Projekt öffnen | Vorhandenes Zeitstrahlprojekt öffnen |
| NAV-PROJECT-SAVE-001 | Projekt speichern | Aktiven Projektstand speichern | SaveCommand | Befehlsgruppe Projekt, Menü Datei | Reduzierte Diskette mit klarer Aussparung | Projekt speichern | Projekt speichern (Strg+S) |
| NAV-EDIT-UNDO-001 | Rückgängig | Letzte fachliche Bearbeitung zurücknehmen | UndoCommand | Befehlsgruppe Bearbeiten, Menü Bearbeiten | Nach links zurücklaufender gebogener Pfeil | Rückgängig | Letzte Änderung rückgängig machen (Strg+Z) |
| NAV-EDIT-REDO-001 | Wiederholen | Zurückgenommene Bearbeitung erneut anwenden | RedoCommand | Befehlsgruppe Bearbeiten, Menü Bearbeiten | Nach rechts vorlaufender gebogener Pfeil | Wiederholen | Letzte rückgängig gemachte Änderung wiederholen (Strg+Y) |
| NAV-VIEW-HORIZONTAL-001 | Horizontale Ansicht | Horizontale Timeline aktivieren | SetHorizontalTimelineCommand | segmentierte Befehlsgruppe Ansicht | Rechteckrahmen mit horizontaler Achse und drei Markern | Horizontale Zeitstrahlansicht | Horizontalen Zeitstrahl anzeigen |
| NAV-VIEW-VERTICAL-001 | Vertikale Ansicht | Vertikale Timeline aktivieren | SetVerticalTimelineCommand | segmentierte Befehlsgruppe Ansicht | Rechteckrahmen mit vertikaler Achse und drei Markern | Vertikale Zeitstrahlansicht | Vertikalen Zeitstrahl anzeigen |
| NAV-ACTION-SEARCH-001 | Suchen | Projektweite Suche fokussieren | MainWindow.FocusSearchCommand | Befehlsgruppe Aktionen, Menü Werkzeuge | Lupe mit kreisförmiger Linse und kurzem Griff | Projekt durchsuchen | Suche fokussieren (Strg+F) |
| NAV-ACTION-ANALYZE-001 | Dokumente analysieren | Analysierbare Anhänge lokal neu analysieren | AnalyzeAttachmentsCommand | Befehlsgruppe Aktionen, Menü Werkzeuge | Dokumentblatt mit kleiner abstrahierter Prüffläche aus drei Punkten/Linien; kein Stern-/KI-Motiv | Dokumente analysieren | Anhänge des ausgewählten Ereignisses lokal analysieren |
| NAV-EXPORT-PDF-001 | PDF exportieren | Druckoptimierte PDF-Vorschau öffnen | PdfExportCommand | Befehlsgruppe Export, Menü Werkzeuge | Dokumentblatt mit deutlich lesbarer, aber geometrisch reduzierter Kennzeichnung PDF | PDF exportieren | Zeitstrahl als PDF exportieren |
| NAV-EXPORT-HTML-001 | HTML exportieren | Offline-HTML-Export starten | HtmlExportCommand | Befehlsgruppe Export, Menü Werkzeuge | Dokumentblatt mit zwei spitzen Klammern als Codezeichen | HTML exportieren | Zeitstrahl als eigenständige HTML-Datei exportieren |
| NAV-SETTINGS-001 | Einstellungen | Projektbezogene Darstellungseinstellungen öffnen | SettingsCommand | rechte Befehlsgruppe, Menü Ansicht | Einfaches Zahnrad mit sechs Zähnen und runder Mitte | Projekteinstellungen | Projekteinstellungen öffnen |
| LAYOUT-SIDEBAR-TOGGLE-001 | Navigation ein-/ausblenden | Linke Projekt-/Suchseitenleiste umschalten | MainWindow.SidebarToggle_Click | linker Rand der Projektansicht/Kopfbereich | Fensterrahmen mit hervorgehobener linker Spalte und kleinem Pfeil nach innen | Linke Navigation umschalten | Projektnavigation ein- oder ausblenden |
| LAYOUT-INSPECTOR-TOGGLE-001 | Details ein-/ausblenden | Rechten Ereignisinspektor umschalten | MainWindow.InspectorToggle_Click | rechter Rand der Projektansicht/Kopfbereich | Fensterrahmen mit hervorgehobener rechter Spalte und kleinem Pfeil nach innen | Ereignisdetails umschalten | Ereignisdetails ein- oder ausblenden |
| EVENT-ADD-001 | Ereignis hinzufügen | Neues Ereignis im aktiven Projekt anlegen | AddEventCommand | lokale Ereignisaktionsleiste, Menü Ereignis | Kleine Ereigniskarte mit Plus rechts unten | Ereignis hinzufügen | Neues Ereignis hinzufügen (Strg+N im Projekt) |
| FILTER-OPEN-001 | Filter | Erweiterte Suchfilter öffnen oder schließen | Expander-Zustand, kein fachlicher Command | linke Seitenleiste neben Suchfeld/Filterkopf | Trichter mit klarer oberer Öffnung und schmalem Auslauf | Erweiterte Filter | Erweiterte Filter ein- oder ausblenden |

## Assetdetails je Icon-ID

Für alle folgenden Befehlsicons gelten vollständig die gemeinsamen Angaben zu Perspektive, Outline, Strichstärke, Eckenform, 16×16-Arbeitsfläche, 16-DIP-Darstellung, Pixelgrößen, Mono-/Themefarben, Zuständen, PNG-Format, Zielordnern und getrennten Hell-/Dunkel-Assets. Diese Tabelle legt den eindeutigen Slug und damit alle acht exakten Dateinamen fest.

| Icon-ID | Slug | Exakte Dateinamen je Theme und DPI |
| --- | --- | --- |
| NAV-PROJECT-NEW-001 | new-project | new-project.light.16.png, new-project.light.20.png, new-project.light.24.png, new-project.light.32.png; new-project.dark.16.png, new-project.dark.20.png, new-project.dark.24.png, new-project.dark.32.png |
| NAV-PROJECT-OPEN-001 | open-project | open-project.light.16.png, open-project.light.20.png, open-project.light.24.png, open-project.light.32.png; open-project.dark.16.png, open-project.dark.20.png, open-project.dark.24.png, open-project.dark.32.png |
| NAV-PROJECT-SAVE-001 | save-project | save-project.light.16.png, save-project.light.20.png, save-project.light.24.png, save-project.light.32.png; save-project.dark.16.png, save-project.dark.20.png, save-project.dark.24.png, save-project.dark.32.png |
| NAV-EDIT-UNDO-001 | undo | undo.light.16.png, undo.light.20.png, undo.light.24.png, undo.light.32.png; undo.dark.16.png, undo.dark.20.png, undo.dark.24.png, undo.dark.32.png |
| NAV-EDIT-REDO-001 | redo | redo.light.16.png, redo.light.20.png, redo.light.24.png, redo.light.32.png; redo.dark.16.png, redo.dark.20.png, redo.dark.24.png, redo.dark.32.png |
| NAV-VIEW-HORIZONTAL-001 | timeline-horizontal | timeline-horizontal.light.16.png, timeline-horizontal.light.20.png, timeline-horizontal.light.24.png, timeline-horizontal.light.32.png; timeline-horizontal.dark.16.png, timeline-horizontal.dark.20.png, timeline-horizontal.dark.24.png, timeline-horizontal.dark.32.png |
| NAV-VIEW-VERTICAL-001 | timeline-vertical | timeline-vertical.light.16.png, timeline-vertical.light.20.png, timeline-vertical.light.24.png, timeline-vertical.light.32.png; timeline-vertical.dark.16.png, timeline-vertical.dark.20.png, timeline-vertical.dark.24.png, timeline-vertical.dark.32.png |
| NAV-ACTION-SEARCH-001 | search | search.light.16.png, search.light.20.png, search.light.24.png, search.light.32.png; search.dark.16.png, search.dark.20.png, search.dark.24.png, search.dark.32.png |
| NAV-ACTION-ANALYZE-001 | analyze-document | analyze-document.light.16.png, analyze-document.light.20.png, analyze-document.light.24.png, analyze-document.light.32.png; analyze-document.dark.16.png, analyze-document.dark.20.png, analyze-document.dark.24.png, analyze-document.dark.32.png |
| NAV-EXPORT-PDF-001 | export-pdf | export-pdf.light.16.png, export-pdf.light.20.png, export-pdf.light.24.png, export-pdf.light.32.png; export-pdf.dark.16.png, export-pdf.dark.20.png, export-pdf.dark.24.png, export-pdf.dark.32.png |
| NAV-EXPORT-HTML-001 | export-html | export-html.light.16.png, export-html.light.20.png, export-html.light.24.png, export-html.light.32.png; export-html.dark.16.png, export-html.dark.20.png, export-html.dark.24.png, export-html.dark.32.png |
| NAV-SETTINGS-001 | project-settings | project-settings.light.16.png, project-settings.light.20.png, project-settings.light.24.png, project-settings.light.32.png; project-settings.dark.16.png, project-settings.dark.20.png, project-settings.dark.24.png, project-settings.dark.32.png |
| LAYOUT-SIDEBAR-TOGGLE-001 | toggle-sidebar | toggle-sidebar.light.16.png, toggle-sidebar.light.20.png, toggle-sidebar.light.24.png, toggle-sidebar.light.32.png; toggle-sidebar.dark.16.png, toggle-sidebar.dark.20.png, toggle-sidebar.dark.24.png, toggle-sidebar.dark.32.png |
| LAYOUT-INSPECTOR-TOGGLE-001 | toggle-inspector | toggle-inspector.light.16.png, toggle-inspector.light.20.png, toggle-inspector.light.24.png, toggle-inspector.light.32.png; toggle-inspector.dark.16.png, toggle-inspector.dark.20.png, toggle-inspector.dark.24.png, toggle-inspector.dark.32.png |
| EVENT-ADD-001 | add-event | add-event.light.16.png, add-event.light.20.png, add-event.light.24.png, add-event.light.32.png; add-event.dark.16.png, add-event.dark.20.png, add-event.dark.24.png, add-event.dark.32.png |
| FILTER-OPEN-001 | filter | filter.light.16.png, filter.light.20.png, filter.light.24.png, filter.light.32.png; filter.dark.16.png, filter.dark.20.png, filter.dark.24.png, filter.dark.32.png |

## Anwendungssymbol APP-BRAND-001

- Perspektive/Form/Stil: orthografisch, geometrisch reduziert, gefüllte Markenfläche mit klarer Outline; leicht gerundete Ecken; Strichstärke optisch 1,5 Pixel bei 16 Pixel.
- Arbeitsfläche: quadratisch; zentrale Safe Area 14×14 innerhalb 16×16 logischer Einheiten.
- Darstellungsgröße: 20 DIP im späteren Markenbereich; Windows verwendet zusätzlich systemabhängige Größen.
- Erforderliche Pixelgrößen für 100/125/150/200 Prozent: 20×20, 25×25, 30×30 und 40×40; zusätzlich übliche ICO-Frames 16, 24, 32, 48, 64, 128 und 256 Pixel.
- Farbigkeit: mehrfarbig, aber auf Akzentblau, neutrales Grau und Weiß begrenzt.
- Hell/Dunkel: ein kontrastgeprüftes gemeinsames mehrrahmiges ICO; kein separates Themeasset nötig.
- Zustände: keine Hover-/Pressed-/Selected-Zustände; Disabled nicht anwendbar.
- Format: Mehrrahmen-ICO mit Alpha-Transparenz.
- Exakter Dateiname: zeitstrahl-studio.ico.
- Zielordner: src/ZeitstrahlStudio.App/Assets/Brand.
- AutomationProperties.Name: Zeitstrahl Studio.
- Tooltip: Zeitstrahl Studio.

## Liefer- und Lizenzprüfung

Vor Einbindung muss für jede Datei Herkunft, Urheber, Lizenz, Änderungsrecht und Weitergaberecht dokumentiert werden. Gelieferte Dateien werden auf exakte Abmessungen, Alpha-Hintergrund, Lesbarkeit in beiden Themes, konsistente optische Größe und fehlende Metadaten geprüft. Erst danach werden sie als WPF Resource eingebunden. Fehlende Dateien blockieren die textbasierte Phase-1–3-Oberfläche nicht.
