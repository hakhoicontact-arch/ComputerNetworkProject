using RCS.Common.Models;
using System;
using System.Linq;
using System.Management; // Đảm bảo đã chạy: dotnet add package System.Management
using System.Net.NetworkInformation;
using System.Text;

namespace RCS.Agent.Services.Windows
{
    public class SystemInfoManager
    {
        public SystemSpecs GetSpecs()
        {
            return new SystemSpecs
            {
                PcName = Environment.MachineName,
                OsName = GetOsInfo(),
                Uptime = GetUptime(),
                CpuName = GetCpuInfo(out string cores),
                CpuCores = cores,
                TotalRam = GetRamInfo(out string ramDetail),
                RamDetail = ramDetail,
                GpuName = GetGpuInfo(),
                DiskInfo = GetDiskInfoDetailed(),
                LocalIp = GetNetworkInfo(out string mac),
                MacAddress = mac
            };
        }

        private string GetOsInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Caption, OSArchitecture FROM Win32_OperatingSystem");
                foreach (var os in searcher.Get())
                {
                    return $"{os["Caption"]} ({os["OSArchitecture"]})";
                }
            }
            catch { }
            return "Windows (Unknown)";
        }

        private string GetUptime()
        {
            try
            {
                // TickCount là ms từ lúc bật máy. Chuyển sang TimeSpan
                var time = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return $"{time.Days}d {time.Hours}h {time.Minutes}m";
            }
            catch { return "0m"; }
        }

        private string GetCpuInfo(out string cores)
        {
            cores = "? Cores";
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
                foreach (var item in searcher.Get())
                {
                    cores = $"{item["NumberOfCores"]} Cores / {item["NumberOfLogicalProcessors"]} Threads";
                    return item["Name"]?.ToString().Replace("(R)", "").Replace("(TM)", "").Trim();
                }
            }
            catch { }
            return "Unknown CPU";
        }

        private string GetRamInfo(out string detail)
        {
            detail = "Unknown MHz";
            try
            {
                // Tổng RAM
                using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                long totalBytes = 0;
                foreach (var item in searcher.Get()) totalBytes = Convert.ToInt64(item["TotalPhysicalMemory"]);

                // Chi tiết RAM (Speed, Manufacturer)
                using var memSearcher = new ManagementObjectSearcher("SELECT Speed, Manufacturer FROM Win32_PhysicalMemory");
                string speed = "";
                foreach (var item in memSearcher.Get())
                {
                    speed = $"{item["Speed"]} MHz"; // Lấy thanh đầu tiên tìm thấy
                    break;
                }
                
                detail = speed;
                return $"{totalBytes / 1024 / 1024 / 1024} GB";
            }
            catch { }
            return "0 GB";
        }

        private string GetGpuInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
                foreach (var item in searcher.Get())
                {
                    // Lấy GPU rời (thường có RAM > 0 hoặc tên chứa NVIDIA/AMD)
                    // Ở đây lấy cái đầu tiên cho đơn giản
                    return item["Name"]?.ToString();
                }
            }
            catch { }
            return "Basic Render Driver";
        }

        private string GetDiskInfoDetailed()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
                {
                    double free = d.TotalFreeSpace / 1024.0 / 1024.0 / 1024.0;
                    double total = d.TotalSize / 1024.0 / 1024.0 / 1024.0;
                    sb.Append($"{d.Name} [Free: {free:F0}GB / {total:F0}GB] ");
                }
                return sb.ToString().Trim();
            }
            catch { return ""; }
        }

        private string GetNetworkInfo(out string mac)
        {
            mac = "";
            try
            {
                // Lấy IP ưu tiên (Interface đang Up và không phải Loopback)
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var props = ni.GetIPProperties();
                        foreach (var ip in props.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                // SỬA LỖI: Dùng biến tạm để xử lý chuỗi MAC, tránh lỗi CS1988 với tham số out
                                string tempMac = ni.GetPhysicalAddress().ToString();
                                
                                if (!string.IsNullOrEmpty(tempMac))
                                {
                                    // Format MAC đẹp hơn (AA:BB:CC...)
                                    mac = string.Join(":", Enumerable.Range(0, tempMac.Length / 2)
                                                                     .Select(i => tempMac.Substring(i * 2, 2)));
                                }
                                
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            catch { }
            
            // Trả về localhost nếu không tìm thấy mạng nào khả dụng
            return "127.0.0.1";
        }
    }
}