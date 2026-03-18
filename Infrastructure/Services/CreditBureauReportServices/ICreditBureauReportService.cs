namespace Infrastructure.Services.CreditBureauReportServices;

public interface ICreditBureauReportService
{
    Task CreditBureauReportProcessing() => CreditBureauReportProcessing(CancellationToken.None);
    Task CreditBureauReportProcessing(CancellationToken cancellationToken);
}
