namespace Infrastructure.Services.CreditBureauServices
{
    public interface ICreditBureauService
    {
        Task CreditBureauProcessing() => CreditBureauProcessing(CancellationToken.None);
        Task CreditBureauProcessing(CancellationToken cancellationToken);
    }
}