# Beispielprojekt

`ZeitstrahlStudio-Beispiel.zeitprojekt` ist ein vollständig lokales, frei
weitergebbares Demonstrationsprojekt für Zeitstrahl Studio. Es enthält zehn
frei erfundene Ereignisse von 1954 bis 2038, alle unterstützten
Datumsgenauigkeiten, mehrere Ereignisse am selben Datum, vier Fristzustände,
unterschiedliche Farben und Prioritäten, Schlagwörter, einen Link auf die
reservierte Domain `example.com`, manuelle Layoutpositionen und eine deutlich
sichtbare große Zeitlücke.

Im Archiv liegen vollständige Projektkopien der fünf programmatisch erzeugten
Dokumente aus `test-documents/`:

- zwei textbasierte PDF-Dateien,
- eine PNG-Planungstafel,
- eine DOCX-Werkstattnotiz,
- eine XLSX-Meilensteintabelle.

PDF-, DOCX- und XLSX-Inhalte sind im Projekt bereits lokal analysiert und
durchsuchbar. Die Begriffe `Kupferstern`, `Morgenfalter` und `Blattgold` dienen
als eindeutige Suchprüfungen. Für das Bild ist eine kleine lokale
Zeitstrahlminiatur enthalten. Das Projekt enthält keine realen Namen,
vertraulichen Angaben oder übernommenen Fremdinhalte.

## Öffnen

Das Archiv kann in der Anwendung über **Projekt öffnen** gewählt oder direkt
als Kommandozeilenargument an `ZeitstrahlStudio.App.exe` übergeben werden. Die
separaten Dateien in `test-documents/` sind nicht zum Öffnen des Archivs nötig;
sie bleiben nur als nachvollziehbare, einzeln prüfbare Ausgangsdokumente im
Repository.

## Reproduzierbare Erzeugung

Vom Repository-Stamm:

```powershell
dotnet run --project tools/ZeitstrahlStudio.SampleGenerator/ZeitstrahlStudio.SampleGenerator.csproj -c Release -- --output samples
```

Der Generator verwendet dieselben lokalen Dienste wie die Anwendung:
SQLite-Repository, sicheren Anhangsimport, Dokumentanalyse,
Thumbnail-Erzeugung und validierten Projektarchivexport. Nach der Erzeugung
importiert und prüft er das Archiv erneut. Maschinenspezifische
Ursprungsdateipfade werden nicht in das Beispiel übernommen.

## Lizenz

Alle Dateien in diesem Ordner werden vollständig durch den Generator erzeugt
und unter der beigefügten MIT-Lizenz bereitgestellt. Es werden keine
urheberrechtlich geschützten Fremdtexte, Bilder, Schriften oder Vorlagen
eingebettet.
