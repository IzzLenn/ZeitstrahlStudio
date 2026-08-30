# UI-Audit 0.2.1

> **Archivhinweis (Version 0.2.1, Audit vom 22.07.2026):** Dieses Dokument hält den damaligen Ausgangszustand und die Planungsgrundlage des UI-Redesigns fest. Es ist keine Beschreibung der heutigen Oberfläche. Den aktuellen Stand zeigen [`README.md`](README.md) und [`STATUS.md`](STATUS.md).

Stand: 22.07.2026
Ausgangs-Commit: `38d94282bc6bbe3ecf96bf40a5efb36976d89dd1`
Branch: `ui/redesign-0.3.0`

## Ausgewertete Bildanhänge

| Bild | IST/SOLL | Ansicht | Theme | Zweck | Relevante Phase |
| --- | --- | --- | --- | --- | --- |
| `01_IST_0.2.1_Hell_Horizontal.png` | IST | Horizontale Timeline | Hell | Tatsächlicher Ausgangszustand der installierten Version 0.2.1; belegt lange Kopfleiste, überladene Seitenleiste, kleine Typografie/Karten und große ungenutzte Arbeitsfläche. Keine Designvorgabe. | Phase 0 (Audit) |
| `02_IST_0.2.1_Dunkel_Horizontal.png` | IST | Horizontale Timeline | Dunkel | Tatsächlicher Ausgangszustand der installierten Version 0.2.1; belegt inkonsistente Theme-Vererbung und schwache Kontraste. Keine Designvorgabe. | Phase 0 (Audit) |
| `03_IST_0.2.1_Hell_Vertikal.png` | IST | Vertikale Timeline | Hell | Tatsächlicher Ausgangszustand der installierten Version 0.2.1; belegt sehr große Leerräume, kleine Karten, lange Seitenleiste und die aktuelle zentrale Achse. Keine Designvorgabe. | Phase 0 (Audit) |
| `04_SOLL_Hauptansicht_Hell.png` | SOLL | Hauptansicht mit horizontaler Timeline | Hell | Primäre Designreferenz mit höchster Priorität für Hauptnavigation, Informationshierarchie, linke Seitenleiste, zentrale Arbeitsfläche, rechten Detailinspektor, Typografie, Abstände und Bedienelemente. | Phasen 1–3; Karten-/Timeline-Details später |
| `05_SOLL_Projekteinstellungen_Hell.png` | SOLL | Modal: Projekteinstellungen | Hell | Referenz für Dialogkopf, Formulargruppen, Steuerelementgrößen, Hinweise sowie primäre/sekundäre Aktionen. In diesem Lauf nur analysiert; keine Dialogüberarbeitung. | Phase 0; Umsetzung ab Phase 4 |
| `06_SOLL_Vertikale_Timeline_Dunkel.png` | SOLL | Vertikale Timeline | Dunkel | Referenz für dunkle Kontraste, zentrale Achse, Kartenanordnung, Verbindungen, Kollisionsfreiheit, Abstände und nutzbare Arbeitsfläche. In diesem Lauf nur Themeaspekte, kein Timeline-Layout. | Phase 0/1; Layout später in Phase 6 |

Priorität der SOLL-Referenzen: (1) `04_SOLL_Hauptansicht_Hell.png`, (2) `05_SOLL_Projekteinstellungen_Hell.png` für Dialoge, (3) `06_SOLL_Vertikale_Timeline_Dunkel.png` für vertikale Timeline und dunkles Theme. Die drei IST-Bilder definieren ausschließlich den Ausgangszustand.

## Abgleich von Screenshots und tatsächlichem Code

