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

### Pushstatus Phase 4 – Teilmeilenstein Startansicht

- Commit `4f2febaed8e173f90cadccf87e09828e86c0d332` (`ui: Startansicht weiter vereinheitlichen`) wurde am 25.07.2026 regulär nach `origin/ui/redesign-0.3.0` gepusht.
- Nach dem Push stimmen lokaler und entfernter HEAD überein: `4f2febaed8e173f90cadccf87e09828e86c0d332`.
- Nicht gestaged und unverändert geschützt: `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` sowie die vorhandene fremde Änderung `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html`.
- Nächster atomarer Schritt: verbleibende Dialoge auf gemeinsamen Kopf-/Inhalts-/Aktionsleistenvertrag prüfen und die gemeinsamen, rein präsentationalen Korrekturen umsetzen.

### Phase 4 – Teilmeilenstein Ereignisdialog

- Umsetzung: `EventEditorDialog.xaml` besitzt nun einen gemeinsamen semantischen Kopfbereich, einen scrollbar verbleibenden Inhaltsbereich und eine eindeutig getrennte Aktionsleiste. Fachlogik, Bindings, Validierung und Enter/Escape wurden nicht verändert.
- Prüfung: Debug-Build erfolgreich (0 Warnungen/0 Fehler); gezielte Hauptfenster-/Dialogtests 19/19 bestanden.
- Nächster atomarer Schritt: Vorschau-, Analyse- und Protokolldialoge auf fehlende Kopf-/Aktionsleisten und Unicode-Pseudo-Icons prüfen; danach Phase 4 abschließen.

### Pushstatus Phase 4 – Teilmeilenstein Ereignisdialog

- Commit `badd7a5fd82599d3df68f261f7b6b9bb9c2bb404` (`ui: Ereignisdialog strukturieren`) wurde regulär nach `origin/ui/redesign-0.3.0` gepusht; lokaler und entfernter HEAD stimmten anschließend überein.

### Phase 5 – Teilmeilenstein Timeline-Werkzeuge

- Umsetzung: Die Werkzeugleiste ist als umbruchfähiges, beschriftetes Panel für Ansicht, Zoom, Orientierung und Layout strukturiert. Zoomschalter verwenden klare Texte anstelle von Unicode-Pseudo-Icons; alle vorhandenen Commands und Tooltips bleiben verbunden.
- Test: `MainWindow_GroupsTimelineToolsWithoutHorizontalOverflow` ergänzt und bei 1280×720 bestanden; Debug-Build 0/0; gezielte MainWindow-/Timeline-Tests 18/18 bestanden.
- Nächster atomarer Schritt: Karten-Renderer und Layoutengine auf vollständige Status-/Prioritäts-/Fristinformation und sichtbare manuelle Konflikte prüfen.

### Pushstatus Phase 5 – Teilmeilenstein Timeline-Werkzeuge

- Commit `bc7981fc9757abd63acc801018167cb0417cf4ca` (`ui: Timeline-Werkzeuge gruppieren`) wurde regulär nach `origin/ui/redesign-0.3.0` gepusht; lokaler und entfernter HEAD stimmten anschließend überein.

### Phase 5/6 – Teilmeilenstein Karten- und Konfliktzustände

- Umsetzung: Karten kommunizieren Priorität, Status, Frist, Anhänge und manuellen Zustand nun zusätzlich als Text. Das Layoutmodell erkennt Überschneidungen, an denen mindestens eine Karte manuell positioniert wurde; der Renderer markiert sie sichtbar und nicht ausschließlich durch Farbe.
- Tests: neuer Unit-Test für manuelle Kartenüberschneidung; Debug-Build 0/0; Layoutengine-Tests 17/17, TimelineView-Tests 5/5 bestanden.
- Nächster atomarer Schritt: Lücken-/Frist-/Tickbeschriftungen und vertikale Kollisionsbahnen gegen Sichtbereichsanforderungen prüfen; anschließend Phase 6 abschließen.

### Pushstatus Phase 5/6 – Teilmeilenstein Karten- und Konfliktzustände

