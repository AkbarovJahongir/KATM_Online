namespace Application.Repositories.RequestManager
{
    public interface IRequestManagerRepository
    {
        enum IsXml
        {
            NotXml = 0,
            Xml = 1
        }
        Task<(string code, string message)> InsertLog(string keyLoanHistoryKb, string url, string jsonData, string method, int statusCode, string result, DateTime dateRequest, DateTime dateResponse, IsXml isXml) => InsertLog(keyLoanHistoryKb, url, jsonData, method, statusCode, result, dateRequest, dateResponse, isXml, CancellationToken.None);
        Task<(string code, string message)> InsertLog(string keyLoanHistoryKb, string url, string jsonData, string method, int statusCode, string result, DateTime dateRequest, DateTime dateResponse, IsXml isXml, CancellationToken cancellationToken);
        Task<(string code, string message)> InsertRequestLog(
            string url,
            string requestBody,
            string httpMethod,
            string responseCode,
            string responseBody,
            DateTime dateRequest,
            DateTime dateResponse,
            string loankey,
            CancellationToken cancellationToken);
    }
}
