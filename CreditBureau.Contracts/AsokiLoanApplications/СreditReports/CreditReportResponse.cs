namespace CreditBureau.Contracts.AsokiLoanApplications.СreditReports
{
    public class CreditReportResponse
    {
        public string? result { get; set; }
        public string? resultMessage { get; set; }
        public string? reportBase64 { get; set; }
        public string? token { get; set; }
    }
}