- Commit `95896cfbff9a092836b2e0476e8a1bd9fbac83ef` (`ui: Kartenstatus und Layoutkonflikte anzeigen`) wurde am 25.07.2026 regulär nach `origin/ui/redesign-0.3.0` gepusht.
- Lokaler und entfernter HEAD sind identisch: `95896cfbff9a092836b2e0476e8a1bd9fbac83ef`.
- Arbeitsbaum: ausschließlich die weiterhin geschützten fremden Änderungen an `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` und `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html`; nichts gestaged.
- Nächster atomarer Schritt: verbleibende Phase-6-Ansichts-/Beschriftungskollisionen anhand der vorhandenen Layout- und Renderer-Verträge prüfen und gezielt testen.

### Phase 6 – Teilmeilenstein Achsenbeschriftungen

- Umsetzung: Die Achse zeichnet weiterhin jede Markierung, zeigt Text aber nur bei ausreichendem Abstand und außerhalb des reservierten Bereichs einer Lückenbeschriftung. Das reduziert Überlagerungen ohne Zeitdaten oder Navigationsverhalten zu verändern.
- Prüfung: Debug-Build 0/0; TimelineView-Tests 5/5 bestanden.
- Nächster atomarer Schritt: Phase-4-Dialogrest und Phase-6-Viewport-/DPI-Abdeckung zusammenführen, dann Phase 7 beginnen.

### Pushstatus Phase 6 – Teilmeilenstein Achsenbeschriftungen

- Commit `1f89abc376f01421f2c43c9f868840a1cf8df0fb` (`ui: Achsenbeschriftungen entzerren`) wurde regulär nach `origin/ui/redesign-0.3.0` gepusht.
- Lokaler und entfernter HEAD sind identisch; weiterhin uncommittet bleiben ausschließlich die zwei geschützten fremden Beispiel-Dateien.
- Nächster atomarer Schritt: restliche Phase-4-Dialogabnahme und die Phase-7-Accessibility-/DPI-Prüfung fortsetzen.

## Konsolidierter Übergabestand nach Commit 1f89abc – 25.07.2026

- Aktueller Commit und Pushstatus: `1f89abc376f01421f2c43c9f868840a1cf8df0fb` (`ui: Achsenbeschriftungen entzerren`) ist erfolgreich auf `origin/ui/redesign-0.3.0`; lokaler und entfernter HEAD sind identisch.
- In diesem Lauf erzeugte Commits: `4f2feba` Startansicht, `badd7a5` Ereignisdialog, `bc7981f` Timeline-Werkzeuge, `95896cf` Kartenstatus/Layoutkonflikte, `1f89abc` Achsenbeschriftungen.
- Phase 4: Startansicht und Ereignisdialog sind modernisiert. Noch offen sind die systematische Abnahme und ggf. Präsentationsangleichung der verbleibenden Sicherungs-, Export-, Vorschau-, Analyse- und Protokolldialoge.
- Phase 5: Werkzeugleiste ist gruppiert und umbruchfähig; `MainWindow_GroupsTimelineToolsWithoutHorizontalOverflow` besteht bei 1280×720. Karten zeigen Priorität, Status, Frist, Anhang und manuellen Zustand textlich.
- Phase 6: manuelle Kartenüberschneidungen werden sichtbar als Konflikt markiert; Achsenbeschriftungen sind entzerrt. Noch offen sind breitere Layout-/Viewport-/Größenabnahmen inklusive vertikaler Ansichten.
- Phase 7: offen – vollständige Accessibility-, Fokus-, AutomationProperties-, High-Contrast- und DPI-Prüfung bei 100/125/150/200 Prozent.
- Phase 8: offen – erst am visuellen Review-Gate programmatische Screenshots erzeugen, Metadaten prüfen, dann auf echte Chat-Anhänge zur visuellen Analyse warten.
- Ausgeführte Prüfungen in diesem Lauf: Debug-Builds jeweils erfolgreich mit 0 Warnungen/0 Fehlern; Dialog-/MainWindow-Tests 19/19, Timeline-Werkzeug-/Viewtests 18/18, Layoutengine-Tests 17/17, TimelineView-Tests 5/5.
- Bekannte Abweichungen zum SOLL: abschließende visuelle Prüfung steht aus; die Dialogfamilie ist noch nicht vollständig vereinheitlicht, und die vollständige Kartengeometrie/vertikale Lane-Abnahme erfordert die vorgesehenen Screenshots.
- Werkzeugblocker: `apply_patch` nicht verwenden; Computer Use nicht verwendbar; lokaler PNG-Bildhelper nicht verwendbar. Alternativen: PowerShell/.NET-Dateibearbeitung, UI Automation, programmatische Screenshot-Erzeugung sowie visuelle Analyse nach erneutem Hochladen als echter Chat-Anhang.
- Arbeitsbaum: geschützt und niemals zu stagen sind `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` und die fremde Änderung `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html`.
- Nächster atomarer Schritt: die verbleibenden Phase-4-Dialogmarkups strukturell prüfen, nur fehlende Kopf-/Inhalts-/Aktionsleisten gezielt angleichen und durch vorhandene Dialogtests absichern.

