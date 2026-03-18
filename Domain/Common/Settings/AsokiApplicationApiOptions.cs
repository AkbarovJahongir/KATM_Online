namespace Domain.Common.Settings
{
    public class AsokiApplicationApiOptions
    {
        public string IndividualPersonApplicationUrl { get; set; } = null!;
        public string LegalEntityApplicationUrl { get; set; } = null!;
        public string DeclineApplicationUrl { get; set; } = null!;
        public string CreditRegistrationUrl { get; set; } = null!;
        public string CreditScheduleUrl { get; set; } = null!;
        public string CreditPledgeOwnerUrl { get; set; }
        public string CreditPledgeSecurityUrl { get; set; } = null!;
        public string CreditRegistrationRepaymentUrl { get; set; } = null!;
        public string CreditRegistrationRepaymentBankDitailUrl { get; set; } = null!;
        public string AccountStatusUrl { get; set; } = null!;
        public string CreditLeasingUrl { get; set; } = null!;
        public string LeasingScheduleUrl { get; set; } = null!;
        public string LeasingRepaymentUrl { get; set; } = null!;
        public string HostAddress { get; set; } = null!;
    }
}
