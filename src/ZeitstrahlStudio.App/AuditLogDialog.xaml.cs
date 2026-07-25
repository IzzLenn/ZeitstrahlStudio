using System.Windows;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Reine Anzeige des lokalen projektbezogenen Änderungsprotokolls.</summary>
public partial class AuditLogDialog : Window
{
    public AuditLogDialog(IReadOnlyList<AuditEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        InitializeComponent();
        DataContext = new AuditLogDialogModel(entries);
    }

    private sealed record AuditLogDialogModel(IReadOnlyList<AuditEntry> Entries)
    {
        public bool HasEntries => Entries.Count > 0;
    }
}
