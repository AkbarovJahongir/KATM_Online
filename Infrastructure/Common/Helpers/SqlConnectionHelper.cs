using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Common.Helpers;

public static class SqlConnectionHelper
{
    private const int DeadlockErrorNumber = 1205;

    public static bool IsDeadlockException(SqlException ex) => ex.Number == DeadlockErrorNumber;

    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ILogger logger,
        string operationName,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (SqlException ex) when (IsDeadlockException(ex) && attempt < maxAttempts)
            {
                var delayMs = (int)Math.Pow(2, attempt) * 1000;
                logger.LogWarning(ex,
                    "{OperationName} encountered deadlock (attempt {AttemptNumber}/{MaxAttempts}), retrying after {DelayMs}ms",
                    operationName, attempt, maxAttempts, delayMs);

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Failed to complete {operationName} after {maxAttempts} attempts due to deadlock");
    }
}
