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
            AddNextAccess
        }
        Task KatmHelper(string keyLoanHistory, string data, TypeOperation typeOperation, CancellationToken cancellationToken);
        Task KatmHelperXml(string keyLoanHistory, string data, TypeOperation typeOperation, CancellationToken cancellationToken);
    }
}
