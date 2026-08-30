# Drittanbieterkomponenten und Lizenzstatus

Stand: 30.08.2026

Dieses Dokument trennt Paket-/Lizenzmetadaten vom tatsächlich gebündelten Bestand an Originaltexten. Es ist eine technische Inventur und keine Rechtsberatung oder pauschale Zusicherung der Lizenzkonformität. Vor jeder Distribution ist eine eigenständige rechtliche und technische Prüfung des finalen Publish erforderlich.

## Direkte Produktionspakete

Die folgenden `PackageReference`-Einträge stehen direkt in den Produktions-`.csproj`-Dateien:

| Paket | Version | Projekt / Zweck | Lizenz laut geprüften Paketmetadaten |
| --- | --- | --- | --- |
| `Microsoft.Data.Sqlite` | 8.0.29 | Infrastructure: lokaler SQLite-Zugriff | MIT |
| `Microsoft.Extensions.DependencyInjection` | 8.0.1 | App: Composition Root und Lebenszyklen | MIT |
| `PdfPig` | 0.1.15 | DocumentProcessing: lokale PDF-Textextraktion | Apache-2.0 |
| `PDFtoImage` | 5.2.1 | DocumentProcessing: lokale PDF-Seitenvorschau/OCR-Rendering über PDFium | MIT |
| `SkiaSharp` | 3.119.2 | Export: PDF-Rendering und Bild-/Thumbnailverarbeitung | MIT |
| `SkiaSharp.NativeAssets.Win32` | 3.119.2 | Export: native Windows-x64-Skia-Bibliothek | MIT |

Direkte Referenz bedeutet nicht, dass nur diese Dateien ausgeliefert werden. NuGet löst transitive Abhängigkeiten und Runtimeassets auf; maßgeblich ist der final wiederhergestellte und veröffentlichte `win-x64`-Graph.

## Relevante transitive und plattformspezifische Komponenten

Die bestehende lokale Restore-/Publish-Prüfung hat insbesondere folgende Komponenten ergeben:

| Komponente | Version | Einordnung für `win-x64` | Lizenz laut Paketmetadaten |
| --- | --- | --- | --- |
| `Microsoft.Data.Sqlite.Core` | 8.0.29 | transitiver verwalteter SQLite-Kern | MIT |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0.2 | transitive DI-Abstraktionen | MIT |
| `SQLitePCLRaw.bundle_e_sqlite3` | 2.1.6 | transitives SQLite-Bundle | Apache-2.0 |
| `SQLitePCLRaw.core` | 2.1.6 | transitive verwaltete Bindings | Apache-2.0 |
| `SQLitePCLRaw.provider.e_sqlite3` | 2.1.6 | transitiver SQLite-Provider | Apache-2.0 |
| `SQLitePCLRaw.lib.e_sqlite3` | 2.1.6 | native SQLite-Bibliothek im Windows-Publish | Apache-2.0 |
| `System.Memory` | 4.5.3 | transitive Speicherabstraktion | MIT |
| `bblanchon.PDFium.Win32` | 147.0.7690 | native PDFium-Runtime für Windows | Apache-2.0 |
| `Microsoft.Windows.SDK.NET` | 10.0.19041.56 | .NET-Projektion lokaler Windows-OCR-/Bild-APIs | Microsoft Windows SDK License |
| `WinRT.Runtime` | 2.2.0.48161 | Windows-Runtimeprojektion | zugehörige Paket-/SDK-Lizenz |
| `SkiaSharp.NativeAssets.Win32` | 3.119.2 | direkt referenziertes natives Windowsasset | MIT |

Im Restoregraph können zusätzlich Runtimepakete für andere Plattformen erscheinen:

- `bblanchon.PDFium.Linux` und `bblanchon.PDFium.macOS` 147.0.7690
- `SkiaSharp.NativeAssets.Linux.NoDependencies` und `SkiaSharp.NativeAssets.macOS` 3.119.2

Diese Pakete gehören zum aufgelösten Graphen, werden im geprüften self-contained `win-x64`-Publish aber nicht als Linux-/macOS-Runtime ausgeliefert. Ihre Metadaten bleiben für Restore- und Abhängigkeitsprüfung relevant.

Diese Tabelle ist eine Auswahl relevanter transitiver Komponenten, kein Ersatz für den vollständigen maschinenlesbaren Assets-/Publishvergleich. Jede neue Restore-Auflösung kann den Graphen ändern.

## Framework, Build und Tests

Die Anwendung verwendet .NET 8.x und WPF. Das Repository enthält kein `global.json`; deshalb ist die tatsächlich ausgewählte SDK-Version für jeden Build mit `dotnet --info` zu protokollieren, statt eine einzelne Patchversion dauerhaft als Projektstandard festzuschreiben. Die self-contained Veröffentlichung bringt Framework- und Runtimebestandteile mit, deren Notices im finalen Publish ebenfalls geprüft werden müssen.

