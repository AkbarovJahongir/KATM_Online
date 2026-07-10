using Application.Repositories.CreditBureauReportRepositories;
using Application.Repositories.Helpers;
using Application.Repositories.RequestManager;
using CreditBureauService.Contracts.CreditBureauApplications;
using CreditBureauService.Contracts.Common;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.CreditReports;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Moq;

namespace Infrastructure.Tests.CreditReports;

public class CreditReportServiceTests
{
    private static (
        CreditReportService Sut,
        Mock<IRequestManagerService> RequestManager,
        Mock<IHelperRepository> Helper,
        Mock<IRequestManagerRepository> RequestManagerRepository,
        Mock<ICreditBureauReportRepository> Repository,
        Mock<ITelegramNotificationService> Telegram,
        CreditBureauReportApiOptions Options) CreateSut()
    {
        var requestManager = new Mock<IRequestManagerService>();
        var helper = new Mock<IHelperRepository>();
        var requestManagerRepository = new Mock<IRequestManagerRepository>();
        var repository = new Mock<ICreditBureauReportRepository>();
        var telegram = new Mock<ITelegramNotificationService>();
        var options = new CreditBureauReportApiOptions
        {
            HostAddress = "https://bureau.test",
            ReportUrl = "/credit/report",
            ReportStatusUrl = "/credit/report/status",
            PHead = "head",
            PCode = "code",
            PLogin = "login",
            PPassword = "password",
            CheckReportStatusInterval = 60000,
            ReportTimeToLiveInterval = 0
        };
        var security = new RequestSecurity { pLogin = "login", pPassword = "password" };
        var logWriter = new LogWriter(Path.GetTempPath(), false);

        repository.Setup(r => r.ResetCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.IncrementCi017AttemptAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.UpsertCiStatusAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.GetLoanAppAndCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));
        requestManagerRepository.Setup(r => r.InsertRequestLog(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("0", "ok"));
        helper.Setup(h => h.KatmHelper(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IHelperRepository.TypeOperation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        telegram.Setup(t => t.NotifyErrorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new CreditReportService(
            requestManager.Object,
            options,
            security,
            logWriter,
            helper.Object,
            requestManagerRepository.Object,
            repository.Object,
            telegram.Object);

        return (sut, requestManager, helper, requestManagerRepository, repository, telegram, options);
    }

    private const string WaitAndTryAgainResponse =
        """{"data":{"result":"05050","resultMessage":null,"reportBase64":null,"token":"tok-123"},"errorMessage":null,"code":0}""";

    [Fact]
    public async Task CreditReport_WhenStatusChangedSincePreviousRun_ResetsAttemptCounterBeforeEvaluatingLimit()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "42", PClaimId = "claim-42", Status = "02" };

        repository.Setup(r => r.GetCi017StateAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 5, LastStatus: "01", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "42", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        await sut.CreditReport(application, CancellationToken.None);

        repository.Verify(r => r.ResetCi017AttemptAsync(42, "02", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpsertCiStatusAsync(
            42, 17, 0, It.Is<string?>(m => m != null && m.Contains("reset")), null, It.IsAny<CancellationToken>()), Times.Once);
        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "42", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReport_WhenAttemptsAtNewLimitOfThree_DoesNotCallBureauAndMarksError()
    {
        var (sut, requestManager, _, _, repository, telegram, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "7", PClaimId = "claim-7", Status = "02" };

        repository.Setup(r => r.GetCi017StateAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "02", LastAttemptAt: null));

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(7, 17, 2, "Max attempts (3) reached", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-017 max attempts", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReport_WhenAttemptsBelowNewLimitOfThree_StillCallsBureau()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "9", PClaimId = "claim-9", Status = "02" };

        repository.Setup(r => r.GetCi017StateAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 2, LastStatus: "02", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "9", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "9", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReport_WhenAttemptsAtLimitCalledTwice_NotifiesTelegramOnlyOnce()
    {
        var (sut, _, _, _, repository, telegram, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "11", PClaimId = "claim-11", Status = "02" };

        repository.Setup(r => r.GetCi017StateAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "02", LastAttemptAt: null));

        await sut.CreditReport(application, CancellationToken.None);
        await sut.CreditReport(application, CancellationToken.None);

        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-017 max attempts", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportStatus_WhenLastAttemptWithinConfiguredInterval_SkipsBureauCallEntirely()
    {
        var (sut, requestManager, _, _, repository, _, options) = CreateSut();
        options.CheckReportStatusInterval = 60000;
        var application = new LoanApplication { KeyCreditBureauKb = "20", PClaimId = "claim-20", Status = "03", PToken = "tok-20" };

        repository.Setup(r => r.GetCi017StateAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "03", LastAttemptAt: DateTime.UtcNow));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await sut.CreditReportStatus(application, cts.Token);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.IncrementCi017AttemptAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditReportStatus_WhenLastAttemptOutsideConfiguredInterval_CallsBureauOnce()
    {
        var (sut, requestManager, _, _, repository, _, options) = CreateSut();
        options.CheckReportStatusInterval = 60000;
        var application = new LoanApplication { KeyCreditBureauKb = "21", PClaimId = "claim-21", Status = "03", PToken = "tok-21" };

        repository.Setup(r => r.GetCi017StateAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "03", LastAttemptAt: DateTime.UtcNow.AddSeconds(-70)));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "21", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await sut.CreditReportStatus(application, cts.Token);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "21", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.IncrementCi017AttemptAsync(21, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportStatus_WhenBureauKeepsRespondingWaitAndTryAgain_CallsBureauExactlyOnce()
    {
        var (sut, requestManager, _, _, repository, _, options) = CreateSut();
        options.CheckReportStatusInterval = 10;
        var application = new LoanApplication { KeyCreditBureauKb = "22", PClaimId = "claim-22", Status = "03", PToken = "tok-22" };

        repository.Setup(r => r.GetCi017StateAsync(22, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 0, LastStatus: "03", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "22", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await sut.CreditReportStatus(application, cts.Token);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "22", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
    }
}
