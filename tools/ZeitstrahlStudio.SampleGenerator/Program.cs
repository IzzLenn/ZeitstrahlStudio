namespace ZeitstrahlStudio.SampleGenerator;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            var outputDirectory = ParseOutputDirectory(args);
            var result = await new SampleProjectGenerator()
                .GenerateAsync(outputDirectory, cancellation.Token)
                .ConfigureAwait(false);
            Console.WriteLine($"Beispielprojekt: {result.ArchivePath}");
            Console.WriteLine($"Ereignisse: {result.EventCount}");
            Console.WriteLine($"Testdokumente: {result.DocumentPaths.Count}");
            return 0;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Console.Error.WriteLine("Die Erzeugung der Beispieldaten wurde abgebrochen.");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Die Beispieldaten konnten nicht erzeugt werden: {exception.Message}");
            return 1;
        }
    }

    private static string ParseOutputDirectory(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return Path.Combine(FindRepositoryRoot(), "samples");
        }

        if (args.Count == 2 && string.Equals(args[0], "--output", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(args[1]);
        }

        throw new ArgumentException(
            "Aufruf: ZeitstrahlStudio.SampleGenerator [--output <Zielordner>]");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ZeitstrahlStudio.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Der Repository-Stamm mit ZeitstrahlStudio.sln wurde nicht gefunden. " +
            "Bitte --output explizit angeben.");
    }
}