| Sichtbarer Befund | Beleg im aktuellen Stand | Auswirkung |
| --- | --- | --- |
| Dunkelmodus wirkt nur teilweise dunkel | Jedes Fenster bindet lokal `Theme.Light.xaml` ein; `ApplicationThemeService` ersetzt nur das Dictionary in `Application.Resources`. Lokale Ressourcen haben Vorrang. | Dialoge und Hauptfenster können den dynamisch gewählten App-Pinsel verdecken; der direkt gezeichnete Zeitstrahl erhält dagegen separat `IsDarkTheme`. Dies erklärt die Mischdarstellung im IST-Dunkelscreenshot. |
| Lange, gleichrangige Kopfleiste | `MainWindow.xaml` enthält einen einzigen Header mit `WrapPanel` für Neu, Öffnen, Speichern, Speichern unter, Duplizieren, Schließen, Undo, Redo und Suche sowie rechts Einstellungen, Sicherungen und Protokoll. | Keine Menüebene, schwache Gruppierung und riskanter Umbruch/Abschneiden bei normaler Breite. |
| Linke Seitenleiste überladen | Eine 320-DIP-Spalte enthält Projektinformationen, sämtliche zwölf Suchkriterien, Ergebnisliste mit fester Höhe 330, zuletzt verwendete Projekte und Recovery in einem äußeren `ScrollViewer`. | Filter und Startkontext konkurrieren mit der Ereignisliste; die nutzbare Trefferhöhe ist nicht adaptiv. |
| Kein rechter Detailinspektor | Hauptgrid besitzt nur Seitenleiste, Splitter und Arbeitsbereich. Anhangsdetails existieren nur im zweiten Haupttab. | Auswahlkontext und Anhänge sind nicht dauerhaft erkennbar; Bearbeitung erfordert Dialoge oder Tabwechsel. |
| Kleine Typografie und Ziele | `FontSizeXs=10`, `Sm=11`, `Base=12`; Toolbarhöhe 32, kompakte Buttons 28. | Meta- und Bedieninformation liegt unter den Zielwerten 11–12 beziehungsweise 13–14 DIP. |
| Unklare aktive Orientierung | Horizontal/Vertikal sind zwei normale Buttons; der aktive Zustand ist nur über `CanExecute=False` ableitbar. | Deaktiviert und ausgewählt sind visuell nicht dasselbe, die Ansicht ist nicht als Segmentumschalter erkennbar. |
| Große ungenutzte Timelinefläche | `TimelineLayoutEngine` verwendet feste Kartenmaße und zentriert die Querachse anhand des Content-Extents. | Der IST-Screenshot zeigt kleine Karten in einer sehr großen Fläche. Kartenlayout ist erst ab Phase 5/6 zu ändern. |
| Überlagerte Timelinebeschriftungen | Ticks, Unterbrechungen, Fristen und Karten werden in `TimelineView.OnRender` nacheinander gezeichnet; für Beschriftungen existiert keine eigene Kollisionsspur. | Im IST-Screenshot überlagern sich rote Lücken-/Fristtexte und nahe Ticks. Die Behebung gehört ausdrücklich nicht in Phase 0–3. |
| Dialoge wirken uneinheitlich | Dialoge definieren eigene Header- und Labelstile und laden jeweils das helle Theme lokal. | Themewechsel und Hierarchie sind inkonsistent. In Phase 1 wird nur die Theme-Vererbung konsolidiert; die Dialogüberarbeitung bleibt Phase 4. |
| Projekteinstellungsdialog weicht vom SOLL ab | ProjectSettingsDialog.xaml nutzt einen durchgehenden dunklen Header, eine einfache vertikale Formularliste und Textfelder für Schriftgrößen. | Bild 05 fordert einen hell integrierten Dialogkopf, klarere Gruppen, größere Auswahlfelder und Stepper. Diese Struktur wird dokumentiert, aber erst ab Phase 4 umgesetzt. |

## Aktuelle UI-Struktur

1. Fensterweite Eingabebindungen für Speichern, Suche, kontextabhängiges Neu, Undo, Redo, Löschen und Abbruch.
2. Einzeiliger dunkler Header mit Markenblock, Hauptbefehlen und drei Verwaltungsbefehlen.
3. Linke, per Code-behind auf 0/320 DIP schaltbare Seitenleiste mit Projekt, Suche, Filtern, Treffern, Recent Projects und Recovery.
4. Zentraler Startzustand oder Projektzustand.
5. Projektzustand mit Titel/Aktionszeile und Tabs `Zeitstrahl`/`Ereignisliste`.
6. Im Zeitstrahltab zwei lokale Werkzeugzeilen und ein `TimelineView` in einem `ScrollViewer`.
7. Im Ereignislistentab zweispaltige Liste plus 310-DIP-Anhangsbereich.
8. Untere Statusleiste mit Status, Busy, Abbruch, Zoom, Zeitraum und Filterzahl.

