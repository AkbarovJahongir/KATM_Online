using Newtonsoft.Json;

namespace CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 22. Сведения о гарантии (CI-025, ТЗ 10.0)
    /// </summary>
    public class CreditRegistrationGuarantee
    {
        [JsonProperty("pHead")]
        public string? PHead { get; set; }

        [JsonProperty("pCode")]
        public string? PCode { get; set; }

        [JsonProperty("pClaimId")]
        public string? PClaimId { get; set; }

        [JsonProperty("pContractId")]
        public string? PContractId { get; set; }

        /// <summary>
        /// ИНН (9 символов). Имя поля в ТЗ 10.0 читается неоднозначно — сверить с Swagger КАТМ.
        /// </summary>
        [JsonProperty("pTin")]
        public string? PTin { get; set; }

        [JsonProperty("pNibbd")]
        public string? PNibbd { get; set; }

        /// <summary>
        /// Тип гарантии (справочник A20)
        /// </summary>
        [JsonProperty("pGuaranteeType")]
        public string? PGuaranteeType { get; set; }

        [JsonProperty("pStartDate")]
        public string? PStartDate { get; set; }

        [JsonProperty("pEndDate")]
        public string? PEndDate { get; set; }

        /// <summary>
        /// Код валюты (017)
        /// </summary>
        [JsonProperty("pCurrency")]
        public string? PCurrency { get; set; }

        [JsonProperty("pCreditAmount")]
        public decimal? PCreditAmount { get; set; }

        /// <summary>
        /// Качество (справочник 036)
        /// </summary>
        [JsonProperty("pQuality")]
        public string? PQuality { get; set; }

        /// <summary>
        /// Источник (справочник 113)
        /// </summary>
        [JsonProperty("pSourceType")]
        public string? PSourceType { get; set; }

        /// <summary>
        /// Иностранная организация (справочник 041)
        /// </summary>
        [JsonProperty("pForeignOrg")]
        public string? PForeignOrg { get; set; }

        [JsonProperty("pGuaranteeInfo")]
        public string? PGuaranteeInfo { get; set; }

        /// <summary>
        /// Дата отправки (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("pDate")]
        public string? PDate { get; set; }

        /// <summary>
        /// Справочник A16
        /// </summary>
        [JsonProperty("pActivity")]
        public string? PActivity { get; set; }

        /// <summary>
        /// Справочник A17
        /// </summary>
        [JsonProperty("pReason")]
        public string? PReason { get; set; }
    }
}
