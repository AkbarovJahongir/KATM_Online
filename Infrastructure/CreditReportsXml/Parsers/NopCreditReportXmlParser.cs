namespace Infrastructure.CreditReportsXml.Parsers;

public sealed class NopCreditReportXmlParser : ICreditReportXmlParser
{
    public Task ParseAndPersistAsync(string KeyLoanHistoryKb, string reportBase64, CancellationToken cancellationToken) => Task.CompletedTask;
}
