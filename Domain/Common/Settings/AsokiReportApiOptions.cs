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
        public string IPTest { get; set; }
        public string DomainTest { get; set; }
        public string IP { get; set; }
        public string Domain { get; set; }
        public bool IsProduction { get; set; }
        public int CheckReportStatusInterval { get; set; }
        public int ReportTimeToLiveInterval { get; set; }
    }
}
