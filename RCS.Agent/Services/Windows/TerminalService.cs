using RCS.Common.Models;
using RCS.Common.Protocols;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RCS.Agent.Services.Windows
{
    public class TerminalService
    {
        private Process _cmdProcess;
        private readonly SignalRClient _signalRClient;
        private bool _isRunning = false;

        public TerminalService(SignalRClient signalRClient)
        {
            _signalRClient = signalRClient;
        }

        public void StartTerminal()
        {
            if (_isRunning && _cmdProcess != null && !_cmdProcess.HasExited) return;

            try
            {
                _cmdProcess = new Process();
                _cmdProcess.StartInfo.FileName = "cmd.exe";
                
                // Cấu hình Redirect IO
                _cmdProcess.StartInfo.UseShellExecute = false; 
                _cmdProcess.StartInfo.RedirectStandardInput = true;  
                _cmdProcess.StartInfo.RedirectStandardOutput = true; 
                _cmdProcess.StartInfo.RedirectStandardError = true;  
                _cmdProcess.StartInfo.CreateNoWindow = true;         
                
                // Fix lỗi font tiếng Việt trong CMD (dùng CodePage 850 hoặc UTF8 tùy máy)
                _cmdProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                _cmdProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                // Đăng ký sự kiện hứng kết quả
                _cmdProcess.OutputDataReceived += async (sender, e) => await SendOutput(e.Data);
                _cmdProcess.ErrorDataReceived += async (sender, e) => await SendOutput(e.Data);

                _cmdProcess.Start();

                // Bắt đầu đọc luồng bất đồng bộ
                _cmdProcess.BeginOutputReadLine();
                _cmdProcess.BeginErrorReadLine();

                _isRunning = true;
                
                // Gửi thông báo mở đầu
                _ = SendOutput("--- REMOTE TERMINAL SESSION STARTED ---");
                _ = SendOutput($"Host: {Environment.MachineName} | User: {Environment.UserName}");
            }
            catch (Exception ex)
            {
                _ = SendOutput($"[Error Starting Terminal]: {ex.Message}");
            }
        }

        public void StopTerminal()
        {
            if (!_isRunning) return;
            try
            {
                if (_cmdProcess != null && !_cmdProcess.HasExited)
                {
                    _cmdProcess.StandardInput.WriteLine("exit"); // Thử thoát mềm
                    if (!_cmdProcess.WaitForExit(500)) 
                    {
                        _cmdProcess.Kill(); // Thoát cứng nếu treo
                    }
                    _cmdProcess.Dispose();
                    _cmdProcess = null;
                }
            }
            catch { }
            _isRunning = false;
            _ = SendOutput("--- SESSION CLOSED ---");
        }

        public void WriteInput(string command)
        {
            if (!_isRunning || _cmdProcess == null || _cmdProcess.HasExited)
            {
                StartTerminal();
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(command))
                {
                    _cmdProcess.StandardInput.WriteLine(command);
                }
            }
            catch (Exception ex)
            {
                _ = SendOutput($"[Input Error]: {ex.Message}");
            }
        }

        // --- HÀM GÂY LỖI CŨ (ĐÃ FIX) ---
        private async Task SendOutput(string data)
        {
            if (data == null) return;
            
            // FIX: Kiểm tra _signalRClient có null không trước khi gọi
            if (_signalRClient != null)
            {
                await _signalRClient.SendUpdateAsync(new RealtimeUpdate 
                { 
                    Event = ProtocolConstants.EventTerminalOutput, 
                    Data = data 
                });
            }
        }
    }
}