Direkte Testpakete in beiden Testprojekten:

| Paket | Version | Verwendung | Lizenz laut Paketmetadaten |
| --- | --- | --- | --- |
| `Microsoft.NET.Test.Sdk` | 17.8.0 | Testhost | MIT |
| `xunit` | 2.5.3 | Testframework | Apache-2.0 |
| `xunit.runner.visualstudio` | 2.5.3 | Testadapter | Apache-2.0 |
| `coverlet.collector` | 6.0.0 | optionale Codeabdeckung | MIT |

Diese Testpakete sind Entwicklungsabhängigkeiten und gehören nicht absichtlich in den Endnutzer-Publish. Das finale Artefakt ist dennoch auf versehentlich mitgelieferte Testdateien zu prüfen.

## Tatsächlich vorhandene Originaltexte

Der Repository-Ordner `licenses/` enthält am 29.08.2026 exakt drei Dateien:

```text
licenses/
├── MIT-Microsoft.Extensions.DependencyInjection.txt
├── MIT-SkiaSharp.txt
└── MIT-System.Memory.txt
```

Das ist kein vollständiges Lizenz-/Notice-Bundle für alle ausgelieferten Produktionskomponenten. Insbesondere für die übrigen direkten und transitiven Runtimebestandteile fehlen lokal noch erforderliche Original-Lizenz-, Copyright- oder Notice-Texte beziehungsweise deren dokumentierte rechtliche Bewertung.

**Folge:** Eine Distribution ist aus Dokumentationssicht blockiert, bis das finale Paket inventarisiert, die jeweiligen Verpflichtungen rechtlich geprüft und alle erforderlichen Texte/Notices vollständig sowie korrekt zugeordnet gebündelt sind. Eine Lizenzbezeichnung in der Tabelle ersetzt nicht den Originaltext.

## Projekt- und Samplelizenzen

- [`LICENSE.txt`](LICENSE.txt) ist die MIT-Lizenz des Projektquellcodes.
- [`samples/LICENSE.txt`](samples/LICENSE.txt) ist ein separater Lizenz-/Nutzungshinweis für die frei erfundenen Beispieldaten und darf nicht mit der Projektlizenz gleichgesetzt werden.
- Drittanbietertexte liegen im separaten Ordner `licenses/`.

Alle drei Ebenen müssen für das jeweilige Artefakt bewusst geprüft werden.

## Paketierungs-Iststand

`build.ps1 -Task PackagePortable` kopiert:

- `THIRD_PARTY_LICENSES.md` sowie README, Datenschutz, Changelog und Root-`LICENSE.txt`
- den vollständigen aktuellen Ordner `licenses/`
- den vollständigen aktuellen Ordner `samples/` einschließlich dessen `LICENSE.txt`

Das lokale 1.0.0-ZIP enthielt am 29.08.2026 keine separate Projekt-`LICENSE.txt`. Für 1.1.0 kopiert `PackagePortable` die Root-Datei ausdrücklich in den Publish-Baum und damit in die Portable-ZIP. Der geschlossene ZIP-Inhalt ist vor Veröffentlichung weiterhin zu prüfen.

Das Inno-Setup-Skript nimmt die Root-`LICENSE.txt` für 1.1.0 ohne bedingten Installations-Check als Installationsquelle auf. Bei der vollständigen `All`-Kette übernimmt der Installer außerdem die zuvor in den Publish kopierten `licenses/`- und `samples/`-Inhalte. Der geschlossene Installer und eine reale Installation müssen deshalb zwingend auf alle erwarteten Texte geprüft werden.

## Releaseprüfung

Vor jeder Freigabe:

1. frischen Restore mit dem vorgesehenen .NET-8-SDK ausführen und SDK protokollieren;
2. direkte und transitive Paketversionen aus `project.assets.json` beziehungsweise `dotnet list ... package --include-transitive` inventarisieren;
3. den tatsächlichen self-contained `win-x64`-Publish nach verwalteten und nativen Komponenten durchsuchen;
4. Paketmetadaten und Originalquellen der Lizenzen/Notices prüfen;
5. vollständige, unveränderte und korrekt benannte Originaltexte ins Bundle aufnehmen;
6. portable ZIP und Installer nach dem Schließen erneut inventarisieren;
7. Abweichungen als Releaseblocker dokumentieren.

Build und Paketierung: [`BUILD.md`](BUILD.md). Freigabeprozess: [`RELEASE.md`](RELEASE.md). Manuelle Prüfung: [`MANUAL_RELEASE_CHECKLIST.md`](MANUAL_RELEASE_CHECKLIST.md).
