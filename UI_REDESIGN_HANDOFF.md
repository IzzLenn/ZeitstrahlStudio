# UI-Redesign - Übergabeprotokoll

- Letzte Aktualisierung: 2026-07-22 14:27:54 +02:00
- Repository: `C:\Projekte\ZeitstrahlStudio`
- Branch: `ui/redesign-0.3.0`
- Remote: `origin` (`https://github.com/IzzLenn/ZeitstrahlStudio.git`)
- Ausgangs-Commit: `38d94282bc6bbe3ecf96bf40a5efb36976d89dd1`
- Phase-3-Funktionscommit: `bf5901b` (`feat(ui): responsive Hauptansicht mit Detailinspektor umsetzen`).
- Remote-Status: Phase-3-Funktionscommit und dieses abschließende Protokoll sind auf `origin/ui/redesign-0.3.0` enthalten.
- Ausführendes Modell: Codex auf Basis von GPT-5; eine separate Reasoning-Konfiguration wird von dieser Laufzeit nicht ausgewiesen.
- Aktuell bearbeitete Phase: Phase 3 abgeschlossen; der Auftrag endet hier.

## Verbindlicher Umfang dieses Laufs

In diesem Lauf werden ausschließlich die Phasen 0 bis 3 abgeschlossen. Die Phasen 4 bis 8 sind zukünftige Arbeiten für einen nachfolgenden Codex-Chat und werden in diesem Lauf nicht begonnen.

Ausgeschlossen sind insbesondere Startansicht und Dialogüberarbeitung (Phase 4), Timeline-Werkzeuge und Ereigniskarten (Phase 5), Timeline-Layout (Phase 6), Accessibility-/DPI-Gesamtprüfung (Phase 7), vollständige visuelle Abnahme (Phase 8), vollständiger finaler Release-Prüfzyklus, Publish, Vorschauinstaller, Versionsänderungen und Releasevorbereitung.

## Abgeschlossene Phasen

### Phase 0 - Tatsächlichen Ausgangszustand prüfen

- `SPEC.md`, `STATUS.md`, `DECISIONS.md`, Build-/Release-Dokumentation, die vollständige Aufgabenbeschreibung sowie relevante XAML-, C#-, Theme-, Layout- und Testdateien gelesen.
- Alle sechs Anhänge eindeutig als drei IST- und drei SOLL-Bilder eingeordnet; `04_SOLL_Hauptansicht_Hell.png` besitzt die höchste Gestaltungspriorität.
- `05_SOLL_Projekteinstellungen_Hell.png` für spätere Dialogarbeit und `06_SOLL_Vertikale_Timeline_Dunkel.png` für spätere vertikale/dunkle Timelinearbeit analysiert, ohne diese Arbeiten zu beginnen.
- Ist-Struktur, Befehle, Theme-Ressourcen, Typografie, Breiten, Scrollbereiche, Dialoge, Timelinekarten, Kollisionsmodell, vorhandene Unicode-Symbole und Tests dokumentiert.
- Umsetzungsplan für die sichtbaren Arbeiten der Phasen 1 bis 3 konkreten Dateien und Komponenten zugeordnet.
- Icon-Gate abgeschlossen: keine geeigneten App-Assets vorhanden; klare Textbeschriftungen werden bis zur Lieferung freigegebener WPF-Assets verwendet.
- Baseline gebaut und mit gezielten Tests verifiziert.

### Phase 1 - Designsystem und Themes

