# Drittanbieterkomponenten und Lizenzen

Stand: 19. Juli 2026. Produktionsabhängigkeiten werden erst eingeführt, wenn sie für einen umgesetzten Meilenstein technisch erforderlich sind.

## Aktuell verwendete Build- und Testkomponenten

| Komponente | Version | Einsatz | Lizenz |
| --- | --- | --- | --- |
| .NET 8 / WPF | SDK 8.0.423 | Laufzeit, Compiler und Desktop-Framework | MIT; einzelne Bestandteile gemäß zugehörigen Notices |
| Microsoft.NET.Test.Sdk | 17.8.0 | Testhost, nur Entwicklung/Test | MIT |
| xunit | 2.5.3 | Unit- und Integrationstests | Apache-2.0 |
| xunit.runner.visualstudio | 2.5.3 | Testadapter, nur Entwicklung/Test | Apache-2.0 |
| coverlet.collector | 6.0.0 | optionale Codeabdeckung, nur Entwicklung/Test | MIT |

Die Paketmetadaten wurden aus den lokal wiederhergestellten NuGet-Paketen übernommen. Lizenztexte der ausgelieferten Produktionskomponenten werden vor dem Release in das Veröffentlichungsverzeichnis kopiert und in dieser Übersicht ergänzt.
