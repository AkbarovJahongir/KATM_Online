using Infrastructure.Services.Notifications;

namespace Infrastructure.Tests.Services.Notifications;

public class TelegramErrorFormatterTests
{
    [Fact]
    public void Format_WhenSqlNetworkTimeout_ReturnsReadableSqlUnavailableMessage()
    {
        var ex = new Exception(
            "A network-related or instance-specific error occurred while establishing a connection to SQL Server. " +
            "The server was not found or was not accessible. (provider: TCP Provider, error: 0 - The wait operation timed out.)");

        var text = TelegramErrorFormatter.Format(ex, context: "Обработчик CI-001", loanKey: "123");

        Assert.Contains("Контекст: Обработчик CI-001", text);
        Assert.Contains("Категория: SQL Server недоступен", text);
        Assert.Contains("Причина: Не удалось подключиться к базе данных", text);
        Assert.Contains("Что сделать:", text);
        Assert.Contains("LoanKey: 123", text);
        Assert.DoesNotContain("StackTrace", text);
        Assert.DoesNotContain("at Microsoft.Data", text);
    }

    [Fact]
    public void Format_WhenBusinessError_IncludesAllSections()
    {
        var text = TelegramErrorFormatter.Format(
            category: "Ошибка API КАТМ",
            reason: "Неверный ИНН",
            action: "Исправьте данные заявки.",
            context: "CI-001",
            loanKey: "42",
            response: "{\"result\":\"fail\"}");

        Assert.Contains("Категория: Ошибка API КАТМ", text);
        Assert.Contains("Причина: Неверный ИНН", text);
        Assert.Contains("Что сделать: Исправьте данные заявки.", text);
        Assert.Contains("LoanKey: 42", text);
        Assert.Contains("Ответ: {\"result\":\"fail\"}", text);
    }

    [Fact]
    public void Format_WhenTimeoutException_ReturnsTimeoutCategory()
    {
        var text = TelegramErrorFormatter.Format(new TimeoutException("operation timed out"));

        Assert.Contains("Категория: Таймаут", text);
        Assert.Contains("Тип: TimeoutException", text);
        Assert.Contains("Детали: operation timed out", text);
    }

    [Fact]
    public void Format_WhenGenericException_ReturnsUnhandledCategory()
    {
        var text = TelegramErrorFormatter.Format(new InvalidOperationException("boom"));

        Assert.Contains("Категория: Необработанная ошибка", text);
        Assert.Contains("Детали: boom", text);
    }
}
