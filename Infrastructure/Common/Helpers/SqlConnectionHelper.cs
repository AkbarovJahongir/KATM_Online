using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Common.Helpers;

public static class SqlConnectionHelper
{
    private const int DeadlockErrorNumber = 1205;

    public static bool IsDeadlockException(Exception ex) =>
        ex is SqlException sql && sql.Number == DeadlockErrorNumber
        || ex.InnerException is SqlException inner && inner.Number == DeadlockErrorNumber;

    public static bool IsDeadlockException(SqlException ex) => ex.Number == DeadlockErrorNumber;

    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ILogger logger,
        string operationName,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        SqlException? lastDeadlock = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (SqlException ex) when (IsDeadlockException(ex) && attempt < maxAttempts)
            {
                lastDeadlock = ex;
                var delayMs = (int)Math.Pow(2, attempt) * 1000;
                logger.LogWarning(ex,
                    "{OperationName} encountered deadlock (attempt {AttemptNumber}/{MaxAttempts}), retrying after {DelayMs}ms",
                    operationName, attempt, maxAttempts, delayMs);

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        if (lastDeadlock is not null)
        {
            throw lastDeadlock;
        }

        throw new InvalidOperationException(
            $"Failed to complete {operationName} after {maxAttempts} attempts due to deadlock");
    }

    public static async Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        ILogger logger,
        string operationName,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, logger, operationName, maxAttempts, cancellationToken);
    }
}