- In beiden Themes 21 identische semantische Ressourcen für Navigation, Befehlsleiste, Arbeitsfläche, Karten, Inspektor, Dialoge, Menüs, Nur-Lesen, Fehler und Zustände ergänzt.
- Typografieskala auf 12/13/14/16 DIP für Meta-, Grund- und Abschnittstexte sowie 20/24 DIP für Überschriften angehoben.
- Gemeinsame Steuerungsstile für Pressed, Fokus, Auswahl, Deaktiviert, Nur-Lesen und Invalid erweitert; Menü-/Tooltip-Stile vorbereitet.
- Feste `Theme.Light.xaml`-Einbindung aus `MainWindow.xaml` und elf Dialogen entfernt; nur `App.xaml` hält das globale Starttheme.
- `ThemeResourceTests.cs` prüft Schlüsselvertrag, kritische Kontraste, Typografieskala und fehlende lokale Theme-Festlegung.
- Debug-Build erfolgreich mit 0 Warnungen/0 Fehlern; 17 gezielte Tests bestanden.

### Phase 2 - Globale Navigation

- Hauptmenü mit `Datei`, `Bearbeiten`, `Ansicht`, `Ereignis`, `Werkzeuge` und `Hilfe` ergänzt und vorhandene Commands vollständig zugeordnet.
- Gruppierte Befehlsleiste mit Navigation, Neu/Öffnen/Speichern, Undo/Redo, Horizontal/Vertikal, Suchen/Analysieren, PDF/HTML und Einstellungen umgesetzt.
- Unicode-Hamburger entfernt; Navigation bleibt gemäß Icon-Gate textbasiert.
- Aktive Timelineorientierung über Selected-Ressourcen dargestellt; beide Orientierungscommands bleiben ausführbar, identische Auswahl ist weiterhin ein No-op.
- Lokalen Info-Menüpunkt ergänzt und Navigationstest für Struktur, Befehle, Symbolfreiheit und 1280-DIP-Grenzen hinzugefügt.
- Debug-Build erfolgreich mit 0 Warnungen/0 Fehlern; 11 gezielte MainWindow-/Theme-Tests bestanden.
- Laufzeit-UI-Automation bestätigt Hauptmenü und 13 sichtbare Befehlsleistenbuttons innerhalb des 1280×760-Fensters.

### Phase 3 - Responsive Hauptansicht

- Hauptansicht als adaptive Präsentationsschale mit linker Projekt-/Filterleiste, zentraler Timeline-/Listenfläche und rechtem Detailinspektor umgesetzt.
- Linke Navigation auf aktuelle Projektinformation, progressive Schnell-/Erweitertfilter und eine verbleibende Ereignisergebnisliste konzentriert; der Startzustand bleibt funktional erhalten und wurde nicht als Phase-4-Redesign bearbeitet.
- Detailinspektor mit Allgemein-, Anhänge- und Notizenbereichen ergänzt; Anzeigeformate sind deutsch, Änderungen laufen ausschließlich über bestehende Bearbeiten-/Löschen-/Anhangsbefehle.
- Doppelte Anhangsfläche aus der Ereignisliste entfernt und ohne Funktionsverlust in den kontextbezogenen Inspektor verlagert.
- Navigation und Inspektor sind unabhängig ein-/ausblendbar; automatische Breitenentscheidung liegt als reine Darstellungslogik in `MainWindow.xaml.cs`.
- Referenzgrößen 1280×720, 1366×768, 1600×900, 1920×1080 und 2560×1440 durch automatisierte Layouttests abgedeckt; zusätzlicher Test prüft das Einklappen und Wiederherstellen beider Seitenbereiche.
- Debug-Build erfolgreich mit 0 Warnungen/0 Fehlern; 17/17 gezielte MainWindow-/Theme-Tests bestanden.
- Laufzeit-UI-Automation bei 1280×720 bestätigte eine sichtbare Timeline und 45 Schaltflächen ohne Überlauf über das Fensterrechteck.

## Bildreferenzen und Priorität

