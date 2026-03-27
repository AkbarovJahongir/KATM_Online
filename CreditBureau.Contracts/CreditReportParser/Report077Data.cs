using System.Text.Json.Serialization;

namespace CreditBureau.Contracts.CreditReportParser;

public sealed class Report077Data
{
    [JsonPropertyName("Katm_077_Claims_without_contract_count")]
    public int ClaimsWithoutContractCount { get; set; }       // Кол-во заявок без договора

    [JsonPropertyName("Katm_077_Credit_contracts_count")]
    public int CreditContractsCount { get; set; }             // Кол-во кредитных договоров

    [JsonPropertyName("Katm_077_Overdue_credit_contracts_count")]
    public int OverdueCreditContractsCount { get; set; }      // Кол-во просроченных кредитов

    [JsonPropertyName("Katm_077_Credit_contracts_total_sum")]
    public decimal CreditContractsTotalSum { get; set; }      // Общая сумма кредитных договоров

    [JsonPropertyName("Katm_077_Overdue_credit_contracts_total_sum")]
    public decimal OverdueCreditContractsTotalSum { get; set; } // Общая сумма просроченных кредитов

    [JsonPropertyName("Katm_077_Court_credit_contracts_count")]
    public int CourtCreditContractsCount { get; set; }        // Кол-во кредитов в суде

    [JsonPropertyName("Katm_077_Court_credit_contracts_total_sum")]
    public decimal CourtCreditContractsTotalSum { get; set; } // Сумма кредитов в суде

    [JsonPropertyName("Katm_077_Overdue_history_count")]
    public int OverdueHistoryCount { get; set; }              // Кол-во записей с историей просрочек

    [JsonPropertyName("Katm_077_Scorring_grade")]
    public int ScorringGrade { get; set; }                    // Скоринговый балл
}

public sealed class Report025Data
{
    public string App { get; set; } = string.Empty; // Идентификатор приложения
    public int? Num { get; set; }   // Нумерация начисления
    public DateTime? Period { get; set; }   // нормализовано как 1-е число месяца
    public string? OrgName { get; set; }   // Наименование источника дохода
    public string? OrgInn { get; set; }   // ИНН источника дохода (сохраняем ведущие нули)
    public decimal? IncomeSumma { get; set; }   // Сумма дохода
    public decimal? InpsSumma { get; set; }   // Сумма поступлений ИНПС
    public DateTime? SendDate { get; set; }   // Дата поступления
    public DateTime? OperDate { get; set; }   // Дата операции
}