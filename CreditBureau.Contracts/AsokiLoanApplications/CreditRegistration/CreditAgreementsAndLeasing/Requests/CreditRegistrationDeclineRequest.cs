using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 3.	Отклонение заявки (CI-003)
    /// </summary>
    public class CreditRegistrationDeclineRequest
    {
        /// <summary>
        /// Головной код организации
        /// </summary>
        [JsonProperty(PropertyName = "pHead")]
        public string? PHead { get; set; }

        /// <summary>
        /// Код организации
        /// </summary>
        [JsonProperty(PropertyName = "pCode")]
        public string? PCode { get; set; }

        /// <summary>
        /// Дата отказа(yyyy-MM-dd'T'HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pDeclineDate")]
        public string? PDeclineDate { get; set; }

        /// <summary>
        /// Уникальный ID заявки
        /// </summary>
        [JsonProperty(PropertyName = "pClaimId")]
        public string? PClaimId { get; set; }

        /// <summary>
        /// ID отказа 
        /// </summary>
        [JsonProperty(PropertyName = "pDeclineNumber")]
        public string? PDeclineNumber { get; set; }

        /// <summary>
        /// Код причины отказа (0A8)
        /// </summary>
        [JsonProperty(PropertyName = "pDeclineReason")]
        public string? PDeclineReason { get; set; }

        /// <summary>
        /// Примечание к отказу
        /// </summary>
        [JsonProperty("pDeclineReasonNote")]
        public string? PDeclineReasonNote { get; set; }

        /// <summary>
        /// Дата отправки информации(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("pDate")]
        public string? PDate { get; set; }
    }
}
