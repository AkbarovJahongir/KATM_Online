using Application.Repositories.CreditBureauReportRepositories;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureauService.Contracts.Common;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.CreditBureauReportServices.Handlers;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;

namespace Infrastructure.Tests.CreditBureauReportServices.Handlers;

public class Ci016BankDetailRequestHandlerTests
{
    private static (
        Ci016BankDetailRequestHandler Sut,
        Mock<IRequestManagerService> RequestManager,
        Mock<ICreditBureauReportRepository> Repository,
        Mock<ITelegramNotificationService> Telegram) CreateSut()
    {
        var requestManager = new Mock<IRequestManagerService>();
        var repository = new Mock<ICreditBureauReportRepository>();
        var telegram = new Mock<ITelegramNotificationService>();
        var reportOptions = new CreditBureauReportApiOptions { PHead = "head", PCode = "code" };
        var apiOptions = new CreditBureauApiOptions
        {
            HostAddress = "https://bureau.test",
            CreditRegistrationRepaymentBankDitailUrl = "/credit/registration/bank-detail"
        };
        var security = new RequestSecurity { pLogin = "login", pPassword = "password" };
        var bankHeader = new BankHeader { Type = "type", Code = "code", Head = "head" };
        var logWriter = new LogWriter(Path.GetTempPath(), false);

        repository.Setup(r => r.UpsertCiStatusAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.GetLoanAppAndCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));
        telegram.Setup(t => t.NotifyErrorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new Ci016BankDetailRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci016BankDetailRequestHandler>>().Object);

        return (sut, requestManager, repository, telegram);
    }

    private static CreditRegistrationBankDitailRequest NewRequest() => new() { PRepaymentDetArray = [] };

    private const string SuccessResponse = """{"result":"00000","resultMessage":"ok"}""";

    [Fact]
    public async Task ProcessAsync_ReportsCiCodeSixteen()
    {
        var (sut, _, repository, _) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationBankDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Assert.Equal(16, sut.CiCode);
        await sut.ProcessAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ProcessAsync_WhenRequestIsNull_MarksErrorAndNotifiesWithoutCallingBureau()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationBankDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 1, Request = null! }]);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(1, 16, 2, "CI-016 request is null", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-016 null request", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenBureauReturnsSuccess_MarksSuccessWithoutNotifying()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationBankDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 2, Request = NewRequest() }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/bank-detail", It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 16, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenBureauReturnsErrorResult_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationBankDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 3, Request = NewRequest() }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/bank-detail", It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":"05555","resultMessage":"Freeze active"}""");

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 16, 2, "Freeze active", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-016 API error", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenResponseIsEmpty_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationBankDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 4, Request = NewRequest() }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/bank-detail", It.IsAny<string>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(4, 16, 2, "CI-016 returned empty response", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-016 empty response", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenSendPostRequestThrows_MarksErrorAndNotifiesWithoutStoppingQueue()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationBankDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new() { LoanKey = 5, Request = NewRequest() },
                new() { LoanKey = 6, Request = NewRequest() }
            ]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/bank-detail", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/bank-detail", It.IsAny<string>(), 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(2, result.Processed);
        Assert.Equal(1, result.Error);
        Assert.Equal(1, result.Success);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-016 processing exception", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendByPeriodAsync_UsesPeriodQueueWithGivenDatesAndLoanKey()
    {
        var (sut, requestManager, repository, _) = CreateSut();
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);
        repository.Setup(r => r.GetCreditRegistrationBankDetailsRequestsByPeriodAsync(start, end, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 7, Request = NewRequest() }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/bank-detail", It.IsAny<string>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.SendByPeriodAsync(start, end, 7, CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.GetCreditRegistrationBankDetailsRequestsByPeriodAsync(start, end, 7, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpsertCiStatusAsync(7, 16, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