### Exakter Fortsetzungsprompt

> Öffne `C:\Projekte\ZeitstrahlStudio` auf `ui/redesign-0.3.0`. Prüfe zuerst HEAD, Remote und Arbeitsbaum. Bewahre `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` und `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` unverändert und außerhalb jedes Stagings. Lies `SPEC.md`, `STATUS.md`, `DECISIONS.md`, `UI_REDESIGN_HANDOFF.md` und die relevanten UI-Dateien. Setze zuerst die verbleibende Phase-4-Dialogangleichung fort, danach Phase 6/7. Verwende weder apply_patch noch Computer Use noch den lokalen PNG-Bildhelper. Baue und teste jeden Teilmeilenstein, aktualisiere Status/Handoff, committe und pushe ausschließlich nach `origin/ui/redesign-0.3.0`. Erzeuge am visuellen Review-Gate die vorgegebenen Screenshots unter `artifacts/ui-validation/final-review/`, prüfe nur Metadaten und halte dann auf die Chat-Bildanhänge.

### Phase 4 – Teilmeilenstein Dokumentanalyse-Dialog

- Umsetzung: `AttachmentAnalysisDialog.xaml` verwendet einen themefähigen Kopfbereich mit Datei-, Status-, Medien- und Extraktionsinformation, einen klar abgegrenzten Tab-Inhalt und eine feste Aktionsleiste.
- Prüfung: Debug-Build 0/0; gezielte Analyse-/Export-/Sicherungstests 7/7 bestanden.
- Nächster atomarer Schritt: die Bildvorschau und das Änderungsprotokoll auf denselben Dialogvertrag prüfen und fehlende Struktur gezielt ergänzen.

### Pushstatus Phase 4 – Teilmeilenstein Dokumentanalyse-Dialog

- Commit `d320cd1d5b92ebd06aea392b335c14acb46ccd07` (`ui: Dokumentanalyse-Dialog vereinheitlichen`) wurde regulär nach `origin/ui/redesign-0.3.0` gepusht; lokaler und entfernter HEAD sind identisch.
- Nächster atomarer Schritt: Bildvorschau und Änderungsprotokoll strukturell gegen den gemeinsamen Dialogvertrag prüfen.

### Phase 4 – Teilmeilenstein Bildvorschau und Änderungsprotokoll

