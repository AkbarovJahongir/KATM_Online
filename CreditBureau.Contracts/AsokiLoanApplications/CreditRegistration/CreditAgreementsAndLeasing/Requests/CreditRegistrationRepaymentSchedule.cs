using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 8.	Сведения о графике погашения кредитного договора (CI-005)
    /// </summary>
    public class CreditRegistrationRepaymentSchedule
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
        [JsonProperty(PropertyName = "pNibbd")]
        public string? PNibbd { get; set; }

        /// <summary>
        /// Дата отправки информации(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("pDate")]
        public string? PDate { get; set; }

        /// <summary>
        /// График погашения
        /// </summary>
        [JsonProperty("pPlanArray")]
        public List<PPlanArray>? PPlanArray { get; set; }

        /// <summary>
        /// 0 – вставка(default), 1 – обновление
        /// </summary>
        [JsonProperty("pIsUpdate")]
        public string? PIsUpdate { get; set; }
    }
    public class PPlanArray
    {
        /// <summary>
        /// Плановая дата погашенияя (yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("date")]
        public string? Date { get; set; }

        /// <summary>
        /// Сумма платежа по процентам в тийинах
        /// </summary>
        [JsonProperty("percent")]
        public string? Percent { get; set; }

        /// <summary>
        /// Код валюты (017)
        /// </summary>
        [JsonProperty("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// Сумма платежа по основному долгу в тийинах
        /// </summary>
        [JsonProperty("amount")]
        public string? Amount { get; set; }
    }
}
