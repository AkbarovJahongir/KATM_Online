using CreditBureau.Contracts.AsokiLoanApplications;

namespace Application.Repositories.AsokiRepositories
{
    public interface IAsokiRepository
    {
        Task<List<LoanApplication>> GetLoanApplications(CancellationToken cancellationToken);
        Task<List<LoanApplication>> GetLoanApplicationsXml(CancellationToken cancellationToken);

        /// <summary>
        /// Обновить статус Request_History_Xml.
        /// </summary>
        /// <param name="keyLoanHistoryKb">Ключ истории займа</param>
        /// <param name="status">Новый статус</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task UpdateRequestHistoryXmlStatusAsync(string keyLoanHistoryKb, string status, CancellationToken cancellationToken);
    }
}