- Ausgangs-HEAD: `f4b29784a5045f1fb4d8c9b1d6be734a6eac79c6` (`docs: jüngsten UI-Teilstand protokollieren`), regulär auf `origin/ui/redesign-0.3.0` gepusht.
- Geänderte Dateien: `AttachmentImagePreviewDialog.xaml`, `AuditLogDialog.xaml`, `AuditLogDialog.xaml.cs` sowie `DialogAccessibilityTests.cs`.
- Bildvorschau: nutzt den gemeinsamen themefähigen Kopfbereich mit Dateiname und Bildmaßen, eine klar umrandete und in beide Richtungen scrollbar bleibende Bildfläche sowie eine getrennte Aktionsleiste. Die bestehende Dekodierung, Bildanzeige und Escape-/Enter-Schließen bleiben erhalten.
- Änderungsprotokoll: nutzt denselben Header-/Inhalts-/Footer-Vertrag, einen textlichen Leerzustand, eindeutige Automation-Namen und horizontale/vertikale Scrollleisten. Lange Beschreibungen werden gekürzt angezeigt und vollständig per Tooltip erreichbar; Erfolg bleibt als Textspalte sichtbar und nicht nur farbcodiert.
- Automation und Layout: neue STA-basierte WPF-Tests öffnen beide Dialoge mit temporären Testdaten, prüfen Fenstertitel, primäre Controls, AutomationProperties, Leerzustand, Mindestgröße und Einhaltung des Content-Rechtecks. Die Controls sind erreichbar; beide Schließenaktionen bleiben Standard- und Abbrechenaktion. Eine separate externe UI-Automation-Bibliothek ist im Repository nicht vorhanden; keine neue parallele Testarchitektur eingeführt.
- Prüfung: `dotnet build ZeitstrahlStudio.sln -c Debug --no-restore` erfolgreich (0 Warnungen, 0 Fehler). Gezielte Dialog-/Audit-/Themevertragstests: 9/9 bestanden.
- Iconanforderungen: keine neuen Icons nötig; die bestehenden Textaktionen bleiben unverändert.
- Stand Phase 4: Start-, Ereignis-, Dokumentanalyse-, Bildvorschau- und Änderungsprotokolldialog sind vereinheitlicht. Noch zu prüfen sind die verbleibenden Phase-4-Dialoge, insbesondere PDF-Vorschau, PDF-/HTML-Export und Sicherungsverwaltung, soweit deren Struktur noch abweicht.
- Stand Phase 5/6: gruppierte Timeline-Werkzeuge, textliche Kartenstatus, sichtbare manuelle Layoutkonflikte und entzerrte Achsenbeschriftungen sind abgeschlossen; breitere Viewport-/vertikale/DPI-Abnahmen sind noch offen.
- Werkzeugblocker: `apply_patch`, Computer Use und der lokale PNG-Bildhelper bleiben nicht verwendbar. Verfügbar bleiben PowerShell/.NET-Dateibearbeitung, WPF-Automationstests, programmatische Screenshot-Erzeugung und spätere visuelle Analyse nach erneutem Chat-Bildanhang.
- Nächster atomarer Schritt: die verbleibenden Phase-4-Dialogmarkups (PDF-Vorschau, Export und Sicherungsverwaltung) strukturell gegen den gemeinsamen Dialogvertrag prüfen und nur belegte Abweichungen angleichen.

### Exakter Fortsetzungsprompt nach diesem Teilmeilenstein

> Öffne `C:\Projekte\ZeitstrahlStudio` auf `ui/redesign-0.3.0`. Prüfe Branch, Remote, beide HEADs und den Arbeitsbaum. Bewahre `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` und `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` unverändert und außerhalb jedes Stagings. Lies `SPEC.md`, `STATUS.md`, `DECISIONS.md`, `UI_REDESIGN_PLAN.md`, `ICON_REQUIREMENTS.md` und `UI_REDESIGN_HANDOFF.md`. Prüfe ausschließlich die verbleibenden Phase-4-Dialogmarkups (PDF-Vorschau, Export und Sicherungsverwaltung) gegen den vorhandenen Header-/Inhalts-/Aktionsleistenvertrag. Ändere nur belegte Abweichungen, baue und teste den atomaren Teilmeilenstein, aktualisiere Status/Handoff, committe und pushe ausschließlich normal nach `origin/ui/redesign-0.3.0`. Verwende weder apply_patch noch Computer Use noch den lokalen PNG-Bildhelper und beginne danach keinen weiteren größeren Schritt ohne neue Freigabe.

### Pushstatus Phase 4 – Teilmeilenstein Bildvorschau und Änderungsprotokoll

- Commit `7c1d3f09e7a2523b3dec4c0d4ecf918470c16715` (`ui: Bildvorschau und Änderungsprotokoll vereinheitlichen`) wurde regulär nach `origin/ui/redesign-0.3.0` gepusht; lokaler und entfernter HEAD waren danach identisch.
- Der Commit umfasst die dialogbezogenen XAML-/Darstellungsänderungen, `DialogAccessibilityTests.cs`, `STATUS.md` und das vorherige Handoff.
- Nächster atomarer Schritt nach neuer Freigabe: verbleibende Phase-4-Dialogmarkups für PDF-Vorschau, Export und Sicherungsverwaltung gegen den gemeinsamen Dialogvertrag prüfen.

