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

## ADR-023: Integritätsprüfung vor Vorschau und ShellExecute

**Status:** angenommen am 19.07.2026

Vorschau und externes Öffnen verwenden ausschließlich die in das Projekt kopierte Datei. Ein zentraler Infrastructure-Dienst löst den normalisierten relativen Pfad unterhalb des Workspace auf, lehnt Reparse Points ab und prüft Länge, Änderungsstabilität sowie SHA-256, bevor er den Pfad an die WPF-Vorschau oder auf ausdrücklichen Benutzerwunsch an Windows ShellExecute übergibt.

Bildvorschauen verwenden die in WPF vorhandenen lokalen Windows-Codecs und benötigen keine weitere Produktionsabhängigkeit. Die integrierte Darstellung dekodiert höchstens 2.400 Pixel Breite und lehnt Dateien über 512 MiB mit einer verständlichen Ausweichmöglichkeit auf das Standardprogramm ab. Eine automatische Ausführung beim Import wurde verworfen: Externe Programme werden nur nach einer sichtbaren Benutzeraktion gestartet.

## ADR-024: PDFium für die integrierte PDF-Seitenvorschau

**Status:** angenommen am 19.07.2026

PDF-Seiten werden mit PDFtoImage 5.2.1 (MIT) und der lokal gebündelten PDFium-Windows-Binärdatei 147.0.7690 (NuGet-Lizenzausdruck Apache-2.0) gerendert. Der Application-Port liefert ausschließlich begrenzte PNG-Seiten an die WPF-App; PDFium und SkiaSharp bleiben Implementierungsdetails der Dokumentverarbeitung. Für `win-x64` werden nur `pdfium.dll`, die passende Skia-Binärdatei und die verwalteten Assemblies veröffentlicht. Es werden weder externe Prozesse noch Netzwerkzugriffe verwendet.

Pro Seite gelten höchstens 24 Millionen Pixel, 8.000 Pixel je Kante und 100 MiB PNG-Daten; Dokumente mit mehr als 100.000 Seiten werden abgelehnt. Die Projektkopie bleibt während Seitenermittlung und Rendering ohne Schreib-/Löschfreigabe geöffnet. Rendering läuft außerhalb des UI-Threads. Ein bereits laufender nativer Einzelseitenaufruf ist durch PDFium nicht unterbrechbar, der Abbruch wird jedoch vor dem Öffnen, vor und unmittelbar nach dem Rendern sowie vor der UI-Übernahme geprüft. PDFtoImage serialisiert seine PDFium-Zugriffe prozessweit, weil die native Bibliothek nicht thread-sicher ist.

PdfPig.Rendering.Skia wurde nach einem reproduzierbaren Integrationstest verworfen: Beim Rendern einer gültigen PDF mit der Standard-14-Schrift Helvetica durchsuchte die Bibliothek installierte Windows-Schriftdateien und ließ eine Parserausnahme einer anderen Schriftdatei ungefangen bis zur Anwendung gelangen. Ein öffentlicher Font-Resolver-Hook ist in der geprüften Version nicht vorhanden. Eine Vorschau, deren Erfolg vom installierten Schriftbestand abhängt, erfüllt die Robustheitsanforderung nicht. Der PDFium-basierte Ersatz rendert genau diesen Standardfont-Testfall erfolgreich.

## ADR-025: Windows OCR als lokale Texterkennung

**Status:** angenommen am 19.07.2026

Bildanhänge und bildbasierte PDF-Seiten werden mit `Windows.Media.Ocr` verarbeitet. Die Dokumentverarbeitung zielt dafür auf `net8.0-windows10.0.19041.0`; als unterstützte Mindestplattform bleibt Windows 10 Version 1507 bestehen, weil die verwendeten OCR- und Bilddekodierungsverträge dort bereits vorhanden sind. Der Application-Port und das Domainmodell bleiben plattformneutral. Die .NET-WinRT-Projektion wird durch den Windows-SDK-Targeting-Pack bereitgestellt; es werden keine Cloud- oder Fremdprozesse gestartet.

Deutsch ist die verbindliche OCR-Sprache. Die Anwendung verwendet nur eine bereits auf dem Gerät installierte deutsche Windows-Texterkennungsressource und meldet verständlich, wie eine fehlende Ressource über die Windows-Spracheinstellungen ergänzt wird. Die Erkennung selbst bleibt offline. Ein Tesseract-Wrapper wurde verworfen: Die geprüfte aktuelle Fassung setzt zusätzlich die Visual-C++-2022-Runtime voraus, und ein passend versioniertes, lizenzklar mitlieferbares deutsches Sprachmodell steht nicht als seriöses NuGet-Paket zur Verfügung. Ein Build-Download externer Modelldateien hätte reproduzierbare Offline-Builds und die portable Auslieferung verschlechtert.

