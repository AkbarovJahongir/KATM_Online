using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;

/// <summary>
/// Сведения о бухгалтерских проводках для хозяйствующих субъектов (CI-022)
/// URL: {base_url}/credit/registration/repayment/business/details
/// </summary>
public class CreditRegistrationBusinessDetailRequest
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
    /// Код типа кредитования (0A6): 1=кредит, 2=лизинг
    /// </summary>
    [JsonProperty("pContractType")]
    public string? PContractType { get; set; }

    /// <summary>
    /// Уникальный ID договора
    /// </summary>
    [JsonProperty("pContractId")]
    public string? PContractId { get; set; }

    /// <summary>
    /// Дата отправки информации (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
    /// </summary>
    [JsonProperty("pDate")]
    public string? PDate { get; set; }

    /// <summary>
    /// Детализированная информация по бухгалтерским проводкам
    /// </summary>
    [JsonProperty("pRepaymentDetArray")]
    public List<PBusinessRepaymentDetArray>? PRepaymentDetArray { get; set; }
}

public class PBusinessRepaymentDetArray
{
    /// <summary>
    /// Дебетовый план счётов (4 знака, COA)
    /// </summary>
    [JsonProperty("accountA")]
    public string? AccountA { get; set; }

    /// <summary>
    /// Кредитовый план счётов (4 знака, COA)
    /// </summary>
    [JsonProperty("accountB")]
    public string? AccountB { get; set; }

    /// <summary>
    /// Код валюты платежа (017)
    /// </summary>
    [JsonProperty("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Код назначения платежа (060)
    /// </summary>
    [JsonProperty("destination")]
    public string? Destination { get; set; }

    /// <summary>
    /// Дата проведения платежа (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
    /// </summary>
    [JsonProperty("docDate")]
    public string? DocDate { get; set; }

    /// <summary>
    /// Номер платёжного документа
    /// </summary>
    [JsonProperty("docNum")]
    public string? DocNum { get; set; }

    /// <summary>
    /// Код типа платёжного документа (026)
    /// </summary>
    [JsonProperty("docType")]
    public string? DocType { get; set; }

    /// <summary>
    /// Наименование отправителя
    /// </summary>
    [JsonProperty("nameA")]
    public string? NameA { get; set; }

    /// <summary>
    /// Наименование получателя
    /// </summary>
    [JsonProperty("nameB")]
    public string? NameB { get; set; }

    /// <summary>
    /// Форма выдачи / погашения (004)
    /// </summary>
    [JsonProperty("payType")]
    public string? PayType { get; set; }

    /// <summary>
    /// Уникальный номер платёжного документа
    /// </summary>
    [JsonProperty("paymentId")]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Цель платежа
    /// </summary>
    [JsonProperty("purpose")]
    public string? Purpose { get; set; }

    /// <summary>
    /// Сумма платежа в тийинах
    /// </summary>
    [JsonProperty("summa")]
    public decimal? Summa { get; set; }
}