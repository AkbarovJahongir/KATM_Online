namespace Application.Repositories.CreditBureauReportRepositories;

public class CreditReportQueueItem
{
    public int LoanKey { get; set; }
    public string? PClaimId { get; set; }
    public string? PReportId { get; set; }
    public string? PLoanSubject { get; set; }
    public string? PLoanSubjectType { get; set; }
    public string? PPin { get; set; }
    public string? PTin { get; set; }
    public int PReportFormat { get; set; }
    public int? PReportReason { get; set; }
    public string? PToken { get; set; } // KATM-SIR или polling-токен
}
