namespace RCS.Common.Models
{
    public class ProcessInfo
    {
        public int Pid { get; set; }
        public string Name { get; set; }
        public string Cpu { get; set; } // Ví dụ: "12%"
        public string Mem { get; set; } // Ví dụ: "150 MB"
    }
}