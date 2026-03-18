namespace CreditBureau.Contracts.Common
{
    public class BaseResponse<T>
    {
        public T? data { get; set; }
        public string? errorMessage { get; set; }
        public int code { get; set; }
    }
}
