using CreditBureauService.Contracts.CreditReportParser;
using Domain.Common.DbContext;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Application.Repositories.CreditReportXmlParserRepositories;

public sealed class CreditReportXmlParserRepository(DatabaseSettings databaseSettings) : ICreditReportXmlParserRepository
{
    private readonly DatabaseSettings _databaseSettings = databaseSettings;
    public async Task ExternalInfo_Save(string app, string payloadJson, string reportName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(app))
            throw new ArgumentException("App is required.", nameof(app));
        if (app.Length > 16)
            throw new ArgumentOutOfRangeException(nameof(app), "App must be <= 16 characters.");
        // payloadJson может быть большим: NVARCHAR(MAX) это выдержит.
        payloadJson ??= string.Empty;

        await using var connect = new SqlConnection(_databaseSettings.DBConnection);
        await using var cmd = new SqlCommand("dbo.External_Info_Save", connect)
        {
            CommandType = CommandType.StoredProcedure
        };

        // @App varchar(16)
        cmd.Parameters.Add("@App", SqlDbType.VarChar, 16).Value = app;

        // @Payload nvarchar(max)
        cmd.Parameters.Add("@Payload", SqlDbType.NVarChar, -1).Value = payloadJson;

        // @ReportCode nvarchar(100) = NULL   (reportName пробрасываем сюда)
        cmd.Parameters.Add("@ReportCode", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(reportName) ? DBNull.Value : reportName;

        await connect.OpenAsync(cancellationToken);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string> GetAppByKeyLoanHistoryKb(string keyLoanHistoryKb, CancellationToken cancellationToken)
    {
        await using var connect = new SqlConnection(_databaseSettings.DBConnection);
        await using var cmd = new SqlCommand(
            @"SELECT TOP (1) App as App FROM dbo.Loan_History_KB WITH(NOLOCK) WHERE [key] = @KeyLoanHistory",
            connect);

        cmd.CommandType = CommandType.Text;

        // Лучше не AddWithValue: явно задать тип/длину параметра (подставь свою длину!)
        var p = cmd.Parameters.Add("@KeyLoanHistory", SqlDbType.NVarChar, 64);
        p.Value = keyLoanHistoryKb;

        await connect.OpenAsync(cancellationToken);

        var obj = await cmd.ExecuteScalarAsync(cancellationToken);
        return obj is DBNull or null ? string.Empty : Convert.ToString(obj)!;
    }

    public async Task InsertReport025(List<Report025Data> report025s)
    {
        if (report025s is null || report025s.Count == 0)
            return;

        using var connect = new SqlConnection(_databaseSettings.DBConnection);
        await connect.OpenAsync();

        using var cmd = new SqlCommand(@"
        INSERT INTO [dbo].[External_Katm_025_Inps_Income]
            ([App],
             [Num],
             [Period],
             [PeriodRaw],
             [OrgName],
             [OrgInn],
             [IncomeSumma],
             [InpsSumma],
             [SendDate],
             [OperDate],
             [DateIn])
        VALUES
            (@App,
             @Num,
             @Period,
             @PeriodRaw,
             @OrgName,
             @OrgInn,
             @IncomeSumma,
             @InpsSumma,
             @SendDate,
             @OperDate,
             GETDATE())", connect);

        cmd.Parameters.Add("@App", SqlDbType.VarChar, 16);
        cmd.Parameters.Add("@Num", SqlDbType.Int);
        cmd.Parameters.Add("@Period", SqlDbType.DateTime);
        cmd.Parameters.Add("@PeriodRaw", SqlDbType.VarChar, 10);
        cmd.Parameters.Add("@OrgName", SqlDbType.NVarChar, 100);
        cmd.Parameters.Add("@OrgInn", SqlDbType.VarChar, 9);
        cmd.Parameters.Add("@IncomeSumma", SqlDbType.Decimal);
        cmd.Parameters.Add("@InpsSumma", SqlDbType.Decimal);
        cmd.Parameters.Add("@SendDate", SqlDbType.DateTime);
        cmd.Parameters.Add("@OperDate", SqlDbType.DateTime);

        foreach (var report in report025s)
        {
            cmd.Parameters["@App"].Value = (object?)report.App ?? DBNull.Value;
            cmd.Parameters["@Num"].Value = (object?)report.Num ?? DBNull.Value;
            cmd.Parameters["@Period"].Value = (object?)report.Period ?? DBNull.Value;
            cmd.Parameters["@OrgName"].Value = (object?)report.OrgName ?? DBNull.Value;
            cmd.Parameters["@OrgInn"].Value = (object?)report.OrgInn ?? DBNull.Value;
            cmd.Parameters["@IncomeSumma"].Value = (object?)report.IncomeSumma ?? DBNull.Value;
            cmd.Parameters["@InpsSumma"].Value = (object?)report.InpsSumma ?? DBNull.Value;
            cmd.Parameters["@SendDate"].Value = (object?)report.SendDate ?? DBNull.Value;
            cmd.Parameters["@OperDate"].Value = (object?)report.OperDate ?? DBNull.Value;

            await cmd.ExecuteNonQueryAsync();
        }
        await connect.CloseAsync();
    }
}
