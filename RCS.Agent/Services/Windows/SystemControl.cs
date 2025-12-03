// -----------------------------------------------------------------------------
// File: System
// Description:
//      Định nghĩa dịch vụ hệ thống
//
//      Mục đích: Cung cấp các chức năng để shut down, restart
// -----------------------------------------------------------------------------

using System.Diagnostics;

namespace RCS.Agent.Services.Windows
{
    public class SystemControl
    {
        // Tắt máy
        public void Shutdown()
        {
            RunCommand("shutdown", "/s /t 0");
        }

        // Khởi động lại
        public void Restart()
        {
            RunCommand("shutdown", "/r /t 0");
        }

        private void RunCommand(string fileName, string args)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false
                // RedirectStandardOutput = true, // Có thể cần để bắt lỗi hoặc kết quả
                // RedirectStandardError = true // Có thể cần để bắt lỗi hoặc kết quả
            });
        }
    }
}