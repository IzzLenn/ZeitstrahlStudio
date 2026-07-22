# UI-Redesign - Übergabeprotokoll

- Letzte Aktualisierung: 2026-07-22 13:22:48 +02:00
- Repository: `C:\Projekte\ZeitstrahlStudio`
- Branch: `ui/redesign-0.3.0`
- Remote: `origin` (`https://github.com/Chisaga/ZeitstrahlStudio.git`)
- Ausgangs-Commit: `38d94282bc6bbe3ecf96bf40a5efb36976d89dd1`
- Aktueller HEAD vor dem Phase-0-Commit: `38d94282bc6bbe3ecf96bf40a5efb36976d89dd1`
- Stand von `origin/ui/redesign-0.3.0`: `38d94282bc6bbe3ecf96bf40a5efb36976d89dd1`
- Ausführendes Modell: Codex auf Basis von GPT-5; eine separate Reasoning-Konfiguration wird von dieser Laufzeit nicht ausgewiesen.
- Aktuell bearbeitete Phase: Phase 0 abgeschlossen und zur Sicherung bereit; als Nächstes Phase 1.

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

Noch keine Commits dieses Redesign-Auftrags; der Phase-0-Dokumentationscommit ist der nächste Schritt.

## Betroffene Dateien

- `UI_AUDIT_0.2.1.md` - vollständiger Audit einschließlich Sechs-Bilder-Tabelle.
- `UI_REDESIGN_PLAN.md` - verbindliche Abbildung der Phasen 1 bis 3 und spätere Einordnung der Phasen 4 bis 8.
- `ICON_REQUIREMENTS.md` - Asset-Gate, Spezifikation und Dateinamen.
- `UI_REDESIGN_HANDOFF.md` - dieses fortlaufende Protokoll.
- `STATUS.md` - aktueller Redesignstand und Phase-0-Ergebnis.
- `DECISIONS.md` - ADR-037 zur Theme-Vererbung, Navigation und adaptiven Präsentationsschale.

## Tatsächlich sichtbare Änderungen

Noch keine Änderungen an der laufenden Anwendung. Phase 0 verändert ausschließlich Dokumentation; sichtbare Implementierung beginnt mit Phase 1.

## Build- und Testergebnisse

- `dotnet build ZeitstrahlStudio.sln -c Debug --no-restore`: erfolgreich, 0 Warnungen, 0 Fehler.
- Gezielte Debug-Integrationstests mit Filter auf `MainWindowAccessibilityTests`, `TimelineViewTests`, `ProjectSettingsDialogViewModelTests`, `BackupManagerDialogTests` und `PdfExportDialogTests`: 17/17 bestanden.

## Aktuelle Screenshots

Die sechs Gesprächsanhänge wurden vollständig ausgewertet. Eigene Laufzeitscreenshots wurden in Phase 0 nicht erzeugt, weil noch keine sichtbare Implementierung vorliegt.

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
- Eigene, noch nicht committed Phase-0-Dateien: `STATUS.md`, `DECISIONS.md`, `UI_AUDIT_0.2.1.md`, `UI_REDESIGN_PLAN.md`, `ICON_REQUIREMENTS.md`, `UI_REDESIGN_HANDOFF.md`.
- `origin/ui/redesign-0.3.0` steht noch auf dem Ausgangs-Commit; Push folgt nach dem geprüften Phase-0-Commit.

## Fehlende Icons und Iconanforderungsstatus

Im App-Projekt existiert kein verwendbarer Icon-/Asset-Satz für die geforderte Navigation. `ICON_REQUIREMENTS.md` definiert Perspektive, Kontur, Strichstärke, Rundungen, Raster, Größen, Farben, Zustände, Format, Theme-Trennung, exakte Zielordner und Dateinamen für 17 Funktionsgruppen. Neue Grafiken werden in diesem Lauf nicht erfunden; Befehle erhalten eindeutige Textbezeichnungen.

## Offene Aufgaben

### Aktueller Auftrag

- Phase 0 abschließen: Dokumente committen und pushen.
- Phase 1 umsetzen: Designsystem und Themes.
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

Die sechs Phase-0-Dokumentationsdateien gezielt stagen, dabei `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` ausschließen, den Diff prüfen, als `docs: UI-Ausgangszustand und Redesignplan dokumentieren` committen und auf `origin/ui/redesign-0.3.0` pushen. Danach Phase 1 mit den globalen Theme-Ressourcen beginnen.

## Exakter Fortsetzungsprompt für einen nachfolgenden Chat

> Öffne `C:\Projekte\ZeitstrahlStudio`, arbeite ausschließlich auf Branch `ui/redesign-0.3.0` und lies `SPEC.md`, `STATUS.md`, `DECISIONS.md`, `UI_AUDIT_0.2.1.md`, `UI_REDESIGN_PLAN.md`, `ICON_REQUIREMENTS.md` und `UI_REDESIGN_HANDOFF.md` vollständig. Bewahre die nicht zu diesem Auftrag gehörende Änderung an `samples/ZeitstrahlStudio-Beispiel.zeitprojekt` unverändert und außerhalb aller Commits. Setze erst dann den in `UI_REDESIGN_HANDOFF.md` bezeichneten nächsten Arbeitsschritt um. Beginne keine späteren Phasen vor ihrer ausdrücklich dokumentierten Freigabe und führe weder Publish noch Installer, Versionsänderung, Releasevorbereitung, Merge, Tag oder Force-Push aus.
