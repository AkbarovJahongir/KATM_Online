namespace CreditBureau.Contracts.AsokiLoanApplications
{
    public record LoanApplication
    {
        public string KeyLoanHistoryKb { get; set; } = null!;
        public string PClaimId { get; set; } = null!;
        public string? PReportId { get; set; } = null!;
        public string? PLoanSubject { get; set; }
        public string? PLoanSubjectType { get; set; }
        public string? PPin { get; set; }
        public string? PTin { get; set; }
        public string? PToken { get; set; }
        public string? Status { get; set; }
        public int? QuantitySelected { get; set; }
        public string? ApplicationsSubjectType { get; set; }
    }
}