### Phase 4 – Teilmeilenstein PDF-Vorschau und Exportdialoge: Start

- Ausgangs-HEAD und Remote-HEAD: `3b2f2e38a8e33ed1d08496434b92ba855f6411cc` auf `ui/redesign-0.3.0`.
- Arbeitsbaum zu Beginn: ausschließlich die geschützten, fremden Änderungen `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt`; nichts gestaged.
- Teilaufgabe: ausschließlich `AttachmentPdfPreviewDialog`, `PdfExportDialog` und `HtmlExportOptionsDialog` gegen den vorhandenen semantischen Dialogvertrag prüfen und angleichen. Sicherungsverwaltung und weitere UI-Teilmeilensteine sind ausdrücklich ausgenommen.
- Prüfmethoden: Debug-Build, gezielte nicht-pixelbasierte WPF-/Theme-/Exporttests, STA-basierte Dialogprüfungen und vorhandene lokale Exporttests; keine visuelle Abnahme ohne echten Chat-Bildanhang.

### Phase 4 – Teilmeilenstein PDF-Vorschau und Exportdialoge: Abschluss vor Commit

- Geänderte Dateien: `AttachmentPdfPreviewDialog.xaml`, `PdfExportDialog.xaml`, `HtmlExportOptionsDialog.xaml` und `DialogAccessibilityTests.cs`; außerdem dieses Protokoll und `STATUS.md`.
- Entscheidung: Die nativen Windows-Speicherdialoge bleiben unverändert die alleinige Zielpfad- und Dateiauswahl. Der HTML-Optionsdialog erklärt diesen anschließenden Schritt; es wurde keine parallele Export- oder Pfadlogik eingeführt.
- PDF-Vorschau: Header, Arbeitsfläche und Footer folgen semantischen Dialogressourcen. Die Werkzeugleiste verwendet zugängliche Klartextbefehle für Seitenwechsel und Zoom; Seitenanzeige, Zoomzustand, Neuladen, lokales Öffnen, Lade- und Fehleroverlay bleiben unverändert erhalten.
- PDF-Export: Optionen bleiben scrollbar, die Vorschauwerkzeuge sind textbasiert und benannt, und die primäre Aktion lautet eindeutig `PDF exportieren`. Der vorhandene ViewModel-Vertrag deaktiviert sie bis zu einer gültigen, aktuellen Vorschau; Validierungs-/Fehler- und Beschäftigtzustände bleiben ViewModel-gesteuert.
- HTML-Export: Optionsbereich ist bei geringer Höhe scrollbar, erklärt eingebettete Vorschauen sowie die unveränderliche Offline-Momentaufnahme und führt mit `HTML exportieren …` eindeutig zur vorhandenen Zielpfadauswahl. Fachliche Optionen und Code-behind bleiben unverändert.
- Tests: Der erste Build scheiterte einmal ausschließlich an einem fehlenden Test-namespace und der zweite an einer falsch benannten Testeigenschaft; beide ohne Produktivcodefehler korrigiert. Der erste gezielte Testlauf hatte 19/20, weil ein nicht modal angezeigtes Testfenster kein `DialogResult` setzen darf. Der Test wurde stabil auf die prüfbare Standard-/Abbrechenaktion umgestellt. Abschließend: Debug-Build 0 Warnungen/0 Fehler; gezielte PDF-/HTML-/Dialog-/Themevertragstests 20/20 bestanden.
- WPF-Automation: Tests prüfen Titel, benannte Hauptbereiche, AutomationProperties, Standard-/Abbrechenaktionen, Scrollcontainer, Mindestlayout und die textbasierten Werkzeugbefehle. Für PDF-Export wird zusätzlich die echte ViewModel-Vorschau auf einem STA-Thread instanziiert. Eine externe UI-Automation-Bibliothek ist nicht vorhanden.
- Abweichungen zum SOLL: Keine visuelle Abnahme, weil keine Screenshots als Chat-Anhang analysiert wurden. Die nativen Speicherdialoge sind absichtlich nicht nachgebaut und liegen außerhalb der XAML-Dialogfamilie.
- Iconanforderungen: keine neuen Icons; die vorhandenen Funktionen nutzen Textbeschriftungen.
- Verbleibend in Phase 4: ausschließlich Sicherungsverwaltung prüfen und – nur bei belegter Abweichung – angleichen. Phase 5/6 bleiben unverändert abgeschlossen; Phase 7 (vollständige DPI-/Accessibility-Abnahme) bleibt offen.
- Nächster atomarer Schritt nach neuer Freigabe: Sicherungsverwaltung gegen den gemeinsamen Dialogvertrag prüfen. Kein weiterer Teilmeilenstein in diesem Lauf.

