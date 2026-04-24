using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;
using Domain.Common.DbContext;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Application.Repositories.CreditBureauReportRepositories;

public class CreditBureauReportRepository(DatabaseSettings databaseSettings) : ICreditBureauReportRepository
{
    private readonly DatabaseSettings _databaseSettings = databaseSettings;

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationIndividualRequest>>>
        GetCreditRegistrationIndividualRequestsAsync(CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync("KATM_Report_001", MapCreditRegistrationIndividualRequest,
            cancellationToken);
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationEntityRequest>>>
        GetCreditRegistrationEntityRequestsAsync(CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync("KATM_Report_002", MapCreditRegistrationEntityRequest,
            cancellationToken);
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationDeclineRequest>>>
        GetCreditRegistrationDeclineRequestsAsync(CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync("KATM_Report_003", MapCreditRegistrationDeclineRequest,
            cancellationToken);
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationRequest>>> GetCreditRegistrationRequestsAsync(
        CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync("KATM_Report_004", MapCreditRegistrationRequest, cancellationToken);
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationRepaymentSchedule>>>
        CreditRegistrationRepaymentSchedulesAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_005]()", connection);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationRepaymentSchedule>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey", "LoanKey", "keyLoanHistoryKb", "KeyLoanHistoryKb") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationRepaymentSchedule>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationRepaymentSchedule
                    {
                        PHead = GetString(reader, "pHead", "head"),
                        PCode = GetString(reader, "pCode", "code"),
                        PClaimId = GetString(reader, "pClaimId"),
                        PContractId = GetString(reader, "pContractId"),
                        PNibbd = GetString(reader, "PNibbd"),
                        PDate = GetString(reader, "pDate"),
                        PIsUpdate = GetString(reader, "PIsUpdate"),
                        PPlanArray = new List<PPlanArray>()
                    }
                };
            }

            result[loanKey].Request.PPlanArray.Add(new PPlanArray
            {
                Amount = GetString(reader, "Amount", "principalAmount"),
                Currency = GetString(reader, "Currency", "currency"),
                Date = GetString(reader, "Date", "scheduleDate"),
                Percent = GetString(reader, "Percent", "percentAmount")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationPledgeOwner>>>
        CreditRegistrationPledgeOwnerAsync(
            CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync("KATM_Report_020", MapCreditRegistrationPledgeOwnerRequest,
            cancellationToken);
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationPledgeSecurity>>>
        GetPledgeSecurityRequestsAsync(CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync("KATM_Report_021", MapCreditRegistrationPledgeSecurity,
            cancellationToken);
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_015]()", connection);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationRepayment>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey", "LoanKey", "keyLoanHistoryKb", "KeyLoanHistoryKb") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationRepayment>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationRepayment
                    {
                        PHead = GetString(reader, "pHead", "head"),
                        PCode = GetString(reader, "pCode", "code"),
                        PContractId = GetString(reader, "pContractId"),
                        PContractType = GetString(reader, "pContractType"),
                        PDate = GetString(reader, "pDate"),
                        PLoanStatus = GetString(reader, "pLoanStatus"),
                        PRepaymentArray = new List<PRepaymentArray>()
                    }
                };
            }

            result[loanKey].Request.PRepaymentArray!.Add(new PRepaymentArray
            {
                Account = GetString(reader, "account", "Account"),
                Date = GetString(reader, "date", "Date", "updateDate"),
                StartBalance = GetDecimal(reader, "startBalance", "StartBalance"),
                Debit = GetDecimal(reader, "debit", "Debit"),
                Credit = GetDecimal(reader, "credit", "Credit"),
                EndBalance = GetDecimal(reader, "endBalance", "EndBalance")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsByDateAsync(DateTime date, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_015_ByDate](@Date)", connection);
        command.Parameters.AddWithValue("@Date", date.Date);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationRepayment>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey", "LoanKey", "keyLoanHistoryKb", "KeyLoanHistoryKb") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationRepayment>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationRepayment
                    {
                        PHead = GetString(reader, "pHead", "head"),
                        PCode = GetString(reader, "pCode", "code"),
                        PContractId = GetString(reader, "pContractId"),
                        PContractType = GetString(reader, "pContractType"),
                        PDate = GetString(reader, "pDate"),
                        PLoanStatus = GetString(reader, "pLoanStatus"),
                        PRepaymentArray = new List<PRepaymentArray>()
                    }
                };
            }

