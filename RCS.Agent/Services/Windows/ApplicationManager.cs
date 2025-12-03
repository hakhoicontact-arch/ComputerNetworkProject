using Microsoft.Win32;
using RCS.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RCS.Agent.Services.Windows
{
    public class ApplicationManager
    {
        public List<ApplicationInfo> GetInstalledApps()
        {
            // Dùng Dictionary để tự động loại bỏ các app trùng tên
            var appDictionary = new Dictionary<string, ApplicationInfo>();
            
            // 1. Lấy danh sách process đang chạy
            var runningProcesses = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLower())
                .ToHashSet();

            // 2. Danh sách các vị trí Registry cần quét (QUAN TRỌNG: Thêm CurrentUser)
            var hives = new List<(RegistryKey Hive, string Path)>
            {
                // App 64-bit cài cho toàn máy
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                // App 32-bit cài cho toàn máy
                (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
                // App cài riêng cho User (Zalo, VSCode, Zoom nằm ở đây) -> Đây là chỗ thiếu lúc nãy
                (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall")
            };

            foreach (var (hive, path) in hives)
            {
                try
                {
                    using (var key = hive.OpenSubKey(path))
                    {
                        if (key == null) continue;

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                try
                                {
                                    // Lấy Tên hiển thị
                                    var name = subKey.GetValue("DisplayName") as string;
                                    if (string.IsNullOrWhiteSpace(name)) continue;

                                    // Lọc bớt các Update của Windows để danh sách đỡ rác
                                    if (name.StartsWith("Security Update") || name.StartsWith("Update for")) continue;

                                    // Thuật toán tìm đường dẫn file .exe (Ưu tiên DisplayIcon -> InstallLocation -> UninstallString)
                                    string exePath = GetExePath(subKey.GetValue("DisplayIcon") as string) 
                                                  ?? GetExePath(subKey.GetValue("InstallLocation") as string)
                                                  ?? GetExePath(subKey.GetValue("UninstallString") as string);

                                    // Chỉ lấy những cái nào tìm được file .exe thực sự để có thể bấm nút "Mở"
                                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                                    {
                                        string exeName = Path.GetFileNameWithoutExtension(exePath).ToLower();
                                        string status = runningProcesses.Contains(exeName) ? "Running" : "Stopped";

                                        // Nếu chưa có trong danh sách thì thêm vào (Key là tên app)
                                        if (!appDictionary.ContainsKey(name))
                                        {
                                            appDictionary[name] = new ApplicationInfo
                                            {
                                                Name = name,
                                                Path = exePath,
                                                Status = status
                                            };
                                        }
                                        else if (status == "Running") 
                                        {
                                            // Nếu trùng tên nhưng cái này đang chạy thì ưu tiên cập nhật trạng thái
                                            appDictionary[name].Status = "Running";
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }

            // Sắp xếp A-Z và trả về
            return appDictionary.Values.OrderBy(x => x.Name).ToList();
        }

        // Helper: Làm sạch đường dẫn lấy từ Registry
        private string GetExePath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath)) return null;

            // Registry thường trả về dạng: "C:\Path\App.exe" /arg hoặc C:\Path\App.exe,0
            // Bước 1: Xử lý dấu ngoặc kép
            string clean = rawPath.Trim();
            if (clean.StartsWith("\""))
            {
                int endQuote = clean.IndexOf("\"", 1);
                if (endQuote > 0) clean = clean.Substring(1, endQuote - 1);
            }

            // Bước 2: Cắt bỏ các tham số phía sau (nếu có dấu phẩy hoặc khoảng trắng)
            if (clean.Contains(",")) clean = clean.Split(',')[0];
            
            // Bước 3: Đảm bảo đuôi là .exe
            if (!clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // Một số trường hợp InstallLocation thiếu \app.exe, thử đoán xem
                // Nhưng để an toàn, chỉ lấy cái nào chắc chắn là .exe
                return null;
            }

            return clean;
        }

        public void StartApp(string path)
        {
            try 
            { 
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true, 
                    WorkingDirectory = Path.GetDirectoryName(path) 
                });
            } 
            catch { }
        }

        public void StopApp(string pathOrName)
        {
            try 
            {
                // Thử kill bằng tên file exe trước (chính xác hơn)
                string processName = Path.GetFileNameWithoutExtension(pathOrName);
                var procs = Process.GetProcessesByName(processName);
                if (procs.Length > 0)
                {
                    foreach (var p in procs) p.Kill();
                }
                else
                {
                    // Nếu không tìm thấy, thử tìm theo tên cửa sổ (fallback)
                    foreach (var p in Process.GetProcesses())
                    {
                        if (p.MainWindowTitle.Contains(pathOrName)) p.Kill();
                    }
                }
            } 
            catch { }
        }
    }
}