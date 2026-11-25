namespace Domain.Common.Settings
{
    public class CreditBureauApiOptions
    {
        public string IndividualPersonApplicationUrl { get; set; } = null!;
        public string LegalEntityApplicationUrl { get; set; } = null!;
        public string DeclineApplicationUrl { get; set; } = null!;
        public string CreditRegistrationUrl { get; set; } = null!;
        public string CreditScheduleUrl { get; set; } = null!;
        public string CreditPledgeOwnerUrl { get; set; } = null!;
        public string CreditPledgeSecurityUrl { get; set; } = null!;
        public string CreditRegistrationRepaymentUrl { get; set; } = null!;
        public string CreditRegistrationRepaymentBankDitailUrl { get; set; } = null!;
        public string AccountStatusUrl { get; set; } = null!;
        public string CreditLeasingUrl { get; set; } = null!;
        public string LeasingScheduleUrl { get; set; } = null!;
        public string LeasingRepaymentUrl { get; set; } = null!;
        public string CreditFactroingUrl { get; set; } = null!;
        public string CreditRegistrationBusinessDetailsUrl { get; set; } = null!;
        public string CreditRegistrationSubjectUrl { get; set; } = null!;
        /// <summary>CI-024 (ТЗ 10.0). Пусто — метод не отправляется.</summary>
        public string BalanceReportUrl { get; set; } = "";
        /// <summary>CI-025 (ТЗ 10.0). Пусто — метод не отправляется.</summary>
        public string GuaranteeUrl { get; set; } = "";
        public string HostAddress { get; set; } = null!;
    }
}
