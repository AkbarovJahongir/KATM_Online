using Infrastructure.CreditRegistration;
using Infrastructure.CreditReports;
using Infrastructure.CreditReportsXml;
using Infrastructure.Services.AsokiServices;
using Infrastructure.Services.AsokiXmlServices;
using Infrastructure.Services.CreditBureauReportServices;
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
            services.AddSingleton<ICreditBureauReportService, CreditBureauReportService>();
        }
    }
}
