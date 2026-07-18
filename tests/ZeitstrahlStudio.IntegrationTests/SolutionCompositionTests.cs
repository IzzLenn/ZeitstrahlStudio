using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Export;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class SolutionCompositionTests
{
    [Fact]
    public void ArchitecturalAssemblies_AreIndependentAndLoadable()
    {
        var assemblies = new[]
        {
            typeof(IProjectRepository).Assembly,
            DocumentProcessingAssembly.MarkerType.Assembly,
            ExportAssembly.MarkerType.Assembly,
            InfrastructureAssembly.MarkerType.Assembly,
        };

        Assert.Equal(assemblies.Length, assemblies.Distinct().Count());
        Assert.All(assemblies, assembly => Assert.False(string.IsNullOrWhiteSpace(assembly.FullName)));
    }
}
