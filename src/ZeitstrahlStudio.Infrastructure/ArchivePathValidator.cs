namespace ZeitstrahlStudio.Infrastructure;

internal static class ArchivePathValidator
{
    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public static string ValidateAndNormalize(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath) || archivePath.Length > 4096)
        {
            throw new InvalidDataException("Das Projektarchiv enthält einen leeren oder zu langen Pfad.");
        }

        if (archivePath.Contains('\\') ||
            archivePath.StartsWith("/", StringComparison.Ordinal) ||
            Path.IsPathRooted(archivePath))
        {
            throw new InvalidDataException($"Der Archivpfad '{archivePath}' ist nicht relativ oder nicht normalisiert.");
        }

        var segments = archivePath.Split('/', StringSplitOptions.None);
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            throw new InvalidDataException($"Der Archivpfad '{archivePath}' enthält unsichere Segmente.");
        }

        foreach (var segment in segments)
        {
            if (segment.Length > 255 ||
                segment.EndsWith(' ') ||
                segment.EndsWith('.') ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                ReservedWindowsNames.Contains(Path.GetFileNameWithoutExtension(segment)))
            {
                throw new InvalidDataException($"Der Archivpfad '{archivePath}' ist unter Windows ungültig.");
            }
        }

        return string.Join('/', segments);
    }

    public static string ResolveUnderRoot(string rootDirectory, string archivePath)
    {
        var normalized = ValidateAndNormalize(archivePath);
        var root = EnsureTrailingSeparator(Path.GetFullPath(rootDirectory));
        var localRelativePath = normalized.Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(root, localRelativePath));
        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Der Archivpfad '{archivePath}' verlässt den Projektordner.");
        }

        return candidate;
    }

    public static bool IsUnderRoot(string rootDirectory, string candidatePath)
    {
        var root = EnsureTrailingSeparator(Path.GetFullPath(rootDirectory));
        var candidate = Path.GetFullPath(candidatePath);
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
