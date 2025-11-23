namespace RCS.Common.Models
{
    public class ApplicationInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Status { get; set; } // "Running", "Stopped", "Installed"
    }
}