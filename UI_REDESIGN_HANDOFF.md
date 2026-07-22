# UI-Redesign - Übergabeprotokoll

- Letzte Aktualisierung: 2026-07-22 13:40:27 +02:00
- Repository: `C:\Projekte\ZeitstrahlStudio`
- Branch: `ui/redesign-0.3.0`
- Remote: `origin` (`https://github.com/IzzLenn/ZeitstrahlStudio.git`)
- Ausgangs-Commit: `38d94282bc6bbe3ecf96bf40a5efb36976d89dd1`
- Aktueller HEAD: `41511b136060c6c1ea4f4781a183f578a50da3b0`
- Stand von `origin/ui/redesign-0.3.0`: `41511b136060c6c1ea4f4781a183f578a50da3b0` (identisch mit lokalem HEAD)
- Ausführendes Modell: Codex auf Basis von GPT-5; eine separate Reasoning-Konfiguration wird von dieser Laufzeit nicht ausgewiesen.
- Aktuell bearbeitete Phase: Phase 1 abgeschlossen und zur Sicherung bereit; als Nächstes Phase 2.

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

## Tatsächlich sichtbare Änderungen

Hell- und Dunkelmodus besitzen nun denselben semantischen Ressourcenvertrag. Hauptfenster und Dialoge übernehmen das aktive Anwendungstheme konsistent; Meta-/Grundtexte und Überschriften sind größer, und gemeinsame Bedienelemente zeigen klarere Zustände für Fokus, Auswahl, Deaktiviert, Nur-Lesen und Fehler.

## Build- und Testergebnisse

- `dotnet build ZeitstrahlStudio.sln -c Debug --no-restore`: erfolgreich, 0 Warnungen, 0 Fehler.
- Gezielte Debug-Integrationstests mit Filter auf `MainWindowAccessibilityTests`, `TimelineViewTests`, `ProjectSettingsDialogViewModelTests`, `BackupManagerDialogTests` und `PdfExportDialogTests`: 17/17 bestanden.
- Phase 1: erneuter Debug-Build erfolgreich, 0 Warnungen/0 Fehler; 17/17 gezielte Tests einschließlich `ThemeResourceTests` bestanden.

## Aktuelle Screenshots

Die sechs Gesprächsanhänge wurden vollständig ausgewertet. Der gebaute Phase-1-Stand wurde mit dem Beispielprojekt erfolgreich gestartet und als `artifacts/ui-redesign/phase-1-theme.png` aufgenommen. Die direkte Bildanzeige scheiterte zweimal am Timeout des Windows-Sandbox-Bildhelpers; ein alternativer In-Memory-Versuch konnte das PNG ebenfalls nicht rendern.

## Technische Entscheidungen

- ADR-037: Themes werden global aus `Application.Resources` vererbt; lokale feste Light-Theme-Einbindungen werden entfernt.
- Die neue Navigation bleibt textbasiert, solange keine freigegebenen, themefähigen WPF-Assets vorliegen.
- Die Hauptansicht wird als adaptive linke Seitenleiste, zentrale Arbeitsfläche und rechter Inspektor umgesetzt; reine Breiten-/Sichtbarkeitszustände dürfen in der View liegen, Fachänderungen bleiben in ViewModel und Diensten.
- Die vorhandene Benutzeränderung an `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` wird weder verändert noch gestaged noch committed.
- Es wird ausschließlich auf `ui/redesign-0.3.0` gearbeitet; kein Merge, Release-Tag, Publish, Installer, Versionswechsel oder Force-Push.

## Abweichungen und bekannte Fehler

- Keine Abweichung vom korrigierten Umfang: Phase 4 bis 8 wurden nicht begonnen.
- Keine neuen Laufzeitfehler in der Baseline ermittelt.
- Die vorhandene Änderung am Beispielarchiv ist eine fremde Arbeitsbaumänderung und bleibt geschützt.
- `apply_patch` wird entsprechend der Werkzeugvorgabe in diesem Chat nicht verwendet.

## Arbeitsbaum und Push-Status

- Vorhandene fremde Änderung: `M samples/ZeitstrahlStudio-Beispiel.zeitprojekt`.
- Eigene Phase-1-Änderungen betreffen Themes, gemeinsame Styles, Theme-Vererbung, `ThemeResourceTests.cs`, `STATUS.md` und dieses Handoff; sie sind geprüft, aber noch nicht committed.
- Lokaler HEAD und `origin/ui/redesign-0.3.0` stehen beide auf `41511b136060c6c1ea4f4781a183f578a50da3b0`.

## Fehlende Icons und Iconanforderungsstatus

Im App-Projekt existiert kein verwendbarer Icon-/Asset-Satz für die geforderte Navigation. `ICON_REQUIREMENTS.md` definiert Perspektive, Kontur, Strichstärke, Rundungen, Raster, Größen, Farben, Zustände, Format, Theme-Trennung, exakte Zielordner und Dateinamen für 17 Funktionsgruppen. Neue Grafiken werden in diesem Lauf nicht erfunden; Befehle erhalten eindeutige Textbezeichnungen.

## Offene Aufgaben

### Aktueller Auftrag

- Phase 0 abgeschlossen: dokumentiert, getestet, committed und gepusht.
- Phase 1 abgeschlossen: Designsystem und Themes implementiert, gebaut und gezielt getestet; Commit/Push folgen.
- Phase 2 umsetzen: globale Navigation.
- Phase 3 umsetzen: responsive Hauptansicht.
- Build, gezielte Tests, Commit, Push und Übergabe durchführen.
- Nach Phase 3 diesen Auftrag beenden.

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

Ausschließlich die Phase-1-Dateien und diese fortlaufende Dokumentation stagen, das Beispielarchiv ausschließen, den geprüften Stand committen und pushen. Danach Phase 2 mit der globalen Navigation beginnen.

## Exakter Fortsetzungsprompt für einen nachfolgenden Chat

> Öffne `C:\Projekte\ZeitstrahlStudio`, arbeite ausschließlich auf Branch `ui/redesign-0.3.0` und lies `SPEC.md`, `STATUS.md`, `DECISIONS.md`, `UI_AUDIT_0.2.1.md`, `UI_REDESIGN_PLAN.md`, `ICON_REQUIREMENTS.md` und `UI_REDESIGN_HANDOFF.md` vollständig. Bewahre die nicht zu diesem Auftrag gehörende Änderung an `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` unverändert und außerhalb aller Commits. Setze erst dann den in `UI_REDESIGN_HANDOFF.md` bezeichneten nächsten Arbeitsschritt um. Beginne keine späteren Phasen vor ihrer ausdrücklich dokumentierten Freigabe und führe weder Publish noch Installer, Versionsänderung, Releasevorbereitung, Merge, Tag oder Force-Push aus.
