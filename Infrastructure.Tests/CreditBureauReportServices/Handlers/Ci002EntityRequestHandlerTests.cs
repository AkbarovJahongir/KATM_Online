using Application.Repositories.CreditBureauReportRepositories;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;
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

public class Ci002EntityRequestHandlerTests
{
    private static (
        Ci002EntityRequestHandler Sut,
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
            LegalEntityApplicationUrl = "/credit/registration/entity"
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

        var sut = new Ci002EntityRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci002EntityRequestHandler>>().Object);

        return (sut, requestManager, repository, telegram);
    }

    [Fact]
    public async Task ProcessAsync_ReportsCiCodeTwo()
    {
        var (sut, _, repository, _) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationEntityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Assert.Equal(2, sut.CiCode);
        await sut.ProcessAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ProcessAsync_WhenRequestIsNull_MarksErrorAndNotifiesWithoutCallingBureau()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationEntityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 1, Request = null! }]);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-002 null request", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenBureauReturnsSuccess_MarksSuccessWithKatmSirToken()
    {
        var (sut, requestManager, repository, _) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationEntityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 2, Request = new CreditRegistrationEntityRequest { ClaimId = "claim-2" } }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/entity", It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":{"code":"00000","message":"ok"},"header":{},"response":{"claim_id":"claim-2","katm_sir":"sir-2"}}""");

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 2, 1, "ok", "sir-2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenBureauReturnsErrorCode_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationEntityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 3, Request = new CreditRegistrationEntityRequest { ClaimId = "claim-3" } }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/entity", It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":{"code":"05555","message":"Freeze active"},"header":{},"response":{}}""");

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 2, 2, "Freeze active", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-002 API error", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenResponseIsEmpty_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationEntityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 4, Request = new CreditRegistrationEntityRequest { ClaimId = "claim-4" } }]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/entity", It.IsAny<string>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(4, 2, 2, "CI-002 returned empty response", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-002 empty response", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenSendPostRequestThrows_MarksErrorAndNotifiesWithoutStoppingQueue()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        repository.Setup(r => r.GetCreditRegistrationEntityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new() { LoanKey = 5, Request = new CreditRegistrationEntityRequest { ClaimId = "claim-5" } },
                new() { LoanKey = 6, Request = new CreditRegistrationEntityRequest { ClaimId = "claim-6" } }
            ]);
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/entity", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        requestManager.Setup(r => r.SendPostRequest(
                "https://bureau.test/credit/registration/entity", It.IsAny<string>(), 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":{"code":"00000","message":"ok"},"header":{},"response":{"katm_sir":"sir-6"}}""");

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(2, result.Processed);
        Assert.Equal(1, result.Error);
        Assert.Equal(1, result.Success);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-002 processing exception", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
