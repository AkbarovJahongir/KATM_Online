namespace CreditBureau.Contracts.AsokiLoanApplications.СreditReports
{
    public class CreditReportData
    {
        public string KeyLoanHistoryKb { get; set; } = null!;
        public string pClaimId { get; set; } = null!;
        public string pReportId { get; set; } = null!;
        public int? pReportFormat { get; set; }
        public string? pLoanSubject { get; set; }
        public string? pLoanSubjectType { get; set; }
        public string? pPin { get; set; }
        public string? pTin { get; set; }
        public string? pToken { get; set; }
        public string? Status { get; set; }
        public int? QuantitySelected { get; set; }
    }
}
