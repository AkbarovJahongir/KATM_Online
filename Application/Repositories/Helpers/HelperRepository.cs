using Domain.Common.DbContext;
using Microsoft.Data.SqlClient;
using static Application.Repositories.Helpers.IHelperRepository;

namespace Application.Repositories.Helpers
{
    public class HelperRepository(DatabaseSettings databaseSettings) : IHelperRepository
    {
        private readonly DatabaseSettings _databaseSettings = databaseSettings;
        public async Task KatmHelper(string keyLoanHistoryKb, string data, TypeOperation typeOperation, CancellationToken cancellationToken)
        {
            using var connect = new SqlConnection(_databaseSettings.DBConnection);
            using var cmd = new SqlCommand(cmdText: @"EXEC [Loan_History_KB_KatmHelper] @KeyLoanHistory, @Text, @TypeOperation", connection: connect);
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@KeyLoanHistory", keyLoanHistoryKb);
            cmd.Parameters.AddWithValue("@Text", data ?? "_");
            cmd.Parameters.AddWithValue("@TypeOperation", typeOperation.ToString());
            await connect.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            await connect.CloseAsync();
        }
        public async Task KatmHelperXml(string keyLoanHistoryKb, string data, TypeOperation typeOperation, CancellationToken cancellationToken)
        {
            using var connect = new SqlConnection(_databaseSettings.CIBConnection);
            using var cmd = new SqlCommand(cmdText: @"EXEC [Loan_History_KB_KatmHelper_Xml] @KeyLoanHistory, @Text, @TypeOperation", connection: connect);
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@KeyLoanHistory", keyLoanHistoryKb);
            cmd.Parameters.AddWithValue("@Text", data ?? "_");
            cmd.Parameters.AddWithValue("@TypeOperation", typeOperation.ToString());
            await connect.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            await connect.CloseAsync();
        }
    }
}
