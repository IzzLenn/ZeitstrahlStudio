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

## ADR-009: Microsoft.Data.Sqlite mit expliziten SQL-Migrationen

**Status:** angenommen am 19.07.2026

Für SQLite wird `Microsoft.Data.Sqlite` 8.0.29 (MIT) mit dem gebündelten lokalen e_sqlite3 verwendet. Das Paket ist klein, ADO.NET-nah und unterstützt die benötigten Transaktionen, FTS5 und asynchronen Aufrufe. SQLitePCLRaw 2.1.6 wird transitiv unter Apache-2.0 eingebunden.

Das Schema wird durch eigene, fortlaufend versionierte SQL-Migrationen verwaltet. Ein vollständiges ORM wurde verworfen: Das normalisierte Schema, FTS5, atomare Massensynchronisierung und die bewusste Erhaltung von Analysetabellen sind mit explizitem SQL transparenter und vermeiden eine zusätzliche produktive Abhängigkeit. Jede Migration und jede Aggregatspeicherung läuft in einer Transaktion; neuere unbekannte Schema-Versionen werden schreibgeschützt abgelehnt.

## ADR-010: Archivimport über Staging und harte Ressourcenlimits

**Status:** angenommen am 19.07.2026

Projektarchive werden streamend verarbeitet. Der Import extrahiert ausschließlich im Manifest gelistete und per SHA-256 geprüfte Dateien in einen neuen temporären Geschwisterordner. Erst nach erfolgreicher Datenbankprüfung wird dieser Ordner auf den endgültigen Workspace-Namen verschoben. Vorhandene Zielordner werden nie überschrieben.

Als Sicherheitsgrenzen gelten derzeit 100.000 Dateien, 64 GiB pro Datei, 512 GiB dekomprimierte Gesamtgröße, 4 MiB für das Manifest und ein maximales Kompressionsverhältnis von 1000:1 für große Einträge. Zusätzlich bleibt eine Reserve von 64 MiB auf dem Ziellaufwerk. Die Werte erlauben die spezifizierten mehrgigabytegroßen Projekte, begrenzen aber ZIP-Bomben und unbeabsichtigte Ressourcenerschöpfung.

Der Export checkpointet SQLite, erzeugt ein neues Archiv im Zielverzeichnis, validiert es nach dem Schließen vollständig und verwendet auf Windows `File.Replace` für den atomaren Austausch eines vorhandenen Archivs. Die bisher gültige Datei bleibt bis zum erfolgreichen Austausch erhalten.

## ADR-011: Projektduplikate erhalten neue Projekt-ID

**Status:** angenommen am 19.07.2026

Eine Projektkopie erhält eine neue Projekt-GUID und einen aus dem Zielnamen abgeleiteten Projektnamen. Ereignis- und Anhangs-IDs bleiben innerhalb des separaten Archivs erhalten, damit alle internen Beziehungen, Analysetexte und Layoutpositionen unverändert bleiben. Das SQLite-Schema verwendet deshalb für Projekt-Fremdschlüssel `ON UPDATE CASCADE`.

## ADR-012: Lokaler Anwendungszustand als atomare JSON-Dateien

**Status:** angenommen am 19.07.2026

Die maximal 20 zuletzt verwendeten Archivpfade und Recovery-Sitzungsmarker sind anwendungsbezogener Zustand und gehören nicht in eine Projektdatenbank. Sie werden als kleine versionierte UTF-8-JSON-Dateien unter dem lokalen Anwendungsdatenordner beziehungsweise im nicht exportierten `metadata/session.json` des Workspace gespeichert. Jeder Schreibvorgang erzeugt zuerst eine neue Datei und ersetzt den vorherigen Stand atomar.

Recovery-Marker enthalten Projekt-ID, Archivpfad, Aktualisierungszeit und Prozessidentität. Ein Workspace eines nachweislich noch laufenden Prozesses wird nicht zur Wiederherstellung angeboten. Fehlende Marker verhindern die Wiederherstellung einer ansonsten gültigen verwaisten Datenbank nicht.

## ADR-013: Autosave serialisiert über denselben Workspace-Dienst

**Status:** angenommen am 19.07.2026

Autosave verwendet denselben vollständigen Repository-/Archivpfad wie manuelles Speichern. Ein `SemaphoreSlim` im Workspace-Dienst verhindert konkurrierende Speicherungen. Der Koordinator speichert nur als geändert markierte Workspaces, respektiert `CancellationToken` und meldet erwartbare Fehler, ohne seine periodische Schleife zu beenden. So entsteht kein zweiter, semantisch abweichender Speicherweg.

