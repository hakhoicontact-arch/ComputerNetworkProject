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
        /// <summary>
        /// Quét toàn bộ Registry để lấy danh sách các phần mềm đã cài đặt.
        /// Kết hợp kiểm tra xem ứng dụng nào đang chạy.
        /// </summary>
        public List<ApplicationInfo> GetInstalledApps()
        {
            // Dùng Dictionary để loại bỏ trùng lặp (Key là đường dẫn EXE)
            var uniqueApps = new Dictionary<string, ApplicationInfo>();
            
            // Lấy danh sách process đang chạy để check status
            var runningProcesses = Process.GetProcesses();
            var runningPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach(var p in runningProcesses)
            {
                try { 
                    // Cố gắng lấy đường dẫn file đang chạy
                    if(p.MainModule != null) runningPaths.Add(p.MainModule.FileName);
                } catch { }
            }

            // --- CHIẾN THUẬT 1: QUÉT APP PATHS (Nơi chứa Word, Excel, Outlook...) ---
            ScanAppPaths(uniqueApps, runningPaths);

            // --- CHIẾN THUẬT 2: QUÉT UNINSTALL KEYS (Nơi chứa các phần mềm cài đặt thông thường) ---
            ScanUninstallKeys(uniqueApps, runningProcesses);

            // Sắp xếp và trả về
            return uniqueApps.Values.OrderBy(x => x.Name).ToList();
        }

        private void ScanAppPaths(Dictionary<string, ApplicationInfo> apps, HashSet<string> runningPaths)
        {
            string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
            using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            // App Paths luôn có giá trị (Default) là đường dẫn full tới exe
                            var path = subKey.GetValue("") as string;
                            AddAppIfValid(apps, runningPaths, path);
                        }
                    }
                }
            }
        }

        private void ScanUninstallKeys(Dictionary<string, ApplicationInfo> apps, Process[] runningProcs)
        {
            var hives = new List<(RegistryKey Hive, string Path)>
            {
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
                (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall")
            };

            var runningNames = runningProcs.Select(p => p.ProcessName.ToLower()).ToHashSet();

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
                                // Lấy thông tin cơ bản
                                var name = subKey.GetValue("DisplayName") as string;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                
                                // Lọc rác
                                if (IsSystemComponent(name)) continue;

                                // Tìm đường dẫn EXE
                                string exePath = GetExePathFromRegistry(subKey);

                                // Nếu tìm thấy path thì thêm kiểu xịn
                                if (!string.IsNullOrEmpty(exePath))
                                {
                                    AddAppIfValid(apps, null, exePath, name, runningNames);
                                }
                                // Nếu không tìm thấy exe nhưng có tên trong Registry (VD: DLC, Plugin...)
                                // Ta vẫn hiện ra nhưng không cho bấm Start (Path rỗng)
                                else if (!apps.Values.Any(a => a.Name == name))
                                {
                                    // Logic phụ: Nếu không tìm thấy exe, coi như Stopped
                                    // apps[name] = new ApplicationInfo { Name = name, Path = "", Status = "Installed" };
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void AddAppIfValid(Dictionary<string, ApplicationInfo> apps, HashSet<string> runningPaths, string path, string forceName = null, HashSet<string> runningNames = null)
        {
            path = CleanPath(path);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return;

            // Kiểm tra trùng lặp bằng đường dẫn file
            if (apps.ContainsKey(path)) return;

            try 
            {
                // Lấy thông tin chi tiết từ file .exe
                var info = FileVersionInfo.GetVersionInfo(path);
                
                // Ưu tiên: Tên ép buộc (từ Registry) > FileDescription (từ .exe) > ProductName > Tên file
                string appName = forceName;
                if (string.IsNullOrEmpty(appName)) appName = info.FileDescription;
                if (string.IsNullOrEmpty(appName)) appName = info.ProductName;
                if (string.IsNullOrEmpty(appName)) appName = Path.GetFileNameWithoutExtension(path);

                // Lọc rác lần 2 (những file exe hệ thống không nên hiện)
                if (string.IsNullOrEmpty(appName) || appName.Contains("Installer")) return;

                // Kiểm tra trạng thái Running
                string status = "Stopped";
                
                // Cách 1: Check theo đường dẫn chính xác (chính xác nhất)
                if (runningPaths != null && runningPaths.Contains(path)) 
                {
                    status = "Running";
                }
                // Cách 2: Check theo tên process (nếu cách 1 không có dữ liệu runningPaths)
                else if (runningNames != null)
                {
                    string procName = Path.GetFileNameWithoutExtension(path).ToLower();
                    if (runningNames.Contains(procName)) status = "Running";
                }

                apps[path] = new ApplicationInfo
                {
                    Name = appName,
                    Path = path,
                    Status = status
                };
            }
            catch { }
        }

        private string GetExePathFromRegistry(RegistryKey key)
        {
            // 1. DisplayIcon (Thường chuẩn nhất)
            string p = CleanPath(key.GetValue("DisplayIcon") as string);
            if (IsValidExe(p)) return p;

            // 2. InstallLocation (Thường là thư mục, cần mò file exe)
            string dir = CleanPath(key.GetValue("InstallLocation") as string);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                // Thử tìm file exe trùng tên thư mục (VD: C:\App\Zalo\Zalo.exe)
                string folderName = new DirectoryInfo(dir).Name;
                string guessPath = Path.Combine(dir, folderName + ".exe");
                if (IsValidExe(guessPath)) return guessPath;

                // Thử tìm file exe lớn nhất trong thư mục (thường là file chính)
                try {
                    var largestExe = new DirectoryInfo(dir).GetFiles("*.exe")
                                        .OrderByDescending(f => f.Length)
                                        .FirstOrDefault();
                    if (largestExe != null) return largestExe.FullName;
                } catch { }
            }

            return null;
        }

        private bool IsValidExe(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }

        private string CleanPath(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            string s = raw.Trim();
            if (s.StartsWith("\"")) 
            {
                int end = s.IndexOf("\"", 1);
                if (end > 0) s = s.Substring(1, end - 1);
            }
            if (s.Contains(",")) s = s.Split(',')[0]; // Icon index
            return s;
        }

        private bool IsSystemComponent(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            return name.StartsWith("Security Update") || 
                   name.StartsWith("Update for") ||
                   name.StartsWith("Microsoft Visual C++") || // Ẩn thư viện runtime cho gọn
                   name.Contains("Redistributable");
        }


        /// <summary>
        /// Khởi động một ứng dụng dựa trên đường dẫn file .exe.
        /// </summary>
        /// <param name="path">Đường dẫn đầy đủ tới file thực thi.</param>
        public void StartApp(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true, // Cần thiết để chạy như một lệnh Shell, tránh lỗi quyền hạn
                    WorkingDirectory = Path.GetDirectoryName(path) // Đặt thư mục làm việc là thư mục chứa file exe để load dll ổn định
                });
            }
            catch { }
        }

        /// <summary>
        /// Dừng ứng dụng. Thử đóng nhẹ nhàng trước, nếu không được thì buộc dừng (Kill).
        /// </summary>
        /// <param name="pathOrName">Đường dẫn file hoặc tên ứng dụng.</param>
        public void StopApp(string pathOrName)
        {
            try 
            {
                string processName = Path.GetFileNameWithoutExtension(pathOrName);
                
                // Tìm tất cả process có tên trùng
                var procs = Process.GetProcessesByName(processName);
                
                // Fallback: Tìm theo tên cửa sổ nếu không tìm thấy exe
                if (procs.Length == 0)
                {
                    procs = Process.GetProcesses().Where(p => p.MainWindowTitle.Contains(pathOrName)).ToArray();
                }

                foreach (var p in procs) 
                {
                    try 
                    {
                        // Bước 1: Yêu cầu đóng nhẹ nhàng (giống bấm nút X)
                        p.CloseMainWindow();
                        
                        // Bước 2: Chờ tối đa 1 giây xem nó có chịu tắt không
                        if (!p.WaitForExit(1000)) 
                        {
                            // Bước 3: Nếu lì lợm không tắt -> KILL ngay lập tức
                            p.Kill();
                            
                            // Bước 4: Chờ Windows dọn dẹp xong process này
                            p.WaitForExit(); 
                        }
                    }
                    catch 
                    {
                        // Nếu có lỗi (ví dụ Access Denied), cố gắng Kill lần nữa
                        try { p.Kill(); } catch { }
                    }
                }
            } 
            catch { }
        }

        // =========================================================================
        // PHẦN PRIVATE HELPER (Hàm hỗ trợ nội bộ)
        // =========================================================================

        /// <summary>
        /// Làm sạch chuỗi đường dẫn lấy từ Registry để ra được đường dẫn file .exe chuẩn.
        /// </summary>
        private string GetExePath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath)) return null;

            // Registry thường trả về dạng phức tạp như: "C:\Path\App.exe" /arg hoặc C:\Path\App.exe,0
            
            // 1. Xử lý dấu ngoặc kép bao quanh (nếu có)
            string clean = rawPath.Trim();
            if (clean.StartsWith("\""))
            {
                int endQuote = clean.IndexOf("\"", 1);
                if (endQuote > 0) clean = clean.Substring(1, endQuote - 1);
            }

            // 2. Cắt bỏ các tham số phía sau (ngăn cách bởi dấu phẩy)
            // Ví dụ: C:\Program Files\App\app.exe,0 -> Lấy phần trước dấu phẩy
            if (clean.Contains(",")) clean = clean.Split(',')[0];

            // 3. Kiểm tra đuôi file
            // Chỉ chấp nhận nếu kết thúc là .exe để đảm bảo đây là file chạy được
            if (!clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return clean;
        }
    }
}