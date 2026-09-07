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

public class Ci018AccountStatusRequestHandlerTests
{
    private static (
        Ci018AccountStatusRequestHandler Sut,
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
            AccountStatusUrl = "/credit/registration/account-status"
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

        var sut = new Ci018AccountStatusRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci018AccountStatusRequestHandler>>().Object);

        return (sut, requestManager, repository, telegram);
    }

    private static CreditRegistrationAccountStatus NewRequest() => new() { PAccountStatusesArray = [] };

    private const string SuccessResponse = """{"result":"00000","resultMessage":"ok"}""";

    [Fact]
    public async Task ProcessAsync_ReportsCiCodeEighteen()
    {
        var (sut, _, repository, _) = CreateSut();
        repository.Setup(r => r.GetAccountStatusRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Assert.Equal(18, sut.CiCode);
        await sut.ProcessAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ProcessAsync_WhenRequestIsNull_MarksErrorAndNotifiesWithoutCallingBureau()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetAccountStatusRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 1, Request = null! }]);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(1, 18, 2, "CI-018 request is null", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-018 null request", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenBureauReturnsSuccess_MarksSuccessWithoutNotifying()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetAccountStatusRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 2, Request = NewRequest() }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/account-status", It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 18, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenBureauReturnsErrorResult_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetAccountStatusRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 3, Request = NewRequest() }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/account-status", It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":"05555","resultMessage":"Freeze active"}""");

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 18, 2, "Freeze active", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-018 API error", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenResponseIsEmpty_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetAccountStatusRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 4, Request = NewRequest() }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/account-status", It.IsAny<string>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(4, 18, 2, "CI-018 returned empty response", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-018 empty response", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenSendPostRequestThrows_MarksErrorAndNotifiesWithoutStoppingQueue()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetAccountStatusRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new() { LoanKey = 5, Request = NewRequest() },
                new() { LoanKey = 6, Request = NewRequest() }
            ]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/account-status", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/account-status", It.IsAny<string>(), 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(2, result.Processed);
        Assert.Equal(1, result.Error);
        Assert.Equal(1, result.Success);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-018 processing exception", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendByPeriodAsync_UsesPeriodQueueWithGivenDatesAndLoanKey()
    {
        var (sut, requestManager, repository, _) = CreateSut();
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);
        repository.Setup(r => r.GetAccountStatusRequestsByPeriodAsync(start, end, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 7, Request = NewRequest() }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/account-status", It.IsAny<string>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.SendByPeriodAsync(start, end, 7, CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.GetAccountStatusRequestsByPeriodAsync(start, end, 7, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpsertCiStatusAsync(7, 18, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
