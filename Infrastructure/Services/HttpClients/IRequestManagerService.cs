using static Application.Repositories.RequestManager.IRequestManagerRepository;

namespace Infrastructure.Services.HttpClients
{
    public interface IRequestManagerService
    {
        Task<string> SendPostRequest(string url, string jsonData, string KeyLoanHistoryKb, IsXml isxml, CancellationToken cancellationToken);
        Task<string> SendPostRequest(string url, string jsonData, int LoanKey, CancellationToken cancellationToken);
    }
}
