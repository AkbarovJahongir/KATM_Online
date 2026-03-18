using CreditBureau.Contracts.Common;
using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications
{
    public class BaseRequestForCreditApplications<T>
    {
        [JsonProperty(PropertyName = "header")]
        public BankHeader? Header { get; set; }
        [JsonProperty(PropertyName = "request")]
        public T? Request { get; set; }
        [JsonProperty(PropertyName = "security")]
        public RequestSecurity? Security { get; set; }
    }
    public class BankHeader
    {
        [JsonProperty(PropertyName = "type")]
        public string? Type { get; set; }
        [JsonProperty(PropertyName = "code")]
        public string? Code { get; set; }
    }
}
