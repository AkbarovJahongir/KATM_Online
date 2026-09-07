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

        repository.Setup(r => r.IncrementCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.UpsertCiStatusAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.UpdateRequestHistoryStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
    public async Task CreditReport_WhenAttemptsExhaustedUnderPreviousStatus_DoesNotResetCounterAndSkipsBureauCall()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "42", PClaimId = "claim-42", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 5, LastStatus: "01", LastAttemptAt: null));

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "42", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(42, 17, 2, "Max attempts (3) reached", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReport_WhenStatusChangedButAttemptsNotExhausted_DoesNotResetAndStillCallsBureau()
    {
        // The CI-017 attempt counter is never reset on a status change - the request
        // may be sent at most MaxCi017Attempts times per loan, regardless of status.
        // On 05050 we only save the token; status is polled later after CheckReportStatusInterval.
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "43", PClaimId = "claim-43", Status = "02", PToken = "tok-123", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(43, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(43, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "01", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.IncrementCi017AttemptAsync(43, "02", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReport_When05050ReturnsToken_SavesTokenAndDoesNotCallStatusImmediately()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication
        {
            KeyCreditBureauKb = "55",
            PClaimId = "claim-55",
            Status = "00",
            PToken = null,
            ApplicationsSubjectType = "0"
        };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 0, LastStatus: "00", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "55", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        await sut.CreditReport(application, CancellationToken.None);

        Assert.Equal("tok-123", application.PToken);
        repository.Verify(r => r.UpsertCiStatusAsync(55, 17, 0, "Waiting", "tok-123", It.IsAny<CancellationToken>()), Times.Once);
        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "55", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReport_WhenAttemptsAtNewLimitOfThree_DoesNotCallBureauAndMarksError()
    {
        var (sut, requestManager, _, _, repository, telegram, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "7", PClaimId = "claim-7", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "02", LastAttemptAt: null));

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(7, 17, 2, "Max attempts (3) reached", null, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(7, "09", It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-017 max attempts", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReport_WhenAttemptsBelowNewLimitOfThree_StillCallsBureau()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "9", PClaimId = "claim-9", Status = "02", PToken = "tok-123", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "02", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "9", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "9", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.IncrementCi017AttemptAsync(9, "02", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReport_WhenAttemptsAtLimitCalledTwice_NotifiesTelegramOnlyOnce()
    {
        var (sut, _, _, _, repository, telegram, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "11", PClaimId = "claim-11", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCi017StateAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "02", LastAttemptAt: null));

        await sut.CreditReport(application, CancellationToken.None);
        await sut.CreditReport(application, CancellationToken.None);

        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-017 max attempts", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportStatus_WhenAttemptsAtNewLimitOfThree_DoesNotCallBureauAndUpdatesRequestHistoryStatus()
    {
        var (sut, requestManager, _, _, repository, telegram, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "23", PClaimId = "claim-23", Status = "03", PToken = "tok-23" };

        repository.Setup(r => r.GetCi017StateAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "03", LastAttemptAt: null));

        await sut.CreditReportStatus(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(23, 17, 2, "Max attempts (3) reached", null, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(23, "09", It.IsAny<CancellationToken>()), Times.Once);
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
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "03", LastAttemptAt: DateTime.UtcNow.AddHours(5)));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await sut.CreditReportStatus(application, cts.Token);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.IncrementCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditReportStatus_WhenLastAttemptOutsideConfiguredInterval_CallsBureauOnce()
    {
        var (sut, requestManager, _, _, repository, _, options) = CreateSut();
        options.CheckReportStatusInterval = 60000;
        var application = new LoanApplication { KeyCreditBureauKb = "21", PClaimId = "claim-21", Status = "03", PToken = "tok-21" };

        repository.Setup(r => r.GetCi017StateAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "03", LastAttemptAt: DateTime.UtcNow.AddHours(5).AddSeconds(-70)));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "21", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await sut.CreditReportStatus(application, cts.Token);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "21", IRequestManagerRepository.IsXml.NotXml, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.IncrementCi017AttemptAsync(21, "03", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportStatus_WhenNoTokenReceivedYet_DoesNotCallBureau()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "24", PClaimId = "claim-24", Status = "02", PToken = null };

        repository.Setup(r => r.GetCi017StateAsync(24, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 0, LastStatus: null, LastAttemptAt: null));

        await sut.CreditReportStatus(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.IncrementCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
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

    [Fact]
    public async Task CreditReport_WhenCi001NotConfirmedForIndividual_DoesNotCallBureauAndMarksRequestHistory09()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication
        {
            KeyCreditBureauKb = "50",
            PClaimId = "claim-50",
            Status = "02",
            ApplicationsSubjectType = "0"
        };

        repository.Setup(r => r.GetCreditBureau001StatusAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)null);

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(50, "09", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetCreditBureau002StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditReport_WhenCi002NotConfirmedForEntity_DoesNotCallBureauAndMarksRequestHistory09()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication
        {
            KeyCreditBureauKb = "51",
            PClaimId = "claim-51",
            Status = "02",
            ApplicationsSubjectType = "1"
        };

        repository.Setup(r => r.GetCreditBureau002StatusAsync(51, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)0);

        await sut.CreditReport(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(51, "09", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetCreditBureau001StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
