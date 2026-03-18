using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 2.	Сведения о лизинговых договорах (CI-011)
    /// </summary>
    public class CreditRegistrationLeasingRequest
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
        /// Дата начала договора(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pStartDate")]
        public string? PStartDate { get; set; }

        /// <summary>
        /// Дата начала договора(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pEndDate")]
        public string? PEndDate { get; set; }

        /// <summary>
        /// Номер выдачи нотариального удостоверения
        /// </summary>
        [JsonProperty(PropertyName = "pNotariusCertNumber")]
        public string? PNotariusCertNumber { get; set; }

        /// <summary>
        /// Номер выдачи нотариального удостоверения
        /// </summary>
        [JsonProperty(PropertyName = "pNotariusCertDate")]
        public string? PNotariusCertDate { get; set; }

        /// <summary>
        /// Номер выдачи нотариального удостоверения
        /// </summary>
        [JsonProperty(PropertyName = "pNotariusRegNumber")]
        public string? PNotariusRegNumber { get; set; }

        /// <summary>
        /// Дата регистрации договора у нотариуса(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pNotariusRegDate")]
        public string? PNotariusRegDate { get; set; }

        /// <summary>
        /// Номер государственной регистрации
        /// </summary>
        [JsonProperty(PropertyName = "pGovernmentRegNum")]
        public string? PGovernmentRegNum { get; set; }

        /// <summary>
        /// Дата государственной регистрации(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pGovernmentRegDate")]
        public string? PGovernmentRegDate { get; set; }

        /// <summary>
        /// Сумма кредита (в тийинах)
        /// </summary>
        [JsonProperty(PropertyName = "pCreditAmount")]
        public decimal PCreditAmount { get; set; }

        /// <summary>
        /// Код валюты (017)
        /// </summary>
        [JsonProperty(PropertyName = "pCurrency")]
        public string? PCurrency { get; set; }

        /// <summary>
        /// Процентная ставка по кредиту
        /// </summary>
        [JsonProperty(PropertyName = "pPercent")]
        public decimal PPercent { get; set; }

        /// <summary>
        /// Количество объектов лизинга
        /// </summary>
        [JsonProperty(PropertyName = "pCountObject")]
        public int PCountObject { get; set; }

        /// <summary>
        /// Дата отправки информации(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pDate")]
        public string? PDate { get; set; }
    }
}
