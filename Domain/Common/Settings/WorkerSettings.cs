namespace Domain.Common.Settings;

/// <summary>
/// A settings class used to configure the delay between executions.
/// </summary>
public sealed class WorkerSettings
{
    public int DelayMilliseconds { get; set; }
    public bool Logs { get; init; }
    public bool IsStopFactorParser { get; init; }
}
