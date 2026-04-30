using CreditBureauService.Contracts.CreditReportParser;

namespace Application.Repositories.CreditReportXmlParserRepositories;

public interface ICreditReportXmlParserRepository
{
    Task ExternalInfo_Save(string App, string PayloadJson, string ReportName, CancellationToken cancellationToken);
    Task<string> GetAppByKeyLoanHistoryKb(string keyLoanHistoryKb, CancellationToken cancellationToken);
    Task InsertReport025(List<Report025Data> report025s);
}
