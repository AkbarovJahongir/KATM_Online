using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications
{
    public class CreditRegistrationSubjectResponse
    {
        [JsonProperty(PropertyName = "result")]
        public Result? Result { get; set; }
        [JsonProperty(PropertyName = "header")]
        public Header? Header { get; set; }
        [JsonProperty(PropertyName = "response")]
        public Response? Response { get; set; }
    }
    public class Result
    {
        [JsonProperty(PropertyName = "code")]
        public string? Code { get; set; }
        [JsonProperty(PropertyName = "message")]
        public string? Message { get; set; }
    }
    public class Header
    {
        [JsonProperty(PropertyName = "received")]
        public string? RequestTime { get; set; }
        [JsonProperty(PropertyName = "answered")]
        public string? ResponseTime { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string? Type { get; set; }
        [JsonProperty(PropertyName = "code")]
        public string? Code { get; set; }
    }
    public class Response
    {
        [JsonProperty(PropertyName = "claim_id")]
        public string? ClaimId { get; set; }
        [JsonProperty(PropertyName = "katm_sir")]
        public string? KatmSir { get; set; }
    }
}
