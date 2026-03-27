using Newtonsoft.Json;

namespace CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications
{
    public class CreditRegistrationIndividualRequest
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
        [JsonProperty(PropertyName = "document_type")]
        public string? DocumentType { get; set; }
        [JsonProperty(PropertyName = "document_serial")]
        public string? DocumentSerial { get; set; }
        [JsonProperty(PropertyName = "document_number")]
        public string? DocumentNumber { get; set; }
        [JsonProperty(PropertyName = "document_date")]
        public string? DocumentDate { get; set; }
        [JsonProperty(PropertyName = "gender")]
        public string? Gender { get; set; }
        [JsonProperty(PropertyName = "client_type")]
        public string? ClientType { get; set; }
        [JsonProperty(PropertyName = "birth_date")]
        public string? BirthDate { get; set; }
        [JsonProperty(PropertyName = "document_region")]
        public string? DocumentRegion { get; set; }
        [JsonProperty(PropertyName = "document_district")]
        public string? DocumentDistrict { get; set; }
        [JsonProperty(PropertyName = "nibbd")]
        public string? Nibbd { get; set; }
        [JsonProperty(PropertyName = "family_name")]
        public string? FamilyName { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }
        [JsonProperty(PropertyName = "patronymic")]
        public string? Patronymic { get; set; }
        [JsonProperty(PropertyName = "registration_region")]
        public string? RegistrationRegion { get; set; }
        [JsonProperty(PropertyName = "registration_district")]
        public string? RegistrationDistrict { get; set; }
        [JsonProperty(PropertyName = "registration_address")]
        public string? RegistrationAddress { get; set; }
        [JsonProperty(PropertyName = "phone")]
        public string? Phone { get; set; }
        [JsonProperty(PropertyName = "pin")]
        public string? Pin { get; set; }
        [JsonProperty(PropertyName = "katm_sir")]
        public string? KatmSir { get; set; }
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
        [JsonProperty(PropertyName = "live_address")]
        public string? LiveAddress { get; set; }
        [JsonProperty(PropertyName = "live_cadastr")]
        public string? LiveCadastr { get; set; }
        [JsonProperty(PropertyName = "registration_cadastr")]
        public string? RegistrationCadastr { get; set; }
        [JsonProperty(PropertyName = "method")]
        public string? Method { get; set; } = "1";
    }
}
