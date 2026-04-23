using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications;
using Domain.Common.DbContext;
using Microsoft.Data.SqlClient;

namespace Application.CreditRegistration
{
    public class CreditRegistrationRepository(DatabaseSettings databaseSettings) : ICreditRegistrationRepository
    {
        private readonly DatabaseSettings _databaseSettings = databaseSettings;
        public async Task<List<RequestHistoryData>> GetRequestHistoryDatasAsync(CancellationToken cancellationToken)
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

            var resultData = new List<RequestHistoryData>();
            while (await reader.ReadAsync(cancellationToken))
            {
                resultData.Add(new RequestHistoryData()
                {
                    KeyLoanHistoryKb = reader["keyLoanHistoryKb"].ToString()!,
                    PClaimId = reader["pClaimId"].ToString()!,
                    ApplicationsSubjectType = reader["ApplicationsSubjectType"].ToString()!, // Тип если 0 то физ лицо если 1 то юр лицо
                    Status = reader["status"] is DBNull ? null : reader["status"].ToString(),
                });
            }
            await connect.CloseAsync();
            return resultData;
        }
        /// <summary>
        /// Получаем объект для Регистрация кредитной заявки физического лица
        /// </summary>
        /// <param name="keyAbsLoanHistory">Ключ из таблицы Loan_History_Kb</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Возвращает объект кредитной заявки физ. лица если успешно иначе null.</returns>
        public async Task<CreditRegistrationIndividualRequest?> GetCreditRegistrationIndividualRequest(string keyAbsLoanHistory, CancellationToken cancellationToken)
        {
            try
            {
                using var connect = new SqlConnection(_databaseSettings.DBConnection);
                using var cmd = new SqlCommand(
                    "select * from [dbo].[Report_KATM_001_Api](@keyAbsLoanHistory)",
                    connect);
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@keyAbsLoanHistory", keyAbsLoanHistory);
                await connect.OpenAsync(cancellationToken);
                using var reader = cmd.ExecuteReader();
                CreditRegistrationIndividualRequest? creditRegistrationIndividualRequest = null;
                if (await reader.ReadAsync(cancellationToken))
                {
                    creditRegistrationIndividualRequest = MapCreditRegistrationIndividualRequest(reader);
                }
                await connect.CloseAsync();
                return creditRegistrationIndividualRequest;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static CreditRegistrationIndividualRequest MapCreditRegistrationIndividualRequest(SqlDataReader reader)
        {
            CreditRegistrationIndividualRequest? creditRegistrationIndividualRequest;
            creditRegistrationIndividualRequest = new CreditRegistrationIndividualRequest()
            {
                ClaimId = reader["claimId"] is DBNull ? null : reader["claimId"].ToString(), // УникальныйНомерЗаявки
                ClaimDate = reader["Date_in"] is DBNull ? null : reader["Date_in"].ToString(), // ДатаПодачиЗаявки
                Inn = reader["N_National"] is DBNull ? null : reader["N_National"].ToString(), //ИННзаявителя
                ClaimNumber = reader["app_bank"] is DBNull ? null : reader["app_bank"].ToString(), //ЮридическийНомерЗаявки
                AgreementNumber = reader["app_bank2"] is DBNull ? null : reader["app_bank2"].ToString(), //НомерДокументаСогласияЗаемщика
                AgreementDate = reader["Date_in2"] is DBNull ? null : reader["Date_in2"].ToString(),    //ДатаДокументаСогласияЗаемщика
                Resident = reader["Resident"] is DBNull ? null : reader["Resident"].ToString(),         //КодРезидентностиПотенциальногоЗаемщика	UZ_s027
                DocumentType = reader["Type_doc"] is DBNull ? null : reader["Type_doc"].ToString(),     //КодУдостоверенияЛичности	UZ_s008
                DocumentSerial = reader["Series_doc"] is DBNull ? null : reader["Series_doc"].ToString(), //СерияДокумента	
                DocumentNumber = reader["Num_doc"] is DBNull ? null : reader["Num_doc"].ToString(),    //НомерДокумента	
                DocumentDate = reader["Date_doc"] is DBNull ? null : reader["Date_doc"].ToString(),     //ДатаВыдачиУдостоверяющегоДокумента	
                Gender = reader["Sex"] is DBNull ? null : reader["Sex"].ToString(),                //Пол	UZ_s007
                ClientType = reader["Sector"] is DBNull ? null : reader["Sector"].ToString(),         //КодТипаКлиента	UZ_s021
                BirthDate = reader["Birth_date"] is DBNull ? null : reader["Birth_date"].ToString(),      //ДатаРождения	
                DocumentRegion = reader["Doc_country"] is DBNull ? null : reader["Doc_country"].ToString(), //КодОбластиВыдачиПаспорта	UZ_s016
                DocumentDistrict = reader["Doc_region"] is DBNull ? null : reader["Doc_region"].ToString(), //КодРайонаВыдачиПаспорта	UZ_s052
                Nibbd = reader["ID"] is DBNull ? null : reader["ID"].ToString(),                    //КодКлиентаПоНИББД
                FamilyName = reader["Last_name"] is DBNull ? null : reader["Last_name"].ToString(),        //Фамилия
                Name = reader["First_name"] is DBNull ? null : reader["First_name"].ToString(),             //Имя
                Patronymic = reader["Father_name"] is DBNull ? null : reader["Father_name"].ToString(),      //Отчество
                RegistrationRegion = reader["country_ID"] is DBNull ? null : reader["country_ID"].ToString(), //КодОбластиПрописки	UZ_s016
                RegistrationDistrict = reader["region_ID"] is DBNull ? null : reader["region_ID"].ToString(),//КодРайонаПрописки		UZ_s052
                RegistrationAddress = reader["Adress"] is DBNull ? null : reader["Adress"].ToString(),    //ПочтовыйАдресПрописки
                Phone = reader["Tel"] is DBNull ? null : reader["Tel"].ToString(),                     //НомерТелефона
                Pin = reader["Tax_number"] is DBNull ? null : reader["Tax_number"].ToString(),                     //ПерсональныйКодГражданина
                KatmSir = reader["KATMSIR"] is DBNull ? null : reader["KATMSIR"].ToString(),                     //KATMSIR
                LiveAddress = reader["Adress2"] is DBNull ? null : reader["Adress2"].ToString(),                     //ПочтовыйАдресПроживания	
                LiveCadastr = reader["Cadastral_N"] is DBNull ? null : reader["Cadastral_N"].ToString(),                 //КадастровыйНомерПомещенияПроживанияСКИ	
                RegistrationCadastr = reader["Cadastral_N2"] is DBNull ? null : reader["Cadastral_N2"].ToString(),         //КадастровыйНомерПомещенияПропискиСКИ	
                CreditType = reader["Cod_loan"] is DBNull ? null : reader["Cod_loan"].ToString(),                 //КодВидаКредита
                Summa = reader["Summ"] is DBNull ? null : reader["Summ"].ToString(),//Convert.ToDecimal(reader["Summ"]).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), //СуммаДоговора
                Procent = reader["Procent"] is DBNull ? null : Convert.ToDecimal(reader["Procent"]).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), //ГодоваяПроцентнаяСтавка
                CreditDuration = reader["CreditDuration"] is DBNull ? null : reader["CreditDuration"].ToString(),   //Предполагаемыйсроккредита
                CreditExemption = reader["CreditExemption"] is DBNull ? null : reader["CreditExemption"].ToString(), //Льготныйпериодосновногодолга
                Currency = reader["Curr"] is DBNull ? null : reader["Curr"].ToString()   //[Кодвалюты] UZ_s017
            };
            return creditRegistrationIndividualRequest;
        }

        public Task<CreditRegistrationEntityRequest?> GetCreditRegistrationEntityRequest(string keyAbsLoan, CancellationToken cancellationToken)
            => GetCreditRegistrationEntityRequestInternal(keyAbsLoan, cancellationToken);

        private async Task<CreditRegistrationEntityRequest?> GetCreditRegistrationEntityRequestInternal(string keyAbsLoanHistory, CancellationToken cancellationToken)
        {
            try
            {
                using var connect = new SqlConnection(_databaseSettings.DBConnection);
                using var cmd = new SqlCommand(
                    "select * from [dbo].[Report_KATM_002_Api](@keyAbsLoanHistory)",
                    connect);
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@keyAbsLoanHistory", keyAbsLoanHistory);
                await connect.OpenAsync(cancellationToken);
                using var reader = cmd.ExecuteReader();
                CreditRegistrationEntityRequest? creditRegistrationEntityRequest = null;
                if (await reader.ReadAsync(cancellationToken))
                {
                    creditRegistrationEntityRequest = MapCreditRegistrationEntityRequest(reader);
                }
                await connect.CloseAsync();
                return creditRegistrationEntityRequest;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static CreditRegistrationEntityRequest MapCreditRegistrationEntityRequest(SqlDataReader reader)
        {
            return new CreditRegistrationEntityRequest()
            {
                ClaimId = reader["claimId"] is DBNull ? null : reader["claimId"].ToString(),
                ClaimDate = reader["Date_in"] is DBNull ? null : reader["Date_in"].ToString(),
                Inn = reader["N_National"] is DBNull ? null : reader["N_National"].ToString(),
                ClaimNumber = reader["app_bank"] is DBNull ? null : reader["app_bank"].ToString(),
                AgreementNumber = reader["app_bank2"] is DBNull ? null : reader["app_bank2"].ToString(),
                AgreementDate = reader["Date_in2"] is DBNull ? null : reader["Date_in2"].ToString(),
                Resident = reader["Resident"] is DBNull ? null : reader["Resident"].ToString(),
                JuridicalStatus = reader["juridicalStatus"] is DBNull ? null : reader["juridicalStatus"].ToString(),
                Nibbd = reader["ID"] is DBNull ? null : reader["ID"].ToString(),
                ClientType = reader["Sector"] is DBNull ? null : reader["Sector"].ToString(),
                Name = reader["First_name"] is DBNull ? null : reader["First_name"].ToString(),
                LiveCadastr = reader["Cadastral_N"] is DBNull ? null : reader["Cadastral_N"].ToString(),
                OwnerForm = reader["ownerForm"] is DBNull ? null : reader["ownerForm"].ToString(),
                Goverment = reader["goverment"] is DBNull ? null : reader["goverment"].ToString(),
                RegistrationRegion = reader["country_ID"] is DBNull ? null : reader["country_ID"].ToString(),
                RegistrationDistrict = reader["region_ID"] is DBNull ? null : reader["region_ID"].ToString(),
                RegistrationAddress = reader["Adress"] is DBNull ? null : reader["Adress"].ToString(),
                Phone = reader["Tel"] is DBNull ? null : reader["Tel"].ToString(),
                Hbranch = reader["hBranch"] is DBNull ? null : reader["hBranch"].ToString(),
                Oked = reader["oked"] is DBNull ? null : reader["oked"].ToString(),
                KatmSir = reader["KATMSIR"] is DBNull ? null : reader["KATMSIR"].ToString(),
                Okpo = reader["okpo"] is DBNull ? null : reader["okpo"].ToString(),
                CreditType = reader["Cod_loan"] is DBNull ? null : reader["Cod_loan"].ToString(),
                Summa = reader["Summ"] is DBNull ? null : reader["Summ"].ToString(),
                Procent = reader["Procent"] is DBNull ? null : Convert.ToDecimal(reader["Procent"]).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                CreditDuration = reader["CreditDuration"] is DBNull ? null : reader["CreditDuration"].ToString(),
                CreditExemption = reader["CreditExemption"] is DBNull ? null : reader["CreditExemption"].ToString(),
                Currency = reader["Curr"] is DBNull ? null : reader["Curr"].ToString()
            };
        }
    }
}
