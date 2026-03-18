using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// CI-012: Сведения о графике погашения лизингового договора
    /// URL: {base_url}/credit/registration/leasing/repayment/schedule
    /// </summary>
    public class CreditRegistrationLeasingRepaymentSchedule
    {
        /// <summary>Головной код организации</summary>
        [JsonProperty("pHead")]
        public string? PHead { get; set; }

        /// <summary>Код организации</summary>
        [JsonProperty("pCode")]
        public string? PCode { get; set; }

        /// <summary>Уникальный ID заявки</summary>
        [JsonProperty("pClaimId")]
        public string? PClaimId { get; set; }

        /// <summary>Уникальный ID договора</summary>
        [JsonProperty("pContractId")]
        public string? PContractId { get; set; }

        /// <summary>НИББД клиента — 8 символов</summary>
        [JsonProperty("pNibbd")]
        public string? PNibbd { get; set; }

        /// <summary>Дата отправки информации (yyyy-MM-dd'T'HH:mm:ss.SSSZ)</summary>
        [JsonProperty("pDate")]
        public string? PDate { get; set; }

        /// <summary>График погашения — массив плановых записей</summary>
        [JsonProperty("pPlanArray")]
        public List<LeasingPPlanArray>? PPlanArray { get; set; }

        /// <summary>0 – вставка (default), 1 – обновление пересмотренного графика</summary>
        [JsonProperty("pIsUpdate")]
        public string? PIsUpdate { get; set; }
    }

    public class LeasingPPlanArray
    {
        /// <summary>Плановая дата погашения (yyyy-MM-dd'T'HH:mm:ss.SSSZ)</summary>
        [JsonProperty("date")]
        public string? Date { get; set; }

        /// <summary>Сумма платежа по процентам в тийинах</summary>
        [JsonProperty("percent")]
        public string? Percent { get; set; }

        /// <summary>Код валюты ISO-4217 (например, 860 = UZS)</summary>
        [JsonProperty("currency")]
        public string? Currency { get; set; }

        /// <summary>Сумма платежа по основному долгу в тийинах</summary>
        [JsonProperty("amount")]
        public string? Amount { get; set; }
    }
}