using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 11.	Сведения о владельце залога (CI-020)
    /// </summary>
    public class CreditRegistrationPledgeOwner
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
        [JsonProperty(PropertyName = "pInn")]
        public string? PInn { get; set; }

        /// <summary>
        /// Уникальный ID договора
        /// </summary>
        [JsonProperty(PropertyName = "pNibbd")]
        public string? PNibbd { get; set; }

        /// <summary>
        /// Дата документа согласия на получеие КО(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pAgreementDate")]
        public string? PAgreementDate { get; set; }

        /// <summary>
        /// Номер документа согласия на получеие КО
        /// </summary>
        [JsonProperty(PropertyName = "pAgreementNumber")]
        public string? PAgreementNumber { get; set; }

        /// <summary>
        /// Уникальный номер клиента (KATM-SIR)
        /// </summary>
        [JsonProperty(PropertyName = "pClientId")]
        public string? PClientId { get; set; }

        /// <summary>
        /// Код типа клиента (021)
        /// </summary>
        [JsonProperty(PropertyName = "pClientType")]
        public string? PClientType { get; set; }

        /// <summary>
        /// Дата отправки информации (yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pDate")]
        public string? PDate { get; set; }

        /// <summary>
        /// Дата рождения (yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pDateBirthday")]
        public string? PDateBirthday { get; set; }

        /// <summary>
        /// Фамилия, Имя
        /// </summary>
        [JsonProperty(PropertyName = "pFio")]
        public string? PFio { get; set; }

        /// <summary>
        /// Наименование клиента
        /// </summary>
        [JsonProperty(PropertyName = "pFullName")]
        public string? PFullName { get; set; }

        /// <summary>
        /// Дата выдачи удостоверяющего документа(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pIdentityCardDate")]
        public string? PIdentityCardDate { get; set; }

        /// <summary>
        /// Номер документа
        /// </summary>
        [JsonProperty(PropertyName = "pIdentityCardNumber")]
        public string? PIdentityCardNumber { get; set; }

        /// <summary>
        /// Серия документа
        /// </summary>
        [JsonProperty(PropertyName = "pIdentityCardSerial")]
        public string? PIdentityCardSerial { get; set; }

        /// <summary>
        /// Код удостоверения личности (008)
        /// </summary>
        [JsonProperty(PropertyName = "pIdentityCardType")]
        public string? PIdentityCardType { get; set; }

        /// <summary>
        /// Юридический адрес (прописки)
        /// </summary>
        [JsonProperty(PropertyName = "pLegalAddress")]
        public string? PLegalAddress { get; set; }

        /// <summary>
        /// Уникальный номер субъекта кредитной информации
        /// </summary>
        [JsonProperty(PropertyName = "pOwnerId")]
        public string? POwnerId { get; set; }

        /// <summary>
        /// Персональный код гражданина
        /// </summary>
        [JsonProperty(PropertyName = "pPersonalCode")]
        public string? PPersonalCode { get; set; }

        /// <summary>
        /// Код резидентности владельца залога (027)
        /// </summary>
        [JsonProperty(PropertyName = "pResident")]
        public string? PResident { get; set; }

        /// <summary>
        /// Пол (007)
        /// </summary>
        [JsonProperty(PropertyName = "pSex")]
        public string? PSex { get; set; }

        /// <summary>
        /// Код юридического статуса клиента (006)
        /// </summary>
        [JsonProperty(PropertyName = "pSubjectType")]
        public string? PSubjectType { get; set; }
    }
}
