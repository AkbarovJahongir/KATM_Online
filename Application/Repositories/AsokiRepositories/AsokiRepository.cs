using CreditBureau.Contracts.AsokiLoanApplications;
using Domain.Common.DbContext;
using System.Data.SqlClient;

namespace Application.Repositories.AsokiRepositories
{
    public class AsokiRepository(DatabaseSettings databaseSettings) : IAsokiRepository
    {
        private readonly DatabaseSettings _databaseSettings = databaseSettings;

        public async Task<List<LoanApplication>> GetLoanApplications(CancellationToken cancellationToken)
        {
            using var connect = new SqlConnection(_databaseSettings.DBConnection);
            using var cmd = new SqlCommand(
                "[dbo].[Loan_History_KB_Request] @kibType",
                connect);
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@kibType", "katm");
            await connect.OpenAsync(cancellationToken);
            using var reader = cmd.ExecuteReader();

            var resultData = new List<LoanApplication>();
            while (await reader.ReadAsync(cancellationToken))
            {
                resultData.Add(new LoanApplication()
                {
                    KeyLoanHistoryKb = reader["keyLoanHistoryKb"].ToString()!,
                    PClaimId = reader["pClaimId"].ToString()!,
                    PReportId = reader["pReportId"].ToString()!,
                    PLoanSubject = reader["pLoanSubject"] is DBNull ? null : reader["pLoanSubject"].ToString(),
                    PLoanSubjectType = reader["pLoanSubjectType"] is DBNull ? null : reader["pLoanSubjectType"].ToString(),
                    PPin = reader["pPin"] is DBNull ? null : reader["pPin"].ToString(),
                    PTin = reader["pTin"] is DBNull ? null : reader["pTin"].ToString(),
                    PToken = reader["pToken"] is DBNull ? null : reader["pToken"].ToString(),
                    Status = reader["status"] is DBNull ? null : reader["status"].ToString(),
                    QuantitySelected = reader["QuantitySelected"] is DBNull ? null : (int)reader["QuantitySelected"],
                    ApplicationsSubjectType = reader["ApplicationsSubjectType"] is DBNull ? null : reader["ApplicationsSubjectType"].ToString()
                });
            }
            await connect.CloseAsync();
            return resultData;
        }

        public async Task<List<LoanApplication>> GetLoanApplicationsXml(CancellationToken cancellationToken)
        {
            using var connect = new SqlConnection(_databaseSettings.CIBConnection);
            using var cmd = new SqlCommand(
                "[dbo].[Get_Request_History_Xml]",
                connect);
            cmd.CommandType = System.Data.CommandType.Text;
            await connect.OpenAsync(cancellationToken);
            using var reader = cmd.ExecuteReader();

            var resultData = new List<LoanApplication>();
            while (await reader.ReadAsync(cancellationToken))
            {
                resultData.Add(new LoanApplication()
                {
                    KeyLoanHistoryKb = reader["keyLoanHistoryKb"].ToString()!,
                    PClaimId = reader["pClaimId"].ToString()!,
                    PReportId = reader["pReportId"].ToString()!,
                    PLoanSubject = reader["pLoanSubject"] is DBNull ? null : reader["pLoanSubject"].ToString(),
                    PLoanSubjectType = reader["pLoanSubjectType"] is DBNull ? null : reader["pLoanSubjectType"].ToString(),
                    PPin = reader["pPin"] is DBNull ? null : reader["pPin"].ToString(),
                    PTin = reader["pTin"] is DBNull ? null : reader["pTin"].ToString(),
                    PToken = reader["pToken"] is DBNull ? null : reader["pToken"].ToString(),
                    Status = reader["status"] is DBNull ? null : reader["status"].ToString(),
                    QuantitySelected = reader["QuantitySelected"] is DBNull ? null : (int)reader["QuantitySelected"],
                    ApplicationsSubjectType = reader["ApplicationsSubjectType"] is DBNull ? null : reader["ApplicationsSubjectType"].ToString()
                });
            }
            await connect.CloseAsync();
            return resultData;
        }
    }
}