## ADR-014: Technische Logs als rotierende JSON Lines

**Status:** angenommen am 19.07.2026

Technische Anwendungslogs werden ausschließlich lokal als ein JSON-Objekt pro Zeile geschrieben. Standardmäßig sind fünf Dateien mit je höchstens 5 MiB vorgesehen. Nachrichten und technische Details werden begrenzt, damit keine versehentlich übergebenen vollständigen Dokumentinhalte das Log unkontrolliert vergrößern. Lesen, manueller Export und Löschen sind über eine Anwendungsschnittstelle verfügbar.

## ADR-015: Microsoft DI als WPF-Composition-Root

**Status:** angenommen am 19.07.2026

Die WPF-Anwendung erstellt beim Start einen einzigen validierten Microsoft.Extensions.DependencyInjection-Container. Sie registriert konkrete lokale Adapter gegen die Ports der Application-Schicht und hält ViewModels als einzige Orchestrierungsschicht zwischen UI und Anwendungsdiensten. Code-behind bleibt auf Fenster- und Dialoglebenszyklus beschränkt.

StartupUri wird nicht verwendet, weil das Hauptfenster erst nach erfolgreichem Aufbau des Containers erzeugt werden darf. Asynchrone Initialisierung, Kommandozeilenöffnung, Autosave und globales lokales Fehlerlogging werden vom Anwendungslebenszyklus koordiniert. Ein Service-Locator in ViewModels sowie statische globale Diensteinzelobjekte wurden wegen versteckter Abhängigkeiten und schlechter Testbarkeit verworfen.

## ADR-016: Ereignisbearbeitung durch validierten atomaren Ersatz

**Status:** angenommen am 19.07.2026

Eine Ereignisbearbeitung verändert nicht schrittweise das bereits im Projekt befindliche Objekt. Die Application-Schicht konstruiert aus der vollständigen Eingabe zuerst ein neues, durch die Domain validiertes Ereignis und ersetzt erst danach den bisherigen Eintrag mit derselben ID. Anhänge, Erstellungszeitpunkt, manuelle Sortierposition und bestehende Link-IDs werden dabei erhalten.

Damit bleibt das Aggregat bei einem ungültigen späteren Feld – beispielsweise einem fehlerhaften Link – vollständig unverändert. Schrittweise Setter-Aufrufe mit manueller Rückabwicklung wurden verworfen, weil jede neue Eigenschaft eine weitere unvollständige Fehlerkombination erzeugen könnte. Der atomare Ersatz bildet zugleich eine klare Basis für Undo/Redo-Snapshots.

## ADR-017: Begrenzte Snapshot-Historie und projektinternes SQLite-Audit

**Status:** angenommen am 19.07.2026

Undo/Redo hält pro geöffnetem Projekt höchstens 100 validierte Vorher-/Nachher-Snapshot-Operationen im Arbeitsspeicher. Ein Schritt kann mehrere Ereignisse enthalten, sodass die manuelle Reihenfolge einer Gruppe mit identischem Datum geschlossen rückgängig gemacht wird. Snapshots teilen ausschließlich unveränderliche Anhänge und Wertobjekte; das Ereignis selbst wird bei jeder Bearbeitung ersetzt.

Die Sitzungshistorie wird nicht dauerhaft im Projektarchiv gespeichert und beim Schließen freigegeben. Das fachliche Änderungsprotokoll ist davon getrennt: erfolgreiche Operationen werden dauerhaft in der bereits migrierten AuditLog-Tabelle der lokalen Projekt-SQLite-Datenbank gespeichert. Ein nicht schreibbarer Audit-Eintrag darf eine bereits erfolgreiche fachliche Änderung nicht zurückrollen; der Fehler wird stattdessen im technischen Lokalprotokoll erfasst.

## ADR-018: Streaming-Anhangsimport mit GUID-Zielpfaden

**Status:** angenommen am 19.07.2026

Jeder importierte Anhang wird unter attachments/{Ereignis-ID}/{Anhangs-ID}.{Endung} gespeichert. Der ursprüngliche Name bleibt reine Metainformation. Dadurch können beliebig viele gleichnamige Dateien ohne Überschreiben nebeneinander bestehen. Zielpfade werden mit derselben kanonischen Root-Prüfung wie Archivpfade auf den Workspace begrenzt.

