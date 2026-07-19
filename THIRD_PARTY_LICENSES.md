# Drittanbieterkomponenten und Lizenzen

Stand: 19. Juli 2026. Produktionsabhängigkeiten werden nur eingeführt, wenn sie für einen umgesetzten Meilenstein technisch erforderlich sind.

## Produktionskomponenten

| Komponente | Version | Einsatz | Lizenz |
| --- | --- | --- | --- |
| Microsoft.Data.Sqlite / Microsoft.Data.Sqlite.Core | 8.0.29 | lokaler ADO.NET-SQLite-Zugriff | MIT |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | Composition Root und Lebenszyklusverwaltung der WPF-Anwendung | MIT |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | DI-Schnittstellen und Registrierungsabstraktionen | MIT |
| PdfPig | 0.1.15 | vollständig lokale PDF-Textextraktion und Metadatenanalyse | Apache-2.0 |
| PDFtoImage | 5.2.1 | vollständig lokale PDF-Seitenvorschau über PDFium | MIT |
| bblanchon.PDFium.Win32 | 147.0.7690 | native PDFium-Bibliothek für die Windows-x64-Vorschau | Apache-2.0 |
| bblanchon.PDFium.Linux / bblanchon.PDFium.macOS | 147.0.7690 | transitive, bei `win-x64` nicht veröffentlichte PDFium-Runtimepakete | Apache-2.0 |
| SkiaSharp | 3.119.2 | begrenzte Bitmap- und PNG-Verarbeitung der PDF-Vorschau | MIT |
| SkiaSharp.NativeAssets.Win32 | 3.119.2 | native x64-Skia-Bibliothek für Windows | MIT |
| SkiaSharp.NativeAssets.Linux.NoDependencies / SkiaSharp.NativeAssets.macOS | 3.119.2 | transitive, bei `win-x64` nicht veröffentlichte Skia-Runtimepakete | MIT |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.6 | Bündelung der lokalen nativen SQLite-Bibliothek | Apache-2.0 |
| SQLitePCLRaw.core | 2.1.6 | verwaltete SQLite-Bindings | Apache-2.0 |
| SQLitePCLRaw.provider.e_sqlite3 | 2.1.6 | Provider für die gebündelte SQLite-Bibliothek | Apache-2.0 |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.6 | native x64-SQLite-Binärdatei | Apache-2.0 |
| System.Memory | 4.5.3 | transitive Speicherabstraktionen für den lokalen SQLite-Zugriff | MIT |

## Aktuell verwendete Build- und Testkomponenten

| Komponente | Version | Einsatz | Lizenz |
| --- | --- | --- | --- |
| .NET 8 / WPF | SDK 8.0.423 | Laufzeit, Compiler und Desktop-Framework | MIT; einzelne Bestandteile gemäß zugehörigen Notices |
| Microsoft.NET.Test.Sdk | 17.8.0 | Testhost, nur Entwicklung/Test | MIT |
| xunit | 2.5.3 | Unit- und Integrationstests | Apache-2.0 |
| xunit.runner.visualstudio | 2.5.3 | Testadapter, nur Entwicklung/Test | Apache-2.0 |
| coverlet.collector | 6.0.0 | optionale Codeabdeckung, nur Entwicklung/Test | MIT |

Die Paketnamen, Versionen und SPDX-Lizenzangaben wurden aus den lokal wiederhergestellten NuGet-Paketmetadaten übernommen. Lizenztexte der ausgelieferten Produktionskomponenten werden vor dem Release in das Veröffentlichungsverzeichnis kopiert.
