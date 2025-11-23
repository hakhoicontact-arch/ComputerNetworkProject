using RCS.Common.Models;
using System.Collections.Generic;
using System.Diagnostics;

namespace RCS.Agent.Services.Windows
{
    public class ApplicationManager
    {
        public List<ApplicationInfo> GetInstalledApps()
        {
            var list = new List<ApplicationInfo>();
            foreach (var p in Process.GetProcesses())
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

        public void StartApp(string name)
        {
            try 
            { 
                Process.Start(new ProcessStartInfo
                {
                    FileName = name,
                    UseShellExecute = true // QUAN TRỌNG: Cho phép chạy lệnh shell (vd: notepad, calc)
                });
            } 
            catch { }
        }

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