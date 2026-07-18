# Zeitstrahl Studio – Anweisungen für Codex

## Verbindliche Dokumente

Lies vor jeder größeren Arbeit vollständig:

1. `SPEC.md`
2. `STATUS.md`
3. `DECISIONS.md`
4. vorhandene Build- und Testprotokolle

`SPEC.md` ist die verbindliche fachliche Anforderung. Reduziere den dort beschriebenen Funktionsumfang nicht stillschweigend.

## Arbeitsweise

Arbeite weitgehend selbstständig und treffe bei gewöhnlichen technischen Detailfragen eine sichere, wartbare und dokumentierte Entscheidung.

Stelle keine Rückfrage, wenn eine vernünftige Standardentscheidung möglich ist. Dokumentiere wichtige Entscheidungen in `DECISIONS.md`.

Arbeite in kleinen, überprüfbaren Meilensteinen. Nach jedem Meilenstein:

1. Projekt kompilieren
2. relevante Tests ausführen
3. Fehler beheben
4. `STATUS.md` aktualisieren
5. Änderungen mit Git sichern

Arbeite nicht nur an Entwürfen oder Beispielcode. Erzeuge vollständigen, kompilierbaren und integrierten Code.

## Technische Basis

Verwende grundsätzlich:

* C#
* .NET 8
* WPF
* MVVM
* SQLite
* ausschließlich lokale Datenverarbeitung
* Windows 10 und Windows 11, jeweils 64 Bit
* selbstenthaltende Veröffentlichung für `win-x64`

Verwende nur stabile und weitergabefähige Abhängigkeiten. Bevorzuge MIT-, BSD- oder Apache-2.0-Lizenzen.

Führe keine Telemetrie, Werbung, Cloud-Synchronisation oder automatische Datenübertragung ein.

## Projektstruktur

Erzeuge eine klar getrennte Solution mit mindestens:

* Desktop-Anwendung
* Domain-Schicht
* Application-Schicht
* Infrastructure-Schicht
* Dokumentenverarbeitung
* PDF- und HTML-Export
* Unit-Tests
* Integrationstests

Halte Geschäftslogik aus WPF-Code-behind-Dateien heraus.

## Qualitätsregeln

Verwende:

* Nullable Reference Types
* Dependency Injection
* asynchrone Datei- und Datenbankzugriffe
* CancellationToken bei längeren Vorgängen
* Transaktionen für zusammengehörige Datenbankänderungen
* sichere Verarbeitung von ZIP-Archiven und Dateipfaden
* verständliche deutsche Fehlermeldungen
* lokale strukturierte Protokollierung
* automatisierte Tests

Verwende keine Platzhalter wie `TODO`, `NotImplementedException`, „später ergänzen“ oder ausgelassene Implementierungen in fertig gemeldeten Funktionen.

## Build- und Testschleife

Führe regelmäßig aus:

```powershell
dotnet restore
dotnet build ZeitstrahlStudio.sln -c Debug
dotnet test ZeitstrahlStudio.sln -c Debug --no-restore
dotnet build ZeitstrahlStudio.sln -c Release
dotnet test ZeitstrahlStudio.sln -c Release --no-restore
```

Passe die Befehle nur an, wenn die tatsächliche Solution einen anderen Namen verwendet. Dokumentiere die Änderung.

Vor Abschluss muss außerdem eine selbstenthaltende Veröffentlichung für `win-x64` erfolgreich erstellt werden.

## Fehlerbehandlung

Wenn ein Build oder Test fehlschlägt:

1. Ursache untersuchen
2. Fehler beheben
3. denselben Befehl erneut ausführen
4. erst danach mit der nächsten Phase fortfahren

Ignoriere keine Compilerwarnungen, die auf mögliche Fehler, Nullwerte, Ressourcenlecks oder unsichere Dateiverarbeitung hinweisen.

## Abhängigkeiten

Neue Produktionsabhängigkeiten müssen:

* technisch notwendig sein
* lokal funktionieren
* lizenzrechtlich geeignet sein
* in `THIRD_PARTY_LICENSES.md` dokumentiert werden

Verwende keine Bibliothek nur deshalb, um wenige Zeilen einfach umsetzbarer Logik zu vermeiden.

## Git-Regeln

Erstelle nach funktionsfähigen Meilensteinen kleine, verständliche Commits.

Verwende keine destruktiven Git-Befehle wie:

* `git reset --hard`
* `git clean -fd`
* erzwungenes Überschreiben fremder Änderungen

Überschreibe keine erkennbaren Benutzeränderungen.

## Statusdokumentation

Halte `STATUS.md` jederzeit aktuell. Dokumentiere dort:

* aktuelle Phase
* abgeschlossene Funktionen
* erfolgreiche Build- und Testbefehle
* offene Aufgaben
* bekannte Fehler
* nächsten konkreten Arbeitsschritt

Halte `DECISIONS.md` für Architekturentscheidungen aktuell.

## Definition von „fertig“

Melde das Projekt nur dann als fertig, wenn:

* die vollständige Solution vorhanden ist
* Debug- und Release-Build erfolgreich sind
* alle automatisierten Tests erfolgreich sind
* die wichtigsten Abnahmeszenarien aus `SPEC.md` umgesetzt sind
* Projektimport und Projektexport funktionieren
* PDF- und Standalone-HTML-Export funktionieren
* eine portable `win-x64`-Version erzeugt wurde
* der Installer erzeugt werden kann
* Benutzer- und Build-Dokumentation vorhanden sind
* keine unfertigen Platzhalter in produktivem Code verbleiben

Ein erfolgreicher Compilerlauf allein bedeutet nicht, dass das Projekt fertig ist.
