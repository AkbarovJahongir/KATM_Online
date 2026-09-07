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

public class Ci011ToCi014HandlerTests
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
            CreditLeasingUrl = "/credit/leasing",
            LeasingScheduleUrl = "/credit/leasing/schedule",
            LeasingRepaymentUrl = "/credit/leasing/repayment",
            CreditFactroingUrl = "/credit/factoring"
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

    [Fact]
    public async Task Ci011_ProcessAsync_UsesLeasingQueueAndCreditLeasingUrl()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci011LeasingRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci011LeasingRequestHandler>>().Object);

        repository.Setup(r => r.GetCreditRegistrationLeasingRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CreditBureauReportQueueItem<CreditRegistrationLeasingRequest>
            {
                LoanKey = 1, Request = new CreditRegistrationLeasingRequest()
            }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/leasing", It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(11, sut.CiCode);
        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(1, 11, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci012_ProcessAsync_UsesLeasingScheduleQueueAndLeasingScheduleUrl()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci012LeasingRepaymentScheduleHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci012LeasingRepaymentScheduleHandler>>().Object);

        repository.Setup(r => r.GetLeasingRepaymentSchedulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CreditBureauReportQueueItem<CreditRegistrationLeasingRepaymentSchedule>
            {
                LoanKey = 2, Request = new CreditRegistrationLeasingRepaymentSchedule()
            }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/leasing/schedule", It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(12, sut.CiCode);
        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 12, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci013_ProcessAsync_UsesLeasingRepaymentObjectsQueueAndLeasingRepaymentUrl()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci013LeasingRepaymentHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci013LeasingRepaymentHandler>>().Object);

        repository.Setup(r => r.GetLeasingRepaymentObjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CreditBureauReportQueueItem<CreditRegistrationLeasingRepayment>
            {
                LoanKey = 3, Request = new CreditRegistrationLeasingRepayment()
            }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/leasing/repayment", It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(13, sut.CiCode);
        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 13, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ci014_ProcessAsync_UsesFactoringQueueAndCreditFactoringUrl()
    {
        var (requestManager, repository, reportOptions, apiOptions, security, bankHeader, logWriter, telegram) = CreateDeps();
        var sut = new Ci014FactoringRequestHandler(
            repository.Object, requestManager.Object, reportOptions, apiOptions, security, bankHeader, logWriter,
            telegram.Object, new Mock<ILogger<Ci014FactoringRequestHandler>>().Object);

        repository.Setup(r => r.GetCreditRegistrationFactoringRequestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CreditBureauReportQueueItem<CreditRegistrationFactoring>
            {
                LoanKey = 4, Request = new CreditRegistrationFactoring()
            }]);
        requestManager.Setup(r => r.SendPostRequest("https://bureau.test/credit/factoring", It.IsAny<string>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessResponse);

        var result = await sut.ProcessAsync(CancellationToken.None);

        Assert.Equal(14, sut.CiCode);
        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(4, 14, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
