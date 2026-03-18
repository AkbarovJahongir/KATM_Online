using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests
{
    /// <summary>
    /// 12.	Сведения об обеспечении кредита (CI-021)
    /// </summary>
    public class CreditRegistrationPledgeSecurity
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
        /// Дата договора(yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pContractDate")]
        public string? PContractDate { get; set; }

        /// <summary>
        /// Номер договора
        /// </summary>
        [JsonProperty(PropertyName = "pContractNumber")]
        public string? PContractNumber { get; set; }

        /// <summary>
        /// Номер договора
        /// </summary>
        [JsonProperty(PropertyName = "pCurrency")]
        public string? PCurrency { get; set; }

        /// <summary>
        /// Дата отправки информации (yyyy-MM-dd’T’HH:mm:ss.SSSZ)
        /// </summary>
        [JsonProperty(PropertyName = "pDate")]
        public string? PDate { get; set; }

        /// <summary>
        /// Дополнительные данные по активам(через  разделитель ‘|’ (chr124))
        /// </summary>
        [JsonProperty(PropertyName = "pDescription")]
        public string? PDescription { get; set; }

        /// <summary>
        /// Уникальный номер обеспечения
        /// </summary>
        [JsonProperty(PropertyName = "pGuaranteeId")]
        public string? PGuaranteeId { get; set; }

        /// <summary>
        /// Код типа обеспечения кредита (033)
        /// </summary>
        [JsonProperty(PropertyName = "pGuaranteeType")]
        public string? PGuaranteeType { get; set; }

        /// <summary>
        /// Тип субъекта кредитной информации (A18)
        /// </summary>
        [JsonProperty(PropertyName = "pLoanSubject")]
        public string? PLoanSubject { get; set; }

        /// <summary>
        /// Наименование обеспечения
        /// </summary>
        [JsonProperty(PropertyName = "pName")]
        public string? PName { get; set; }

        /// <summary>
        /// Уникальный номер владельца субъекта кредитной информации
        /// </summary>
        [JsonProperty(PropertyName = "pOwnerId")]
        public string? POwnerId { get; set; }

        /// <summary>
        /// Код статуса обеспечения (A13)
        /// </summary>
        [JsonProperty(PropertyName = "pStatus")]
        public string? PStatus { get; set; }

        /// <summary>
        /// Оценочная стоимость обеспечения
        /// </summary>
        [JsonProperty(PropertyName = "pSumma")]
        public string? PSumma { get; set; }
    }
}
