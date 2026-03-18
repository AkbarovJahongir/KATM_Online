using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// CI-013: Сведения об объекте лизингового договора
    /// URL: {base_url}/credit/registration/leasing/repayment
    /// </summary>
    public class CreditRegistrationLeasingRepayment
    {
        /// <summary>Головной код организации</summary>
        [JsonProperty("pHead")]
        public string? PHead { get; set; }

        /// <summary>Код организации</summary>
        [JsonProperty("pCode")]
        public string? PCode { get; set; }

        /// <summary>Уникальный ID договора</summary>
        [JsonProperty("pContractId")]
        public string? PContractId { get; set; }

        /// <summary>Уникальный номер клиента (KATM-SIR)</summary>
        [JsonProperty("pClientId")]
        public string? PClientId { get; set; }

        /// <summary>ИНН клиента — 9 символов</summary>
        [JsonProperty("pInn")]
        public string? PInn { get; set; }

        /// <summary>НИББД клиента — 8 символов</summary>
        [JsonProperty("pNibbd")]
        public string? PNibbd { get; set; }

        /// <summary>Дата отправки информации (yyyy-MM-dd'T'HH:mm:ss.SSSZ)</summary>
        [JsonProperty("pDate")]
        public string? PDate { get; set; }

        /// <summary>Информация об объекте(ах) лизингового договора</summary>
        [JsonProperty("pDetailsArray")]
        public List<PDetailsArray>? PDetailsArray { get; set; }
    }

    public class PDetailsArray
    {
        /// <summary>Уникальный номер объекта лизинга — M</summary>
        [JsonProperty("objectId")]
        public string? ObjectId { get; set; }

        /// <summary>Стоимость объекта по договору в тийинах — M</summary>
        [JsonProperty("amount")]
        public string? Amount { get; set; }

        /// <summary>Валюта объекта лизинга ISO-4217 (017) — M</summary>
        [JsonProperty("currency")]
        public string? Currency { get; set; }

        /// <summary>Код типа объекта лизинга справочник 0A4 — M</summary>
        [JsonProperty("leasingType")]
        public string? LeasingType { get; set; }

        /// <summary>Наименование объекта лизинга до 100 символов — M</summary>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>Статус объекта справочник A13 (001 = действующий) — M</summary>
        [JsonProperty("status")]
        public string? Status { get; set; }

        /// <summary>Норма амортизации объекта лизинга — O</summary>
        [JsonProperty("amortization")]
        public string? Amortization { get; set; }

        /// <summary>Дата обновления (yyyy-MM-dd'T'HH:mm:ss.SSSZ) — O</summary>
        [JsonProperty("date")]
        public string? Date { get; set; }

        /// <summary>Дополнительные данные по объекту лизинга справочник А15 — O</summary>
        [JsonProperty("details")]
        public string? Details { get; set; }
    }
}