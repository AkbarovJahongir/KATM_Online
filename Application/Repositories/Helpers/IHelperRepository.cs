namespace Application.Repositories.Helpers
{
    public interface IHelperRepository
    {
        enum TypeOperation
        {
            IndividualRequestSuccessful,
            Token,
            Base64,
            Error,
            Comment,
            AddNextAccess
        }
        Task KatmHelper(string keyLoanHistory, string data, TypeOperation typeOperation, CancellationToken cancellationToken, string? requestSubtype = null);
        Task KatmHelperXml(string keyLoanHistory, string data, TypeOperation typeOperation, CancellationToken cancellationToken, string? requestSubtype = null);
    }
}
