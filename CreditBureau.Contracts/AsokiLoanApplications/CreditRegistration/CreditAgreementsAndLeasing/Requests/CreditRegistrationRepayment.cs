using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// Сведения об остатках на счетах (CI-015)
    /// URL: {base_url}/credit/registration/repayment
    /// </summary>
    public class CreditRegistrationRepayment
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
        /// Уникальный ID договора
        /// </summary>
        [JsonProperty(PropertyName = "pContractId")]
        public string? PContractId { get; set; }

        /// <summary>
        /// Код типа кредитования (0A6)
        /// </summary>
        [JsonProperty(PropertyName = "pContractType")]
        public string? PContractType { get; set; }

        /// <summary>
        /// Дата отправки информации (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pDate")]
        public string? PDate { get; set; }

        /// <summary>
        /// Статус ссуды (042)
        /// </summary>
        [JsonProperty(PropertyName = "pLoanStatus")]
        public string? PLoanStatus { get; set; }

        /// <summary>
        /// Информация по остаткам на счетах
        /// </summary>
        [JsonProperty(PropertyName = "pRepaymentArray")]
        public List<PRepaymentArray>? PRepaymentArray { get; set; }
    }

    public class PRepaymentArray
    {
        /// <summary>
        /// Счёт
        /// </summary>
        [JsonProperty(PropertyName = "account")]
        public string? Account { get; set; }

        /// <summary>
        /// Дата обновления (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "date")]
        public string? Date { get; set; }

        /// <summary>
        /// Сальдо на начало периода
        /// </summary>
        [JsonProperty(PropertyName = "startBalance")]
        public decimal? StartBalance { get; set; }

        /// <summary>
        /// Сумма дебета
        /// </summary>
        [JsonProperty(PropertyName = "debit")]
        public decimal? Debit { get; set; }

        /// <summary>
        /// Сумма кредита
        /// </summary>
        [JsonProperty(PropertyName = "credit")]
        public decimal? Credit { get; set; }

        /// <summary>
        /// Сальдо на конец периода
        /// </summary>
        [JsonProperty(PropertyName = "endBalance")]
        public decimal? EndBalance { get; set; }
    }
}