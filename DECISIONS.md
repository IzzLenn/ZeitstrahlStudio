# Architekturentscheidungen

## ADR-001: Schichtenarchitektur mit Ports in der Application-Schicht

**Status:** angenommen am 19.07.2026

Die Solution wird in WPF-App, Application, Domain, Infrastructure, DocumentProcessing, Export und Shared aufgeteilt. Domain bleibt frei von Infrastrukturabhängigkeiten. Application definiert asynchrone, abbrechbare Ports; äußere Schichten implementieren sie. Die WPF-App ist Composition Root und enthält keine Geschäftslogik im Code-behind.

Damit sind Datenbank, Dokumentbibliotheken und Export-Engines austauschbar und die fachliche Logik bleibt ohne Windows-UI testbar. Eine einzelne WPF-Projektstruktur wurde verworfen, weil sie die geforderte Trennung und isolierte Tests erschwert.

## ADR-002: Unvollständige Datumsangaben als Komponenten statt Ersatzdatum

**Status:** angenommen am 19.07.2026

`EventDate` speichert Genauigkeit, eingegebenes Jahr, optionalen Monat/Tag/Uhrzeit und bei Zeiträumen die Endkomponenten getrennt. Ein technischer Vergleichswert darf fehlende Komponenten nur temporär für die Sortierung ergänzen. Anzeige und Persistenz verwenden immer die tatsächliche Genauigkeit.

Ein einzelnes `DateTime` plus Formatflag wurde verworfen: Es würde erfundene Werte wie den 1. Januar fachlich in die Datenbank einschleusen und birgt das Risiko, diese später versehentlich anzuzeigen oder zu exportieren.

## ADR-003: GUIDs und technische UTC-Zeitstempel

**Status:** angenommen am 19.07.2026

Projekt-, Ereignis-, Frist-, Anhangs- und Audit-IDs sind GUIDs, damit Projektarchive ohne zentrale Vergabestelle kollisionsarm zwischen Rechnern übertragen und dupliziert werden können. Technische Zeitpunkte werden als `DateTimeOffset` mit Offset null gespeichert. Deutsche Ortszeit wird erst in der UI beziehungsweise im Export gebildet.

## ADR-004: SQLite-Arbeitskopie und atomar neu erzeugtes ZIP-Archiv

**Status:** angenommen am 19.07.2026

Eine `.zeitprojekt`-Datei ist ein versioniertes ZIP-Archiv, wird aber niemals direkt bearbeitet. Die Anwendung validiert und extrahiert sie in einen lokalen Arbeitsordner. Speichern erzeugt aus einem konsistenten Snapshot ein neues Archiv und ersetzt die Zieldatei erst nach vollständiger Prüfung.

Die direkte Arbeit im ZIP wurde wegen fehlender Transaktionssicherheit und schlechter Leistung verworfen. Eine einzige externe SQLite-Datei wurde verworfen, weil Originaldokumente, Vorschaubilder und extrahierte Texte transportabel mitgeführt werden müssen.

## ADR-005: Sicherheit von Archiv- und Anhangspfaden

**Status:** angenommen am 19.07.2026

Persistierte interne Pfade verwenden normalisierte relative `/`-Pfade. Absolute Pfade, leere Pfade sowie `.`- und `..`-Segmente werden abgelehnt. Beim Archivimport wird zusätzlich jeder kanonische Zielpfad auf den neu angelegten Arbeitsordner begrenzt. SHA-256, deklarierte Länge und Extraktionsgrenzen werden vor der Übernahme geprüft.

Ursprüngliche absolute Dateipfade dürfen ausschließlich als nicht erforderliche Metainformation gespeichert werden.

## ADR-006: Abhängigkeiten nur meilensteinbezogen einführen

**Status:** angenommen am 19.07.2026

Produktionspakete werden erst hinzugefügt, wenn ihre konkrete Implementierung im selben Meilenstein entsteht und Lizenz, Offline-Funktion und x64-Auslieferung geprüft sind. Aktuell sind nur .NET/WPF sowie reine Testpakete vorhanden. Dadurch bleibt die Angriffs- und Lizenzfläche nachvollziehbar.

xUnit wurde für Unit- und Integrationstests gewählt (Apache-2.0). `Microsoft.NET.Test.Sdk` und Coverlet stehen unter MIT. Alle sind reine Entwicklungsabhängigkeiten.

## ADR-007: Erwartbare Grenzfehler als OperationResult

**Status:** angenommen am 19.07.2026

Erwartbare Datei-, Format- und Analysefehler werden an Anwendungsgrenzen als `OperationResult<T>` mit stabilem Code, deutscher Benutzerbotschaft und optionalen technischen Details zurückgegeben. Verletzte Programm- oder Domain-Invarianten bleiben Ausnahmen. So können ViewModels handlungsorientierte Meldungen anzeigen, ohne technische Ausnahmen als normalen Kontrollfluss zu verwenden.

## ADR-008: Ausschließlich x64 und Per-Monitor-DPI

**Status:** angenommen am 19.07.2026

Die WPF-Anwendung wird für `win-x64` gebaut, verlangt keine Administratorrechte, aktiviert Long-Path-Unterstützung und Per-Monitor-V2-DPI-Awareness. Dies entspricht der Zielplattform und reduziert spätere native Varianten für SQLite, PDF und OCR.