1. `01_IST_0.2.1_Hell_Horizontal.png` - tatsächlicher heller horizontaler 0.2.1-Stand, nur IST.
2. `02_IST_0.2.1_Dunkel_Horizontal.png` - tatsächlicher dunkler horizontaler 0.2.1-Stand, nur IST.
3. `03_IST_0.2.1_Hell_Vertikal.png` - tatsächlicher heller vertikaler 0.2.1-Stand, nur IST.
4. `04_SOLL_Hauptansicht_Hell.png` - primäre und höchstpriorisierte Designreferenz für die Hauptansicht.
5. `05_SOLL_Projekteinstellungen_Hell.png` - Referenz für Dialoge; in Phase 0 ausgewertet, Umsetzung erst in einem späteren Auftrag.
6. `06_SOLL_Vertikale_Timeline_Dunkel.png` - Referenz für dunkles Theme und vertikale Timeline; in Phase 0 ausgewertet, Layoutumsetzung erst in einem späteren Auftrag.

Die vollständige Zuordnungstabelle steht in `UI_AUDIT_0.2.1.md`.

## Wesentliche belegte UI-Probleme

- Hauptfenster und Dialoge binden lokal `Theme.Light.xaml` ein und übersteuern dadurch den vom Anwendungstheme gewechselten Ressourcensatz; das erklärt die sichtbare Hell-/Dunkel-Mischdarstellung.
- Die obere Navigation ist eine lange, wenig gegliederte Befehlszeile und priorisiert die Kernaktionen nicht entsprechend der SOLL-Hauptansicht.
- Die geöffnete Projektansicht besitzt nur linke Seitenleiste und Arbeitsfläche; ein permanenter kontextbezogener Detailinspektor fehlt.
- Startinhalte, Projektinformationen, vollständige Filter und Ergebnisliste teilen sich einen starr breiten, außen gescrollten Bereich; progressive Offenlegung und adaptive Breiten fehlen.
- Basisschriftgrößen und kompakte Steuerungshöhen liegen sichtbar unter der SOLL-Anmutung.
- Ereigniskarten und Timeline-Kollisionen weichen von den Zielbildern ab; diese Punkte sind bewusst für die Phasen 5 und 6 zurückgestellt.

## Vorhandene UI-Commits

- `20bf4c6` - `docs: UI-Ausgangszustand und Redesignplan dokumentieren`
- `41511b1` - `docs: Phase-0-Übergabe protokollieren`
- `e30489f` - `feat(ui): Designsystem und Themes vereinheitlichen`
- `78bd82a` - `feat(ui): globale Navigation neu ordnen`
- `bf5901b` - `feat(ui): responsive Hauptansicht mit Detailinspektor umsetzen`

## Betroffene Dateien

- `UI_AUDIT_0.2.1.md` - vollständiger Audit einschließlich Sechs-Bilder-Tabelle.
- `UI_REDESIGN_PLAN.md` - verbindliche Abbildung der Phasen 1 bis 3 und spätere Einordnung der Phasen 4 bis 8.
- `ICON_REQUIREMENTS.md` - Asset-Gate, Spezifikation und Dateinamen.
- `UI_REDESIGN_HANDOFF.md` - dieses fortlaufende Protokoll.
- `STATUS.md` - aktueller Redesignstand und Phase-0-Ergebnis.
- `DECISIONS.md` - ADR-037 zur Theme-Vererbung, Navigation und adaptiven Präsentationsschale.
- `src/ZeitstrahlStudio.App/Themes/Theme.Light.xaml` und `Theme.Dark.xaml` - semantischer Hell-/Dunkelvertrag.
- `src/ZeitstrahlStudio.App/Themes/Typography.xaml` - lesbare Skala.
- `src/ZeitstrahlStudio.App/Themes/ControlStyles.xaml` - gemeinsame Zustände und Menüstile.
- `src/ZeitstrahlStudio.App/MainWindow.xaml` und elf `*Dialog.xaml` - lokale Light-Theme-Übersteuerung entfernt.
- `tests/ZeitstrahlStudio.IntegrationTests/ThemeResourceTests.cs` - Phase-1-Vertragstests.
- `src/ZeitstrahlStudio.App/MainWindow.xaml` - Menü- und Befehlsleiste.
- `src/ZeitstrahlStudio.App/MainWindow.xaml.cs` - lokaler Info-Dialog.
- `src/ZeitstrahlStudio.App/MainWindowViewModel.cs` - ausführbare Orientierungssegmente mit vorhandenem No-op.
- `tests/ZeitstrahlStudio.IntegrationTests/MainWindowAccessibilityTests.cs` - Navigations-, Referenzgrößen- und Panelvertrag.
- `src/ZeitstrahlStudio.App/MainWindow.xaml` - adaptive Drei-Bereichs-Schale und Detailinspektor.
- `src/ZeitstrahlStudio.App/MainWindow.xaml.cs` - responsive Sichtbarkeit und Panel-Toggles.
- `src/ZeitstrahlStudio.App/MainWindowViewModel.cs` - deutsche Präsentationseigenschaften für die aktuelle Ereignisauswahl.

