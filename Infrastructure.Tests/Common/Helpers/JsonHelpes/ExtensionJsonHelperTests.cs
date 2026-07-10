using Infrastructure.Common.Helpers.JsonHelpes;

namespace Infrastructure.Tests.Common.Helpers.JsonHelpes;

public class ExtensionJsonHelperTests
{
    [Fact]
    public void RedactSecurity_WhenSecurityPropertyPresent_ReplacesItWithPlaceholder()
    {
        const string json = """{"data":{"pClaimId":"c1"},"security":{"pLogin":"login","pPassword":"s3cr3t"}}""";

        var redacted = json.RedactSecurity();

        Assert.DoesNotContain("s3cr3t", redacted);
        Assert.DoesNotContain("pPassword", redacted);
        Assert.Contains("\"security\":\"REDACTED\"", redacted);
        Assert.Contains("\"pClaimId\":\"c1\"", redacted);
    }

    [Fact]
    public void RedactSecurity_WhenSecurityPropertyAbsent_LeavesJsonUnchanged()
    {
        const string json = """{"data":{"pClaimId":"c1"}}""";

        var redacted = json.RedactSecurity();

        Assert.Equal(json, redacted);
    }

    [Fact]
    public void RedactSecurity_WhenJsonIsInvalid_ReturnsOriginalStringUnchanged()
    {
        const string notJson = "<html>not json</html>";

        var redacted = notJson.RedactSecurity();

        Assert.Equal(notJson, redacted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RedactSecurity_WhenInputIsNullOrWhitespace_ReturnsInputAsIs(string? input)
    {
        var redacted = input.RedactSecurity();

        Assert.Equal(input ?? string.Empty, redacted);
    }
}
