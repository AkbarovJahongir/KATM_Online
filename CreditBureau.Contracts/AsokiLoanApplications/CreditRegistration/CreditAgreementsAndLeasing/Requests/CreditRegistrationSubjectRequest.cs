using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;

/// <summary>
/// Сведения о связанных субъектах (залогодателе/поручителе/созаёмщике) (CI-023)
/// URL: {base_url}/credit/registration/subject
/// </summary>
public class CreditRegistrationSubjectRequest
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
    /// Уникальный ID заявки
    /// </summary>
    [JsonProperty("pClaimId")]
    public string? PClaimId { get; set; }

    /// <summary>
    /// Код типа субъекта кредитной информации (A18):
    /// 2 = залогодатель, 3 = поручитель, 4 = созаёмщик
    /// </summary>
    [JsonProperty("pLoanSubject")]
    public string? PLoanSubject { get; set; }

    /// <summary>
    /// Статус субъекта кредитной информации (A19):
    /// 001 = действующий, 002 = закрытый
    /// </summary>
    [JsonProperty("pLoanSubjectStatus")]
    public string? PLoanSubjectStatus { get; set; }

    /// <summary>
    /// Уникальный номер субъекта кредитной информации (KATM-SIR или ID клиента)
    /// </summary>
    [JsonProperty("pOwnerId")]
    public string? POwnerId { get; set; }

    /// <summary>
    /// Дата отправки информации (yyyy-MM-dd'T'HH:mm:ss.SSSZ)
    /// </summary>
    [JsonProperty("pDate")]
    public string? PDate { get; set; }
}