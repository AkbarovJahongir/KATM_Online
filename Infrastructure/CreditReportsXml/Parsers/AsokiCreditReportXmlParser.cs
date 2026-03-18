using Application.Repositories.CreditReportXmlParserRepositories;
using CreditBureau.Contracts.CreditReportParser;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Infrastructure.CreditReportsXml.Parsers;

public sealed class AsokiCreditReportXmlParser(ICreditReportXmlParserRepository creditReportXmlParserRepository, IOptions<StopFactorParserSettings> options, LogWriter logWriter) : ICreditReportXmlParser
{
    private readonly LogWriter _logWriter = logWriter;
    private readonly ICreditReportXmlParserRepository _creditReportXmlParserRepository = creditReportXmlParserRepository;
    private readonly StopFactorParserSettings _settings = options.Value;
    public async Task ParseAndPersistAsync(string KeyLoanHistoryKb, string reportBase64, CancellationToken cancellationToken)
    {
        _logWriter.Log("TestParser.txt", $"Начало парсинга: Key={KeyLoanHistoryKb}");
        // получаем app по loan_history_kb_key
        var app = await _creditReportXmlParserRepository.GetAppByKeyLoanHistoryKb(KeyLoanHistoryKb, cancellationToken);
        _logWriter.Log("TestParser.txt", $"Получен app={app}");

        string payload = string.Empty;
        string reportName = string.Empty;
        string reportCode = string.Empty;
        try
        {
            // TODO: Implement the logic to parse the Asoki credit report XML and persist it using the repository.
            // 1. Парсим весь документ
            string document = Encoding.UTF8.GetString(Convert.FromBase64String(reportBase64));
            _logWriter.Log("TestParser.txt", "Base64 → UTF8 успешно");
            XDocument xmlDoc = XDocument.Parse(document);
            _logWriter.Log("TestParser.txt", "XML документ успешно разобран");

            // 2. Собираем информации по полям
            reportCode = xmlDoc
                .Descendants("sysinfo")
                .Elements("report_type")
                .Select(e => (string?)e)   // берём строковое значение элемента или null
                .FirstOrDefault()?         // может быть null
                .Trim()                    // вызываем Trim только если не null
                ?? string.Empty;
            _logWriter.Log("TestParser.txt", $"reportCode={reportCode}");
            if (reportCode == "077")
            {
                Report077Xml(xmlDoc, out payload, out reportName);
                _logWriter.Log("TestParser.txt", payload);
                _logWriter.Log("TestParser.txt", $"Report077 обработан: {reportName}");
                await _creditReportXmlParserRepository.ExternalInfo_Save(app, payload, nameof(Report077Data), cancellationToken);

                _logWriter.Log("TestParser.txt", "Report077 сохранён в ExternalInfo_Save");
            }
            else if (reportCode == "025")
            {
                List<Report025Data> report025s = Report025Xml(app, xmlDoc);
                _logWriter.Log("TestParser.txt", $"Report025 элементов: {report025s.Count}");

                if (report025s.Count > 0)
                {
                    await _creditReportXmlParserRepository.InsertReport025(report025s);
                    _logWriter.Log("TestParser.txt", "Report025 сохранён в БД");
                }
                return;
            }
            else
            {
                _logWriter.Log("TestParser.txt", $"Неизвестный reportCode={reportCode} → выход");
                return;
            }
        }
        catch (Exception ex)
        {
            _logWriter.Log("TestParser.txt", $"Ошибка: {ex}");
            throw;
        }
        finally
        {
            _logWriter.Log("TestParser.txt", $"Завершение обработки Key={KeyLoanHistoryKb}");
        }
    }

    private static List<Report025Data> Report025Xml(string app, XDocument xmlDoc)
    {
        List<Report025Data> report025s = [];
        var elements = xmlDoc.Descendants("income");
        foreach (var element in elements)
        {
            var report025 = new Report025Data
            {
                App = app,
                Num = (int?)element.Element("num"),
                Period = DateTime.TryParseExact(
                    element.Element("period")?.Value, "yyyy-MM",
                    CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var period) ? period : null,
                OrgName = element.Element("orgname")?.Value,
                OrgInn = element.Element("org_inn")?.Value,
                IncomeSumma = (decimal?)element.Element("income_summa"),
                InpsSumma = (decimal?)element.Element("inps_summa"),
                SendDate = DateTime.TryParse(element.Element("send_date")?.Value, out var sendDate) ? sendDate : null,
                OperDate = DateTime.TryParse(element.Element("oper_date")?.Value, out var operDate) ? operDate : null
            };
            report025s.Add(report025);
        }
        return report025s;
    }

