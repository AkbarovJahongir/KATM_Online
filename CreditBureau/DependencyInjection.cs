using Application;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications;
using CreditBureau.Contracts.Common;
using Domain.Common.DbContext;
using Domain.Common.Settings;
using Infrastructure;
using Infrastructure.AESOperation;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.CreditReportsXml.Parsers;

namespace CreditBureau
{
    public static class DependencyInjection
    {
        public static void AddCreditBureau(this IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("⚙️ Старт AddCreditBureau...");

            var workerSettings = new WorkerSettings();
            configuration.Bind(nameof(WorkerSettings), workerSettings);
            services.AddSingleton(workerSettings);
            Console.WriteLine($"WorkerSettings загружены: IsStopFactorParser={workerSettings.IsStopFactorParser}");

            services.AddOptions<StopFactorParserSettings>()
                    .Bind(configuration.GetSection("StopFactorParserSettings"));
            Console.WriteLine("StopFactorParserSettings подключены");

            // Adding Logger To File
            var logger = new LogWriter(Directory.GetCurrentDirectory(), workerSettings.Logs);
            services.AddSingleton(logger);
            Console.WriteLine($"LogWriter инициализирован: Путь={workerSettings.Logs}");
            // Adding Logger To File

            /// START DB SETTINGS
            var databaseSettings = new DatabaseSettings();
            configuration.Bind(nameof(DatabaseSettings), databaseSettings);
            var aes = new AesOperationService();
            databaseSettings.CIBConnection = databaseSettings.CIBConnection.Replace("**", aes.DecryptString(databaseSettings.Key, databaseSettings.CIBPassword));
            databaseSettings.DBConnection = databaseSettings.DBConnection.Replace("**", aes.DecryptString(databaseSettings.Key, databaseSettings.DBPassword));
            services.AddSingleton(databaseSettings);
            Console.WriteLine("DatabaseSettings подключены и расшифрованы");
            // END DB SETTINGS

            /// START Asoki Settings
            var asokiApplicationApiOptions = new AsokiApplicationApiOptions();
            configuration.Bind(nameof(AsokiApplicationApiOptions), asokiApplicationApiOptions);
            services.AddSingleton(asokiApplicationApiOptions);
            Console.WriteLine("AsokiApplicationApiOptions зарегистрированы");

            var bankHeader = new BankHeader();
            configuration.Bind(nameof(BankHeader), bankHeader);
            services.AddSingleton(bankHeader);
            Console.WriteLine("BankHeader зарегистрирован");

            var asokiReportApiOptions = new AsokiReportApiOptions();
            configuration.Bind(nameof(AsokiReportApiOptions), asokiReportApiOptions);
            services.AddSingleton(asokiReportApiOptions);
            Console.WriteLine("AsokiReportApiOptions зарегистрированы");

            var security = new RequestSecurity() { pLogin = asokiReportApiOptions.PLogin, pPassword = asokiReportApiOptions.PPassword };
            services.AddSingleton(security);
            Console.WriteLine("RequestSecurity зарегистрирован");

            services.AddHttpClient();
            Console.WriteLine("HttpClient добавлен");
            // END Asoki Settings

            // Условная регистрация парсера по фичефлагу
            if (workerSettings.IsStopFactorParser)
            {
                services.AddSingleton<ICreditReportXmlParser, AsokiCreditReportXmlParser>();
                Console.WriteLine("ICreditReportXmlParser => AsokiCreditReportXmlParser");
            }
            else
            {
                services.AddSingleton<ICreditReportXmlParser, NopCreditReportXmlParser>();
                Console.WriteLine("ICreditReportXmlParser => NopCreditReportXmlParser");
            }

            // Adding Dependency Project
            services.AddApplication();
            services.AddInfrastructure();
            Console.WriteLine("Добавлены Application + Infrastructure");
            // Adding DependencyProject

            Console.WriteLine("✅ AddCreditBureau завершён");
        }
    }
}