## Tatsächlich sichtbare Änderungen

Hell- und Dunkelmodus besitzen denselben semantischen Ressourcenvertrag. Die globale Navigation besteht aus einem normalen sechsgruppigen Hauptmenü und einer kompakten, textbasierten Befehlsleiste. Im geöffneten Projekt gliedert sich die Oberfläche nun klar in Projekt-/Filterleiste, zentrale Timeline beziehungsweise Ereignisliste und rechten Detailinspektor. Auswahlbezogene Metadaten, Anhänge und Notizen stehen ohne Wechsel in einen Bearbeitungsdialog bereit; beide Seitenbereiche lassen sich unabhängig ausblenden.

## Build- und Testergebnisse

- `dotnet build ZeitstrahlStudio.sln -c Debug --no-restore`: erfolgreich, 0 Warnungen, 0 Fehler.
- Gezielte Debug-Integrationstests mit Filter auf `MainWindowAccessibilityTests`, `TimelineViewTests`, `ProjectSettingsDialogViewModelTests`, `BackupManagerDialogTests` und `PdfExportDialogTests`: 17/17 bestanden.
- Phase 1: erneuter Debug-Build erfolgreich, 0 Warnungen/0 Fehler; 17/17 gezielte Tests einschließlich `ThemeResourceTests` bestanden.
- Phase 2: Debug-Build erfolgreich, 0 Warnungen/0 Fehler; nach Korrektur der nichtanzeigenden WPF-Messmethode 1/1 Einzeltest und 11/11 gezielte MainWindow-/Theme-Tests bestanden.
- Phase 3: Debug-Build erfolgreich, 0 Warnungen/0 Fehler; 5/5 Referenzgrößenfälle und anschließend 17/17 gezielte MainWindow-/Theme-Tests bestanden.

## Aktuelle Screenshots

Die sechs Gesprächsanhänge wurden vollständig ausgewertet. Der gebaute Phase-1-Stand wurde mit dem Beispielprojekt erfolgreich gestartet und als `artifacts/ui-redesign/phase-1-theme.png` aufgenommen. Die direkte Bildanzeige scheiterte zweimal am Timeout des Windows-Sandbox-Bildhelpers; ein alternativer In-Memory-Versuch konnte das PNG ebenfalls nicht rendern.

Phase 2 wurde als `artifacts/ui-redesign/phase-2-navigation.png` aufgenommen. Der lokale Bildhelper scheiterte erneut am Timeout; die ergänzende UI-Automation-Prüfung bestätigte das sichtbare Hauptmenü und alle 13 Befehle innerhalb des Fensterrechtecks.

Phase 3 wurde als `artifacts/ui-redesign/phase-3-main-window.png` bei 1280×720 aufgenommen. Der Windows-Sandbox-Bildhelper scheiterte weiterhin beim Start; UI Automation bestätigte die sichtbare Timeline sowie 45 Schaltflächen ohne horizontalen Überlauf.

## Technische Entscheidungen

