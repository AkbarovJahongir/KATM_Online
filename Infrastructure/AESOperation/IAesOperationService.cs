namespace Infrastructure.AESOperation
{
    public interface IAesOperationService
    {
        string EncryptString(string key, string plainText);
        string DecryptString(string key, string cipherText);

    }
}
