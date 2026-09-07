using Application.Repositories.CreditBureauReportRepositories;
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

/// <summary>
/// Minimal concrete subclass exposing the protected ProcessCiRequestsAsync/ProcessResponseAsync
/// pipeline shared by Ci011-Ci014 (and reused here to characterize the shared control flow).
/// </summary>
internal sealed class TestCiHandler(
    ICreditBureauReportRepository creditBureauReportRepository,
    IRequestManagerService requestManagerService,
    CreditBureauReportApiOptions creditBureauReportApiOptions,
    CreditBureauApiOptions creditBureauApiOptions,
    RequestSecurity requestSecurity,
    BankHeader bankHeader,
    LogWriter logWriter,
    ITelegramNotificationService telegramNotificationService,
    ILogger<CiHandlerBase<string>> logger)
    : CiHandlerBase<string>(
        creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
        requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
{
    public override int CiCode => 999;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException("Use InvokeAsync in tests.");

    public Task<CiProcessingResult> InvokeAsync(
        List<CreditBureauReportQueueItem<string>> queue,
        CancellationToken cancellationToken = default) =>
        ProcessCiRequestsAsync(
            _ => Task.FromResult(queue),
            request => new BaseRequest<string> { Data = request, Security = requestSecurity },
            "https://bureau.test/ci-999",
            "TestCiHandler.txt",
            cancellationToken: cancellationToken);

    public static string FormatIsoStartOfDay(DateTimeOffset dateTime) =>
        FormatKatmIsoDateAtStartOfDay(dateTime);
}

public class CiHandlerBaseTests
{
    private static (
        TestCiHandler Sut,
        Mock<IRequestManagerService> RequestManager,
        Mock<ICreditBureauReportRepository> Repository,
        Mock<ITelegramNotificationService> Telegram) CreateSut()
    {
        var requestManager = new Mock<IRequestManagerService>();
        var repository = new Mock<ICreditBureauReportRepository>();
        var telegram = new Mock<ITelegramNotificationService>();
        var logger = new Mock<ILogger<CiHandlerBase<string>>>();
        var options = new CreditBureauReportApiOptions { PHead = "head", PCode = "code" };
        var apiOptions = new CreditBureauApiOptions { HostAddress = "https://bureau.test" };
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

        var sut = new TestCiHandler(
            repository.Object, requestManager.Object, options, apiOptions, security, bankHeader, logWriter,
            telegram.Object, logger.Object);

        return (sut, requestManager, repository, telegram);
    }

    private static CreditBureauReportQueueItem<string> Item(int loanKey, string? request) =>
        new() { LoanKey = loanKey, Request = request! };

    [Fact]
    public async Task ProcessAsync_WhenRequestIsNull_MarksErrorAndNotifiesWithoutCallingBureau()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();

        var result = await sut.InvokeAsync([Item(1, null)]);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Error);
        Assert.Equal(0, result.Success);
        requestManager.Verify(r => r.SendPostRequest(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.UpsertCiStatusAsync(1, 999, 2, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-999 null request", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenErrorNotified_TelegramMessageDoesNotContainSecurityCredentials()
    {
        var (sut, requestManager, _, telegram) = CreateSut();
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>not json</html>");

        string? capturedMessage = null;
        telegram.Setup(t => t.NotifyErrorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string?, string?, CancellationToken>((_, message, _, _, _) => capturedMessage = message)
            .Returns(Task.CompletedTask);

        await sut.InvokeAsync([Item(8, "payload")]);

        Assert.NotNull(capturedMessage);
        Assert.DoesNotContain("\"pPassword\":\"password\"", capturedMessage);
        Assert.Contains("\"security\":\"REDACTED\"", capturedMessage);
    }

    [Fact]
    public async Task ProcessAsync_WhenResponseIsEmpty_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var result = await sut.InvokeAsync([Item(2, "payload")]);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(2, 999, 2, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-999 empty response", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenResponseIsNotJson_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync("<html>not json</html>");

        var result = await sut.InvokeAsync([Item(3, "payload")]);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(3, 999, 2, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-999 invalid response format", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenResponseIsSuccess_MarksSuccessWithoutNotifying()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":"00000","resultMessage":"ok"}""");

        var result = await sut.InvokeAsync([Item(4, "payload")]);

        Assert.Equal(1, result.Success);
        Assert.Equal(0, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(4, 999, 1, "ok", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenBureauReturnsErrorResult_MarksErrorAndNotifies()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":"05555","resultMessage":"Freeze active"}""");

        var result = await sut.InvokeAsync([Item(5, "payload")]);

        Assert.Equal(1, result.Error);
        repository.Verify(r => r.UpsertCiStatusAsync(5, 999, 2, "Freeze active", null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-999 API error", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenSendPostRequestThrows_MarksErrorAndNotifiesWithoutStoppingQueue()
    {
        var (sut, requestManager, repository, telegram) = CreateSut();
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), 6, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        requestManager.Setup(r => r.SendPostRequest(
                It.IsAny<string>(), It.IsAny<string>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":"00000","resultMessage":"ok"}""");

        var result = await sut.InvokeAsync([Item(6, "payload"), Item(7, "payload")]);

        Assert.Equal(2, result.Processed);
        Assert.Equal(1, result.Error);
        Assert.Equal(1, result.Success);
        repository.Verify(r => r.UpsertCiStatusAsync(6, 999, 2, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        telegram.Verify(t => t.NotifyErrorAsync(
            "CI-999 processing exception", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("2026-09-07T15:30:45.123+05:00", "2026-09-07T00:00:00.000+0500")]
    [InlineData("2026-01-01T23:59:59.999+05:00", "2026-01-01T00:00:00.000+0500")]
    public void FormatKatmIsoDateAtStartOfDay_ForcesMidnightWithFixedOffset(string input, string expected)
    {
        var dateTime = DateTimeOffset.Parse(input);
        Assert.Equal(expected, TestCiHandler.FormatIsoStartOfDay(dateTime));
    }
}
