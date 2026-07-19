using System.Text.Json;
using ZeitstrahlStudio.Export;

namespace ZeitstrahlStudio.UnitTests;

public sealed class StandaloneHtmlDataEncoderTests
{
    [Fact]
    public void Serialize_PreventsScriptTerminationAndPreservesRoundTripValue()
    {
        const string hostileValue = "</script><img src=x onerror=alert('x')> & ÄÖÜ";

        var json = StandaloneHtmlDataEncoder.Serialize(new { DisplayText = hostileValue });

        Assert.DoesNotContain("</script", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\u003C/script\\u003E", json, StringComparison.Ordinal);
        Assert.Contains("\\u0026", json, StringComparison.Ordinal);
        using var parsed = JsonDocument.Parse(json);
        Assert.Equal(hostileValue, parsed.RootElement.GetProperty("displayText").GetString());
    }
}