### Exakter Fortsetzungsprompt nach Exportdialogen

> Öffne `C:\Projekte\ZeitstrahlStudio` auf `ui/redesign-0.3.0`. Prüfe zuerst Branch, Remote, beide HEADs und den Arbeitsbaum. Bewahre `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` und `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` unverändert und außerhalb jedes Stagings. Lies `SPEC.md`, `STATUS.md`, `DECISIONS.md`, `UI_REDESIGN_HANDOFF.md` sowie die Sicherungsdialog-, Theme- und Testdateien. Bearbeite ausschließlich den nächsten atomaren Schritt Sicherungsverwaltung gegen den vorhandenen Header-/Inhalts-/Aktionsleistenvertrag; nutze weder apply_patch noch Computer Use noch den lokalen PNG-Bildhelper. Baue, teste, aktualisiere Status/Handoff, committe und pushe nur nach `origin/ui/redesign-0.3.0`, dann halte an.

### Pushstatus Phase 4 – Teilmeilenstein PDF-Vorschau und Exportdialoge

- Commit `3e2f4b06c67f205e6737dbd41f35da4462176236` (`ui: PDF- und HTML-Exportdialoge vereinheitlichen`) wurde regulär nach `origin/ui/redesign-0.3.0` gepusht; lokaler und entfernter HEAD waren danach identisch.
- Geschützte Benutzeränderungen blieben ungestaged: `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt`.
- Nächster atomarer Schritt nach neuer Freigabe: ausschließlich Sicherungsverwaltung gegen den gemeinsamen Dialogvertrag prüfen.

## Phase 4 – Sicherungsverwaltung: Beginn des finalen atomaren Teilmeilensteins (25.07.2026)

- Ausgangs-HEAD und bestätigter Remote-HEAD: `70d5880e3c71a203ff89366afbf3535297036ea8` auf `ui/redesign-0.3.0`.
- Pushstatus: Der dokumentierte Exportdialog-Meilenstein ist erfolgreich auf `origin/ui/redesign-0.3.0` veröffentlicht.
- Arbeitsbaum vor Beginn: ausschließlich die geschützten, unveränderten Benutzeränderungen an `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` und `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html`; nichts gestaged.
- Umfang dieses letzten Phase-4-Schritts: Sicherungsverwaltung und Wiederherstellungsdarstellung gegen den bestehenden Dialogvertrag prüfen und ausschließlich reale UI-/Zugänglichkeitslücken schließen. Keine Änderungen an Sicherungslogik, Persistenz oder den geschützten Beispieldateien.
## Phase 4 – Sicherungsverwaltung abgeschlossen (25.07.2026)

- Der finale Dialogabgleich vereinheitlicht `BackupManagerDialog`: semantische Dialogfläche, beschriebener Kopfbereich, zugänglicher Aufbewahrungsbereich, explizite Namen/Tooltips für Einstellungen, Aktualisieren, manuelle Sicherung, Wiederherstellung und Schließen sowie eine eindeutige Busy-/Fehlermeldung. Sicherungs-, Wiederherstellungs- und Aufbewahrungslogik wurden nicht verändert.
- Wiederherstellungskarten der Startansicht zeigen jetzt zusätzlich den Zeitpunkt der letzten Arbeitskopie und exponieren ihre Aktionen mit eindeutigen Automation-Namen und Tooltips.
- Die vorhandene STA-Prüfung deckt Header, Aufbewahrungsbereich, Sicherungsgrid, ausgeblendeten Reserve-Leerzustand und Escape-Schließen ab.
- Verifikation: `dotnet build ZeitstrahlStudio.sln -c Debug` erfolgreich (0 Warnungen, 0 Fehler); gezielter Integrationstestlauf für Sicherung, Wiederherstellung, MainWindow-/Dialog-Accessibility und Theme: 31/31 erfolgreich.
- Phase 4 ist damit abgeschlossen. Phase 5 ist durch gruppierte Timeline-Werkzeuge und die vereinheitlichte Startansicht fortgeschritten; Phase 6 umfasst Kartenstatus, manuelle Layoutkonflikte und entzerrte Achsenbeschriftungen. Kein weiterer Phase-5-bis-8-Schritt wurde in diesem Lauf begonnen.
- Bekannte Abweichung: keine visuelle Abnahme ohne echten Bildanhang; Computer Use, apply_patch und lokaler PNG-Helfer bleiben nicht verwendbar.
- Nächster atomarer Schritt erst nach neuer Freigabe: Phase 5/6 anhand des dokumentierten Layout- und Viewport-Rests neu abgrenzen; Phase 7 erst danach separat beginnen.

