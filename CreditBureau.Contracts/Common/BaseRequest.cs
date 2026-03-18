using Newtonsoft.Json;

namespace CreditBureau.Contracts.Common
{
    public class BaseRequest<T>
    {
        [JsonProperty(PropertyName = "data")]
        public T? Data { get; set; }
        [JsonProperty(PropertyName = "security")]
        public RequestSecurity? Security { get; set; }
    }
}
