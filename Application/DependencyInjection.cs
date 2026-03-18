using Application.CreditRegistration;
using Application.CreditReports;
using Application.Repositories.AsokiRepositories;
using Application.Repositories.CreditBureauReportRepositories;
using Application.Repositories.CreditReportXmlParserRepositories;
using Application.Repositories.Helpers;
using Application.Repositories.RequestManager;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ICreditReportRepository, CreditReportRepository>();
        services.AddSingleton<ICreditRegistrationRepository, CreditRegistrationRepository>();
        services.AddSingleton<IRequestManagerRepository, RequestManagerRepository>();
        services.AddSingleton<IHelperRepository, HelperRepository>();
        services.AddSingleton<IAsokiRepository, AsokiRepository>();
        services.AddSingleton<ICreditReportXmlParserRepository, CreditReportXmlParserRepository>();
        services.AddSingleton<ICreditBureauReportRepository, CreditBureauReportRepository>();
    }
}