### Exakter Fortsetzungsprompt

> Öffne `C:\Projekte\ZeitstrahlStudio` auf `ui/redesign-0.3.0`. Prüfe Branch, Remote, beide HEADs und Arbeitsbaum. Bewahre die beiden geschützten Beispieldateien unverändert und ungestaged. Lies die verbindlichen Dokumente sowie `UI_REDESIGN_HANDOFF.md`. Beginne keinen neuen Dialogschritt; grenze zuerst den kleinsten noch offenen Layout-/Viewport-Schritt aus Phase 5/6 ab, baue und teste ihn gezielt, aktualisiere die Dokumentation, committe und pushe nur nach `origin/ui/redesign-0.3.0`. Nutze weder apply_patch noch Computer Use noch den lokalen PNG-Helfer.
### Pushstatus Phase 4 – Sicherungsverwaltung

- Commit `5d99667` (`ui: Sicherungsverwaltung vereinheitlichen`) wurde regulär nach `origin/ui/redesign-0.3.0` gepusht.
- Der Commit enthält ausschließlich die geprüften Sicherungs-/Wiederherstellungsoberflächen, den STA-Integrationstest und die zugehörige Statusdokumentation; die beiden geschützten Beispieldateien blieben ungestaged.
## Phase 5 – Beginn und Bestandsaufnahme (25.07.2026)

- Ausgangs-HEAD und bestätigter Remote-HEAD: `da0610855de10208008ea77b78d7bc685a7c6e5c` auf `ui/redesign-0.3.0`.
- Arbeitsbaum vor Änderungen: ausschließlich die geschützten Benutzeränderungen `samples/Beispielchronik Bürgerlabor Sonnenwinkel.html` und `samples/ZeitstrahlStudio-Beispiel.zeitprojekt`; nichts gestaged. Beide Dateien bleiben vollständig außerhalb aller Tests, Änderungen und Commits.
- Phase 4 ist abgeschlossen; dieser Auftrag bearbeitet ausschließlich Phase 5 (Timeline-Werkzeuge und Ereigniskarten).
- Visuelle Referenzen wurden als echte Chat-Anhänge ausgewertet. Die lokalen, später erzeugten Validierungsscreenshots werden nicht visuell geöffnet; die nachfolgende visuelle Abnahme bleibt einem erneuten Chat-Anhang vorbehalten.

### Vergleich visueller Referenzen

| Bereich | aktueller Screenshot | SOLL-Referenz | belegte Abweichung | Änderung in Phase 5 |
| --- | --- | --- | --- | --- |
| Timeline-Werkzeuge | Werkzeuge mehrzeilig gruppiert, aber aktive Orientierung nur in der globalen Leiste | kompakte, klar gegliederte lokale Leiste mit deutlich aktivem Umschalter | lokale Orientierung ist nicht segmentiert; Gruppe `ORIENTIERUNG` enthält Ansichtsbefehle | lokalen segmentierten Umschalter und semantische Gruppen ergänzen |
| Ereigniskarten | Datum, Titel, Kurztext, Metadaten, Auswahl und Konflikt sichtbar | dichte Karten mit lesbaren Metadaten, Auswahl-/Fokuszuständen und Miniaturen | Hover- und Fokuszustand der einzelnen Karte nicht getrennt belegbar | Karteninteraktion und Darstellung ohne Änderung der Layoutlogik ergänzen |
| Helles/dunkles Theme | beide Themes vorhanden | in beiden Themes klare Konturen und Textkontrast | Status/Konflikt textlich vorhanden, Fokus/Hover noch nicht separat markiert | zustandsabhängige Kontur und Automation-Text ergänzen |

