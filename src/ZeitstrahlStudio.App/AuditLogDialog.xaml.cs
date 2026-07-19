using System.Windows;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Reine Anzeige des lokalen projektbezogenen Änderungsprotokolls.</summary>
public partial class AuditLogDialog : Window
{
    public AuditLogDialog(IReadOnlyList<AuditEntry> entries)
    {
        InitializeComponent();
        DataContext = entries ?? throw new ArgumentNullException(nameof(entries));
    }
}