Die OCR wird prozessweit serialisiert und zusätzlich von der bestehenden Analysewarteschlange begrenzt. Pro Bildseite gelten höchstens 24 Millionen dekodierte Pixel, dynamisch zusätzlich `OcrEngine.MaxImageDimension`; komprimierte Bilddateien sind auf 512 MiB, Mehrseitenbilder und OCR-bedürftige PDFs auf 250 Seiten und erkannter Text auf zehn Millionen Zeichen begrenzt. PDF-Seiten werden mit PDFium bei dreifacher Basisskalierung gerendert. Eingebetteter Text bleibt vorrangig; nur textleere Seiten erhalten OCR. Gemischte PDFs werden mit einer eigenen Extraktionsmethode gekennzeichnet.

OCR-Ergebnisse erhalten dauerhaft den Anhangsstatus „Warnung“, einen sichtbaren Fehlerhinweis, die verwendete Sprache und die betroffenen PDF-Seiten. Sie werden wie direkter Dokumenttext transaktional gespeichert und in derselben Transaktion in FTS5 aufgenommen. Einzelbild-/Seitenschritte werden an die vorhandene Fortschrittsanzeige weitergereicht; CancellationToken wird vor, zwischen und in den WinRT-Async-Aufrufen geprüft.

## ADR-026: Gemeinsames deterministisches Zeitstrahl-Layoutmodell

**Status:** angenommen am 19.07.2026

Horizontale und vertikale Zeitstrahlen verwenden denselben UI-unabhängigen `TimelineLayoutEngine` in der Application-Schicht. Das Ergebnis enthält reine Achsen-/Kartenkoordinaten, Bahnen, Skalenticks, eindeutig beschriftete Unterbrechungen und Fristverbindungen. WPF, PDF-Export und Standalone-HTML können dadurch später dieselben fachlichen Projektionsregeln verwenden, ohne Darstellungslogik aus einer konkreten Oberfläche zu kopieren.

Die Zeiteinheit wird aus der gesamten belegten Projektspanne inklusive Ereigniszeiträumen, Projektgrenzen und Fristen gewählt. Die Projektion ist stückweise linear. Große leere Intervalle werden nur oberhalb skalenabhängiger Schwellen komprimiert; ein von einem Ereigniszeitraum belegtes Intervall ist ausdrücklich keine leere Lücke. Die feste sichtbare Restlänge einer Unterbrechung wächst moderat mit dem Zoom, sodass eine Unterbrechung stets als reale Zeitspanne erkennbar bleibt.

Karten wechseln zunächst die Achsenseite. Überlappende Karten derselben Seite werden der ersten freien Bahn zugeordnet. Persistierte `LayoutPosition`-Werte werden danach als rein visuelle orientierungsabhängige Versätze angewendet und ändern niemals das Datum. Tickzahl und manuelle Darstellungsversätze sind begrenzt, alle Eingaben müssen endlich sein. Diese deterministische Stufe bleibt schnell genug für mindestens 5.000 Ereignisse und wird vor der WPF-Integration vollständig durch Unit-Tests abgesichert.

## ADR-027: Viewport-Zeichnung statt einer visuellen Karteninstanz pro Ereignis

**Status:** angenommen am 19.07.2026

Die interaktive WPF-Zeitstrahlansicht ist ein eigenes `FrameworkElement` mit `IScrollInfo`. Sie übernimmt das reine Ergebnis des `TimelineLayoutEngine`, transformiert es anhand der Scrollposition in den Viewport und zeichnet nur sichtbare Ticks, Unterbrechungen, Fristverbindungen und Karten. Ein `ItemsControl` mit `Canvas` und einer WPF-Elementinstanz pro Ereignis wurde verworfen, weil übliche Virtualisierungspanels die frei positionierten, beidseitig verteilten Karten nicht korrekt virtualisieren und bei mindestens 5.000 Ereignissen unnötig viele Layout- und Visualobjekte erzeugen würden.

Der Renderer besitzt keine fachliche Datums- oder Speicherlogik. Projekt, Auswahl, Orientierung, Zoom, Lückenkompression und eine reine Layoutrevision werden über Dependency Properties eingespeist. Auswahl und Viewportnavigation bleiben lokale Präsentationszustände; Änderungen der projektweiten Orientierung und Lückenkompression laufen über das Haupt-ViewModel, markieren das Projekt als geändert und erzeugen einen Audit-Eintrag.