- ADR-037: Themes werden global aus `Application.Resources` vererbt; lokale feste Light-Theme-Einbindungen werden entfernt.
- Die neue Navigation bleibt textbasiert, solange keine freigegebenen, themefähigen WPF-Assets vorliegen.
- Die Hauptansicht wird als adaptive linke Seitenleiste, zentrale Arbeitsfläche und rechter Inspektor umgesetzt; reine Breiten-/Sichtbarkeitszustände dürfen in der View liegen, Fachänderungen bleiben in ViewModel und Diensten.
- Die vorhandene Benutzeränderung an `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` wird weder verändert noch gestaged noch committed.
- Es wird ausschließlich auf `ui/redesign-0.3.0` gearbeitet; kein Merge, Release-Tag, Publish, Installer, Versionswechsel oder Force-Push.

## Abweichungen und bekannte Fehler

- Keine Abweichung vom korrigierten Umfang: Phase 4 bis 8 wurden nicht begonnen.
- Keine offenen Laufzeitfehler in Phase 3 ermittelt. Ein zunächst ungültiger Button-Stilverweis und die WPF-Testmessung eines nicht angezeigten Fensters wurden vor der erfolgreichen Wiederholung korrigiert.
- Der neue Navigationstest scheiterte zunächst zweimal an `ActualWidth`/`DesiredSize` eines nicht angezeigten `Window`; nach Umstellung auf das bereits verwendete direkte Messen von `window.Content` bestand der Einzeltest und anschließend der vollständige gezielte Filter.
- Die vorhandene Änderung am Beispielarchiv ist eine fremde Arbeitsbaumänderung und bleibt geschützt.
- `apply_patch` wird entsprechend der Werkzeugvorgabe in diesem Chat nicht verwendet.

## Arbeitsbaum und Push-Status

- Vorhandene fremde Änderung: `M samples/ZeitstrahlStudio-Beispiel.zeitprojekt`; sie wurde nicht verändert, gestaged oder committed.
- Phase 3 ist in `bf5901b` committed und auf `origin/ui/redesign-0.3.0` gepusht.
- Dieses nach dem Push aktualisierte Handoff wird als separater Dokumentationsabschluss committed und gepusht.
- Danach verbleibt ausschließlich die geschützte Benutzeränderung am Beispielarchiv im Arbeitsbaum.

## Fehlende Icons und Iconanforderungsstatus

Im App-Projekt existiert kein verwendbarer Icon-/Asset-Satz für die geforderte Navigation. `ICON_REQUIREMENTS.md` definiert Perspektive, Kontur, Strichstärke, Rundungen, Raster, Größen, Farben, Zustände, Format, Theme-Trennung, exakte Zielordner und Dateinamen für 17 Funktionsgruppen. Neue Grafiken werden in diesem Lauf nicht erfunden; Befehle erhalten eindeutige Textbezeichnungen.

## Offene Aufgaben

### Aktueller Auftrag

- Phase 0 abgeschlossen.
- Phase 1 abgeschlossen.
- Phase 2 abgeschlossen.
- Phase 3 abgeschlossen.
- Debug-Build, gezielte Tests, Commit, Push und Übergabe abgeschlossen.
- Dieser Auftrag endet zwingend an dieser Stelle.

### Späterer Auftrag

- Phasen 4 bis 8.
- Vollständige Endverifikation einschließlich vollständigem Release-Prüfzyklus.
- Lokaler Vorschauinstaller.
- Publish, Versionsänderungen und Releasevorbereitung.

## Voraussetzungen für Phase 4 in einem späteren Chat