## Globale und kontextbezogene Befehle

- Projekt/Anwendung: `NewProjectCommand`, `OpenProjectCommand`, `OpenRecentCommand`, `SaveCommand`, `SaveAsCommand`, `DuplicateCommand`, `CloseProjectCommand`, `RefreshCommand`, `RecoverCommand`, `DiscardRecoveryCommand`, `SettingsCommand`, `ManageBackupsCommand`, `ShowAuditLogCommand`.
- Ereignis/Sortierung: `AddEventCommand`, `EditEventCommand`, `DeleteEventCommand`, `MoveEventEarlierCommand`, `MoveEventLaterCommand`, `ReorderEventCommand`, `UndoCommand`, `RedoCommand`.
- Timeline/Suche: `SetHorizontalTimelineCommand`, `SetVerticalTimelineCommand`, `ToggleGapCompressionCommand`, `MoveTimelineCardCommand`, `ResetTimelineLayoutCommand`, `ShowTimelineRangeCommand`, `ResetSearchFiltersCommand`, `SelectSearchResultCommand` sowie lokale View-Aktionen für Zoom, Alles anzeigen, Auswahl zentrieren und Ansicht zurücksetzen.
- Dokumente/Export: `AddAttachmentsCommand`, `ImportDroppedFilesCommand`, `AnalyzeAttachmentsCommand`, `ShowAttachmentAnalysisCommand`, `PreviewImageCommand`, `PreviewPdfCommand`, `OpenAttachmentCommand`, `RemoveAttachmentCommand`, `CancelAttachmentImportCommand`, `PdfExportCommand`, `HtmlExportCommand`.

## Theme- und Ressourcenbestand

- `Theme.Light.xaml` und `Theme.Dark.xaml` definieren Hintergründe, Text, Akzent, Rahmen, Zustände, Gefahr/Warnung/Erfolg/Info sowie Timeline-Pinsel.
- Semantische Lücken: eigenständige Schlüssel für Navigation, Arbeitsfläche, Kartenoberfläche, erhöhte Oberfläche, Read-only, Invalid, Menü, Inspektor und ausgewählte Navigation fehlen.
- `ApplicationThemeService` unterstützt Hell, Dunkel und Windows-App-Einstellung ohne Neustart.
- Kritischer Fehler: lokale Theme-Dictionaries in jedem Fenster übersteuern den dynamischen Anwendungspinsel.
- Hart codierte produktive Farben außerhalb der Theme-Dictionaries: zwei Highlight-Pinsel in `HighlightedTextBlock.cs`, Deadline-Rahmen `#92400E` und die Rendererpalette in `TimelineView.ApplyPalette`. Die Rendererpalette ist durch ADR-034 bewusst lokal; Highlight- und Deadlinefarben sollen später semantisch beziehungsweise palettengebunden werden.

## Typografie

- Schriftfamilie: `Segoe UI`, Überschriften `Segoe UI Semibold`, Monospace `Consolas`; auf Windows 10/11 vorhanden.
- Skala: 10, 11, 12, 13, 14, 16, 18, 20, 24, 28 DIP.
- Ist-Verwendung: Projektname im Hauptbereich 20 DIP, Sidebar-Projektname 18 DIP, Grundtext 12 DIP, zahlreiche Metaangaben 10 DIP.
- Ziel für Phase 1: Grundtext 13 DIP, Meta 12 DIP, Abschnitt 14 DIP, Seitenüberschrift 20 DIP, Projekttitel 24 DIP; 10-DIP-Texte werden aus dem Hauptfenster entfernt.

## Größen, Breiten und Scrollbereiche

- Hauptfenster: 1280×760, Minimum 960×600.
- Linke Spalte: 320 DIP, Minimum 260, Maximum 420; Splitter 5 DIP.
- Noch keine rechte Hauptspalte und kein rechter Splitter.
- Ergebnisliste: feste Höhe 330 DIP; Ereignislisten-Anhangsbereich: feste Breite 310 DIP.
- Timeline-Datumsfelder: feste Breite 125 DIP; Projekt-Starttext 470 DIP.
- Äußere Sidebar, Timeline, Ereignisliste, Anhänge und mehrere Dialoginhalte besitzen eigene ScrollViewer. Der äußere Sidebar-ScrollViewer verhindert eine sinnvoll mitwachsende Trefferliste.

