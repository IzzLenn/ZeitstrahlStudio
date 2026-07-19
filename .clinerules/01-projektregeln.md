# Zeitstrahl Studio – Projektregeln

## Verbindliche Projektquellen

Vor jeder Umsetzung lesen:

1. AGENTS.md
2. SPEC.md
3. STATUS.md
4. DECISIONS.md
5. aktuellen Git-Status und die letzten Commits

SPEC.md enthält die fachlichen Anforderungen.
STATUS.md enthält den tatsächlich erreichten Stand.
DECISIONS.md enthält verbindliche technische Entscheidungen.

## Übergaberegel

Der letzte sichere Codex-Commit für Meilenstein 11A ist:

69952fd796472130c8e83edd9bea61dfe0e13ee4

Nach diesem Commit wurden Änderungen für Meilenstein 11B vorgenommen.
Der Codex-Bericht nennt zuletzt:

- Debug und Release: 0 Warnungen, 0 Fehler
- 62 Unit-Tests bestanden
- 88 Integrationstests bestanden
- Formatprüfung bestanden
- win-x64-Publish erfolgreich

STATUS.md und DECISIONS.md wurden dafür noch nicht aktualisiert.

AGENTS.md enthält eine vorhandene Benutzeränderung.
AGENTS.md nicht verändern, zurücksetzen oder committen, außer der Benutzer
erteilt dafür ausdrücklich den Auftrag.

## Arbeitsweise

- Keine abgeschlossenen Meilensteine neu implementieren.
- Vor Änderungen immer git status und git diff prüfen.
- Pro Cline-Aufgabe nur ein klar abgegrenztes Ziel bearbeiten.
- Keine breit angelegten Refactorings während der Releasephase.
- Bestehende öffentliche Schnittstellen nur ändern, wenn zwingend notwendig.
- Keine Cloud-Dienste oder Telemetrie hinzufügen.
- Keine Projektdokumente oder Anhänge hochladen.
- Keine Geheimnisse oder API-Schlüssel in Dateien schreiben.

## Kosteneffiziente Tests

- Während der Implementierung nur betroffene Tests oder Testklassen ausführen.
- Vollständige Debug- und Release-Testläufe nur am Ende eines Teilmeilensteins.
- Denselben fehlgeschlagenen Test nicht mehr als zweimal ohne relevante
  Codeänderung wiederholen.
- Nach zwei erfolglosen Reparaturversuchen Ursache und Blockade dokumentieren.
- Große Testausgaben gezielt filtern statt mehrfach vollständig einzulesen.

## Git

- Keine destruktiven Git-Befehle.
- Kein git reset --hard.
- Kein git clean -fd.
- Benutzeränderungen nicht überschreiben.
- Nach einem erfolgreich geprüften Arbeitspaket einen verständlichen Commit
  erstellen.
- Vor jedem Commit git diff --cached prüfen.

## Abschlussprüfung

Am Ende eines Releasepakets mindestens:

dotnet build ZeitstrahlStudio.sln -c Debug --no-restore
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore --no-build
dotnet build ZeitstrahlStudio.sln -c Release --no-restore
dotnet test ZeitstrahlStudio.sln -c Release --no-restore --no-build
dotnet format ZeitstrahlStudio.sln --verify-no-changes --no-restore
dotnet publish src\ZeitstrahlStudio.App\ZeitstrahlStudio.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\publish\win-x64

## Dokumentation

Nach jedem abgeschlossenen Teilmeilenstein:

- STATUS.md aktualisieren
- DECISIONS.md bei neuen Entscheidungen aktualisieren
- tatsächliche Testergebnisse dokumentieren
- bekannte Einschränkungen ehrlich festhalten
- Git-Commit erstellen