- Phase 1 bis 3 müssen committed, auf `origin/ui/redesign-0.3.0` gepusht und im Übergabeprotokoll dokumentiert sein.
- Der neue globale Theme-Satz, die Navigation und die responsive Hauptansicht müssen als stabile Grundlage vorliegen.
- `05_SOLL_Projekteinstellungen_Hell.png` ist als primäre Dialogreferenz zu verwenden; Dialogänderungen dürfen erst dann begonnen werden.
- Die geschützte Benutzeränderung am Beispielprojekt muss weiterhin getrennt bleiben.
- Vor Beginn sind `SPEC.md`, `STATUS.md`, `DECISIONS.md`, `UI_AUDIT_0.2.1.md`, `UI_REDESIGN_PLAN.md`, `ICON_REQUIREMENTS.md` und dieses Protokoll vollständig zu lesen.

## Nächster atomarer Arbeitsschritt

Kein weiterer Arbeitsschritt in diesem Lauf. Ein neuer, ausdrücklich freigegebener Auftrag kann mit Phase 4 beginnen; Phasen 4 bis 8 bleiben zukünftige Arbeiten.

## Exakter Fortsetzungsprompt für einen nachfolgenden Chat

> Öffne `C:\Projekte\ZeitstrahlStudio`, arbeite ausschließlich auf Branch `ui/redesign-0.3.0` und lies `SPEC.md`, `STATUS.md`, `DECISIONS.md`, `UI_AUDIT_0.2.1.md`, `UI_REDESIGN_PLAN.md`, `ICON_REQUIREMENTS.md` und `UI_REDESIGN_HANDOFF.md` vollständig. Bewahre die nicht zu diesem Auftrag gehörende Änderung an `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` unverändert und außerhalb aller Commits. Beginne ausschließlich nach ausdrücklicher Freigabe mit Phase 4. Die Phasen 5 bis 8 bleiben bis zu ihrer jeweiligen Freigabe unangetastet. Führe weder Publish noch Installer, Versionsänderung, Releasevorbereitung, Merge, Tag oder Force-Push aus.

## Fortsetzungslauf Phase 4–6 – 22.07.2026

- Modell/Reasoning: GPT-5 Codex, Standard.
- Ausgangs-HEAD: `afce53472e28b435a283343d563a20db921cc366` (`origin/ui/redesign-0.3.0` identisch).
- Computer Use: aktiviert; Initialisierung und Sicherheitsleitfaden wurden gelesen. Sichtprüfung erfolgt ausschließlich damit.
- Aktuelle Phase: 4 (Startansicht und Dialoge).
- Arbeitsbaum zu Beginn: ausschließlich die geschützte, fremde Änderung `M samples/ZeitstrahlStudio-Beispiel.zeitprojekt`; nichts gestaged.
- Sichtbare Baseline: Phase-3-Drei-Bereichs-Hauptansicht gemäß Übergabe vorhanden; die aktuellen SOLL-Referenzen und die gesamte Phase-4–6-Anweisung wurden geprüft.
- Nächster atomarer Arbeitsschritt: bestehende Startansicht, Dialoge, TimelineView und Layouttests vollständig lesen und den Phase-4-Umfang ableiten.

### Zwischenstand Phase 4

- Computer-Use-Prüfung: aktueller Debug-EXE-Pfad `src/ZeitstrahlStudio.App/bin/Debug/net8.0-windows10.0.19041.0/win-x64/ZeitstrahlStudio.App.exe`, Commit `afce53472e28b435a283343d563a20db921cc366`. Start und Fenstererkennung waren erfolgreich; die notwendige Zustandsaufnahme schlägt reproduzierbar mit `SetIsBorderRequired failed: Schnittstelle nicht unterstützt (0x80004002)` fehl. Daher keine visuelle Abnahme und keine Screenshots; dies ist ein technischer Computer-Use-Blocker.
- Umgesetzt: Der Dialog „Neues Projekt“ verwendet jetzt einen klar gegliederten Kopfbereich mit Erklärung, ein dauerhaft sichtbares Label, ein 36-DIP-Feld und eine getrennte Aktionsleiste. Der Projekteinstellungen-Dialog verwendet die semantische Dialogoberfläche, damit er sicher dem aktiven Theme folgt.
- Technische Prüfung: `dotnet build ZeitstrahlStudio.sln -c Debug --no-restore` erfolgreich (0 Warnungen, 0 Fehler); gezielte Dialog-/Hauptfenstertests 19/19 bestanden.
- Verbleibend in Phase 4: Startansicht und die weiteren priorisierten Dialoge nach derselben Struktur überarbeiten, danach erst Phase 5 beginnen.