    private void Report077Xml(XDocument xmlDoc, out string payload, out string reportName)
    {
        _logWriter.Log("TestParser.txt", "Начало обработки Report077");

        // Claims_Without_Contract_Count - количество заявок без договоров
        // последние 7 календарных дней, включая сегодня
        var today = DateTime.UtcNow.Date;        // или DateTime.Today, если так принято
        var days = _settings.DaysForClaimsWithoutContract;
        var from = today.AddDays(-days);
        int claimsWithoutContractCount = xmlDoc.Descendants("claim_wo_contract")
            .Where(x =>
            {
                var s = (string?)x.Element("claim_date");
                return DateTime.TryParseExact(
                           s, "yyyy-MM-dd",
                           CultureInfo.InvariantCulture,
                           DateTimeStyles.None,
                           out var d)
                       && d >= from && d <= today;
            })
            .Count();

        // [credit_contracts_count] - количество кредитных договоров
        int creditContractsCount = xmlDoc.Descendants("open_contract").Count();

        // overdue_credit_contracts_count - количество кредитных договоров с просрочкой
        int overdueCreditContractsCount = xmlDoc
            .Descendants("open_contract")
            .Count(x =>
                (decimal?)x.Element("overdue_debt_sum") > 0 // или другой тег, указывающий на просрочку
            );

        // credit_contracts_total_sum - сумма всех кредитных договоров
        decimal creditContractsTotalSum = (decimal?)xmlDoc
            .Descendants("open_contracts")
            .Elements("all_debt_sum")
            .FirstOrDefault() ?? 0m;

        // overdue_credit_contracts_total_sum - сумма всех просроченных договоров
        decimal overdueCreditContractsTotalSum = (decimal?)xmlDoc
            .Descendants("open_contracts")
            .Elements("all_overdue_debt_sum")
            .FirstOrDefault() ?? 0m;

        // court_credit_contracts_count - количество договоров в суде
        int courtCreditContractsCount = xmlDoc
            .Descendants("contracts")
            .Descendants("contract")
            .Count(x => (decimal?)x.Element("lawsuit_principal_sum") > 0);

        // court_credit_contracts_total_sum - сумма задолженности в суде
        decimal courtCreditContractsTotalSum = xmlDoc
            .Descendants("contracts")
            .Descendants("contract")
            .Sum(x => (decimal?)x.Element("lawsuit_principal_sum") ?? 0m);

        // overdue_history_count - количество записей о просрочках
        int overdueHistoryCount = (int?)xmlDoc
            .Descendants("overview")
            .Elements("max_overdue_principal_days")
            .FirstOrDefault() ?? 0;
        // скоринговый балл Скоринговый балл: 338
        int scorringGrade = (int?)xmlDoc
            .Descendants("scorring")
            .Elements("scoring_grade")
            .FirstOrDefault() ?? 0;
        // сохранение данных
        var report077 = new Report077Data()
        {
            ClaimsWithoutContractCount = claimsWithoutContractCount,
            CreditContractsCount = creditContractsCount,
            OverdueCreditContractsCount = overdueCreditContractsCount,
            CreditContractsTotalSum = creditContractsTotalSum,
            OverdueCreditContractsTotalSum = overdueCreditContractsTotalSum,
            CourtCreditContractsCount = courtCreditContractsCount,
            CourtCreditContractsTotalSum = courtCreditContractsTotalSum,
            OverdueHistoryCount = overdueHistoryCount,
            ScorringGrade = scorringGrade
        };
        payload = JsonSerializer.Serialize(report077);
        reportName = nameof(report077);
        _logWriter.Log("TestParser.txt", "Report077 сформирован");
    }
}
