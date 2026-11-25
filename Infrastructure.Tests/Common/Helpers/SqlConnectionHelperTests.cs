using Infrastructure.Common.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Common.Helpers;

public class SqlConnectionHelperTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_WhenDeadlockThenSuccess_RetriesAndReturns()
    {
        var logger = new Mock<ILogger>();
        var attempts = 0;

        var result = await SqlConnectionHelper.ExecuteWithRetryAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw CreateDeadlockException();
                }

                return Task.FromResult(42);
            },
            logger.Object,
            "test-op",
            maxAttempts: 3);

        Assert.Equal(42, result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenAlwaysDeadlock_RethrowsSqlException()
    {
        var logger = new Mock<ILogger>();

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            SqlConnectionHelper.ExecuteWithRetryAsync<int>(
                _ => throw CreateDeadlockException(),
                logger.Object,
                "test-op",
                maxAttempts: 3));

        Assert.Equal(1205, ex.Number);
    }

    private static SqlException CreateDeadlockException()
    {
        // SqlException cannot be constructed publicly; use reflection like production test helpers.
        var collectionType = typeof(SqlException).Assembly.GetType("Microsoft.Data.SqlClient.SqlErrorCollection")!;
        var errorCollection = Activator.CreateInstance(collectionType, nonPublic: true)!;
        var errorType = typeof(SqlException).Assembly.GetType("Microsoft.Data.SqlClient.SqlError")!;
        var errorCtor = errorType.GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var ctorParams = errorCtor.GetParameters();
        var args = new object?[ctorParams.Length];
        for (var i = 0; i < ctorParams.Length; i++)
        {
            var t = ctorParams[i].ParameterType;
            args[i] = t == typeof(int) ? 1205
                : t == typeof(byte) ? (byte)0
                : t == typeof(string) ? "deadlock"
                : t.IsValueType ? Activator.CreateInstance(t)
                : null;
            if (ctorParams[i].Name is "infoNumber" or "number" || (i == 0 && t == typeof(int)))
            {
                args[i] = 1205;
            }
        }

        // Prefer well-known SqlError ctor shape used by Microsoft.Data.SqlClient
        object error;
        try
        {
            error = Activator.CreateInstance(
                errorType,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                binder: null,
                args:
                [
                    1205, (byte)0, (byte)0, "server", "Transaction was deadlocked", "proc", 1, null
                ],
                culture: null)!;
        }
        catch
        {
            error = errorCtor.Invoke(args)!;
        }

        collectionType.GetMethod("Add", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(errorCollection, [error]);

        var createException = typeof(SqlException).GetMethod(
            "CreateException",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            binder: null,
            types: [collectionType, typeof(string)],
            modifiers: null);

        if (createException is not null)
        {
            return (SqlException)createException.Invoke(null, [errorCollection, "7.0.0"])!;
        }

        createException = typeof(SqlException).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .First(m => m.Name == "CreateException");
        var parameters = createException.GetParameters();
        var invokeArgs = new object?[parameters.Length];
        invokeArgs[0] = errorCollection;
        for (var i = 1; i < parameters.Length; i++)
        {
            invokeArgs[i] = parameters[i].ParameterType == typeof(string) ? "7.0.0"
                : parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType)
                : null;
        }

        return (SqlException)createException.Invoke(null, invokeArgs)!;
    }
}
