using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 4.	Сведения о договорах факторинга (CI-014)
    /// </summary>
    public class CreditRegistrationFactoring
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
        /// Уникальный ID заявки
        /// </summary>
        [JsonProperty(PropertyName = "pClaimId")]
        public string? PClaimId { get; set; }

        /// <summary>
        /// Уникальный ID договора
        /// </summary>
        [JsonProperty(PropertyName = "pContractId")]
        public string? PContractId { get; set; }

        /// <summary>
        /// Уникальный ID договора
        /// </summary>
        [JsonProperty(PropertyName = "pInn")]
        public string? PInn { get; set; }

        /// <summary>
        /// Уникальный ID договора
        /// </summary>
        [JsonProperty(PropertyName = "pNibbd")]
        public string? PNibbd { get; set; }

        /// <summary>
        /// Сумма кредита (в тийинах)
        /// </summary>
        [JsonProperty(PropertyName = "pCreditAmount")]
        public decimal PCreditAmount { get; set; }

        /// <summary>
        /// Код валюты
        /// </summary>
        [JsonProperty(PropertyName = "pCurrency")]
        public string? PCurrency { get; set; }

        /// <summary>
        /// Код органа банка принявшего решение о выдаче факторинга (0A5)
        /// </summary>
        [JsonProperty("pBankElement")]
        public string? PBankElement { get; set; }

        /// <summary>
        /// Номер решения о выдаче факторинга
        /// </summary>
        [JsonProperty("pFactoringNumber")]
        public string? PFactoringNumber { get; set; }

        /// <summary>
        /// Сумма обязательства должника (с учетом дисконта)
        /// </summary>
        [JsonProperty("pSummaLiability")]
        public string? PSummaLiability { get; set; }

        /// <summary>
        /// Сумма дисконта
        /// </summary>
        [JsonProperty("pSummaDiscount")]
        public string? PSummaDiscount { get; set; }

        /// <summary>
        /// ИНН клиента. Сведение о должнике
        /// </summary>
        [JsonProperty("pInnDebtor")]
        public string? PInnDebtor { get; set; }

        /// <summary>
        /// Дата отправки информации(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("pDate")]
        public string? PDate { get; set; }


        /// <summary>
        /// Дата начала договора(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("pStartDate")]
        public string? PStartDate { get; set; }


        /// <summary>
        /// Дата окончания договора(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("pEndDate")]
        public string? PEndDate { get; set; }
    }
}
