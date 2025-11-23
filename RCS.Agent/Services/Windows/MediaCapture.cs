using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms; 
using System.Threading;

namespace RCS.Agent.Services.Windows
{
    public class MediaCapture : IDisposable
    {
        // --- API WINDOWS ---
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("avicap32.dll")]
        private static extern IntPtr capCreateCaptureWindowA(string lpszWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hwndParent, int nID);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hwnd);

        // Hằng số Webcam
        private const int WM_CAP_START = 0x400;
        private const int WM_CAP_DRIVER_CONNECT = WM_CAP_START + 10;
        private const int WM_CAP_DRIVER_DISCONNECT = WM_CAP_START + 11;
        private const int WM_CAP_GRAB_FRAME = WM_CAP_START + 60;
        private const int WM_CAP_FILE_SAVEDIB = WM_CAP_START + 25;

        private IntPtr _hWndC = IntPtr.Zero;
        private bool _isWebcamReady = false;

        public MediaCapture()
        {
            try { SetProcessDPIAware(); } catch { }
        }

        // --- 1. CHỤP MÀN HÌNH ---
        public string CaptureScreenBase64()
        {
            try
            {
                Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }
                    return BitmapToBase64(bitmap);
                }
            }
            catch { return ""; }
        }

        // --- 2. QUẢN LÝ WEBCAM (SỬA LỖI CRASH) ---

        // Hàm này chỉ gọi 1 lần khi bắt đầu Stream
        public bool StartWebcam()
        {
            if (_isWebcamReady) return true;

            try
            {
                _hWndC = capCreateCaptureWindowA("RCS_Webcam", 0, 0, 0, 320, 240, IntPtr.Zero, 0);
                if (_hWndC != IntPtr.Zero)
                {
                    // Kết nối driver
                    IntPtr result = SendMessage(_hWndC, WM_CAP_DRIVER_CONNECT, IntPtr.Zero, IntPtr.Zero);
                    if (result != IntPtr.Zero)
                    {
                        _isWebcamReady = true;
                        return true;
                    }
                }
                return false;
            }
            catch 
            {
                StopWebcam();
                return false; 
            }
        }

        // Hàm này gọi 1 lần khi dừng Stream
        public void StopWebcam()
        {
            if (_hWndC != IntPtr.Zero)
            {
                try 
                {
                    SendMessage(_hWndC, WM_CAP_DRIVER_DISCONNECT, IntPtr.Zero, IntPtr.Zero);
                    DestroyWindow(_hWndC);
                } 
                catch { }
                _hWndC = IntPtr.Zero;
            }
            _isWebcamReady = false;
        }

        // Hàm này gọi liên tục trong vòng lặp (Chỉ chụp, không kết nối lại)
        public string GetWebcamFrame()
        {
            if (!_isWebcamReady) return "";

            try
            {
                // 1. Ra lệnh chụp
                SendMessage(_hWndC, WM_CAP_GRAB_FRAME, IntPtr.Zero, IntPtr.Zero);

                // 2. Lưu ra file tạm
                string tempFile = Path.Combine(Path.GetTempPath(), "rcs_cam.bmp");
                IntPtr hFileName = Marshal.StringToHGlobalAnsi(tempFile);
                SendMessage(_hWndC, WM_CAP_FILE_SAVEDIB, IntPtr.Zero, hFileName);
                Marshal.FreeHGlobal(hFileName);

                // 3. Đọc file AN TOÀN (Fix lỗi Out of Memory)
                // Thay vì dùng Image.FromFile (gây lock file), ta đọc bytes trực tiếp
                if (File.Exists(tempFile))
                {
                    byte[] imageBytes;
                    try 
                    {
                        imageBytes = File.ReadAllBytes(tempFile); 
                    }
                    catch (IOException) 
                    { 
                        return ""; // File đang bị lock bởi Webcam driver, bỏ qua frame này
                    }

                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    using (Bitmap bmp = new Bitmap(ms))
                    {
                        return BitmapToBase64(bmp, 40L); // Nén 40% cho nhẹ mạng
                    }
                }
                return "";
            }
            catch { return ""; }
        }

        public void Dispose()
        {
            StopWebcam();
        }

        private string BitmapToBase64(Bitmap bitmap, long quality = 75L)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                myEncoderParameters.Param[0] = new EncoderParameter(myEncoder, quality);
                bitmap.Save(ms, jpgEncoder, myEncoderParameters);
                return "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }
    }
}