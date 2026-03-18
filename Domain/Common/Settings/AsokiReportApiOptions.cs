namespace Domain.Common.Settings
{
    public class AsokiReportApiOptions
    {
        public string HostAddress { get; set; } = null!;
        public string ReportUrl { get; set; } = null!;
        public string ReportStatusUrl { get; set; } = null!;
        public string PHead { get; set; } = null!;
        public string PCode { get; set; } = null!;
        public string PLogin { get; set; } = null!;
        public string PPassword { get; set; } = null!;
        public int CheckReportStatusInterval { get; set; }
        public int ReportTimeToLiveInterval { get; set; }
    }
}
