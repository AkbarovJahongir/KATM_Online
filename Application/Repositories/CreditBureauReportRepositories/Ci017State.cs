namespace Application.Repositories.CreditBureauReportRepositories;

public sealed record Ci017State(int AttemptCount, string? LastStatus, DateTime? LastAttemptAt);