Zoom wird auf 25 bis 800 Prozent begrenzt und beim Mausrad am Inhalt unter dem Zeiger verankert. `IScrollInfo` stellt dieselben berechneten Ausmaße für Scrollleisten, Mausverschiebung, Zentrierung und Gesamtprojektansicht bereit. Dadurch bleibt die Bedienung unabhängig von der Orientierung konsistent, während die chronologische Liste als zugängliche alternative Darstellung erhalten bleibt.

## ADR-028: Datumsanker bleibt von manueller Kartenposition getrennt

**Status:** angenommen am 19.07.2026

Ein `TimelineCardLayout` enthält sowohl den unveränderten Achsenanker des Ereignisdatums als auch die visuell versetzte Kartenposition. Verbindungslinien und Fristbezüge beginnen am fachlichen Anker, während Zentrierung, Treffertest und Kartendarstellung die manuelle Position verwenden. Dadurch bleibt selbst bei einem Versatz entlang der Zeitachse sichtbar, welchem realen Datum die Karte zugeordnet ist; Drag-and-drop kann in diesem Modus niemals unbemerkt ein Datum ändern.

Die WPF-Ansicht übermittelt beim Ende einer Kartenbewegung ausschließlich die Bildschirmdifferenz, Ereignis-ID und aktuelle Orientierung an das ViewModel. Die Application-Schicht addiert diese Differenz auf die bestehende orientierungsabhängige `LayoutPosition`, begrenzt beide Koordinaten auf ±100.000 Pixel und zeichnet einen Layout-Änderungsschritt in derselben maximal 100 Einträge umfassenden Undo-/Redo-Historie auf. Löschen erfasst zugehörige Positionen mit, damit Undo das vollständige sichtbare Ereignis wiederherstellt. „Auto-Layout“ ist ein einzelner gemeinsamer Historieneintrag für alle entfernten Positionen.

Die Navigation zu einem gewählten Zeitraum projiziert dessen Grenzen über eine öffentliche Datumsabbildung des gemeinsamen Layoutmodells. Eine zweite, vereinfachte lineare Umrechnung in WPF wurde verworfen, weil sie bei komprimierten Lücken einen anderen Ausschnitt als Achse, PDF oder spätere HTML-Darstellung liefern würde.

## ADR-029: Dokument-FTS und aktuelles Aggregat werden getrennt durchsucht

**Status:** angenommen am 19.07.2026

Schema-Version 2 ergänzt `DocumentSearchIndex` als eigenen FTS5-Index für extrahierte PDF-, OCR-, DOCX- und XLSX-Texte. Der bisherige `SearchIndex` bleibt für Kompatibilität und Integritätsprüfungen bestehen. Die Trennung verhindert, dass ein im gespeicherten Index noch vorhandener, inzwischen aber im Arbeitsspeicher geänderter Ereignistitel kurzzeitig als veralteter Treffer erscheint. Migration 2 übernimmt vorhandene extrahierte Texte transaktional; Repository-Speicherungen und Analyseergebnisse aktualisieren beide Indizes in derselben Transaktion.

Der Suchdienst öffnet die Projektdatenbank schreibgeschützt, privat und ohne Pooling. Er zerlegt Eingaben in höchstens 32 alphanumerische Begriffe mit je höchstens 64 Zeichen, setzt sie ausschließlich als parametrisierte, gequotete FTS-Präfixabfrage ein und begrenzt die Ergebnismenge auf 5.000 Ereignisse. Aktuelle Projekt-/Ereignisfelder und alle strukturierten Filter werden gegen das bereits validierte Domainaggregat ausgewertet. Dadurch sind noch nicht archivierte Änderungen sofort sichtbar, während große Dokumentinhalte effizient im SQLite-Index bleiben.

Die Oberfläche startet nach 250 Millisekunden Eingabepause eine abbrechbare Suche. Treffer enthalten mit `⟦…⟧` markierte Ausschnitte, die ein spezialisiertes WPF-Textsteuerelement ohne HTML-Interpretation hervorhebt. Bei aktiven Kriterien wird nur die resultierende Ereignis-ID-Menge an den vorhandenen `TimelineLayoutEngine` gegeben; es existiert kein zweites, abweichendes Filterlayout. Ein Treffer ändert die fachlichen Daten nicht, sondern synchronisiert Auswahl und Zentrierauftrag der Zeitstrahlansicht.

## ADR-030: Deterministische PDF-Seitenplanung vor dem Skia-Vektor-Rendering

**Status:** angenommen am 19.07.2026

