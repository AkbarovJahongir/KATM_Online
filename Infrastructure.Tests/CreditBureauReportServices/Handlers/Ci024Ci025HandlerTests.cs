using Application.Repositories.CreditBureauReportRepositories;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureauService.Contracts.Common;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Repositories.CreditBureauReportRepositories;
using Infrastructure.Services.CreditBureauReportServices.Handlers;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;

namespace Infrastructure.Tests.CreditBureauReportServices.Handlers;

public class Ci024Ci025HandlerTests
{
    private const string SuccessResponse = """{"result":"05000","resultMessage":"ok"}""";

    private readonly Mock<IRequestManagerService> _requestManager = new();
    private readonly Mock<ICreditBureauReportRepository> _repository = new();
    private readonly Mock<ITelegramNotificationService> _telegram = new();
    private readonly CreditBureauReportApiOptions _reportOptions = new() { PHead = "014", PCode = "01234" };
    private readonly CreditBureauApiOptions _apiOptions = new()
    {
        HostAddress = "https://bureau.test",
        BalanceReportUrl = "/balance",
        GuaranteeUrl = "/guarantee"
    };

    private Ci024BalanceReportHandler Ci024() => new(
        _repository.Object, _requestManager.Object, _reportOptions, _apiOptions,
        new RequestSecurity { pLogin = "l", pPassword = "p" }, new BankHeader(), new LogWriter(Path.GetTempPath(), false),
        _telegram.Object, new Mock<ILogger<Ci024BalanceReportHandler>>().Object);

    private Ci025GuaranteeHandler Ci025() => new(
        _repository.Object, _requestManager.Object, _reportOptions, _apiOptions,
        new RequestSecurity { pLogin = "l", pPassword = "p" }, new BankHeader(), new LogWriter(Path.GetTempPath(), false),
        _telegram.Object, new Mock<ILogger<Ci025GuaranteeHandler>>().Object);

    [Fact]
    public void AddBalanceRow_GroupsRowsByReportThenDepartment()
    {
        var reports = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationBalanceReport>>();
        CoaBalance Row(string coa) => new() { Coa = coa, Currency = "000", Date = "2026-05-26T00:00:00Z", StartBalance = 1.50m };

        CreditBureauReportRepository.AddBalanceRow(reports, 7, "014", "2026-06-01T00:00:00Z", "00001", Row("12501"));
        CreditBureauReportRepository.AddBalanceRow(reports, 7, "014", "2026-06-01T00:00:00Z", "00001", Row("12505"));
        CreditBureauReportRepository.AddBalanceRow(reports, 7, "014", "2026-06-01T00:00:00Z", "00002", Row("12501"));
        CreditBureauReportRepository.AddBalanceRow(reports, 8, "014", "2026-06-02T00:00:00Z", "00001", Row("12501"));

        Assert.Equal(2, reports.Count);
        var first = reports[7].Request;
        Assert.Equal("2026-06-01T00:00:00Z", first.PDate);
        Assert.Equal(["00001", "00002"], first.PDepartmentArray!.Select(d => d.Code));
        Assert.Equal(["12501", "12505"], first.PDepartmentArray![0].CoaBalanceArray!.Select(c => c.Coa));
        Assert.Single(reports[8].Request.PDepartmentArray!);
    }

    [Fact]
    public async Task Ci024_SendsNestedJsonWithNumbersAndKeepsBalanceDate()
    {
        var report = new CreditRegistrationBalanceReport
        {
            PDate = "2026-06-01T00:00:00Z",
            PDepartmentArray =
            [
                new DepartmentBalance
                {
                    Code = "00001",
                    CoaBalanceArray = [new CoaBalance { Coa = "12501", Currency = "000", Date = "2026-05-26T00:00:00Z", StartBalance = 1500000.50m, Debit = 0m, Credit = 2m, EndBalance = 3m }]
                }
            ]
        };
        _repository.Setup(r => r.GetBalanceReportRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 7, Request = report }]);
        string? sent = null;
        _requestManager.Setup(r => r.SendPostRequest("https://bureau.test/balance", It.IsAny<string>(), 7, It.IsAny<CancellationToken>()))
            .Callback<string, string, int, CancellationToken>((_, json, _, _) => sent = json)
            .ReturnsAsync(SuccessResponse);

        var result = await Ci024().ProcessAsync(CancellationToken.None);

        Assert.Equal(24, Ci024().CiCode);
        Assert.Equal(1, result.Success);
        var compact = sent!.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        Assert.Contains("\"pHead\":\"014\"", compact);
        Assert.Contains("\"pDate\":\"2026-06-01T00:00:00Z\"", compact);
        Assert.Contains("\"pDepartmentArray\":[{\"code\":\"00001\",\"coaBalanceArray\":[{\"coa\":\"12501\"", compact);
        Assert.Contains("\"startBalance\":1500000.50", compact);
        Assert.Contains("\"currency\":\"000\"", compact);
        _repository.Verify(r => r.UpsertCiStatusAsync(7, 24, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci025_SendsGuaranteeFieldsWithStandardHeaderAndToday()
    {
        _repository.Setup(r => r.GetGuaranteeRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 9, Request = new CreditRegistrationGuarantee { PClaimId = "c1", PTin = "123456789", PCreditAmount = 100.25m, PGuaranteeInfo = "info" } }]);
        string? sent = null;
        _requestManager.Setup(r => r.SendPostRequest("https://bureau.test/guarantee", It.IsAny<string>(), 9, It.IsAny<CancellationToken>()))
            .Callback<string, string, int, CancellationToken>((_, json, _, _) => sent = json)
            .ReturnsAsync(SuccessResponse);

        var result = await Ci025().ProcessAsync(CancellationToken.None);

        Assert.Equal(25, Ci025().CiCode);
        Assert.Equal(1, result.Success);
        var compact = sent!.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        Assert.Contains("\"pHead\":\"014\"", compact);
        Assert.Contains("\"pCode\":\"01234\"", compact);
        Assert.Contains("\"pClaimId\":\"c1\"", compact);
        Assert.Contains("\"pTin\":\"123456789\"", compact);
        Assert.Contains("\"pCreditAmount\":100.25", compact);
        Assert.Contains("\"pGuaranteeInfo\":\"info\"", compact);
        Assert.Contains($"\"pDate\":\"{DateTime.Now:yyyy-MM-dd}T00:00:00.000+0500\"", compact);
    }

    [Fact]
    public async Task EmptyUrl_SkipsWithoutReadingQueue()
    {
        _apiOptions.BalanceReportUrl = "";
        _apiOptions.GuaranteeUrl = "";

        var r24 = await Ci024().ProcessAsync(CancellationToken.None);
        var r25 = await Ci025().ProcessAsync(CancellationToken.None);

        Assert.Equal(0, r24.Processed);
        Assert.Equal(0, r25.Processed);
        _repository.Verify(r => r.GetBalanceReportRequestsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.GetGuaranteeRequestsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _requestManager.VerifyNoOtherCalls();
    }
}
