using System;
using System.Speech.Synthesis;
using RCS.Agent.Services.Windows.UI;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RCS.Agent.Services.Windows
{
    public class AutomationService
    {
        private readonly SpeechSynthesizer _synthesizer;

        public AutomationService()
        {
            // Khởi tạo bộ tổng hợp tiếng nói (chỉ chạy trên Windows)
            if (OperatingSystem.IsWindows())
            {
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.Volume = 100; // Max volume
                _synthesizer.Rate = 0;     // Tốc độ bình thường (-10 đến 10)
            }
        }

        public void ShowMessageBox(string message, bool isPanic = false)
        {
            // Tạo luồng UI riêng biệt
            Thread uiThread = new Thread(() =>
            {
                try
                {
                    // Kích hoạt Visual Styles để UI mượt mà hơn
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    // Khởi chạy Form tùy biến
                    Application.Run(new ModernMessageBox(message, isPanic ? "🚨 SYSTEM ALERT 🚨" : "ADMIN MESSAGE", isPanic));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UI Error] {ex.Message}");
                }
            });

            // Bắt buộc phải là STA cho Windows Forms
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true; // Để khi tắt Agent thì cửa sổ này cũng tắt theo
            uiThread.Start();
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