Der PDF-Export trennt fachliche Auswahl und Seitenaufteilung strikt vom Renderer. Ein UI-unabhängiger Planer validiert Papiermaße und Zeitraum, filtert Ereignisse sowie unabhängige Fristen, bricht alle Textfelder konservativ um und weist jede Karte einer Seite zu. Eine gewöhnliche Karte wird bei Platzmangel vollständig auf die nächste Seite verschoben. Nur ein Inhalt, der selbst höher als eine vollständige Inhaltsseite ist, wird ohne Textverlust auf sichtbar markierte Fortsetzungskarten verteilt. Der große Einzelseitenmodus berechnet seine tatsächliche Höhe aus denselben Karten und warnt ab 1.000 mm; die harte PDF-Grenze beträgt 5.080 mm.

Für die Ausgabe wird das bereits lokal gebündelte und MIT-lizenzierte SkiaSharp 3.119.2 direkt im Exportprojekt verwendet. `SKDocument` schreibt Texte, Achse, Verbindungslinien, Rahmen und Statuskennzeichnungen als PDF-Vektoren. Damit entsteht keine neue Drittanbieterkomponente oder Lizenz. Der vorhandene PDFium-Dienst rendert anschließend genau die temporär erzeugte PDF für die WPF-Exportvorschau; Vorschau und endgültige Datei können dadurch nicht auf zwei abweichenden Renderpfaden beruhen.

Miniaturen werden nicht über unvalidierte relative Pfade geladen. Der Exportdienst fordert die Projektkopie beim zentralen `IAttachmentFileService` an und verwendet für primäre PDFs den vorhandenen begrenzten PDFium-Seitenrenderer, alternativ ein Bild bis 50 MiB Quelldaten. Ein Miniaturfehler lässt den vollständigen textuellen Dokumentverweis bestehen. Die endgültige PDF wird in eine eindeutige temporäre Datei desselben Zielordners geschrieben und erst nach vollständigem Erfolg atomar übernommen. Ziele innerhalb des aktiven Projektarbeitsordners sind gesperrt, damit ein Export niemals Datenbank oder Projektanhänge überschreiben kann.

## ADR-031: Selbstenthaltender HTML-Snapshot mit sicherem JSON-/DOM-Vertrag

**Status:** angenommen am 19.07.2026

Der Standalone-HTML-Export besteht aus einer statischen, versionsgebundenen Vorlage und einem ausschließlich lokal erzeugten JSON-Datenobjekt. CSS, Programmlogik, Projektdaten und optionale Miniaturen werden in eine einzelne UTF-8-Datei eingebettet. Es gibt keine externen Ressourcen oder Laufzeitbibliotheken. Die responsive HTML-Darstellung berechnet ihre Kartenanordnung im Browser neu und übernimmt daher bewusst keine WPF-Pixelkoordinaten; fachliche Reihenfolge, Datumsbereiche und sichtbare Zeitlücken bleiben erhalten, während sich die Darstellung an Fenster, Orientierung, Filter und Zoom anpasst.

Das JSON wird mit dem standardmäßigen sicheren `System.Text.Json`-Encoder serialisiert, sodass insbesondere `<`, `>`, `&` und eine mögliche `</script>`-Sequenz nicht die Dateninsel verlassen können. Die Browserlogik setzt benutzerkontrollierte Werte ausschließlich über `textContent` beziehungsweise erzeugte DOM-Attribute ein und verwendet kein `innerHTML`. Eine Content Security Policy sperrt standardmäßig alle Quellen und erlaubt nur eingebettete Bilder sowie die für die Einzeldatei notwendigen Inline-Styles und das Inline-Skript. Webseitenlinks bleiben sichtbare externe HTTP-/HTTPS-Ziele und erfordern vor dem Öffnen eine Bestätigung.

Dokumentvolltexte werden für die lokale Suche eingebettet, aber nicht automatisch im Detailbereich angezeigt. Private Notizen und Miniaturen sind explizite Exportoptionen. Vorschaubilder durchlaufen dieselbe zentrale Integritätsprüfung wie PDF- und WPF-Vorschauen; Bildquellen werden vor dem Decoding auf 8.000 Pixel je Kante und 24 Millionen Pixel begrenzt und anschließend auf höchstens 360 × 240 Pixel als JPEG reduziert. Fehler einzelner Analysen oder Miniaturen dürfen den übrigen Snapshot nicht verhindern. Die Datei entsteht atomar über eine temporäre Datei im Zielordner; ein Ziel im aktiven Projektarbeitsordner wird abgelehnt.

## ADR-032: Rekonstruierbare lokale Archiv-Snapshots mit sicherer Wiederherstellung

**Status:** angenommen am 19.07.2026

Projektsicherungen verwenden denselben versionierten und atomaren `.zeitprojekt`-Archivdienst wie die reguläre Speicherung. Der Workspace-Dienst serialisiert Snapshot, Speichern und Schließen über dasselbe Gate, persistiert vor dem Export den aktuellen Aggregatzustand in `project.db` und lässt Archivpfad sowie Ungespeichert-Kennzeichen des geöffneten Projekts unverändert. Ein zweiter, abweichender Backup-Dateityp wurde verworfen, weil er Importprüfung, Migration und Prüfsummenlogik duplizieren würde.

