# Drittanbieterkomponenten und Lizenzen

Stand: 19. Juli 2026. Produktionsabhängigkeiten werden nur eingeführt, wenn sie für einen umgesetzten Meilenstein technisch erforderlich sind.

## Produktionskomponenten

| Komponente | Version | Einsatz | Lizenz |
| --- | --- | --- | --- |
| Microsoft.Data.Sqlite / Microsoft.Data.Sqlite.Core | 8.0.29 | lokaler ADO.NET-SQLite-Zugriff | MIT |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.6 | Bündelung der lokalen nativen SQLite-Bibliothek | Apache-2.0 |
| SQLitePCLRaw.core | 2.1.6 | verwaltete SQLite-Bindings | Apache-2.0 |
| SQLitePCLRaw.provider.e_sqlite3 | 2.1.6 | Provider für die gebündelte SQLite-Bibliothek | Apache-2.0 |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.6 | native x64-SQLite-Binärdatei | Apache-2.0 |

## Aktuell verwendete Build- und Testkomponenten

| Komponente | Version | Einsatz | Lizenz |
| --- | --- | --- | --- |
| .NET 8 / WPF | SDK 8.0.423 | Laufzeit, Compiler und Desktop-Framework | MIT; einzelne Bestandteile gemäß zugehörigen Notices |
| Microsoft.NET.Test.Sdk | 17.8.0 | Testhost, nur Entwicklung/Test | MIT |
| xunit | 2.5.3 | Unit- und Integrationstests | Apache-2.0 |
| xunit.runner.visualstudio | 2.5.3 | Testadapter, nur Entwicklung/Test | Apache-2.0 |
| coverlet.collector | 6.0.0 | optionale Codeabdeckung, nur Entwicklung/Test | MIT |

Die Paketnamen, Versionen und SPDX-Lizenzangaben wurden aus den lokal wiederhergestellten NuGet-Paketmetadaten übernommen. Lizenztexte der ausgelieferten Produktionskomponenten werden vor dem Release in das Veröffentlichungsverzeichnis kopiert.
