using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.СreditReports
{
    public class CreditReportRequest
    {
        [JsonProperty(PropertyName = "pHead")]
        public string PHead { get; set; } = null!;
        [JsonProperty(PropertyName = "pCode")]
        public string PCode { get; set; } = null!;
        [JsonProperty(PropertyName = "pClaimId")]
        public string PClaimId { get; set; } = null!;
        [JsonProperty(PropertyName = "pReportId")]
        public string PReportId { get; set; } = null!;
        [JsonProperty(PropertyName = "pReportFormat")]
        public int? PReportFormat { get; set; }
        [JsonProperty(PropertyName = "pLoanSubject")]
        public string? PLoanSubject { get; set; }
        [JsonProperty(PropertyName = "pLoanSubjectType")]
        public string? PLoanSubjectType { get; set; }
        [JsonProperty(PropertyName = "pPin")]
        public string? PPin { get; set; }
        [JsonProperty(PropertyName = "pTin")]
        public string? PTin { get; set; }
    }
}