Kopie und SHA-256-Berechnung erfolgen in einem asynchronen Streaming-Durchlauf mit gepooltem Puffer. Nach Abschluss werden Quelllänge und Änderungszeit erneut geprüft. Ein Batch liefert für jede Datei ein eigenes OperationResult; erwartbare Einzeldateifehler brechen andere Importe nicht ab, ein CancellationToken dagegen beendet den gesamten laufenden Vorgang und entfernt die aktuelle Teildatei.

## ADR-019: Open-XML-Analyse mit sicheren .NET-Bordmitteln

**Status:** angenommen am 19.07.2026

DOCX und XLSX werden als ZIP-basierte Open-XML-Formate direkt und streamend mit System.IO.Compression und XmlReader analysiert. DTD-Verarbeitung und externe XML-Resolver sind deaktiviert. Harte Grenzen für ZIP-Einträge, entpackte Größen, Kompressionsverhältnis und extrahierte Zeichen schützen vor Ressourcenmissbrauch.

Für diese Formate wurde bewusst keine zusätzliche Office-Bibliothek eingeführt: Haupttext, Zellwerte, Shared Strings und Kerneigenschaften lassen sich mit wenigen klar begrenzten Readern vollständig lokal auslesen. Microsoft Office muss weder installiert noch automatisiert werden.

## ADR-020: Analyseablage und FTS-Aktualisierung in einer Transaktion

**Status:** angenommen am 19.07.2026

Text, Extraktionsmethode und Anhangsmetadaten werden gemeinsam mit dem Statuswechsel des Anhangs in einer SQLite-Transaktion ersetzt. Derselbe Commit baut den projektbezogenen FTS5-Inhalt neu auf. Dadurch kann weder ein halbes Analyseergebnis noch ein vom gespeicherten Text abweichender Suchindex sichtbar werden.

UI- und Analyzer-Schichten schreiben nicht direkt in Tabellen, sondern verwenden einen Application-Port. Reservierte interne Metadatenkeys erhalten ein ZeitstrahlStudio-Präfix und werden beim Laden wieder von dokumenteigenen Metadaten getrennt.

## ADR-021: Begrenzte Analysewarteschlange nach lokalem Workspace-Checkpoint

**Status:** angenommen am 19.07.2026

Importierte DOCX- und XLSX-Anhänge werden vor der Analyse durch denselben serialisierten Workspace-Dienst in der lokalen SQLite-Arbeitskopie persistiert. Dieser Checkpoint ersetzt ausdrücklich nicht das Benutzerarchiv und behält den Zustand „ungespeichert“ bei. Erst danach führt eine zentrale, abbrechbare Warteschlange höchstens zwei Analysen gleichzeitig aus und speichert jedes erfolgreiche Einzelergebnis über den Analyse-Port.

Die Obergrenze von zwei parallelen Dokumenten verhindert unkontrollierte Speicher- und CPU-Last und lässt SQLite-Schreibtransaktionen kurz genug konkurrieren. Ein unbegrenztes Task-pro-Datei-Modell wurde verworfen. Analysezustände werden nach dem Stapel in das Domainaggregat zurückgeführt und erneut gecheckpointet, damit eine spätere Aggregatspeicherung den Datenbankzustand nicht zurücksetzt. Diese technischen Zustandswechsel erzeugen keinen separaten Undo-Schritt; die vom Benutzer ausgelöste Anhangszuordnung bleibt die rückgängig machbare Operation.

## ADR-022: PdfPig für eingebettete PDF-Texte

**Status:** angenommen am 19.07.2026

Für das lokale Lesen von PDF-Text und Dokumentmetadaten wird PdfPig 0.1.15 unter Apache-2.0 verwendet. Das Paket stellt ein direktes .NET-8-Ziel ohne transitive Paketabhängigkeiten bereit, benötigt keine Office-Installation und startet keinen externen Prozess. Die synchrone Bibliotheksarbeit wird auf einem Worker-Thread ausgeführt; die Anwendung prüft Abbruch vor dem Öffnen und vor jeder Seite.

Die Anwendung begrenzt die eigene Verarbeitung auf 100.000 Seiten, zehn Millionen extrahierte Zeichen und eine Parser-Stacktiefe von 64. Fehlende Fonts dürfen übersprungen werden, damit ein einzelner defekter Font nicht den übrigen eingebetteten Text verliert. PdfPig wird nicht für Rendering oder OCR zweckentfremdet: Bildbasierte Seiten liefern bis zur späteren lokalen OCR einen leeren eingebetteten Text, während PDF-Vorschau und OCR getrennte, vor Einführung erneut lizenz- und paketierungsgeprüfte Komponenten bleiben.
