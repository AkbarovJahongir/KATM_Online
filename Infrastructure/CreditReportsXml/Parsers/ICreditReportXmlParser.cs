namespace Infrastructure.CreditReportsXml.Parsers
{
    public interface ICreditReportXmlParser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="KeyLoanHistoryKb"></param>
        /// <param name="reportBase64"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ParseAndPersistAsync(string KeyLoanHistoryKb, string reportBase64, CancellationToken cancellationToken);
    }
}
