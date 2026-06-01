using Application;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;
using CreditBureauService.Contracts.Common;
using Domain.Common.DbContext;
using Domain.Common.Settings;
using Infrastructure;
using Infrastructure.AESOperation;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.CreditReportsXml.Parsers;

namespace CreditBureauService
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

/// START CreditBureau Settings
            var creditBureauApiOptions = new CreditBureauApiOptions();
            configuration.Bind(nameof(CreditBureauApiOptions), creditBureauApiOptions);
            services.AddSingleton(creditBureauApiOptions);
            Console.WriteLine("CreditBureauApiOptions зарегистрированы");

            var bankHeader = new BankHeader();
            configuration.Bind(nameof(BankHeader), bankHeader);
            services.AddSingleton(bankHeader);
            Console.WriteLine("BankHeader зарегистрирован");

            var creditBureauReportApiOptions = new CreditBureauReportApiOptions();
            configuration.Bind(nameof(CreditBureauReportApiOptions), creditBureauReportApiOptions);
            services.AddSingleton(creditBureauReportApiOptions);
            Console.WriteLine("CreditBureauReportApiOptions зарегистрированы");

            var security = new RequestSecurity() { pLogin = creditBureauReportApiOptions.PLogin, pPassword = creditBureauReportApiOptions.PPassword };
            services.AddSingleton(security);
            Console.WriteLine("RequestSecurity зарегистрирован");

            services.AddHttpClient();
            Console.WriteLine("HttpClient добавлен");

            var telegramNotificationSettings = new TelegramNotificationSettings();
            configuration.Bind(nameof(TelegramNotificationSettings), telegramNotificationSettings);
            services.AddSingleton(telegramNotificationSettings);
            services.AddSingleton<GlobalExceptionNotifier>();
            Console.WriteLine($"TelegramNotificationSettings зарегистрированы: Enabled={telegramNotificationSettings.Enabled}");
            // END CreditBureau Settings

            // Условная регистрация парсера по фичефлагу
            if (workerSettings.IsStopFactorParser)
            {
                services.AddSingleton<ICreditReportXmlParser, CreditBureauCreditReportXmlParser>();
                Console.WriteLine("ICreditReportXmlParser => CreditBureauCreditReportXmlParser");
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
