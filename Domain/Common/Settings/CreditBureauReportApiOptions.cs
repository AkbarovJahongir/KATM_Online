namespace Domain.Common.Settings
{
    public class CreditBureauReportApiOptions
    {
        public string HostAddress { get; set; } = null!;
        public string ReportUrl { get; set; } = null!;
        public string ReportStatusUrl { get; set; } = null!;
        public string PHead { get; set; } = null!;
        public string PCode { get; set; } = null!;
        public string PLogin { get; set; } = null!;
        public string PPassword { get; set; } = null!;
        /// <summary>ТЗ 10.0: авторизация Bearer JWT вместо security в теле. false — поведение ТЗ 9.16.</summary>
        public bool UseJwtAuth { get; set; }
        public string AuthLoginUrl { get; set; } = null!;
        public int CheckReportStatusInterval { get; set; }
        public int ReportTimeToLiveInterval { get; set; }
    }
}
