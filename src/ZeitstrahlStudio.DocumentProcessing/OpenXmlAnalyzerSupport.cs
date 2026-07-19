using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.DocumentProcessing;

internal static partial class OpenXmlAnalyzerSupport
{
    public const long MaximumEntryLength = 256L * 1024 * 1024;
    public const long MaximumTotalLength = 512L * 1024 * 1024;
    public const int MaximumEntries = 10_000;
    public const int MaximumExtractedCharacters = 10_000_000;

    public static FileStream OpenSource(string localFilePath)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            throw new ArgumentException("Der Dokumentpfad darf nicht leer sein.", nameof(localFilePath));
        }

        return new FileStream(
            Path.GetFullPath(localFilePath),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public static void ValidateArchive(ZipArchive archive)
    {
        if (archive.Entries.Count > MaximumEntries)
        {
            throw new InvalidDataException("Das Office-Dokument enthält zu viele ZIP-Einträge.");
        }

        long totalLength = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.Length < 0 || entry.Length > MaximumEntryLength)
            {
                throw new InvalidDataException("Ein Eintrag des Office-Dokuments ist zu groß.");
            }

            totalLength = checked(totalLength + entry.Length);
            if (totalLength > MaximumTotalLength)
            {
                throw new InvalidDataException("Das entpackte Office-Dokument überschreitet das Größenlimit.");
            }

            if (entry.Length > 1024 * 1024 &&
                entry.CompressedLength > 0 &&
                entry.Length / (double)entry.CompressedLength > 1000)
            {
                throw new InvalidDataException("Das Office-Dokument besitzt ein unsicheres Kompressionsverhältnis.");
            }
        }
    }

    public static XmlReader CreateReader(Stream stream) => XmlReader.Create(
        stream,
        new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            MaxCharactersInDocument = MaximumExtractedCharacters * 4L,
        });

    public static void AppendLimited(StringBuilder builder, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (builder.Length + value.Length > MaximumExtractedCharacters)
        {
            throw new InvalidDataException("Der extrahierte Dokumenttext überschreitet das Sicherheitslimit.");
        }

        builder.Append(value);
    }

    public static IReadOnlyList<string> ExtractDateSuggestions(string text)
    {
        var matches = DatePattern().Matches(text);
        return matches
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToArray();
    }

    public static OperationResult<T> Failure<T>(string fileName, Exception exception) =>
        OperationResult<T>.Failure(new ApplicationError(
            "DocumentAnalysisFailed",
            $"Das Dokument „{fileName}“ konnte nicht lokal analysiert werden.",
            exception.Message));

    public static async Task<IReadOnlyDictionary<string, string>> ReadCorePropertiesAsync(
        ZipArchive archive,
        CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry("docProps/core.xml");
        if (entry is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var supportedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "title", "subject", "creator", "keywords", "description", "lastModifiedBy", "created", "modified",
        };
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var stream = entry.Open();
        using var reader = CreateReader(stream);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType != XmlNodeType.Element ||
                !supportedNames.Contains(reader.LocalName))
            {
                continue;
            }

            var name = reader.LocalName;
            var value = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                metadata[name] = value.Trim();
            }
        }

        return metadata;
    }

    public static string? GetTitle(
        IReadOnlyDictionary<string, string> metadata,
        string localFilePath) =>
        metadata.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)
            ? title
            : Path.GetFileNameWithoutExtension(localFilePath);

    [GeneratedRegex(
        @"\b(?:\d{1,2}\.\d{1,2}\.\d{2,4}|\d{4}-\d{2}-\d{2})\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();
}