            result[loanKey].Request.PRepaymentArray!.Add(new PRepaymentArray
            {
                Account = GetString(reader, "account", "Account"),
                Date = GetString(reader, "date", "Date", "updateDate"),
                StartBalance = GetDecimal(reader, "startBalance", "StartBalance"),
                Debit = GetDecimal(reader, "debit", "Debit"),
                Credit = GetDecimal(reader, "credit", "Credit"),
                EndBalance = GetDecimal(reader, "endBalance", "EndBalance")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_016]()", connection);

        await connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "Lkey") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationBankDitailRequest
                    {
                        PHead = GetString(reader, "Send_code"),
                        PCode = GetString(reader, "Send_code"),
                        PContractType = GetString(reader, "S_ASOKI_0A6"),
                        PContractId = GetString(reader, "App_old"),
                        PDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        PRepaymentDetArray = new List<PRepaymentDetArray>()
                    }
                };
            }

            var accountA = GetString(reader, "Account_db");
            var accountB = GetString(reader, "Account_kr");

            result[loanKey].Request.PRepaymentDetArray!.Add(new PRepaymentDetArray
            {
                AccountA = accountA,
                AccountB = accountB,
                BranchA = GetString(reader, "Send_code"),
                BranchB = GetString(reader, "Reciver_code"),
                CoaA = accountA?.Length >= 5 ? accountA.Substring(0, 5) : accountA,
                CoaB = accountB?.Length >= 5 ? accountB.Substring(0, 5) : accountB,
                Currency = GetString(reader, "Curr_db"),
                Destination = GetString(reader, "CB_60"),
                DocDate = GetString(reader, "Date_trans"),
                DocNum = GetString(reader, "N_doc"),
                DocType = GetString(reader, "UZ_s026"),
                NameA = GetString(reader, "Send_name"),
                NameB = GetString(reader, "Reciver_name"),
                PayType = GetString(reader, "UZ_s004"),
                PaymentId = GetString(reader, "transKey"),
                Purpose = GetString(reader, "Purpose"),
                Summa = GetDecimal(reader, "Sum_NV_db")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsByDateAsync(DateTime date, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_016_ByDate](@Date)", connection);
        command.Parameters.AddWithValue("@Date", date.Date);

        await connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "Lkey") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationBankDitailRequest
                    {
                        PHead = GetString(reader, "Send_code"),
                        PCode = GetString(reader, "Send_code"),
                        PContractType = GetString(reader, "S_ASOKI_0A6"),
                        PContractId = GetString(reader, "App_old"),
                        PDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        PRepaymentDetArray = new List<PRepaymentDetArray>()
                    }
                };
            }

            var accountA = GetString(reader, "Account_db");
            var accountB = GetString(reader, "Account_kr");

            result[loanKey].Request.PRepaymentDetArray!.Add(new PRepaymentDetArray
            {
                AccountA = accountA,
                AccountB = accountB,
                BranchA = GetString(reader, "Send_code"),
                BranchB = GetString(reader, "Reciver_code"),
                CoaA = accountA?.Length >= 5 ? accountA.Substring(0, 5) : accountA,
                CoaB = accountB?.Length >= 5 ? accountB.Substring(0, 5) : accountB,
                Currency = GetString(reader, "Curr_db"),
                Destination = GetString(reader, "CB_60"),
                DocDate = GetString(reader, "Date_trans"),
                DocNum = GetString(reader, "N_doc"),
                DocType = GetString(reader, "UZ_s026"),
                NameA = GetString(reader, "Send_name"),
                NameB = GetString(reader, "Reciver_name"),
                PayType = GetString(reader, "UZ_s004"),
                PaymentId = GetString(reader, "transKey"),
                Purpose = GetString(reader, "Purpose"),
                Summa = GetDecimal(reader, "Sum_NV_db")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }
    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>> GetAccountStatusRequestsAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_018]()", connection);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var grouped = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey") ?? 0;

            if (!grouped.TryGetValue(loanKey, out var queueItem))
            {
                queueItem = new CreditBureauReportQueueItem<CreditRegistrationAccountStatus>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationAccountStatus
                    {
                        PContractId = GetString(reader, "App_old"),       // [УникальныйНомерДоговора]
                        PContractType = GetString(reader, "S_ASOKI_0A6"),   // [КодТипаДоговора]
                        PAccountStatusesArray = new List<AccountStatusItem>()
                    }
                };
                grouped[loanKey] = queueItem;
            }

            var item = MapAccountStatusItem(reader);
            if (item is not null)
                queueItem.Request.PAccountStatusesArray!.Add(item);
        }

        await connection.CloseAsync();
        return grouped.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>> GetAccountStatusRequestsByDateAsync(DateTime date, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_018_ByDate](@Date)", connection);
        command.Parameters.AddWithValue("@Date", date.Date);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var grouped = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey") ?? 0;

            if (!grouped.TryGetValue(loanKey, out var queueItem))
            {
                queueItem = new CreditBureauReportQueueItem<CreditRegistrationAccountStatus>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationAccountStatus
                    {
                        PContractId = GetString(reader, "App_old"),       // [УникальныйНомерДоговора]
                        PContractType = GetString(reader, "S_ASOKI_0A6"),   // [КодТипаДоговора]
                        PAccountStatusesArray = new List<AccountStatusItem>()
                    }
                };
                grouped[loanKey] = queueItem;
            }

            var item = MapAccountStatusItem(reader);
            if (item is not null)
                queueItem.Request.PAccountStatusesArray!.Add(item);
        }

        await connection.CloseAsync();
        return grouped.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>> GetCreditRegistrationRepaymentRequestsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_015_ByPeriod](@StartDate, @EndDate)", connection);
        command.Parameters.AddWithValue("@StartDate", startDate.Date);
        command.Parameters.AddWithValue("@EndDate", endDate.Date);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationRepayment>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey", "LoanKey", "keyLoanHistoryKb", "KeyLoanHistoryKb") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationRepayment>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationRepayment
                    {
                        PHead = GetString(reader, "pHead", "head"),
                        PCode = GetString(reader, "pCode", "code"),
                        PContractId = GetString(reader, "pContractId"),
                        PContractType = GetString(reader, "pContractType"),
                        PDate = GetString(reader, "pDate"),
                        PLoanStatus = GetString(reader, "pLoanStatus"),
                        PRepaymentArray = new List<PRepaymentArray>()
                    }
                };
            }

            result[loanKey].Request.PRepaymentArray!.Add(new PRepaymentArray
            {
                Account = GetString(reader, "account", "Account"),
                Date = GetString(reader, "date", "Date", "updateDate"),
                StartBalance = GetDecimal(reader, "startBalance", "StartBalance"),
                Debit = GetDecimal(reader, "debit", "Debit"),
                Credit = GetDecimal(reader, "credit", "Credit"),
                EndBalance = GetDecimal(reader, "endBalance", "EndBalance")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>> GetCreditRegistrationBankDetailsRequestsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_016_ByPeriod](@StartDate, @EndDate)", connection);
        command.Parameters.AddWithValue("@StartDate", startDate.Date);
        command.Parameters.AddWithValue("@EndDate", endDate.Date);

        await connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "Lkey") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationBankDitailRequest
                    {
                        PHead = GetString(reader, "Send_code"),
                        PCode = GetString(reader, "Send_code"),
                        PContractType = GetString(reader, "S_ASOKI_0A6"),
                        PContractId = GetString(reader, "App_old"),
                        PDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        PRepaymentDetArray = new List<PRepaymentDetArray>()
                    }
                };
            }

            var accountA = GetString(reader, "Account_db");
            var accountB = GetString(reader, "Account_kr");

            result[loanKey].Request.PRepaymentDetArray!.Add(new PRepaymentDetArray
            {
                AccountA = accountA,
                AccountB = accountB,
                BranchA = GetString(reader, "Send_code"),
                BranchB = GetString(reader, "Reciver_code"),
                CoaA = accountA?.Length >= 5 ? accountA.Substring(0, 5) : accountA,
                CoaB = accountB?.Length >= 5 ? accountB.Substring(0, 5) : accountB,
                Currency = GetString(reader, "Curr_db"),
                Destination = GetString(reader, "CB_60"),
                DocDate = GetString(reader, "Date_trans"),
                DocNum = GetString(reader, "N_doc"),
                DocType = GetString(reader, "UZ_s026"),
                NameA = GetString(reader, "Send_name"),
                NameB = GetString(reader, "Reciver_name"),
                PayType = GetString(reader, "UZ_s004"),
                PaymentId = GetString(reader, "transKey"),
                Purpose = GetString(reader, "Purpose"),
                Summa = GetDecimal(reader, "Sum_NV_db")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>> GetAccountStatusRequestsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_018_ByPeriod](@StartDate, @EndDate)", connection);
        command.Parameters.AddWithValue("@StartDate", startDate.Date);
        command.Parameters.AddWithValue("@EndDate", endDate.Date);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var grouped = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey") ?? 0;

            if (!grouped.TryGetValue(loanKey, out var queueItem))
            {
                queueItem = new CreditBureauReportQueueItem<CreditRegistrationAccountStatus>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationAccountStatus
                    {
                        PContractId = GetString(reader, "App_old"),       // [УникальныйНомерДоговора]
                        PContractType = GetString(reader, "S_ASOKI_0A6"),   // [КодТипаДоговора]
                        PAccountStatusesArray = new List<AccountStatusItem>()
                    }
                };
                grouped[loanKey] = queueItem;
            }

            var item = MapAccountStatusItem(reader);
            if (item is not null)
                queueItem.Request.PAccountStatusesArray!.Add(item);
        }

        await connection.CloseAsync();
        return grouped.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>> GetCreditRegistrationRepaymentRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_015_ByPeriod](@StartDate, @EndDate, @LoanKey)", connection);
        command.Parameters.AddWithValue("@StartDate", startDate.Date);
        command.Parameters.AddWithValue("@EndDate", endDate.Date);
        command.Parameters.AddWithValue("@LoanKey", loanKey.HasValue ? (object)loanKey.Value : DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationRepayment>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var lk = GetInt(reader, "loanKey", "LoanKey", "keyLoanHistoryKb", "KeyLoanHistoryKb") ?? 0;

            if (!result.ContainsKey(lk))
            {
                result[lk] = new CreditBureauReportQueueItem<CreditRegistrationRepayment>
                {
                    LoanKey = lk,
                    Request = new CreditRegistrationRepayment
                    {
                        PHead = GetString(reader, "pHead", "head"),
                        PCode = GetString(reader, "pCode", "code"),
                        PContractId = GetString(reader, "pContractId"),
                        PContractType = GetString(reader, "pContractType"),
                        PDate = GetString(reader, "pDate"),
                        PLoanStatus = GetString(reader, "pLoanStatus"),
                        PRepaymentArray = new List<PRepaymentArray>()
                    }
                };
            }

            result[lk].Request.PRepaymentArray!.Add(new PRepaymentArray
            {
                Account = GetString(reader, "account", "Account"),
                Date = GetString(reader, "date", "Date", "updateDate"),
                StartBalance = GetDecimal(reader, "startBalance", "StartBalance"),
                Debit = GetDecimal(reader, "debit", "Debit"),
                Credit = GetDecimal(reader, "credit", "Credit"),
                EndBalance = GetDecimal(reader, "endBalance", "EndBalance")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>> GetCreditRegistrationBankDetailsRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_016_ByPeriod](@StartDate, @EndDate, @LoanKey)", connection);
        command.Parameters.AddWithValue("@StartDate", startDate.Date);
        command.Parameters.AddWithValue("@EndDate", endDate.Date);
        command.Parameters.AddWithValue("@LoanKey", loanKey.HasValue ? (object)loanKey.Value : DBNull.Value);

        await connection.OpenAsync(cancellationToken);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var lk = GetInt(reader, "Lkey") ?? 0;

            if (!result.ContainsKey(lk))
            {
                result[lk] = new CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>
                {
                    LoanKey = lk,
                    Request = new CreditRegistrationBankDitailRequest
                    {
                        PHead = GetString(reader, "Send_code"),
                        PCode = GetString(reader, "Send_code"),
                        PContractType = GetString(reader, "S_ASOKI_0A6"),
                        PContractId = GetString(reader, "App_old"),
                        PDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        PRepaymentDetArray = new List<PRepaymentDetArray>()
                    }
                };
            }

            var accountA = GetString(reader, "Account_db");
            var accountB = GetString(reader, "Account_kr");

            result[lk].Request.PRepaymentDetArray!.Add(new PRepaymentDetArray
            {
                AccountA = accountA,
                AccountB = accountB,
                BranchA = GetString(reader, "Send_code"),
                BranchB = GetString(reader, "Reciver_code"),
                CoaA = accountA?.Length >= 5 ? accountA.Substring(0, 5) : accountA,
                CoaB = accountB?.Length >= 5 ? accountB.Substring(0, 5) : accountB,
                Currency = GetString(reader, "Curr_db"),
                Destination = GetString(reader, "CB_60"),
                DocDate = GetString(reader, "Date_trans"),
                DocNum = GetString(reader, "N_doc"),
                DocType = GetString(reader, "UZ_s026"),
                NameA = GetString(reader, "Send_name"),
                NameB = GetString(reader, "Reciver_name"),
                PayType = GetString(reader, "UZ_s004"),
                PaymentId = GetString(reader, "transKey"),
                Purpose = GetString(reader, "Purpose"),
                Summa = GetDecimal(reader, "Sum_NV_db")
            });
        }

        await connection.CloseAsync();

        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>> GetAccountStatusRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_018_ByPeriod](@StartDate, @EndDate, @LoanKey)", connection);
        command.Parameters.AddWithValue("@StartDate", startDate.Date);
        command.Parameters.AddWithValue("@EndDate", endDate.Date);
        command.Parameters.AddWithValue("@LoanKey", loanKey.HasValue ? (object)loanKey.Value : DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var grouped = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var lk = GetInt(reader, "loanKey") ?? 0;

            if (!grouped.TryGetValue(lk, out var queueItem))
            {
                queueItem = new CreditBureauReportQueueItem<CreditRegistrationAccountStatus>
                {
                    LoanKey = lk,
                    Request = new CreditRegistrationAccountStatus
                    {
                        PContractId = GetString(reader, "App_old"),       // [УникальныйНомерДоговора]
                        PContractType = GetString(reader, "S_ASOKI_0A6"),   // [КодТипаДоговора]
                        PAccountStatusesArray = new List<AccountStatusItem>()
                    }
                };
                grouped[lk] = queueItem;
            }

            var item = MapAccountStatusItem(reader);
            if (item is not null)
                queueItem.Request.PAccountStatusesArray!.Add(item);
        }

        await connection.CloseAsync();
        return grouped.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRequest>>>
        GetCreditRegistrationLeasingRequestsAsync(CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync(
            "KATM_Report_011",
            MapCreditRegistrationLeasingRequest,
            cancellationToken);
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRepaymentSchedule>>>
        GetLeasingRepaymentSchedulesAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_012]()", connection);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // Группировка по loanKey: один кредит = один QueueItem с массивом pPlanArray
        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationLeasingRepaymentSchedule>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationLeasingRepaymentSchedule>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationLeasingRepaymentSchedule
                    {
                        PClaimId = GetString(reader, "pClaimId"),
                        PContractId = GetString(reader, "pContractId"),
                        PNibbd = GetString(reader, "pNibbd"),
                        PDate = GetString(reader, "pDate"),
                        PIsUpdate = GetString(reader, "pIsUpdate"),
                        PPlanArray = new List<LeasingPPlanArray>()
                    }
                };
            }

            result[loanKey].Request!.PPlanArray!.Add(new LeasingPPlanArray
            {
                Date = GetString(reader, "scheduleDate"),
                Percent = GetString(reader, "schedulePercent"),
                Currency = GetString(reader, "scheduleCurrency"),
                Amount = GetString(reader, "scheduleAmount")
            });
        }

        await connection.CloseAsync();
        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRepayment>>>
        GetLeasingRepaymentObjectsAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_013]()", connection);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // Группировка по loanKey: один кредит = один QueueItem с массивом pDetailsArray
        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationLeasingRepayment>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationLeasingRepayment>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationLeasingRepayment
                    {
                        PContractId = GetString(reader, "pContractId"),
                        PClientId = GetString(reader, "pClientId"),
                        PInn = GetString(reader, "pInn"),
                        PNibbd = GetString(reader, "pNibbd"),
                        PDate = GetString(reader, "pDate"),
                        PDetailsArray = new List<PDetailsArray>()
                    }
                };
            }

            result[loanKey].Request!.PDetailsArray!.Add(new PDetailsArray
            {
                ObjectId = GetString(reader, "detailObjectId"),
                Amount = GetString(reader, "detailAmount"),
                Currency = GetString(reader, "detailCurrency"),
                LeasingType = GetString(reader, "detailLeasingType"),
                Name = GetString(reader, "detailName"),
                Status = GetString(reader, "detailStatus"),
                Amortization = GetString(reader, "detailAmortization"),
                Date = GetString(reader, "detailDate"),
                Details = GetString(reader, "detailDetails")
            });
        }

        await connection.CloseAsync();
        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationFactoring>>>
    GetCreditRegistrationFactoringRequestsAsync(CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync("KATM_Report_014", MapCreditRegistrationFactoring, cancellationToken);
    }
    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationBusinessDetailRequest>>>
    GetCreditRegistrationBusinessDetailsRequestsAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_022]()", connection);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new Dictionary<int, CreditBureauReportQueueItem<CreditRegistrationBusinessDetailRequest>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var loanKey = GetInt(reader, "loanKey") ?? 0;

            if (!result.ContainsKey(loanKey))
            {
                result[loanKey] = new CreditBureauReportQueueItem<CreditRegistrationBusinessDetailRequest>
                {
                    LoanKey = loanKey,
                    Request = new CreditRegistrationBusinessDetailRequest
                    {
                        PContractId = GetString(reader, "pContractId"),   // [УникальныйIDДоговора]
                        PContractType = GetString(reader, "pContractType"), // [КодТипаКредитования]
                        PDate = GetString(reader, "pDate"),         // [ДатаОтправки]
                        PRepaymentDetArray = new List<PBusinessRepaymentDetArray>()
                    }
                };
            }

            result[loanKey].Request.PRepaymentDetArray!.Add(new PBusinessRepaymentDetArray
            {
                AccountA = GetString(reader, "accountA"),    // [ДебетовыйПланСчётов] 
                AccountB = GetString(reader, "accountB"),    // [КредитовыйПланСчётов]
                Currency = GetString(reader, "currency"),   // [КодВалюты] UZ_s017
                Destination = GetString(reader, "destination"),// [КодНазначенияПлатежа]
                DocDate = GetString(reader, "docDate"),    // [ДатаПроведенияПлатежа]
                DocNum = GetString(reader, "docNum"),     // [НомерПлатёжногоДокумента]
                DocType = GetString(reader, "docType"),    // [КодТипаПлатёжногоДокумента] 
                NameA = GetString(reader, "nameA"),      // [НаименованиеОтправителя]
                NameB = GetString(reader, "nameB"),      // [НаименованиеПолучателя]
                PayType = GetString(reader, "payType"),    // [ФормаВыдачиПогашения] 
                PaymentId = GetString(reader, "paymentId"), // [УникальныйНомерДокумента]
                Purpose = GetString(reader, "purpose"),   // [ЦельПлатежа]
                Summa = GetDecimal(reader, "summa")     // [СуммаПлатежаВТийинах]
            });
        }

        await connection.CloseAsync();
        return result.Values.ToList();
    }

    public async Task<List<CreditBureauReportQueueItem<CreditRegistrationSubjectRequest>>>
    GetCreditRegistrationSubjectRequestsAsync(CancellationToken cancellationToken)
    {
        return await ExecuteTableFunctionAsync("KATM_Report_023", MapCreditRegistrationSubjectRequest, cancellationToken);
    }

    private static CreditRegistrationSubjectRequest MapCreditRegistrationSubjectRequest(SqlDataReader reader)
    {
        return new CreditRegistrationSubjectRequest
        {
            PClaimId = GetString(reader, "pClaimId"),           // [УникальныйIDЗаявки]
            PLoanSubject = GetString(reader, "pLoanSubject"),       // [КодТипаСубъекта] A18
            PLoanSubjectStatus = GetString(reader, "pLoanSubjectStatus"), // [СтатусСубъекта] A19
            POwnerId = GetString(reader, "pOwnerId"),           // [УникальныйНомерСубъекта]
            PDate = GetString(reader, "pDate"),              // [ДатаОтправки]
        };
    }
    public async Task<List<CreditReportQueueItem>>
    GetCreditReportRequestsAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_017]()", connection);
        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<CreditReportQueueItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CreditReportQueueItem
            {
                LoanKey = GetInt(reader, "loanKey") ?? 0,
                PClaimId = GetString(reader, "pClaimId"),              // [УникальныйIDЗаявки]
                PReportId = GetString(reader, "pReportId"),             // [IDОтчёта]
                PLoanSubject = GetString(reader, "pLoanSubject"),          // [ТипСубъекта] A18
                PLoanSubjectType = GetString(reader, "pLoanSubjectType"),      // [ПодтипСубъекта] A18
                PPin = GetString(reader, "pPin"),                  // [ПИНФЛ] для физлиц
                PTin = GetString(reader, "pTin"),                  // [ИНН] для юрлиц
                PReportFormat = GetInt(reader, "pReportFormat") ?? 0, // [ФорматОтчёта]
                PReportReason = GetInt(reader, "pReportReason") ?? 1,         // [ЦельИзучения] v9.15
                PToken = GetString(reader, "pToken"),                // [KATM-SIR] если есть
            });
        }
        await connection.CloseAsync();
        return result;
    }

    public async Task<List<CreditReportQueueItem>>
        GetCreditReportPollRequestsAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand("SELECT * FROM [dbo].[KATM_Report_017_Poll]()", connection);
        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<CreditReportQueueItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CreditReportQueueItem
            {
                LoanKey = GetInt(reader, "loanKey") ?? 0,
                PClaimId = GetString(reader, "pClaimId"),           // [УникальныйIDЗаявки]
                PToken = GetString(reader, "pToken"),             // [ТокенДляОпроса] (05050)
                PReportFormat = GetInt(reader, "pReportFormat") ?? 0, // [ФорматОтчёта]
            });
        }
        await connection.CloseAsync();
        return result;
    }
    public async Task UpsertCiStatusAsync(
        int loanKey,
        int ciCode,
        byte ciStatus,
        string? message,
        string? token,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.CIBConnection);
        using var command = new SqlCommand("[dbo].[Katm_Methods_Request_UpsertCiStatus]", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Clear();
        command.Parameters.AddWithValue("@loanKey", loanKey);
        command.Parameters.AddWithValue("@ciCode", ciCode);
        command.Parameters.AddWithValue("@ciStatus", ciStatus);
        command.Parameters.AddWithValue("@message", (object?)message ?? DBNull.Value);
        command.Parameters.AddWithValue("@token", (object?)token ?? DBNull.Value);

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await connection.CloseAsync();
    }

    public async Task<byte?> GetCreditBureau001StatusAsync(int loanKey, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.CIBConnection);
        using var command = new SqlCommand("SELECT ci001 FROM [dbo].[Katm_Methods_Request] WHERE [loanKey] = @loanKey", connection);
        command.Parameters.AddWithValue("@loanKey", loanKey);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        await connection.CloseAsync();
        return result == DBNull.Value ? null : Convert.ToByte(result);
    }

    public async Task<byte?> GetCreditBureau002StatusAsync(int loanKey, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.CIBConnection);
        using var command = new SqlCommand("SELECT ci002 FROM [dbo].[Katm_Methods_Request] WHERE [loanKey] = @loanKey", connection);
        command.Parameters.AddWithValue("@loanKey", loanKey);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        await connection.CloseAsync();
        return result == DBNull.Value ? null : Convert.ToByte(result);
    }

    private async Task<List<CreditBureauReportQueueItem<T>>> ExecuteTableFunctionAsync<T>(
        string functionName,
        Func<SqlDataReader, T> mapper,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_databaseSettings.DBConnection);
        using var command = new SqlCommand($"SELECT * FROM [dbo].[{functionName}]()", connection);

        await connection.OpenAsync(cancellationToken);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<CreditBureauReportQueueItem<T>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CreditBureauReportQueueItem<T>
            {
                LoanKey = GetInt(reader, "loanKey", "LoanKey", "keyLoanHistoryKb", "KeyLoanHistoryKb") ?? 0,
                Request = mapper(reader)
            });
        }

        await connection.CloseAsync();
        return result;
    }

    private static CreditRegistrationIndividualRequest MapCreditRegistrationIndividualRequest(SqlDataReader reader)
    {
        return new CreditRegistrationIndividualRequest
        {
            ClaimId = GetString(reader, "claimId"),
            ClaimDate = GetString(reader, "Date_in"),
            Inn = GetString(reader, "N_National"),
            ClaimNumber = GetString(reader, "app_bank"),
            AgreementNumber = GetString(reader, "app_bank2"),
            AgreementDate = GetString(reader, "Date_in2"),
            Resident = GetString(reader, "Resident"),
            DocumentType = GetString(reader, "Type_doc"),
            DocumentSerial = GetString(reader, "Series_doc"),
            DocumentNumber = GetString(reader, "Num_doc"),
            DocumentDate = GetString(reader, "Date_doc"),
            Gender = GetString(reader, "Sex"),
            ClientType = GetString(reader, "Sector"),
            BirthDate = GetString(reader, "Birth_date"),
            DocumentRegion = GetString(reader, "Doc_country"),
            DocumentDistrict = GetString(reader, "Doc_region"),
            Nibbd = GetString(reader, "ID"),
            FamilyName = GetString(reader, "Last_name"),
            Name = GetString(reader, "First_name"),
            Patronymic = GetString(reader, "Father_name"),
            RegistrationRegion = GetString(reader, "country_ID"),
            RegistrationDistrict = GetString(reader, "region_ID"),
            RegistrationAddress = GetString(reader, "Adress"),
            Phone = GetString(reader, "Tel"),
            Pin = GetString(reader, "Tax_number"),
            KatmSir = GetString(reader, "KATMSIR"),
            LiveAddress = GetString(reader, "Adress2"),
            LiveCadastr = GetString(reader, "Cadastral_N"),
            RegistrationCadastr = GetString(reader, "Cadastral_N2"),
            CreditType = GetString(reader, "Cod_loan"),
            Summa = GetString(reader, "Summ"),
            Procent = GetDecimalAsString(reader, "Procent"),
            CreditDuration = GetString(reader, "CreditDuration"),
            CreditExemption = GetString(reader, "CreditExemption"),
            Currency = GetString(reader, "Curr")
        };
    }

    private static CreditRegistrationEntityRequest MapCreditRegistrationEntityRequest(SqlDataReader reader)
    {
        return new CreditRegistrationEntityRequest
        {
            ClaimId = GetString(reader, "claimId", "pClaimId", "claim_id"),
            ClaimDate = GetString(reader, "Date_in", "claimDate", "claim_date"),
            Inn = GetString(reader, "N_National", "inn", "pInn"),
            ClaimNumber = GetString(reader, "app_bank", "claimNumber", "claim_number"),
            AgreementNumber = GetString(reader, "app_bank2", "agreementNumber", "agreement_number"),
            AgreementDate = GetString(reader, "Date_in2", "agreementDate", "agreement_date"),
            Resident = GetString(reader, "Resident", "resident"),
            JuridicalStatus = GetString(reader, "juridicalStatus", "juridical_status"),
            Nibbd = GetString(reader, "ID", "Nibbd", "nibbd"),
            ClientType = GetString(reader, "Sector", "clientType", "client_type"),
            Name = GetString(reader, "First_name", "name"),
            LiveCadastr = GetString(reader, "Cadastral_N", "liveCadastr", "live_cadastr"),
            OwnerForm = GetString(reader, "ownerForm", "owner_form"),
            Goverment = GetString(reader, "goverment"),
            RegistrationRegion = GetString(reader, "country_ID", "registrationRegion", "registration_region"),
            RegistrationDistrict = GetString(reader, "region_ID", "registrationDistrict", "registration_district"),
            RegistrationAddress = GetString(reader, "Adress", "registrationAddress", "registration_address"),
            Phone = GetString(reader, "Tel", "phone"),
            Hbranch = GetString(reader, "hBranch", "hbranch"),
            Oked = GetString(reader, "oked"),
            KatmSir = GetString(reader, "KATMSIR", "katmSir", "katm_sir"),
            Okpo = GetString(reader, "okpo"),
            CreditType = GetString(reader, "Cod_loan", "creditType", "credit_type"),
            Summa = GetString(reader, "Summ", "summa"),
            Procent = GetDecimalAsString(reader, "Procent", "procent"),
            CreditDuration = GetString(reader, "CreditDuration", "creditDuration", "credit_duration"),
            CreditExemption = GetString(reader, "CreditExemption", "creditExemption", "credit_exemption"),
            Currency = GetString(reader, "Curr", "currency")
        };
    }

    private static CreditRegistrationDeclineRequest MapCreditRegistrationDeclineRequest(SqlDataReader reader)
    {
        return new CreditRegistrationDeclineRequest
        {
            PHead = GetString(reader, "pHead", "head"),
            PCode = GetString(reader, "pCode", "code"),
            PDeclineDate = GetString(reader, "pDeclineDate", "declineDate"),
            PClaimId = GetString(reader, "pClaimId", "claimId"),
            PDeclineNumber = GetString(reader, "pDeclineNumber", "declineNumber"),
            PDeclineReason = GetString(reader, "pDeclineReason", "declineReason"),
            PDeclineReasonNote = GetString(reader, "pDeclineReasonNote", "declineReasonNote"),
            PDate = GetString(reader, "pDate", "date")
        };
    }

    private static CreditRegistrationRequest MapCreditRegistrationRequest(SqlDataReader reader)
    {
        return new CreditRegistrationRequest
        {
            PHead = GetString(reader, "pHead", "head"),
            PCode = GetString(reader, "pCode", "code"),
            PClaimId = GetString(reader, "pClaimId", "claimId"),
            PContractId = GetString(reader, "pContractId", "contractId"),
            PInn = GetInt(reader, "pInn", "inn"),
            PNibbd = GetString(reader, "pNibbd", "nibbd"),
            PType = GetString(reader, "pType", "type"),
            PObject = GetString(reader, "pObject", "object"),
            PStartDate = GetString(reader, "pStartDate", "startDate"),
            PEndDate = GetString(reader, "pEndDate", "endDate"),
            PCreditAmount = GetDecimal(reader, "pCreditAmount", "creditAmount"),
            PCurrency = GetString(reader, "pCurrency", "currency"),
            PPercent = GetDecimal(reader, "pPercent", "percent"),
            PJuridicalNumber = GetString(reader, "pJuridicalNumber", "juridicalNumber"),
            PSupply = GetString(reader, "pSupply", "supply"),
            PQuality = GetString(reader, "pQuality", "quality"),
            PUrgency = GetString(reader, "pUrgency", "urgency"),
            PHBranch = GetString(reader, "pHBranch", "hBranch"),
            PActivity = GetString(reader, "pActivity", "activity"),
            PReason = GetString(reader, "pReason", "reason"),
            PFounder = GetString(reader, "pFounder", "founder"),
            PDate = GetString(reader, "pDate", "date")
        };
    }

    /*
        private static List<CreditRegistrationRepaymentSchedule>
            MapCreditRegistrationRepaymentSchedule(SqlDataReader reader)
        {
            var result = new Dictionary<string, CreditRegistrationRepaymentSchedule>();

            while (reader.Read())
            {
                var loanKey = GetString(reader, "loanKey", "loanKey");

                if (!result.ContainsKey(loanKey))
                {
                    result[loanKey] = new CreditRegistrationRepaymentSchedule
                    {
                        PHead = GetString(reader, "pHead", "head"),
                        PCode = GetString(reader, "pCode", "code"),
                        PClaimId = GetString(reader, "pClaimId", "pClaimId"),
                        PContractId = GetString(reader, "pContractId", "pContractId"),
                        PNibbd = GetString(reader, "PNibbd", "PNibbd"),
                        PDate = GetString(reader, "pDate", "pDate"),
                        PIsUpdate = GetString(reader, "PIsUpdate", "pIsUpdate"),
                        PPlanArray = new List<PPlanArray>()
                    };
                }

                result[loanKey].PPlanArray.Add(new PPlanArray
                {
                    Amount = GetString(reader, "Amount", "principalAmount"),
                    Currency = GetString(reader, "Currency", "currency"),
                    Date = GetString(reader, "Date", "scheduleDate"),
                    Percent = GetString(reader, "Percent", "percentAmount")
                });
            }

            return result.Values.ToList();
        }
        */
    private static CreditRegistrationPledgeOwner MapCreditRegistrationPledgeOwnerRequest(SqlDataReader reader)
    {
        return new CreditRegistrationPledgeOwner
        {
            PHead = GetString(reader, "pHead", "head"),
            PCode = GetString(reader, "pCode", "code"),
            PInn = GetString(reader, "pInn", "inn"),
            PNibbd = GetString(reader, "pNibbd", "nibbd"),
            PAgreementDate = GetString(reader, "pAgreementDate", "agreement_date"),
            PAgreementNumber = GetString(reader, "pAgreementNumber", "agreement_number"),
            PClientId = GetString(reader, "pClientId", "client_id"),
            PClientType = GetString(reader, "pClientType", "client_type"),
            PDate = GetString(reader, "pDate", "date"),
            PDateBirthday = GetString(reader, "pDateBirthday", "date_birthday"),
            PFio = GetString(reader, "pFio", "fio"),
            PFullName = GetString(reader, "pFullName", "full_name"),
            PIdentityCardDate = GetString(reader, "pIdentityCardDate", "identity_card_date"),
            PIdentityCardNumber = GetString(reader, "pIdentityCardNumber", "identity_card_number"),
            PIdentityCardSerial = GetString(reader, "pIdentityCardSerial", "identity_card_serial"),
            PIdentityCardType = GetString(reader, "pIdentityCardType", "identity_card_type"),
            PLegalAddress = GetString(reader, "pLegalAddress", "legal_address"),
            POwnerId = GetString(reader, "pOwnerId", "owner_id"),
            PPersonalCode = GetString(reader, "pPersonalCode", "personal_code"),
            PResident = GetString(reader, "pResident", "resident"),
            PSex = GetString(reader, "pSex", "sex"),
            PSubjectType = GetString(reader, "pSubjectType", "subject_type")
        };
    }

    private static CreditRegistrationPledgeSecurity MapCreditRegistrationPledgeSecurity(SqlDataReader reader)
    {
        return new CreditRegistrationPledgeSecurity
        {
            PClaimId = GetString(reader, "pClaimId"), // [УникальныйНомерЗаявки]
            PAgreementDate = GetString(reader, "pAgreementDate"), // [ДатаДокументаСогласияВладельца]
            PAgreementNumber = GetString(reader, "pAgreementNumber"), // [НомерДокументаСогласия]
            PContractDate = GetString(reader, "pContractDate"), // [ДатаДоговораОЗалоге]
            PContractNumber = GetString(reader, "pContractNumber"), // [НомерДоговораОЗалоге]
            PCurrency = GetString(reader, "pCurrency"), // [ВалютаДоговораЗалога] UZ_s017
            PDescription = GetString(reader, "pDescription"), // [ДополнительныеДанныеПоАктивам] (А14)
            PGuaranteeId = GetString(reader, "pGuaranteeId"), // [УникальныйНомерОбеспечения]
            PGuaranteeType = GetString(reader, "pGuaranteeType"), // [КодТипаОбеспечения] UZ_s033
            PLoanSubject = GetString(reader, "pLoanSubject"), // [ТипСубъектаКредитнойИнформации] A18
            PName = GetString(reader, "pName"), // [НаименованиеОбеспечения]
            POwnerId = GetString(reader, "pOwnerId"), // [УникальныйНомерВладельца]
            PStatus = GetString(reader, "pStatus"), // [КодСтатусаОбеспечения] A13
            PSumma = GetString(reader, "pSumma"), // [ОценочнаяСтоимостьОбеспечения]
        };
    }

    /* private static CreditRegistrationRepayment MapCreditRegistrationRepayment(SqlDataReader reader)
     {
         return new CreditRegistrationRepayment
         {
             PHead = GetString(reader, "pHead", "head"),
             PCode = GetString(reader, "pCode", "code"),
             PContractId = GetString(reader, "pContractId"),
             PContractType = GetString(reader, "pContractType"),
             PDate = GetString(reader, "pDate"),
             PLoanStatus = GetString(reader, "pLoanStatus"),
         };
     }
    */
    private static AccountStatusItem? MapAccountStatusItem(SqlDataReader reader)
    {
        var account = GetString(reader, "Account");
        if (string.IsNullOrWhiteSpace(account)) return null;

        return new AccountStatusItem
        {
            Date = GetString(reader, "statusDate"),   // [ДатаОбновленияСтатусаСчёта]
            Account = account,                           // [Счёт]
            Coa = GetString(reader, "coa"),          // [ПланСчётов]
            DateOpen = GetString(reader, "Open_date"),    // [ДатаОткрытияСчёта]
            DateClose = GetString(reader, "Date_end"),     // [ДатаЗакрытияСчёта] (null bo'lishi mumkin)
        };
    }

    private static CreditRegistrationLeasingRequest MapCreditRegistrationLeasingRequest(SqlDataReader reader)
    {
        return new CreditRegistrationLeasingRequest
        {
            PHead = GetString(reader, "pHead"),
            PCode = GetString(reader, "pCode"),
            PClaimId = GetString(reader, "pClaimId"),
            PContractId = GetString(reader, "pContractId"),
            PInn = GetString(reader, "pInn"),
            PNibbd = GetString(reader, "pNibbd"),
            PStartDate = GetString(reader, "pStartDate"),
            PEndDate = GetString(reader, "pEndDate"),
            PNotariusCertNumber = GetString(reader, "pNotariusCertNumber"),
            PNotariusCertDate = GetString(reader, "pNotariusCertDate"),
            PNotariusRegNumber = GetString(reader, "pNotariusRegNumber"),
            PNotariusRegDate = GetString(reader, "pNotariusRegDate"),
            PGovernmentRegNum = GetString(reader, "pGovernmentRegNum"),
            PGovernmentRegDate = GetString(reader, "pGovernmentRegDate"),
            PCreditAmount = GetDecimal(reader, "pCreditAmount") ?? 0m,
            PCurrency = GetString(reader, "pCurrency"),
            PPercent = GetDecimal(reader, "pPercent") ?? 0m,
            PCountObject = GetInt(reader, "pCountObject") ?? 0,
            PDate = GetString(reader, "pDate")
        };
    }

    private static CreditRegistrationFactoring MapCreditRegistrationFactoring(SqlDataReader reader)
    {
        return new CreditRegistrationFactoring
        {
            PClaimId = GetString(reader, "pClaimId"),         // [УникальныйIDЗаявки]
            PContractId = GetString(reader, "pContractId"),      // [УникальныйIDДоговора]
            PInn = GetString(reader, "pInn"),             // [ИННКлиента] (O, обязателен для юр.лиц)
            PNibbd = GetString(reader, "pNibbd"),           // [НИББДКлиента]
            PCreditAmount = GetDecimal(reader, "pCreditAmount") ?? 0m, // [СуммаКредитаВТийинах]
            PCurrency = GetString(reader, "pCurrency"),        // [КодВалюты] UZ_s017
            PBankElement = GetString(reader, "pBankElement"),     // [КодОрганаБанка] 0A5 (O)
            PFactoringNumber = GetString(reader, "pFactoringNumber"), // [НомерРешенияОВыдаче] (O)
            PSummaLiability = GetString(reader, "pSummaLiability"), // [СуммаОбязательстваДолжника] (O)
            PSummaDiscount = GetString(reader, "pSummaDiscount"),  // [СуммаДисконта] (O)
            PInnDebtor = GetString(reader, "pInnDebtor"),      // [ИННДолжника] (O)
            PDate = GetString(reader, "pDate"),           // [ДатаОтправки]
            PStartDate = GetString(reader, "pStartDate"),      // [ДатаНачалаДоговора]
            PEndDate = GetString(reader, "pEndDate"),        // [ДатаОкончанияДоговора]
        };
    }

    private static bool HasColumn(IDataRecord reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetString(SqlDataReader reader, params string[] columnNames)
    {
        foreach (var columnName in columnNames)
        {
            if (!HasColumn(reader, columnName))
            {
                continue;
            }

            var value = reader[columnName];
            if (value is not DBNull)
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static int? GetInt(SqlDataReader reader, params string[] columnNames)
    {
        var value = GetString(reader, columnNames);
        return int.TryParse(value, out var number) ? number : null;
    }

    private static decimal? GetDecimal(SqlDataReader reader, params string[] columnNames)
    {
        var value = GetString(reader, columnNames);
        return decimal.TryParse(value, out var number) ? number : null;
    }

    private static string? GetDecimalAsString(SqlDataReader reader, params string[] columnNames)
    {
        var number = GetDecimal(reader, columnNames);
        return number?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