Sicherungen liegen ausschließlich lokal unter `%LocalAppData%\Zeitstrahl Studio\Backups\{Projekt-ID}`. Verwaltete Dateinamen enthalten UTC-Zeitpunkt, Art und GUID; SQLite speichert nur den normalisierten relativen Pfad, Größe und SHA-256. Metadaten werden erst nach dem vollständig erzeugten und stabil gehashten Archiv geschrieben. Bleibt nach einem Prozessabbruch eine vollständige Datei ohne Datensatz zurück, rekonstruiert die Auflistung deren Metadaten aus dem streng geprüften Namen und der neu berechneten Prüfsumme. Dadurch braucht der Datei-/Datenbankübergang keine verteilte Transaktion. Fehlende Dateien entfernen dagegen ihren veralteten Metadatensatz.

Automatische Sicherungen werden nur nach erfolgreichen Projekterstellungen, expliziten Speicherungen und Autosaves geprüft. Ihre Mindestdistanz ergibt sich aus der konfigurierten Anzahl aktueller Tagessicherungen und ist auf 30 Minuten bis 24 Stunden begrenzt. Die Rotation wertet lokale Kalendertage und ISO-Wochen aus: mehrere aktuelle, je eine tägliche und anschließend je eine wöchentliche automatische Sicherung bleiben erhalten. Manuelle Sicherungen werden nie automatisch rotiert. Eine Rotation beginnt erst, nachdem eine neue automatische Sicherung samt Metadaten erfolgreich vorliegt; ein nicht löschbarer Altstand bleibt mit Warnprotokoll bestehen.

Eine Wiederherstellung validiert den bekannten Datensatz und die Datei zunächst über Pfad, Reparse-Point-Abwehr, Größe und SHA-256. Danach entsteht zwingend eine manuelle Sicherheitssicherung des aktuellen Stands. Die Auswahl wird erneut geprüft, mit dem normalen sicheren Import in einen neuen verwalteten Workspace geladen und nach dem Import nochmals gehasht. Nur dieselbe Projekt-ID ist zulässig. Der bisherige Archivpfad bleibt erhalten, der neue Stand wird als ungespeichert markiert und der erfolgreiche Vorgang im wiederhergestellten Projekt-Audit festgehalten. Die WPF-Schicht schließt erst danach den alten Workspace; Code-behind verwaltet ausschließlich den modalen Fensterlebenszyklus, während Liste, Aufbewahrung, Bestätigungsergebnis und Wiederherstellung im ViewModel beziehungsweise in Application-Ports bleiben.

## ADR-033: Ableitbarer Thumbnail-Dateicache mit sichtbarkeitsgesteuertem WPF-Laden

**Status:** angenommen am 19.07.2026

Die Auswahl des primären visuellen Anhangs liegt als UI-unabhängige Application-Regel vor: Das erste PDF hat Vorrang, andernfalls wird das erste unterstützte Bild verwendet. Interaktiver WPF-Zeitstrahl, PDF-Export und Standalone-HTML greifen auf diese Regel zurück. Drei unabhängig gepflegte Auswahlalgorithmen wurden verworfen, weil sie bei gemischten Anhängen unterschiedliche Dokumente darstellen könnten.

Der neue Application-Port für Kartenminiaturen liefert ausschließlich begrenzte, kodierte Bilddaten. Seine Skia-Implementierung fordert den lokalen Pfad immer über die zentrale Längen-, Stabilitäts-, Reparse-Point- und SHA-256-Prüfung an; PDF-Seiten kommen über den vorhandenen PDFium-Port. Bildquellen sind vor dem Decoding auf 50 MiB, 8.000 Pixel je Kante und 24 Millionen Pixel begrenzt. Die Ausgabe wird mit dem bereits vorhandenen SkiaSharp auf höchstens 360 × 240 Pixel skaliert und als JPEG atomar unter `thumbnails/timeline` gespeichert. Anhangs-ID, verknüpfte PDF-Seite und vollständige SHA-256 bilden den Dateinamen. Der Cache ist damit vollständig aus Originaldatei und Metadaten ableitbar, wird vom bestehenden Projektarchiv mitgenommen und benötigt weder ein neues Datenbankschema noch eine neue Produktionsabhängigkeit.

