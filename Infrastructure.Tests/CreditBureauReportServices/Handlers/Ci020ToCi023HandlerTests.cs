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

public class Ci020ToCi023HandlerTests
{
    private static (
        Mock<IRequestManagerService> RequestManager,
        Mock<ICreditBureauReportRepository> Repository,
        CreditBureauReportApiOptions ReportOptions,
        CreditBureauApiOptions ApiOptions,
        RequestSecurity Security,
        BankHeader BankHeader,
        LogWriter LogWriter,
        Mock<ITelegramNotificationService> Telegram) CreateDeps()
    {
        var requestManager = new Mock<IRequestManagerService>();
        var repository = new Mock<ICreditBureauReportRepository>();
        var telegram = new Mock<ITelegramNotificationService>();
        var reportOptions = new CreditBureauReportApiOptions { PHead = "head", PCode = "code" };
        var apiOptions = new CreditBureauApiOptions
        {
            HostAddress = "https://bureau.test",
            CreditPledgeOwnerUrl = "/credit/pledge-owner",
            CreditPledgeSecurityUrl = "/credit/pledge-security",
            CreditRegistrationBusinessDetailsUrl = "/credit/business-details",
            CreditRegistrationSubjectUrl = "/credit/subject"
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

        return (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram);
    }

    private const string SuccessResponse = """{"result":"00000","resultMessage":"ok"}""";
    private const string ErrorResponse = """{"result":"05555","resultMessage":"Freeze active"}""";

    // ---- CI-020 ----

    [Fact]
    public async Task Ci020_ProcessAsync_ReportsCiCodeTwenty()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci020PledgeOwnerRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci020PledgeOwnerRequestHandler>>().Object);
        repository.Setup(r => r.CreditRegistrationPledgeOwnerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Assert.Equal(20, sut.CiCode);
        await sut.ProcessAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Ci020_ProcessAsync_WhenRequestIsNull_MarksErrorAndNotifies()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci020PledgeOwnerRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci020PledgeOwnerRequestHandler>>().Object);
        repository.Setup(r => r.CreditRegistrationPledgeOwnerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 1, Request = null! }]);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(1, 20, 2, "CI-020 request is null", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-020 null request", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci020_ProcessAsync_WhenBureauReturnsSuccess_MarksSuccess()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci020PledgeOwnerRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci020PledgeOwnerRequestHandler>>().Object);
        repository.Setup(r => r.CreditRegistrationPledgeOwnerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 2, Request = new CreditRegistrationPledgeOwner() }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/pledge-owner", It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 20, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci020_ProcessAsync_WhenBureauReturnsErrorResult_MarksErrorAndNotifies()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci020PledgeOwnerRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci020PledgeOwnerRequestHandler>>().Object);
        repository.Setup(r => r.CreditRegistrationPledgeOwnerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 3, Request = new CreditRegistrationPledgeOwner() }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/pledge-owner", It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 20, 2, "Freeze active", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-020 API error", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- CI-021 ----

    [Fact]
    public async Task Ci021_ProcessAsync_ReportsCiCodeTwentyOne()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci021PledgeSecurityRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci021PledgeSecurityRequestHandler>>().Object);
        repository.Setup(r => r.GetPledgeSecurityRequestsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Assert.Equal(21, sut.CiCode);
        await sut.ProcessAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Ci021_ProcessAsync_WhenRequestIsNull_MarksErrorAndNotifies()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci021PledgeSecurityRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci021PledgeSecurityRequestHandler>>().Object);
        repository.Setup(r => r.GetPledgeSecurityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 1, Request = null! }]);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(1, 21, 2, "CI-021 request is null", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-021 null request", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci021_ProcessAsync_WhenBureauReturnsSuccess_MarksSuccess()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci021PledgeSecurityRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci021PledgeSecurityRequestHandler>>().Object);
        repository.Setup(r => r.GetPledgeSecurityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 2, Request = new CreditRegistrationPledgeSecurity() }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/pledge-security", It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 21, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci021_ProcessAsync_WhenBureauReturnsErrorResult_MarksErrorAndNotifies()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci021PledgeSecurityRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci021PledgeSecurityRequestHandler>>().Object);
        repository.Setup(r => r.GetPledgeSecurityRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 3, Request = new CreditRegistrationPledgeSecurity() }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/pledge-security", It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 21, 2, "Freeze active", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-021 API error", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- CI-022 ----

    [Fact]
    public async Task Ci022_ProcessAsync_ReportsCiCodeTwentyTwo()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci022BusinessDetailRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci022BusinessDetailRequestHandler>>().Object);
        repository.Setup(r => r.GetCreditRegistrationBusinessDetailsRequestsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Assert.Equal(22, sut.CiCode);
        await sut.ProcessAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Ci022_ProcessAsync_WhenRequestIsNull_MarksErrorAndNotifies()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci022BusinessDetailRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci022BusinessDetailRequestHandler>>().Object);
        repository.Setup(r => r.GetCreditRegistrationBusinessDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 1, Request = null! }]);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(1, 22, 2, "CI-022 request is null", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-022 null request", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci022_ProcessAsync_WhenBureauReturnsSuccess_MarksSuccess()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci022BusinessDetailRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci022BusinessDetailRequestHandler>>().Object);
        repository.Setup(r => r.GetCreditRegistrationBusinessDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 2, Request = new CreditRegistrationBusinessDetailRequest { PRepaymentDetArray = [] } }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/business-details", It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 22, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci022_ProcessAsync_WhenBureauReturnsErrorResult_MarksErrorAndNotifies()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci022BusinessDetailRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci022BusinessDetailRequestHandler>>().Object);
        repository.Setup(r => r.GetCreditRegistrationBusinessDetailsRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 3, Request = new CreditRegistrationBusinessDetailRequest { PRepaymentDetArray = [] } }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/business-details", It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 22, 2, "Freeze active", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-022 API error", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- CI-023 ----

    [Fact]
    public async Task Ci023_ProcessAsync_ReportsCiCodeTwentyThree()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci023SubjectRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci023SubjectRequestHandler>>().Object);
        repository.Setup(r => r.GetCreditRegistrationSubjectRequestsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Assert.Equal(23, sut.CiCode);
        await sut.ProcessAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Ci023_ProcessAsync_WhenRequestIsNull_MarksErrorAndNotifies()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci023SubjectRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci023SubjectRequestHandler>>().Object);
        repository.Setup(r => r.GetCreditRegistrationSubjectRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 1, Request = null! }]);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(1, 23, 2, "CI-023 request is null", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-023 null request", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci023_ProcessAsync_WhenBureauReturnsSuccess_MarksSuccess()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci023SubjectRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci023SubjectRequestHandler>>().Object);
        repository.Setup(r => r.GetCreditRegistrationSubjectRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 2, Request = new CreditRegistrationSubjectRequest() }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/subject", It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 23, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci023_ProcessAsync_WhenBureauReturnsErrorResult_MarksErrorAndNotifies()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci023SubjectRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci023SubjectRequestHandler>>().Object);
        repository.Setup(r => r.GetCreditRegistrationSubjectRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { LoanKey = 3, Request = new CreditRegistrationSubjectRequest() }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/subject", It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 23, 2, "Freeze active", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-023 API error", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