### Commit und Push des Phase-4-Teilschritts

- Commit: `71b363bc82a70a4ce932d2a8e1d35d644478fb48` – `ui: Dialogstart vereinheitlichen`.
- Push: erfolgreich; lokaler und entfernter Feature-Branch stehen auf demselben Commit.
- Stagingprüfung vor Commit: ausschließlich `STATUS.md`, `UI_REDESIGN_HANDOFF.md`, `NewProjectDialog.xaml` und `ProjectSettingsDialog.xaml`; das Beispielarchiv war nicht enthalten.
- Verbleibende Phasen dieses Auftrags: Phase 4 vollständig abschließen, danach Phase 5 und 6; Phase 7 und 8 bleiben ausdrücklich ausgenommen.

## Fortsetzungslauf Phase 4–7 bis zum visuellen Review-Gate – 25.07.2026

- Modell/Reasoning: GPT-5 Codex; Laufzeit-Reasoning nicht separat ausgewiesen.
- Ausgangs-HEAD / Remote-HEAD: `5a164e79df3411acb8dbb051bd082632fa0224b9` (identisch).
- Branch: `ui/redesign-0.3.0`; nichts gestaged.
- Arbeitsbaum zu Beginn: zwei fremde, geschützte Änderungen: `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` und `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html`. Beide bleiben unangetastet, außerhalb aller Tests, Stages und Commits.
- Verfügbare Prüfung: Build, Unit-/Integrationstests sowie UI Automation und Windows/.NET-Screenshots; Computer Use und der lokale PNG-Helfer sind gemäß Auftrag blockiert und werden nicht verwendet.
- Ausgewertete Referenzen: Chat-Anhänge 01–03 (IST), 04–06 (SOLL) sowie 07+ (bisheriger Branchstand); die Hauptansicht weicht vor allem bei Karten-/Timeline-Layout, Werkzeuggruppierung und Dialogvereinheitlichung vom SOLL ab.
- Aktuell bearbeitete Phase: 4 – Startansicht und Dialoge.
- Nächster atomarer Schritt: vorhandene Startansicht und alle Dialoge samt zugeordneten ViewModels/Tests vollständig prüfen und die kleinste sichere Phase-4-Änderung ableiten.

### Phase 4 – Teilmeilenstein Startansicht

- Umsetzung: Startansicht ohne Projekt verwendet eine textbasierte Produktmarke anstelle eines Unicode-Pseudo-Icons. Der Wiederherstellungsbereich wird ausschließlich bei Kandidaten angezeigt; `HasRecoveryCandidates` wird nach jedem Abruf benachrichtigt.
- Geänderte Dateien: `MainWindow.xaml`, `MainWindowViewModel.cs`, `STATUS.md`, `UI_REDESIGN_HANDOFF.md`.
- Prüfung: Debug-Build erfolgreich (0 Warnungen/0 Fehler); gezielte Hauptfenster- und Dialogtests 19/19 bestanden.
- UI-Automation: die bestehenden STA-Integrationstests decken Fensterstruktur, Automation-Namen, Fokus und Referenzgrößen ab; eine echte visuelle Abnahme ist weiterhin bis zum Screenshot-Rückgabeprozess aufgeschoben.
- Nächster atomarer Schritt: Dialog-Header und Aktionsleisten der verbleibenden Export-, Sicherungs-, Vorschau- und Protokolldialoge auf den gemeinsamen SOLL-Vertrag abgleichen.
