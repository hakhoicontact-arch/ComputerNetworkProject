using RCS.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RCS.Agent.Services.Windows
{
    public class ProcessMonitor
    {
        public List<ProcessInfo> GetProcesses()
        {
            var list = new List<ProcessInfo>();
            
            // 1. Lấy danh sách tiến trình hiện tại
            var processes = Process.GetProcesses();
            var startCpuUsage = new Dictionary<int, TimeSpan>();
            var startTime = DateTime.UtcNow;

            // 2. Ghi lại thời gian CPU ban đầu của từng tiến trình
            foreach (var p in processes)
            {
                try
                {
                    // Chỉ lấy được thông tin nếu có quyền truy cập
                    startCpuUsage[p.Id] = p.TotalProcessorTime;
                }
                catch { }
            }

            // 3. Tạm dừng 300ms để tạo khoảng chênh lệch (Sampling)
            // Khoảng thời gian này đủ để Windows cập nhật tick CPU
            Thread.Sleep(300);

            var endTime = DateTime.UtcNow;
            var totalSampleTime = (endTime - startTime).TotalMilliseconds;
            var cpuCoreCount = Environment.ProcessorCount; // Số nhân CPU

            // 4. Tính toán lại và xuất danh sách
            // Sắp xếp theo Memory để lấy top những process nặng nhất (tránh gửi hàng nghìn process rác)
            var sortedProcesses = processes.OrderByDescending(p => p.WorkingSet64).Take(100).ToList();

            foreach (var p in sortedProcesses)
            {
                try
                {
                    string cpuText = "0%";
                    
                    // Nếu lúc nãy đã ghi được thời gian bắt đầu thì tính toán
                    if (startCpuUsage.ContainsKey(p.Id))
                    {
                        var endCpuUsage = p.TotalProcessorTime;
                        var cpuUsedMs = (endCpuUsage - startCpuUsage[p.Id]).TotalMilliseconds;
                        
                        // Công thức: (CPU dùng / Tổng thời gian mẫu) / Số nhân * 100
                        double cpuPercent = (cpuUsedMs / totalSampleTime) / cpuCoreCount * 100;
                        
                        // Làm tròn hiển thị
                        if (cpuPercent > 0) 
                            cpuText = cpuPercent.ToString("0.0") + "%";
                    }

                    // Chuyển đổi bytes sang MB
                    double memMb = p.WorkingSet64 / 1024.0 / 1024.0;

                    list.Add(new ProcessInfo
                    {
                        Pid = p.Id,
                        Name = p.ProcessName,
                        Cpu = cpuText,
                        Mem = $"{memMb:F0} MB" // Format không lấy số lẻ cho gọn
                    });
                }
                catch { } // Bỏ qua các process hệ thống không truy cập được
            }

            return list;
        }

        public void StartProcess(string path)
        {
             try 
             { 
                 Process.Start(new ProcessStartInfo
                 {
                     FileName = path,
                     UseShellExecute = true
                 });
             } 
             catch { }
        }

        public void KillProcess(int pid)
        {
            try
            {
                var p = Process.GetProcessById(pid);
                p.Kill();
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"[ProcessMonitor] Error killing {pid}: {ex.Message}");
            }
        }
    }
}