using System.Text;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Services.Notifications;

/// <summary>
/// Единый формат понятных сообщений для Telegram.
/// Полный stack trace остаётся только в логах.
/// </summary>
public static class TelegramErrorFormatter
{
    public static string Format(
        Exception exception,
        string? context = null,
        string? loanKey = null,
        string? claimId = null,
        string? url = null,
        string? request = null,
        string? response = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var root = Unwrap(exception);
        var (category, reason, action) = Classify(root);

        return Build(
            category: category,
            reason: reason,
            action: action,
            context: context,
            loanKey: loanKey,
            claimId: claimId,
            url: url,
            details: Truncate(root.Message.Trim(), 500),
            exceptionType: root.GetType().Name,
            innerDetails: root.InnerException is not null &&
                          !string.Equals(root.InnerException.Message, root.Message, StringComparison.Ordinal)
                ? Truncate(root.InnerException.Message.Trim(), 300)
                : null,
            request: request,
            response: response);
    }

    public static string Format(
        string category,
        string reason,
        string action,
        string? context = null,
        string? loanKey = null,
        string? claimId = null,
        string? url = null,
        string? statusCode = null,
        string? details = null,
        string? request = null,
        string? response = null)
    {
        return Build(
            category: category,
            reason: reason,
            action: action,
            context: context,
            loanKey: loanKey,
            claimId: claimId,
            url: url,
            statusCode: statusCode,
            details: details,
            request: request,
            response: response);
    }

    private static string Build(
        string category,
        string reason,
        string action,
        string? context = null,
        string? loanKey = null,
        string? claimId = null,
        string? url = null,
        string? statusCode = null,
        string? details = null,
        string? exceptionType = null,
        string? innerDetails = null,
        string? request = null,
        string? response = null)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(context))
        {
            builder.Append("Контекст: ").AppendLine(context.Trim());
        }

        builder.Append("Категория: ").AppendLine(category.Trim());
        builder.Append("Причина: ").AppendLine(reason.Trim());
        builder.Append("Что сделать: ").AppendLine(action.Trim());

        AppendField(builder, "LoanKey", loanKey);
        AppendField(builder, "ClaimId", claimId);
        AppendField(builder, "Url", url);
        AppendField(builder, "StatusCode", statusCode);
        AppendField(builder, "Тип", exceptionType);

        if (!string.IsNullOrWhiteSpace(details))
        {
            builder.AppendLine();
            builder.Append("Детали: ").AppendLine(Truncate(details.Trim(), 800));
        }

        if (!string.IsNullOrWhiteSpace(innerDetails))
        {
            builder.Append("Внутренняя: ").AppendLine(Truncate(innerDetails.Trim(), 300));
        }

        if (!string.IsNullOrWhiteSpace(request))
        {
            builder.AppendLine();
            builder.Append("Запрос: ").AppendLine(Truncate(request.Trim(), 1200));
        }

        if (!string.IsNullOrWhiteSpace(response))
        {
            builder.AppendLine();
            builder.Append("Ответ: ").AppendLine(Truncate(response.Trim(), 1200));
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendField(StringBuilder builder, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(name).Append(": ").AppendLine(value.Trim());
    }

    private static Exception Unwrap(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            return aggregate.Flatten().InnerExceptions.FirstOrDefault() ?? exception;
        }

        return exception;
    }

    private static (string Category, string Reason, string Action) Classify(Exception exception)
    {
        if (IsSqlConnectionFailure(exception))
        {
            return (
                "SQL Server недоступен",
                "Не удалось подключиться к базе данных (таймаут или сервер не найден).",
                "Проверьте сеть/VPN, доступность SQL Server и что инстанс запущен.");
        }

        if (exception is SqlException sql)
        {
            if (sql.Number == 1205)
            {
                return (
                    "SQL deadlock",
                    "Конфликт блокировок в базе данных.",
                    "Повтор на следующей итерации worker; если повторяется часто — проверьте нагрузку на БД.");
            }

            if (sql.Number is -2 or 1222)
            {
                return (
                    "SQL timeout",
                    "Запрос к базе данных превысил время ожидания.",
                    "Проверьте нагрузку SQL Server и длительные блокировки.");
            }

            return (
                "Ошибка SQL Server",
                $"Ошибка SQL (код {sql.Number}).",
                "Смотрите детали ниже и логи приложения.");
        }

        if (exception is TimeoutException)
        {
            return (
                "Таймаут",
                "Операция не завершилась вовремя.",
                "Проверьте внешние сервисы и нагрузку.");
        }

        if (exception is HttpRequestException)
        {
            return (
                "Ошибка HTTP",
                "Не удалось выполнить HTTP-запрос к внешнему сервису.",
                "Проверьте сеть и доступность API КАТМ.");
        }

        if (exception is TaskCanceledException or OperationCanceledException)
        {
            return (
                "Операция отменена",
                "Выполнение было прервано (cancellation/timeout).",
                "Обычно при остановке сервиса или таймауте HTTP; проверьте логи.");
        }

        return (
            "Необработанная ошибка",
            "Произошло исключение во время обработки.",
            "Смотрите детали ниже и логи приложения.");
    }

    private static bool IsSqlConnectionFailure(Exception exception)
    {
        if (exception is SqlException sql && sql.Number is 53 or 40 or -1)
        {
            return true;
        }

        var message = exception.Message ?? string.Empty;
        return message.Contains("network-related or instance-specific", StringComparison.OrdinalIgnoreCase)
               || message.Contains("server was not found or was not accessible", StringComparison.OrdinalIgnoreCase)
               || message.Contains("The wait operation timed out", StringComparison.OrdinalIgnoreCase)
               || message.Contains("A connection was successfully established with the server, but then an error occurred", StringComparison.OrdinalIgnoreCase)
               || (exception.InnerException?.Message.Contains("The wait operation timed out", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