Die WPF-Ansicht startet einen Auftrag erst beim Zeichnen einer im Viewport sichtbaren Karte. Der singletonweite Renderer begrenzt Erzeugung auf zwei parallele Vorgänge; Projektwechsel, Dienstwechsel und Entladen brechen veraltete Aufträge ab. Dekodierte `BitmapSource`-Objekte werden eingefroren und in einem auf 128 Einträge begrenzten LRU-Speicher gehalten. Ein `ItemsControl` mit unsichtbaren Bildinstanzen oder ein ungegrenzter Vorab-Scan aller Ereignisse wurde verworfen, weil beides die bereits erreichte Viewport-Virtualisierung für 5.000 Ereignisse unterlaufen würde.

## ADR-034: Persistiertes Projektfarbschema über dynamische WPF-Ressourcen

**Status:** angenommen am 19.07.2026

Farbschema, bevorzugte Orientierung, Lückenkompression, Standardereignisfarbe und Karten-/Achsen-/Exportschriftgrößen bleiben im vorhandenen `ProjectSettings`-Datensatz. Ein gebundener modaler Dialog bearbeitet eine Kopie, validiert sie vollständig und übergibt erst danach ein neues unveränderliches Settings-Objekt. Das Haupt-ViewModel führt die Aggregatänderung, den Workspace-Checkpoint und den Audit-Eintrag aus; Code-behind beschränkt sich auf Bestätigen und modalen Fensterabschluss. Neue Ereignisse erhalten die gespeicherte Standardfarbe, während bestehende Ereignisfarben unverändert bleiben.

Hell, Dunkel und optional die lokale Windows-App-Einstellung werden ohne Neustart durch einen App-lokalen Theme-Dienst auf benannte dynamische Brush-Ressourcen abgebildet. Hauptfenster und Standarddialoge verwenden dieselben Ressourcen; Bild-, PDF- und Druckvorschauflächen behalten absichtlich neutrale dunkle Arbeitsflächen beziehungsweise papierweißes Seitenmaterial. Der direkt gezeichnete Zeitstrahl erhält den effektiven Hell-/Dunkelzustand als Dependency Property und erzeugt daraus seine eigene kontrastreiche, eingefrorene Brush-/Pen-Palette. Das Ersetzen kompletter Fenster oder ein Neustart beim Themewechsel wurde verworfen, weil dadurch Auswahl, Scrollposition und laufende lokale Vorgänge verloren gingen.

Die Kartenschriftgröße fließt in den gemeinsamen `TimelineLayoutEngine` ein. Größere Schrift vergrößert Karten und Kollisionsabstände deterministisch; die Achsenschrift bleibt eine reine Rendergröße. Damit führt eine Darstellungsänderung nicht zu abgeschnittenem Text oder einem zweiten WPF-spezifischen Layoutmodell. Die vorhandene `ExportFontSize` bleibt die gemeinsame persistierte Vorgabe des PDF-Exports.

## ADR-035: Typisierte Drop-Aufträge mit fachlich festem Ziel

**Status:** angenommen am 19.07.2026

WPF verarbeitet bei Drag-and-drop ausschließlich Präsentationsaufgaben: geroutete Drag-Daten lesen, sichtbare Karte beziehungsweise Listenzeile treffen, Vorher/Nachher aus der Zeilenhälfte ableiten und eine Drop-Markierung zeichnen. Daraus entstehen unveränderliche, typisierte `EventReorderRequest`- oder `AttachmentDropRequest`-Objekte. Das Haupt-ViewModel prüft deren Ausführbarkeit und übergibt Sortierungen an den `ProjectEventEditingService` beziehungsweise Dateien an den vorhandenen `IAttachmentImportService`. Eine zweite Import- oder Speicherimplementierung im Code-behind wurde verworfen, weil sie Pfadschutz, Fortschritt, Abbruch, Teilfehler, Historie und Audit duplizieren würde.

Ein Datei-Drop hält die beim Treffertest bestimmte Ereignis-ID für den gesamten asynchronen Batch fest. Konkrete Zeitstrahlkarten und Ereigniszeilen behandeln den gerouteten Vorgang zuerst; nur auf freien Flächen erreicht er als Fallback das Hauptfenster und verwendet dort das aktuell ausgewählte Ereignis. Dadurch kann ein späterer Auswahlwechsel keinen teilweise laufenden Mehrfachimport auf ein anderes Ereignis umlenken. Leerpfade und Duplikate werden bereits an der WPF-Grenze entfernt; vollständige Datei-, Pfad-, Reparse-Point-, Stabilitäts- und Prüfsummenvalidierung bleibt allein im zentralen Importdienst.

