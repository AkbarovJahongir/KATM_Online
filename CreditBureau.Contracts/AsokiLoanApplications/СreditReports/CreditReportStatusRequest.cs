namespace CreditBureau.Contracts.AsokiLoanApplications.СreditReports
{
    public class CreditReportStatusRequest
    {
        public string pClaimId { get; set; } = null!;
        public string pCode { get; set; } = null!;
        public string pHead { get; set; } = null!;
        public int pReportFormat { get; set; } = 0;
        public string pToken { get; set; } = null!;
    }
}
