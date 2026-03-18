using Domain.Common.DbContext;
using System.Data;
using System.Data.SqlClient;
using static Application.Repositories.RequestManager.IRequestManagerRepository;

namespace Application.Repositories.RequestManager
{
    public class RequestManagerRepository(DatabaseSettings databaseSettings) : IRequestManagerRepository
    {
        private readonly DatabaseSettings _databaseSettings = databaseSettings;

        public async Task<(string code, string message)> InsertRequestLog(
            string url,
            string requestBody,
            string httpMethod,
            string responseCode,
            string responseBody,
            DateTime dateRequest,
            DateTime dateResponse,
            string loankey,
            CancellationToken cancellationToken)
        {
            try
            {
                using var connect = new SqlConnection(_databaseSettings.CIBConnection);
                using var cmd = new SqlCommand(cmdText: @"[InsertRequestLogs]", connection: connect);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Clear();

                cmd.Parameters.Add(new SqlParameter("@RequestURL", SqlDbType.NVarChar, 256) { Value = url });
                cmd.Parameters.Add(new SqlParameter("@RequestBody", SqlDbType.NVarChar) { Value = (object?)requestBody ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@HttpMethod", SqlDbType.VarChar, 10) { Value = httpMethod });
                cmd.Parameters.Add(new SqlParameter("@ResponseCode", SqlDbType.VarChar, 3) { Value = (object?)responseCode ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@ResponseBody", SqlDbType.NVarChar) { Value = responseBody });
                cmd.Parameters.Add(new SqlParameter("@DateRequest", SqlDbType.DateTime) { Value = dateRequest });
                cmd.Parameters.Add(new SqlParameter("@DateResponse", SqlDbType.DateTime) { Value = dateResponse });
                cmd.Parameters.Add(new SqlParameter("@loankey", SqlDbType.VarChar) { Value = loankey });

                var resultCodeParam = new SqlParameter("@ResultCode", SqlDbType.VarChar, 2)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(resultCodeParam);

                var resultMessageParam = new SqlParameter("@ResultMessage", SqlDbType.NVarChar, 200)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(resultMessageParam);

                await connect.OpenAsync(cancellationToken);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                await connect.CloseAsync();

                string code = cmd.Parameters["@ResultCode"].Value?.ToString() ?? string.Empty;
                string message = cmd.Parameters["@ResultMessage"].Value?.ToString() ?? string.Empty;
                return (code, message);
            }
            catch (Exception ex)
            {
                return ("1", ex.Message);
            }
        }

        public async Task<(string code, string message)> InsertLog(string keyLoanHistoryKb, string url, string jsonData, string method, int statusCode, string result, DateTime dateRequest, DateTime dateResponse, IsXml isXml, CancellationToken cancellationToken)
        {
            try
            {
                using var connect = new SqlConnection(_databaseSettings.CIBConnection);
                using var cmd = new SqlCommand(cmdText: @"[InsertRequestHistoryLogs]", connection: connect);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Clear();
                var keyLoanHistoryKbParam = new SqlParameter
                {
                    ParameterName = "@KeyLoanHistoryKb",
                    Value = keyLoanHistoryKb
                };
                cmd.Parameters.Add(keyLoanHistoryKbParam);
                var requestURLParam = new SqlParameter
                {
                    ParameterName = "@RequestURL",
                    Value = url
                };
                cmd.Parameters.Add(requestURLParam);
                var requestBodyParam = new SqlParameter
                {
                    ParameterName = "@RequestBody",
                    Value = jsonData
                };
                cmd.Parameters.Add(requestBodyParam);
                var httpMethodParam = new SqlParameter
                {
                    ParameterName = "@HttpMethod",
                    Value = method
                };
                cmd.Parameters.Add(httpMethodParam);
                var responseCodeParam = new SqlParameter
                {
                    ParameterName = "@ResponseCode",
                    Value = statusCode
                };
                cmd.Parameters.Add(responseCodeParam);
                var responseBodyParam = new SqlParameter
                {
                    ParameterName = "@ResponseBody",
                    Value = result
                };
                cmd.Parameters.Add(responseBodyParam);
                var DateRequestParam = new SqlParameter
                {
                    ParameterName = "@DateRequest",
                    Value = dateRequest
                };
                cmd.Parameters.Add(DateRequestParam);
                var dateResponseParam = new SqlParameter
                {
                    ParameterName = "@DateResponse",
                    Value = dateResponse
                };
                cmd.Parameters.Add(dateResponseParam);
                // определяем первый выходной параметр
                var isXmlParam = new SqlParameter
                {
                    ParameterName = "@IsXml",
                    SqlDbType = SqlDbType.VarChar, // тип параметра
                    Size = 1,
                    Value = ((int)isXml).ToString()
                };
                cmd.Parameters.Add(isXmlParam);
                // определяем первый выходной параметр
                var resultCodeParam = new SqlParameter
                {
                    ParameterName = "@ResultCode",
                    SqlDbType = SqlDbType.VarChar, // тип параметра
                    Size = 3,
                    // указываем, что параметр будет выходным
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(resultCodeParam);
                // определяем первый выходной параметр
                var resultMessageParam = new SqlParameter
                {
                    ParameterName = "@ResultMessage",
                    SqlDbType = SqlDbType.NVarChar, // тип параметра
                    Size = 200,
                    // указываем, что параметр будет выходным
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(resultMessageParam);
                await connect.OpenAsync(cancellationToken);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                await connect.CloseAsync();
                string code = cmd.Parameters["@ResultCode"].Value.ToString() ?? string.Empty;
                string message = cmd.Parameters["@ResultMessage"].Value.ToString() ?? string.Empty;
                return (code, message);
            }
            catch (Exception)
            {
                return (string.Empty, string.Empty);
            }
        }
    }
}
