namespace RCS.Common.Models
{
    public class SystemSpecs
    {
        // OS & System
        public string OsName { get; set; }          // Windows 11 Pro 64-bit
        public string Uptime { get; set; }          // 2 Days, 4 Hours
        public string PcName { get; set; }          // DESKTOP-XYZ

        // CPU
        public string CpuName { get; set; }         // Intel Core i9-13900K
        public string CpuCores { get; set; }        // 24 Cores / 32 Threads
        
        // RAM
        public string TotalRam { get; set; }        // 32 GB
        public string RamDetail { get; set; }       // 3200 MHz (Manufacturer)

        // GPU (Mới)
        public string GpuName { get; set; }         // NVIDIA GeForce RTX 4090

        // Disk (Chi tiết hơn)
        public string DiskInfo { get; set; }        // C: (SSD) 100/500GB [NTFS]

        // Network (Mới)
        public string LocalIp { get; set; }         // 192.168.1.15
        public string MacAddress { get; set; }      // AA:BB:CC...
    }
}