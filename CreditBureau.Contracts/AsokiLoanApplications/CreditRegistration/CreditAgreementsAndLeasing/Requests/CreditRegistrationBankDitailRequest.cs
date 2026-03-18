using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;

/// <summary>
/// Детализация банковских платежей (CI-016)
/// </summary>
public class CreditRegistrationBankDitailRequest
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
    /// Код типа кредитования (0A6)
    /// </summary>
    [JsonProperty("pContractType")]
    public string? PContractType { get; set; }

    /// <summary>
    /// Уникальный ID договора
    /// </summary>
    [JsonProperty("pContractId")]
    public string? PContractId { get; set; }

    /// <summary>
    /// Дата отправки
    /// </summary>
    [JsonProperty("pDate")]
    public string? PDate { get; set; }

    /// <summary>
    /// Детализация платежей
    /// </summary>
    [JsonProperty("pRepaymentDetArray")]
    public List<PRepaymentDetArray>? PRepaymentDetArray { get; set; }
}

public class PRepaymentDetArray
{
    /// <summary>
    /// Дебетовый лицевой счет
    /// </summary>
    [JsonProperty("accountA")]
    public string? AccountA { get; set; }

    /// <summary>
    /// Кредитовый лицевой счет
    /// </summary>
    [JsonProperty("accountB")]
    public string? AccountB { get; set; }

    /// <summary>
    /// Код банка отправителя
    /// </summary>
    [JsonProperty("branchA")]
    public string? BranchA { get; set; }

    /// <summary>
    /// Код банка получателя
    /// </summary>
    [JsonProperty("branchB")]
    public string? BranchB { get; set; }

    /// <summary>
    /// План счета отправителя
    /// </summary>
    [JsonProperty("coaA")]
    public string? CoaA { get; set; }

    /// <summary>
    /// План счета получателя
    /// </summary>
    [JsonProperty("coaB")]
    public string? CoaB { get; set; }

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
    /// Дата платежа
    /// </summary>
    [JsonProperty("docDate")]
    public string? DocDate { get; set; }

    /// <summary>
    /// Номер документа
    /// </summary>
    [JsonProperty("docNum")]
    public string? DocNum { get; set; }

    /// <summary>
    /// Тип документа (026)
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
    /// Уникальный ID платежного документа
    /// </summary>
    [JsonProperty("paymentId")]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Назначение платежа
    /// </summary>
    [JsonProperty("purpose")]
    public string? Purpose { get; set; }

    /// <summary>
    /// Сумма платежа
    /// </summary>
    [JsonProperty("summa")]
    public decimal? Summa { get; set; }
}