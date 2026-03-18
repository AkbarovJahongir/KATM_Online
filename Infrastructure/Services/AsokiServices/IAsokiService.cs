namespace Infrastructure.Services.AsokiServices
{
    public interface IAsokiService
    {
        Task AsokiProcessing() => AsokiProcessing(CancellationToken.None);
        Task AsokiProcessing(CancellationToken cancellationToken);
    }
}
