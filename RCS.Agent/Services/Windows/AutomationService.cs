using RCS.Agent.Services.Windows.UI;
using System;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RCS.Agent.Services.Windows
{
    public class AutomationService
    {
        private readonly SpeechSynthesizer _synthesizer;

        public AutomationService()
        {
            if (OperatingSystem.IsWindows())
            {
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.Volume = 100; 
                _synthesizer.Rate = 0;     
            }
        }

        public Task<string> ShowMessageBoxAsync(string message, bool isPanic = false, bool allowReply = false)
        {
            var tcs = new TaskCompletionSource<string>();

            // Chạy form trên luồng UI riêng
            Thread uiThread = new Thread(() =>
            {
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    using (var form = new ModernMessageBox(message, isPanic ? "🚨 SYSTEM ALERT" : "MESSAGE FROM ADMIN", isPanic, allowReply))
                    {
                        // Chạy form và đợi đóng
                        Application.Run(form);

                        // Lấy kết quả sau khi form đóng
                        if (form.DialogResult == DialogResult.OK)
                        {
                            // Trả về nội dung người dùng nhập (hoặc rỗng nếu chỉ bấm OK)
                            tcs.SetResult(form.ReplyText);
                        }
                        else
                        {
                            tcs.SetResult(null); // Người dùng tắt form
                        }
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            uiThread.SetApartmentState(ApartmentState.STA); // Bắt buộc cho WinForms
            uiThread.IsBackground = true;
            uiThread.Start();

            return tcs.Task;
        }

        // Giữ lại hàm cũ để tương thích ngược (nhưng gọi hàm Async bên trong)
        public void ShowMessageBox(string message, bool isPanic = false)
        {
            _ = ShowMessageBoxAsync(message, isPanic, false);
        }

        public void SpeakText(string text)
        {
            if (_synthesizer == null) return;

            Task.Run(() =>
            {
                try
                {
                    _synthesizer.Speak(text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TTS Error] {ex.Message}");
                }
            });
        }
    }
}