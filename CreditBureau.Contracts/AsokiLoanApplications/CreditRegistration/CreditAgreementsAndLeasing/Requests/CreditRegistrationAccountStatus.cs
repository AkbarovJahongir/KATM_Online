using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 14. Сведения о статусе счетов (CI-018)
    /// </summary>
    public class CreditRegistrationAccountStatus
    {
        /// <summary>
        /// Головной код организации
        /// </summary>
        [JsonProperty("pHead")]
        public string? PHead { get; set; }

        /// <summary>
        /// Код организации
        /// </summary>
        [JsonProperty("pCode")]
        public string? PCode { get; set; }

        /// <summary>
        /// Уникальный ID договора
        /// </summary>
        [JsonProperty("pContractId")]
        public string? PContractId { get; set; }

        /// <summary>
        /// Код типа договора (0A6): 1=кредит, 2=лизинг
        /// </summary>
        [JsonProperty("pContractType")]
        public string? PContractType { get; set; }

        /// <summary>
        /// Дата отправки информации (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("pDate")]
        public string? PDate { get; set; }

        /// <summary>
        /// Массив статусов счетов
        /// </summary>
        [JsonProperty("pAccountStatusesArray")]
        public List<AccountStatusItem>? PAccountStatusesArray { get; set; }
    }

    public class AccountStatusItem
    {
        /// <summary>
        /// Дата обновления статуса счёта (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("date")]
        public string? Date { get; set; }

        /// <summary>
        /// Счёт клиента
        /// </summary>
        [JsonProperty("account")]
        public string? Account { get; set; }

        /// <summary>
        /// План счётов (COA)
        /// </summary>
        [JsonProperty("coa")]
        public string? Coa { get; set; }

        /// <summary>
        /// Дата открытия счёта (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("dateOpen")]
        public string? DateOpen { get; set; }

        /// <summary>
        /// Дата закрытия счёта (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty("dateClose")]
        public string? DateClose { get; set; }
    }
}