Ein Sortier-Drop ist nur ausführbar, wenn Quelle und Ziel dieselbe vollständige `EventDate`-Angabe besitzen und die gewünschte Vorher-/Nachher-Position die Reihenfolge tatsächlich ändert. Die Application-Schicht ordnet die vollständige Datumsgruppe neu, normalisiert deren manuelle Positionen in einem gemeinsamen Historieneintrag und verändert kein Datumsfeld. Ein Drop auf andere Datumswerte wird bewusst nicht als Datumsänderung interpretiert; die vorhandenen Früher-/Später-Befehle bleiben die tastaturzugängliche Alternative.

## ADR-036: Programmatisch erzeugtes, semantisch reproduzierbares Beispielprojekt

**Status:** angenommen am 19.07.2026

Das frei weitergebbare Beispielprojekt und seine fünf Quelldokumente werden von einem eigenen .NET-Werkzeug unter `tools/ZeitstrahlStudio.SampleGenerator` erzeugt. PDF, PNG, DOCX und XLSX entstehen ausschließlich aus fest definierten, frei erfundenen Inhalten; vorhandene MIT-lizenzierte Produktionsbibliotheken und Standardformate werden wiederverwendet. Der Generator importiert die Dateien über den produktiven sicheren Anhangsdienst, analysiert PDF-/Office-Texte, erzeugt eine reale Bildminiatur, persistiert das Projekt über SQLite und exportiert es über den normalen `.zeitprojekt`-Archivdienst. Dadurch existiert kein vereinfachtes zweites Beispielprojektformat und keine neue Produktionsabhängigkeit.

Fachliche Projekt-ID, Ereignis-IDs, Zeitpunkte, Texte, Klassifikationen und Layoutbeispiele sind fest definiert. SQLite, ZIP und die PDF-Writer-Bibliothek dürfen jedoch laufzeitabhängige Binärmetadaten erzeugen; eine erneute Erzeugung muss deshalb semantisch, aber nicht byteidentisch zum eingecheckten Archiv sein. Automatisierte Tests vergleichen die fachlichen Ereignisse sowie Dokumentnamen und Medientypen. Für einen tatsächlichen Archivtransfer wird zusätzlich geprüft, dass jede erzeugte SHA-256-Prüfsumme unverändert erhalten bleibt. Maschinenspezifische Ursprungsdateipfade werden vor der Persistenz entfernt.

Das eingecheckte Archiv bleibt ein eigenständiges Abnahmeobjekt. Ein Integrationstest importiert es über die vollständige Manifest-, Pfad- und Prüfsummenprüfung, validiert alle geforderten Datums-, Frist-, Sortier-, Link-, Dokument- und Layoutvarianten, öffnet ein PDF über PDFium, durchsucht die extrahierten Dokumenttexte und erzeugt echte PDF- sowie Standalone-HTML-Exporte. Ein zweiter Test erzeugt das Projekt neu, bearbeitet und überträgt es. Der 5.000-Ereignisse-Test prüft getrennt den SQLite-Roundtrip mit 40 Anhangsmetadaten, einen Treffer am Ende des Bestands und beide begrenzten Layoutausrichtungen.

## ADR-037: Globale semantische WPF-Ressourcen und adaptive Präsentationsschale

**Status:** angenommen am 22.07.2026

Hell- und Dunkelmodus werden ausschließlich über die semantischen Ressourcen in `Application.Resources` bereitgestellt. Fenster und Dialoge dürfen kein fest eingebundenes `Theme.Light.xaml` mehr führen, weil lokale Ressourcen den durch `ApplicationThemeService` gewechselten Anwendungssatz übersteuern. Gemeinsame Typografie- und Steuerungsstile referenzieren Farben dynamisch. Native WPF-Zustände für Fokus, Hover, Aktiv, Deaktiviert, Auswahl, Nur-Lesen und Validierungsfehler bleiben Bestandteil dieses gemeinsamen Systems. Bild-, PDF- und Druckflächen dürfen ihre fachlich erforderliche neutrale Arbeits- beziehungsweise Papierfarbe weiterhin selbst festlegen.

Die Hauptnavigation wird als textbasierte Menü- und Befehlsstruktur umgesetzt. Neue Symbolgrafiken werden erst verwendet, wenn freigegebene Assets den in `ICON_REQUIREMENTS.md` festgelegten hellen und dunklen Varianten entsprechen. Unicode-Zeichen, Emoji oder provisorische Ersatzsymbole werden nicht als neue finale Icon-Lösung eingeführt. Dadurch bleiben vollständige Funktionsbezeichnungen, Tastaturbedienbarkeit und ein konsistenter WPF-Zustandsapparat erhalten, ohne uneinheitliche Bildquellen zu erzeugen.

