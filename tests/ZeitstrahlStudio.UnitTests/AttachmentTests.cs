using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.UnitTests;

public sealed class AttachmentTests
{
    private static readonly DateTimeOffset ImportedAt = new(2026, 7, 19, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_NormalizesSafeRelativePath()
    {
        var attachment = CreateAttachment("attachments\\abc\\beispiel.pdf");

        Assert.Equal("attachments/abc/beispiel.pdf", attachment.ProjectRelativePath);
    }

    [Theory]
    [InlineData("C:\\Daten\\beispiel.pdf")]
    [InlineData("attachments/../beispiel.pdf")]
    [InlineData("../beispiel.pdf")]
    public void Constructor_RejectsUnsafeProjectPath(string projectPath)
    {
        Assert.Throws<DomainValidationException>(() => CreateAttachment(projectPath));
    }

    [Fact]
    public void Constructor_RejectsInvalidChecksum()
    {
        Assert.Throws<DomainValidationException>(() => new Attachment(
            Guid.NewGuid(),
            "beispiel.pdf",
            "application/pdf",
            42,
            "nicht-gueltig",
            null,
            ImportedAt,
            "attachments/beispiel.pdf"));
    }

    private static Attachment CreateAttachment(string projectPath) => new(
        Guid.NewGuid(),
        "beispiel.pdf",
        "application/pdf",
        42,
        new string('a', 64),
        "C:\\Quelle\\beispiel.pdf",
        ImportedAt,
        projectPath);
}
