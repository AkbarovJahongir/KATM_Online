namespace Application.Repositories.CreditBureauReportRepositories;

/// <summary>
/// Composite port for credit-bureau CI data access (queue + status + CI-017 state).
/// Prefer injecting the focused interfaces when a consumer only needs one concern.
/// </summary>
public interface ICreditBureauReportRepository : ICiQueueReaderRepository, ICiStatusStore, ICi017StateStore
{
}
