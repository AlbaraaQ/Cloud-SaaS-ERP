namespace SmartAuditERP.Models
{
    /// <summary>
    /// معلومات التحديث من GitHub Releases
    /// </summary>
    public class AppUpdateInfo
    {
        public string NewVersion { get; set; } = "";
        public string ReleaseTitle { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSize { get; set; } = 0;
    }
}