namespace Infrastructure.Services.CreditBureauXmlServices
{
    public interface ICreditBureauXmlService
    {
        Task CreditBureauProcessingXml() => CreditBureauProcessingXml(CancellationToken.None);
        Task CreditBureauProcessingXml(CancellationToken cancellationToken);
    }
}