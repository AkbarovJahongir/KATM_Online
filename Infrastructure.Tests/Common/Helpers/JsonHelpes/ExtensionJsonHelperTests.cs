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

    [Fact]
    public void RemoveSecurity_WhenSecurityPresent_RemovesItAndKeepsDateAndDecimalFormats()
    {
        const string json = """
            {"data":{"pDate":"2026-06-17T09:18:37.409+0500","claim_date":"03.06.2026 10:05:00","summa":1500000.50,"pReportId":21},
             "security":{"pLogin":"login","pPassword":"s3cr3t"}}
            """;

        var body = json.RemoveSecurity();

        Assert.DoesNotContain("security", body);
        Assert.DoesNotContain("s3cr3t", body);
        Assert.Contains("\"pDate\":\"2026-06-17T09:18:37.409+0500\"", body);
        Assert.Contains("\"claim_date\":\"03.06.2026 10:05:00\"", body);
        Assert.Contains("\"summa\":1500000.50", body);
        Assert.Contains("\"pReportId\":21", body);
    }

    [Fact]
    public void RemoveSecurity_WhenSecurityAbsent_ReturnsSameString()
    {
        const string json = "{\n  \"data\": {\"pClaimId\": \"c1\"}\n}";

        Assert.Same(json, json.RemoveSecurity());
    }
}
