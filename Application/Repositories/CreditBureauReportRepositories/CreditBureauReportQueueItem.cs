namespace Application.Repositories.CreditBureauReportRepositories;

public class CreditBureauReportQueueItem<TRequest>
{
    public int LoanKey { get; set; }
    public required TRequest Request { get; set; }
}
