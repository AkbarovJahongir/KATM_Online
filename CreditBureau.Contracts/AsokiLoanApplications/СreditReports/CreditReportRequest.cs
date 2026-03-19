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
        [JsonProperty(PropertyName = "pToken")]
        public string? PToken { get; set; }  // KATM-SIR или токен для polling

        /// <summary>
        /// Цель изучения кредитного отчёта (обязателен с версии 9.15):
        /// 1=мониторинг, 2=скоринг, 3=антифрод, 4=коллекшн
        /// </summary>
        [JsonProperty(PropertyName = "pReportReason")]
        public string? PReportReason { get; set; }

        [JsonProperty(PropertyName = "pPin")]
        public string? PPin { get; set; }
        [JsonProperty(PropertyName = "pTin")]
        public string? PTin { get; set; }
    }
}
