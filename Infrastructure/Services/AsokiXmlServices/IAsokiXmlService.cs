namespace Infrastructure.Services.AsokiXmlServices
{
    public interface IAsokiXmlService
    {
        Task AsokiProcessingXml() => AsokiProcessingXml(CancellationToken.None);
        Task AsokiProcessingXml(CancellationToken cancellationToken);
    }
}
