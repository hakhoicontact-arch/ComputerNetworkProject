// -----------------------------------------------------------------------------
// File: ApplicationManager.cs
// Description:
//      Định nghĩa dịch vụ quản lý ứng dụng trên Windows
//
//      Mục đích: Cung cấp các chức năng để liệt kê, khởi động và dừng ứng dụng trên hệ điều hành Windows.
// -----------------------------------------------------------------------------

using RCS.Common.Models;
using System.Collections.Generic;
using System.Diagnostics;

namespace RCS.Agent.Services.Windows
{
    // Dịch vụ quản lý ứng dụng trên Windows
    public class ApplicationManager
    {
        // Lấy danh sách ứng dụng đang chạy
        public List<ApplicationInfo> GetInstalledApps()
        {
            var list = new List<ApplicationInfo>();     // lấy danh sách các process có của sổ giao diện người dùng
            foreach (var p in Process.GetProcesses())   // Lặp qua tất cả các process đang chạy
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                {
                    list.Add(new ApplicationInfo
                    {
                        Name = p.MainWindowTitle,
                        Path = p.MainModule?.FileName ?? "Protected/System",
                        Status = "Running"
                    });
                }
            }
            return list;
        }

        // Khởi chạy 1 app
        public void StartApp(string name)
        {
            try 
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = name,
                    UseShellExecute = true // Cho phép chạy lệnh shell
                });
            } 
            catch { }
        }

        // Dừng 1 app
        public void StopApp(string name)
        {
            foreach (var p in Process.GetProcesses())
            {
                // So sánh tên process hoặc tiêu đề cửa sổ
                if ((!string.IsNullOrEmpty(p.MainWindowTitle) && p.MainWindowTitle.Contains(name)) || 
                    p.ProcessName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    try { p.Kill(); } catch { }
                }
            }
        }
    }
}