using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Export;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.App;

/// <summary>Composition Root und Lebenszyklus der WPF-Anwendung.</summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? serviceProvider;
    private MainWindowViewModel? mainViewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            serviceProvider = ConfigureServices().BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
            await serviceProvider.GetRequiredService<IApplicationThemeService>()
                .InitializeAsync(CancellationToken.None).ConfigureAwait(true);
            mainViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
            var window = serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = window;
            DispatcherUnhandledException += HandleDispatcherUnhandledException;
            window.Show();
            await mainViewModel.InitializeAsync().ConfigureAwait(true);

            var projectArgument = e.Args.FirstOrDefault(argument =>
                string.Equals(Path.GetExtension(argument), ".zeitprojekt", StringComparison.OrdinalIgnoreCase));
            if (projectArgument is not null)
            {
                await mainViewModel.OpenPathAsync(projectArgument).ConfigureAwait(true);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Zeitstrahl Studio konnte nicht gestartet werden.\n\n{exception.Message}",
                "Startfehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= HandleDispatcherUnhandledException;
        if (mainViewModel is not null)
        {
            mainViewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static ServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<SqliteSchemaMigrator>();
        services.AddSingleton<IProjectRepository, SqliteProjectRepository>();
        services.AddSingleton<IProjectArchiveService>(provider =>
            new ProjectArchiveService(provider.GetRequiredService<IProjectRepository>()));
        services.AddSingleton(provider => new LocalProjectWorkspaceService(
            provider.GetRequiredService<IProjectRepository>(),
            provider.GetRequiredService<IProjectArchiveService>()));
        services.AddSingleton<IProjectWorkspaceService>(provider =>
            provider.GetRequiredService<LocalProjectWorkspaceService>());
        services.AddSingleton<IProjectRecoveryService>(provider =>
            provider.GetRequiredService<LocalProjectWorkspaceService>());
        services.AddSingleton<IRecentProjectsService>(_ => new JsonRecentProjectsService());
        services.AddSingleton<ILocalLogService>(_ => new JsonLinesLocalLogService());
        services.AddSingleton<IAuditLogService, SqliteAuditLogService>();
        services.AddSingleton<BackupRetentionPolicy>();
        services.AddSingleton<IBackupService>(provider => new LocalBackupService(
            provider.GetRequiredService<IProjectWorkspaceService>(),
            provider.GetRequiredService<IAuditLogService>(),
            provider.GetRequiredService<BackupRetentionPolicy>(),
            provider.GetRequiredService<ILocalLogService>()));
        services.AddSingleton<IProjectSearchService, SqliteProjectSearchService>();
        services.AddSingleton<IAttachmentImportService, LocalAttachmentImportService>();
        services.AddSingleton<IAttachmentFileService, LocalAttachmentFileService>();
        services.AddSingleton<IAttachmentAnalysisStore, SqliteAttachmentAnalysisStore>();
        services.AddSingleton<IPdfPreviewService, PdfiumPdfPreviewService>();
        services.AddSingleton<ITimelineThumbnailService, SkiaTimelineThumbnailService>();
        services.AddSingleton<PdfExportPlanner>();
        services.AddSingleton<IPdfExportService, SkiaPdfExportService>();
        services.AddSingleton<IHtmlExportService, StandaloneHtmlExportService>();
        services.AddSingleton<ILocalOcrService, WindowsLocalOcrService>();
        services.AddSingleton<IDocumentAnalyzer, DocxDocumentAnalyzer>();
        services.AddSingleton<IDocumentAnalyzer, XlsxDocumentAnalyzer>();
        services.AddSingleton<IDocumentAnalyzer, PdfDocumentAnalyzer>();
        services.AddSingleton<IDocumentAnalyzer, ImageDocumentAnalyzer>();
        services.AddSingleton<IAttachmentAnalysisQueue>(provider =>
            new BoundedAttachmentAnalysisQueue(
                provider.GetServices<IDocumentAnalyzer>(),
                provider.GetRequiredService<IAttachmentAnalysisStore>(),
                maximumConcurrency: 2));
        services.AddSingleton<IProjectAutosaveService>(provider => new ProjectAutosaveService(
            provider.GetRequiredService<IProjectWorkspaceService>(),
            provider.GetRequiredService<IBackupService>()));
        services.AddSingleton<ProjectEventEditingService>();
        services.AddSingleton<IApplicationThemeService>(_ => new ApplicationThemeService());
        services.AddSingleton<IUserDialogService, WpfUserDialogService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        return services;
    }

    private async void HandleDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (serviceProvider?.GetService<ILocalLogService>() is { } logService)
        {
            try
            {
                await logService.WriteAsync(
                    new LocalLogEntry(
                        DateTimeOffset.UtcNow,
                        LocalLogLevel.Error,
                        nameof(App),
                        "DispatcherUnhandledException",
                        "Ein unerwarteter Oberflächenfehler wurde abgefangen.",
                        e.Exception.ToString()),
                    CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception logException) when (logException is IOException or UnauthorizedAccessException)
            {
            }
        }

        serviceProvider?.GetService<IUserDialogService>()?.ShowError(
            "Ein unerwarteter Fehler ist aufgetreten. Der Fehler wurde ausschließlich lokal protokolliert.",
            e.Exception.Message);
    }
}