### Phase-5-Bestandsaufnahme

| Anforderung | Bereits umgesetzt | Beleg | Noch erforderlich |
| --- | --- | --- | --- |
| Werkzeuge gruppiert / kein 1280-Überlauf | Ja | `TimelineToolsPanel` ist ein `WrapPanel`; STA-Test bei 1280 × 720 | Gruppen semantisch korrigieren, aktive Orientierung lokal klar markieren |
| Zoom, Zeitraum, Layout, Ansicht | überwiegend | Zoom-/Zeitraum-/Layoutbefehle und Tastenkürzel vorhanden | Tooltips und Automation-Namen für alle lokalen Befehle vereinheitlichen |
| Segmentierter Umschalter | teilweise | globale Leiste hat Styles; lokale Leiste besitzt zwei normale Buttons | lokale segmentierte aktive Darstellung |
| Datum, Titel, Kurztext, Miniatur, Metadaten | Ja | `TimelineView.DrawCard` | keine Layoutlogik ändern |
| Priorität, Status, Frist, Anhänge, manuell, Konflikt | Ja | textliche Badges seit `95896cf` | Fokus/Hover/Dragzustand getrennt und zugänglich machen |
| Lange Titel / optionale Daten | teilweise | Titelfeld besitzt feste Zwei-Zeilenhöhe; optionale Kurzinfo wird aktuell leer gezeichnet | leere Kurzinfo auslassen, Trimmung und Karten-States stabil testen |

- Bereits vorhandene Phase-5-nahe Commits wurden geprüft: `bc7981f` (Werkzeuggruppen), `95896cf` (Status/Konflikt) und `1f89abc` (Achsenbeschriftungen). Keine davon wird zurückgebaut.
- Bewusst Phase 6: zeitliche Skalierung, Lane-Zuweisung, zentrale Kollisionsberechnung, automatische Seitenverteilung und Persistenz manueller Offsets bleiben unverändert.

## Phase 5 – Funktionsstand (25.07.2026)

- Commit adf7f3 schließt die inventarisierten Restlücken: segmentierter lokaler Orientierungsumschalter mit aktiver Darstellung, verständliche Automation-Namen der zentralen lokalen Werkzeuge sowie Karten-Hover und ein von Auswahl getrennter gestrichelter Tastaturfokus.
- Verifikation: dotnet build ZeitstrahlStudio.sln -c Debug --no-restore erfolgreich (0 Warnungen, 0 Fehler); gezielte STA-Integrationstests MainWindowAccessibilityTests und TimelineViewTests 18/18 erfolgreich.
- Keine Phase-6-Logik wurde verändert. Programmatische Screenshots und abschließende visuelle Prüfung stehen noch aus; Phase 5 wird daher nicht als vollständig abgeschlossen behauptet.
- Pushstatus: adf7f3a00050f13a2b839d2fd87b37faec34bf3 ist auf origin/ui/redesign-0.3.0 veröffentlicht.


## Phase 5 – Screenshot-Basistest (25.07.2026)
- Frischer Prozess: PID 91284, Fensterhandle 1119152, Titel Zeitstrahl Studio; EXE: C:\Projekte\ZeitstrahlStudio\src\ZeitstrahlStudio.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\ZeitstrahlStudio.App.exe.
- Methode: WPF/Win32 PrintWindow aus dem ignorierten temporären Aufnahmeprogramm; capture-baseline-test.png ist technisch VALID: 1280×760, 81027 Byte, SHA-256 3EB804394111E07972F05D18E026DDC8994FB8E7E77AA44FF25297C3D0CEE416, 281 Farbstichproben.
- Vollständige Zustandsaufnahmen stehen noch aus; keine lokale visuelle Bewertung. Bilder müssen für die visuelle Abnahme als Chat-Anhänge hochgeladen werden.
