using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications
{
    public class CreditRegistrationEntityRequest
    {  
        [JsonProperty(PropertyName = "claim_id")]
        public string? ClaimId { get; set; }
        [JsonProperty(PropertyName = "claim_date")]
        public string? ClaimDate { get; set; }
        [JsonProperty(PropertyName = "inn")]
        public string? Inn { get; set; }
        [JsonProperty(PropertyName = "claim_number")]
        public string? ClaimNumber { get; set; }
        [JsonProperty(PropertyName = "agreement_number")]
        public string? AgreementNumber { get; set; }
        [JsonProperty(PropertyName = "agreement_date")]
        public string? AgreementDate { get; set; }
        [JsonProperty(PropertyName = "resident")]
        public string? Resident { get; set; }
        [JsonProperty(PropertyName = "juridical_status")]
        public string? JuridicalStatus { get; set; }
        [JsonProperty(PropertyName = "nibbd")]
        public string? Nibbd { get; set; }
        [JsonProperty(PropertyName = "client_type")]
        public string? ClientType { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }
        [JsonProperty(PropertyName = "live_cadastr")]
        public string? LiveCadastr { get; set; }
        [JsonProperty(PropertyName = "owner_form")]
        public string? OwnerForm { get; set; }
        [JsonProperty(PropertyName = "goverment")]
        public string? Goverment { get; set; }
        [JsonProperty(PropertyName = "registration_region")]
        public string? RegistrationRegion { get; set; }
        [JsonProperty(PropertyName = "registration_district")]
        public string? RegistrationDistrict { get; set; }
        [JsonProperty(PropertyName = "registration_address")]
        public string? RegistrationAddress { get; set; }
        [JsonProperty(PropertyName = "phone")]
        public string? Phone { get; set; }
        [JsonProperty(PropertyName = "hbranch")]
        public string? Hbranch { get; set; }
        [JsonProperty(PropertyName = "oked")]
        public string? Oked { get; set; }
        [JsonProperty(PropertyName = "katm_sir")]
        public string? KatmSir { get; set; }
        [JsonProperty(PropertyName = "okpo")]
        public string? Okpo { get; set; }
        [JsonProperty(PropertyName = "credit_type")]
        public string? CreditType { get; set; }
        [JsonProperty(PropertyName = "summa")]
        public string? Summa { get; set; }
        [JsonProperty(PropertyName = "procent")]
        public string? Procent { get; set; }
        [JsonProperty(PropertyName = "credit_duration")]
        public string? CreditDuration { get; set; }
        [JsonProperty(PropertyName = "credit_exemption")]
        public string? CreditExemption { get; set; }
        [JsonProperty(PropertyName = "currency")]
        public string? Currency { get; set; }
        [JsonProperty(PropertyName = "ci_method")]
        public string? Method { get; set; } = "0";
    }
}
