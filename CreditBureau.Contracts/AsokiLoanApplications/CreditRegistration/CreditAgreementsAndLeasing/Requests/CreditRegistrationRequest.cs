using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 1.	Сведения о кредитном договоре (CI-004)
    /// </summary>
    public class CreditRegistrationRequest
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
        /// ИНН клиента
        /// </summary>
        [JsonProperty(PropertyName = "pInn")]
        public int? PInn { get; set; }

        /// <summary>
        /// НИББД клиента
        /// </summary>
        [JsonProperty(PropertyName = "pNibbd")]
        public string? PNibbd { get; set; }

        /// <summary>
        /// Код вида кредитования (031)
        /// </summary>
        [JsonProperty(PropertyName = "pType")]
        public string? PType { get; set; }

        /// <summary>
        /// Код объекта кредитования (034)
        /// </summary>
        [JsonProperty(PropertyName = "pObject")]
        public string? PObject { get; set; }

        /// <summary>
        /// Дата начала договора(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pStartDate")]
        public string? PStartDate { get; set; }

        /// <summary>
        /// Дата окончания договора(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pEndDate")]
        public string? PEndDate { get; set; }

        /// <summary>
        /// Сумма кредита (в тийинах)
        /// </summary>
        [JsonProperty(PropertyName = "pCreditAmount")]
        public decimal? PCreditAmount { get; set; }

        /// <summary>
        /// Код валюты (017)
        /// </summary>
        [JsonProperty(PropertyName = "pCurrency")]
        public string? PCurrency { get; set; }

        /// <summary>
        /// Процентная ставка по кредиту
        /// </summary>
        [JsonProperty(PropertyName = "pPercent")]
        public decimal? PPercent { get; set; }

        /// <summary>
        /// Юридический номер кредитного договора
        /// </summary>
        [JsonProperty(PropertyName = "pJuridicalNumber")]
        public string? PJuridicalNumber { get; set; }

        /// <summary>
        /// Код обеспеченности кредита (035)
        /// </summary>
        [JsonProperty(PropertyName = "pSupply")]
        public string? PSupply { get; set; }

        /// <summary>
        /// Класс качества активов (036)
        /// </summary>
        [JsonProperty(PropertyName = "pQuality")]
        public string? PQuality { get; set; }

        /// <summary>
        /// Код вида срочности договора (030)
        /// </summary>
        [JsonProperty(PropertyName = "pUrgency")]
        public string? PUrgency { get; set; }

        /// <summary>
        /// Код вышестоящей организации (071)
        /// </summary>
        [JsonProperty(PropertyName = "pHBranch")]
        public string? PHBranch { get; set; }

        /// <summary>
        /// Статус договора (A16)
        /// </summary>
        [JsonProperty(PropertyName = "pActivity")]
        public string? PActivity { get; set; }

        /// <summary>
        /// Причины досрочного прекращения кредитного договора (A17)
        /// </summary>
        [JsonProperty(PropertyName = "pReason")]
        public string? PReason { get; set; }

        /// <summary>
        /// Причины досрочного прекращения кредитного договора (A17)
        /// </summary>
        [JsonProperty(PropertyName = "pFounder")]
        public string? PFounder { get; set; }

        /// <summary>
        /// Дата отправки информации(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pDate")]
        public string? PDate { get; set; }
    }
}
