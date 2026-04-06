using Infrastructure.CreditRegistration;
using Infrastructure.CreditReports;
using Infrastructure.CreditReportsXml;
using Infrastructure.Services.AsokiServices;
using Infrastructure.Services.AsokiXmlServices;
using Infrastructure.Services.CreditBureauReportServices;
using Infrastructure.Services.CreditBureauReportServices.Handlers;
using Infrastructure.Services.HttpClients;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<ICreditReportService, CreditReportService>();
            services.AddSingleton<ICreditRegistrationService, CreditRegistrationService>();
            services.AddSingleton<IRequestManagerService, RequestManagerService>();
            services.AddSingleton<IAsokiXmlService, AsokiXmlService>();
            services.AddSingleton<IAsokiService, AsokiService>();
            services.AddSingleton<ICreditReportXmlService, CreditReportXmlService>();
            
            // Обработчики CI-запросов
            services.AddSingleton<ICiHandler, Ci001IndividualRequestHandler>();
            services.AddSingleton<ICiHandler, Ci002EntityRequestHandler>();
            services.AddSingleton<ICiHandler, Ci003DeclineRequestHandler>();
            services.AddSingleton<ICiHandler, Ci004CreditRegistrationRequestHandler>();
            services.AddSingleton<ICiHandler, Ci005RepaymentScheduleHandler>();
            services.AddSingleton<ICiHandler, Ci015RepaymentRequestHandler>();
            services.AddSingleton<ICiHandler, Ci016BankDetailRequestHandler>();
            services.AddSingleton<ICiHandler, Ci018AccountStatusRequestHandler>();
            services.AddSingleton<ICiHandler, Ci020PledgeOwnerRequestHandler>();
            services.AddSingleton<ICiHandler, Ci021PledgeSecurityRequestHandler>();
            services.AddSingleton<ICiHandler, Ci022BusinessDetailRequestHandler>();
            services.AddSingleton<ICiHandler, Ci023SubjectRequestHandler>();
            
            // Менеджер обработки
            services.AddSingleton<CreditBureauProcessingManager>();
            
            // Рефакторенный сервис (использует обработчики)
            services.AddSingleton<ICreditBureauReportService, CreditBureauReportService>();
        }
    }
}