Die geöffnete Projektansicht erhält eine adaptive Präsentationsschale aus linker Projekt-/Filterspalte, zentralem Arbeitsbereich und rechtem Detailinspektor. Breiten, Sichtbarkeit und Splitterzustände sind reine Ansichtsbelange und dürfen im Code-behind verwaltet werden; Projekt-, Ereignis- und Anhangsänderungen bleiben im ViewModel und den vorhandenen Application-Diensten. Der Inspektor zeigt die aktuelle Auswahl und löst vorhandene Befehle aus, ersetzt aber weder das Ereignis-Bearbeitungsmodell noch die später in Phase 4 zu überarbeitenden Dialoge. Unterhalb definierter Platzschwellen werden Seitenbereiche einklappbar, ohne den zentralen Zeitstrahl oder die Startansicht fachlich zu verändern.

## ADR-038: Globale Themepräferenz und themeeigene native WPF-Popups

**Status:** angenommen am 27.07.2026

Das effektive Anwendungsfarbschema ist eine projektunabhängige Benutzerpräferenz. Sie wird versioniert und atomar unter `%LocalAppData%\Zeitstrahl Studio\appearance-settings.json` gespeichert und vor dem Erzeugen des Hauptfensters geladen. Ein Projektwechsel, eine Projekterstellung oder das Schließen eines Projekts darf diese Präferenz nicht neu anwenden oder überschreiben. Das vorhandene Feld `ProjectSettings.Theme` bleibt für Formatkompatibilität und übertragene Projektmetadaten erhalten und wird beim bestätigten Projekt-Einstellungsdialog synchronisiert; es ist beim Öffnen jedoch nicht mehr die Autorität für die laufende Oberfläche. Damit präzisiert ADR-038 den Theme-Geltungsbereich aus ADR-034.

ComboBoxen verwenden eine gemeinsame themefähige visuelle Hülle, weil die visuelle Windows-Abnahme belegt hat, dass das native WPF-Standardtemplate die geschlossene Auswahlfläche trotz überschriebener System-Brush-Schlüssel weiterhin weiß zeichnet. Daten- und Bedienvertrag bleiben unverändert: `ItemsSource`, `SelectedItem`, `SelectedValue`, `SelectedValuePath`, `DisplayMemberPath`, Tastatursteuerung und Popupzustand werden weiterhin ausschließlich vom WPF-`ComboBox`-Steuerelement verwaltet. Der geschlossene Inhalt bindet direkt an `SelectionBoxItem`, `SelectionBoxItemTemplate`, `ItemTemplateSelector` und `SelectionBoxItemStringFormat`; Popup-Einträge verwenden `ContentSource="Content"`. Damit bleiben beschriftete Auswahlobjekte echte Labels und werden weder als Typtext noch leer dargestellt. Pixelbasierte Laufzeittests prüfen die geschlossenen globalen und projektbezogenen Felder; weitere Laufzeittests prüfen Popup-Labels, Hauptfilter und Auswahlrückfluss. Kalender-, Kontextmenü-, Tabellen- und Registerflächen behalten ihre ergänzenden globalen themefähigen Stile. Bild-, PDF- und Druckvorschauen behalten ihre bereits dokumentierten fachlich neutralen Flächen. Diese Entscheidung ersetzt die zwischenzeitliche Rückkehr zum nativen Standardtemplate, nachdem auch diese Variante die visuelle Abnahme nicht bestand.

Ausgewählte Registerkarten und DatePicker verwenden ebenfalls eigene themefähige Präsentationstemplates, weil die native WPF-Darstellung die gesetzten semantischen Brushes bei ausgewählten Tab-Headern, internem DatePicker-Textfeld und Kalenderbutton nicht zuverlässig übernimmt. Die Registerauswahl bleibt beim `TabControl`; das Header-Template rendert nur Hintergrund, Rahmen und Beschriftung. Der DatePicker behält `PART_TextBox`, `PART_Button`, `PART_Popup`, `IsDropDownOpen` und `SelectedDate`; außerdem wird dem intern erzeugten Kalender der semantische `ThemedCalendarStyle` ausdrücklich übergeben. Damit ändern sich weder Registerinhalte noch Datumslogik, Tastaturbedienung oder Bindingvertrag.

Die Ereignisfarbauswahl bietet eine feste, beschriftete und tastaturbedienbare Palette mit Live-Vorschau. Ereigniseditor und Projekt-Standardfarbe verwenden dieselbe unveränderliche `EventColorPalette.Options`-Quelle und denselben Hex-Farbkonverter. Der gespeicherte fachliche Vertrag bleibt weiterhin ein validierter `#RRGGBB`-Wert, sodass freie Farben, bestehende Projekte, Suche, Filter und Exporte unverändert kompatibel bleiben. Eine neue Produktionsabhängigkeit oder ein zweites Farbformat wird nicht eingeführt.