## Dialogbestand

- Dialoge: Neues Projekt, Projekteinstellungen, Ereignisbearbeitung, Sicherungen, PDF-Export, HTML-Export, Bild-/PDF-Vorschau, Anhangsauswahl, Dokumentanalyse und Audit.
- Die meisten Dialoge besitzen Mindestgrößen und Enter/Escape über `IsDefault`/`IsCancel`; lange Inhalte sind überwiegend scrollbar.
- Alle Dialoge laden derzeit lokal das helle Theme und verhindern damit zuverlässige Theme-Vererbung.
- Die strukturelle Dialogüberarbeitung ist ausdrücklich Phase 4 und wird in diesem Auftrag nicht begonnen.

## Timelinekarten und Kollisionsbehandlung

- Kartengröße bei 14 DIP: horizontal ungefähr 260×136 DIP; vertikal werden Achs- und Quermaß getauscht.
- Größe wächst mit `sqrt(CardFontSize/14)` beziehungsweise `24 + 8 × FontSize`.
- Automatische Platzierung wechselt positive/negative Achsenseite und wählt pro Seite die erste freie Lane mit 22 DIP Abstand.
- Manuelle Versätze werden nach der automatischen Lane-Zuweisung angewendet und können deshalb neue Konflikte erzeugen; eine zweite Kollisionsprüfung fehlt.
- Karten werden am Achsenanfang begrenzt, aber nicht explizit auf den aktuellen Viewport; der Scroll-Extent wächst bis zu Karten/Fristen.
- Tick-, Lücken-, Marker- und Kartenbeschriftungen besitzen keine gemeinsame Kollisionsauflösung.
- Diese Punkte werden dokumentiert, aber wegen der Auftragsgrenze nicht in Phase 0–3 geändert.

## Iconbestand und Formatprüfung

- Im App-Projekt existieren keine `.ico`, `.png`, `.svg` oder sonstigen Bildassets.
- Sichtbare Symbolersatzzeichen sind Unicode-Inhalte (`☰`, `＋`, `−`, `◀`, `▶`) und keine lizenzierten Iconressourcen.
- WPF unterstützt ohne neue Abhängigkeit zuverlässig Rasterbilder über `BitmapImage`/Pack-URI: PNG, ICO, BMP, JPEG und GIF. Mehrrahmige ICO-Dateien sind für Anwendung/Fenster geeignet; transparente PNG-Dateien sind für Befehlsleisten geeignet.
- SVG wird von WPF ohne zusätzliche Bibliothek nicht nativ unterstützt. Neue XAML-Path-Geometrien sind laut Auftrag ebenfalls ausgeschlossen.
- Daher bleiben Phasen 1–3 textbasiert. Vollständige Lieferanforderungen stehen in `ICON_REQUIREMENTS.md`.

## Vorhandene UI-Tests

- `MainWindowAccessibilityTests`: Tastengesten, Automation-Namen, Tooltips und Layout bei 100/125/150/200 Prozent.
- `TimelineViewTests`: reales STA-Rendering beider Ausrichtungen, Zoom/Navigation, Thumbnail-Auftrag und DPI-Skalierung.
- `MainWindowViewModelDropTests`: zielgenauer Mehrfachdatei-Drop.
- `BackupManagerDialogTests`, `PdfExportDialogTests`: reale Dialoginitialisierung auf STA.
- `ProjectSettingsDialogViewModelTests`: Theme-/Orientierungs-/Schriftvalidierung.
- Fehlende Abdeckung: Theme-Key-Vollständigkeit und Kontrast, Fensterstruktur bei fünf Zielgrößen, einklappbare Seitenbereiche und fehlender horizontaler Gesamt-Scrollbalken.

## Abgrenzung

Phasen 0–3 verändern keine Geschäftslogik, Datenbank, Archive, Dokumentverarbeitung oder Timeline-Kollisionsberechnung. Startansicht, Dialoglayout, Kartenredesign, Timeline-Layout, vollständige Accessibility-Abnahme, Publish und Installer bleiben Voraussetzungen für den Folgeauftrag ab Phase 4.
