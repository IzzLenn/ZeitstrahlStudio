using Xunit;

// Mehrere Integrationsklassen räumen prozessweite SQLite-Verbindungspools auf.
// Serielle Ausführung verhindert, dass eine Klassenbereinigung gleichzeitig in
// den Datei-/Datenbanklebenszyklus einer anderen Klasse eingreift.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
