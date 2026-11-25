using Application.Repositories.CreditBureauReportRepositories;
using Application.Repositories.Helpers;
using Application.Repositories.RequestManager;
using CreditBureauService.Contracts.CreditBureauApplications;
using CreditBureauService.Contracts.Common;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.CreditReportsXml;
using Infrastructure.CreditReportsXml.Parsers;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Moq;

namespace Infrastructure.Tests.CreditReportsXml;

public class CreditReportXmlServiceTests
{
    private static (
        CreditReportXmlService Sut,
        Mock<IRequestManagerService> RequestManager,
        Mock<IHelperRepository> Helper,
        Mock<IRequestManagerRepository> RequestManagerRepository,
        Mock<ICreditBureauReportRepository> Repository,
        Mock<ITelegramNotificationService> Telegram,
        CreditBureauReportApiOptions Options) CreateSut()
    {
        var requestManager = new Mock<IRequestManagerService>();
        var helper = new Mock<IHelperRepository>();
        var xmlParser = new Mock<ICreditReportXmlParser>();
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

        repository.Setup(r => r.IncrementCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.UpsertCiStatusAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.UpdateRequestHistoryStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        // Default: history key == loan key (tests that care about Appв†’Loan mapping override this).
        repository.Setup(r => r.GetLoanKeyByHistoryKbAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((int historyKey, CancellationToken _) => Task.FromResult<int?>(historyKey));
        repository.Setup(r => r.GetCreditBureau001StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetCreditBureau002StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)1);
        repository.Setup(r => r.GetLoanAppAndCustomerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));
        repository.Setup(r => r.InsertCi017RequestLogAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        requestManagerRepository.Setup(r => r.InsertRequestLog(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("0", "ok"));
        helper.Setup(h => h.KatmHelperXml(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IHelperRepository.TypeOperation>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        telegram.Setup(t => t.NotifyErrorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        xmlParser.Setup(p => p.ParseAndPersistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new CreditReportXmlService(
            requestManager.Object,
            options,
            security,
            logWriter,
            helper.Object,
            xmlParser.Object,
            requestManagerRepository.Object,
            repository.Object,
            telegram.Object);

        return (sut, requestManager, helper, requestManagerRepository, repository, telegram, options);
    }

    private const string WaitAndTryAgainResponse =
        """{"data":{"result":"05050","resultMessage":null,"reportBase64":null,"token":"tok-123"},"errorMessage":null,"code":0}""";

    private const string SuccessResponse =
        """{"data":{"result":"05000","resultMessage":"Success","reportBase64":"SGVsbG8gV29ybGQ=","token":null},"errorMessage":null,"code":0}""";

    [Fact]
    public async Task CreditReportXml_WhenAttemptsExhaustedUnderPreviousStatus_DoesNotResetCounterAndSkipsBureauCall()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "42", PClaimId = "claim-42", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCi017StateAsync(42, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 5, LastStatus: "01", LastAttemptAt: null));

        await sut.CreditReportXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "42", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()), Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(42, 17, 2, "Max attempts (3) reached", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportXml_WhenStatusChangedButAttemptsNotExhausted_DoesNotResetAndStillCallsBureau()
    {
        // The CI-017 attempt counter is never reset on a status change - the request
        // may be sent at most MaxCi017Attempts times per report subtype, regardless of status.
        // On 05050 we only save the token; status is polled later after CheckReportStatusInterval.
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "43", PClaimId = "claim-43", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCi017StateAsync(43, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "01", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        await sut.CreditReportXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "43", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()), Times.Once);
        repository.Verify(r => r.IncrementCi017AttemptAsync(43, It.IsAny<string?>(), "02", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportXml_WhenAttemptsAtNewLimitOfThree_DoesNotCallBureauAndMarksError()
    {
        var (sut, requestManager, _, _, repository, telegram, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "7", PClaimId = "claim-7", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCi017StateAsync(7, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "02", LastAttemptAt: null));

        await sut.CreditReportXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(7, 17, 2, "Max attempts (3) reached", null, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(7, "09", It.IsAny<CancellationToken>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-017 max attempts", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportXml_WhenAttemptsBelowNewLimitOfThree_StillCallsBureau()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "9", PClaimId = "claim-9", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCi017StateAsync(9, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 2, LastStatus: "02", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "9", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        await sut.CreditReportXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "9", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportXml_WhenAttemptsAtLimitCalledTwice_NotifiesTelegramOnlyOnce()
    {
        var (sut, _, _, _, repository, telegram, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "11", PClaimId = "claim-11", Status = "02", ApplicationsSubjectType = "0" };

        repository.Setup(r => r.GetCi017StateAsync(11, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "02", LastAttemptAt: null));

        await sut.CreditReportXml(application, CancellationToken.None);
        await sut.CreditReportXml(application, CancellationToken.None);

        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-017 max attempts", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportXml_WhenReceives05050WithToken_SavesTokenAndDoesNotCallStatusImmediately()
    {
        var (sut, requestManager, helper, _, repository, _, _) = CreateSut();
        var application = new LoanApplication
        {
            KeyCreditBureauKb = "44", PClaimId = "claim-44", Status = "02", ApplicationsSubjectType = "0",
            PReportId = "077", PReportReason = 2
        };

        repository.Setup(r => r.GetCi017StateAsync(44, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 0, LastStatus: "02", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "44", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), "077"))
            .ReturnsAsync(WaitAndTryAgainResponse);

        await sut.CreditReportXml(application, CancellationToken.None);

        Assert.Equal("tok-123", application.PToken);
        repository.Verify(r => r.UpsertCiStatusAsync(44, 17, 0, "Waiting", "tok-123", It.IsAny<CancellationToken>()), Times.Once);
        helper.Verify(h => h.KatmHelperXml("44", "tok-123", IHelperRepository.TypeOperation.Token,
            It.IsAny<CancellationToken>(), "077"), Times.Once);
        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "44", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), "077"), Times.Once);
    }

    [Fact]
    public async Task CreditReportXml_WhenReportSucceeds_SavesBase64ForThatReportSubtype()
    {
        var (sut, requestManager, helper, _, repository, _, _) = CreateSut();
        var application = new LoanApplication
        {
            KeyCreditBureauKb = "56", PClaimId = "claim-56", Status = "00", ApplicationsSubjectType = "0",
            PReportId = "024", PReportReason = 2
        };
        repository.Setup(r => r.GetCi017StateAsync(56, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(0, null, null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "56", IRequestManagerRepository.IsXml.Xml,
                It.IsAny<CancellationToken>(), "024"))
            .ReturnsAsync(SuccessResponse);

        await sut.CreditReportXml(application, CancellationToken.None);

        helper.Verify(h => h.KatmHelperXml("56", "SGVsbG8gV29ybGQ=", IHelperRepository.TypeOperation.Base64,
            It.IsAny<CancellationToken>(), "024"), Times.Once);
        repository.Verify(r => r.UpsertCiStatusAsync(56, 17, 1, "Success", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportXml_WhenTwoSubtypesShareLoan_SendsEachSubtypeOnce()
    {
        var (sut, requestManager, helper, _, repository, _, _) = CreateSut();
        var states = new Queue<Ci017State>(new[]
        {
            new Ci017State(0, null, null),
            new Ci017State(1, "00", DateTime.UtcNow)
        });
        repository.Setup(r => r.GetCi017StateAsync(57, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(states.Dequeue()));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "57", IRequestManagerRepository.IsXml.Xml,
                It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(SuccessResponse);
        var reports = new[] { "077", "024" }.Select(reportId => new LoanApplication
        {
            KeyCreditBureauKb = "57", PClaimId = "claim-57", Status = "00", ApplicationsSubjectType = "0",
            PReportId = reportId, PReportReason = 2
        });

        foreach (var report in reports)
            await sut.CreditReportXml(report, CancellationToken.None);

        foreach (var reportId in new[] { "077", "024" })
        {
            requestManager.Verify(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "57", IRequestManagerRepository.IsXml.Xml,
                It.IsAny<CancellationToken>(), reportId), Times.Once);
            helper.Verify(h => h.KatmHelperXml("57", "SGVsbG8gV29ybGQ=", IHelperRepository.TypeOperation.Base64,
                It.IsAny<CancellationToken>(), reportId), Times.Once);
        }
    }

    [Fact]
    public async Task CreditReportXml_WhenCi001NotConfirmedForIndividual_DoesNotCallBureauAndMarksRequestHistory09()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        const int historyKey = 50;
        const int loanKey = 9001;
        var application = new LoanApplication
        {
            KeyCreditBureauKb = historyKey.ToString(),
            PClaimId = "claim-50",
            Status = "02",
            ApplicationsSubjectType = "0"
        };

        repository.Setup(r => r.GetLoanKeyByHistoryKbAsync(historyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loanKey);
        repository.Setup(r => r.GetCreditBureau001StatusAsync(loanKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)null);

        await sut.CreditReportXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never);
        repository.Verify(r => r.GetCreditBureau001StatusAsync(loanKey, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetCreditBureau001StatusAsync(historyKey, It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(historyKey, "09", It.IsAny<CancellationToken>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        repository.Verify(r => r.GetCreditBureau002StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditReportXml_WhenCi002NotConfirmedForEntity_DoesNotCallBureauAndMarksRequestHistory09()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        const int historyKey = 51;
        const int loanKey = 9002;
        var application = new LoanApplication
        {
            KeyCreditBureauKb = historyKey.ToString(),
            PClaimId = "claim-51",
            Status = "02",
            ApplicationsSubjectType = "1"
        };

        repository.Setup(r => r.GetLoanKeyByHistoryKbAsync(historyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loanKey);
        repository.Setup(r => r.GetCreditBureau002StatusAsync(loanKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte?)0);

        await sut.CreditReportXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never);
        repository.Verify(r => r.GetCreditBureau002StatusAsync(loanKey, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetCreditBureau002StatusAsync(historyKey, It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(historyKey, "09", It.IsAny<CancellationToken>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        repository.Verify(r => r.GetCreditBureau001StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditReportXml_WhenLoanKeyCannotBeResolvedFromHistory_MarksRequestHistory09()
    {
        var (sut, requestManager, _, _, repository, _, _) = CreateSut();
        var application = new LoanApplication
        {
            KeyCreditBureauKb = "77",
            PClaimId = "claim-77",
            Status = "02",
            ApplicationsSubjectType = "0"
        };

        repository.Setup(r => r.GetLoanKeyByHistoryKbAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        await sut.CreditReportXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never);
        repository.Verify(r => r.GetCreditBureau001StatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(77, "09", It.IsAny<CancellationToken>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportStatusXml_WhenAttemptsAtNewLimitOfThree_DoesNotCallBureauAndUpdatesRequestHistoryStatus()
    {
        var (sut, requestManager, _, _, repository, telegram, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "23", PClaimId = "claim-23", Status = "03", PToken = "tok-23" };

        repository.Setup(r => r.GetCi017StateAsync(23, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 3, LastStatus: "03", LastAttemptAt: null));

        await sut.CreditReportStatusXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(23, 17, 2, "Max attempts (3) reached", null, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateRequestHistoryStatusAsync(23, "09", It.IsAny<CancellationToken>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-017 max attempts", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportStatusXml_WhenLastAttemptWithinConfiguredInterval_SkipsBureauCallEntirely()
    {
        var (sut, requestManager, _, _, repository, _, options) = CreateSut();
        options.CheckReportStatusInterval = 60000;
        var application = new LoanApplication { KeyCreditBureauKb = "20", PClaimId = "claim-20", Status = "03", PToken = "tok-20" };

        repository.Setup(r => r.GetCi017StateAsync(20, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "03", LastAttemptAt: DateTime.UtcNow.AddHours(5)));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await sut.CreditReportStatusXml(application, cts.Token);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never);
        repository.Verify(r => r.IncrementCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditReportStatusXml_WhenLastAttemptOutsideConfiguredInterval_CallsBureauOnce()
    {
        var (sut, requestManager, _, _, repository, _, options) = CreateSut();
        options.CheckReportStatusInterval = 60000;
        var application = new LoanApplication { KeyCreditBureauKb = "21", PClaimId = "claim-21", Status = "03", PToken = "tok-21" };

        repository.Setup(r => r.GetCi017StateAsync(21, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 1, LastStatus: "03", LastAttemptAt: DateTime.UtcNow.AddHours(5).AddSeconds(-70)));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "21", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await sut.CreditReportStatusXml(application, cts.Token);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "21", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()), Times.Once);
        repository.Verify(r => r.IncrementCi017AttemptAsync(21, It.IsAny<string?>(), "03", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditReportStatusXml_WhenNoTokenReceivedYet_DoesNotCallBureau()
    {
        var (sut, requestManager, helper, _, repository, _, _) = CreateSut();
        var application = new LoanApplication { KeyCreditBureauKb = "24", PClaimId = "claim-24", Status = "02", PToken = null, PReportId = "077" };

        repository.Setup(r => r.GetCi017StateAsync(24, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 0, LastStatus: null, LastAttemptAt: null));

        await sut.CreditReportStatusXml(application, CancellationToken.None);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IRequestManagerRepository.IsXml>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never);
        repository.Verify(r => r.IncrementCi017AttemptAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        helper.Verify(h => h.KatmHelperXml("24", It.Is<string>(message => message.Contains("ожидается получение токена")),
            IHelperRepository.TypeOperation.Comment, It.IsAny<CancellationToken>(), "077"), Times.Once);
    }

    [Fact]
    public async Task CreditReportStatusXml_WhenBureauKeepsRespondingWaitAndTryAgain_CallsBureauExactlyOnce()
    {
        var (sut, requestManager, helper, _, repository, _, options) = CreateSut();
        options.CheckReportStatusInterval = 10;
        var application = new LoanApplication { KeyCreditBureauKb = "22", PClaimId = "claim-22", Status = "03", PToken = "tok-22", PReportId = "077" };

        repository.Setup(r => r.GetCi017StateAsync(22, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ci017State(AttemptCount: 0, LastStatus: "03", LastAttemptAt: null));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), "22", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .ReturnsAsync(WaitAndTryAgainResponse);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await sut.CreditReportStatusXml(application, cts.Token);

        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), "22", IRequestManagerRepository.IsXml.Xml, It.IsAny<CancellationToken>(), It.IsAny<string?>()), Times.Once);
        helper.Verify(h => h.KatmHelperXml("22", "1", IHelperRepository.TypeOperation.AddNextAccess,
            It.IsAny<CancellationToken>(), "077"), Times.Once);
    }
}
