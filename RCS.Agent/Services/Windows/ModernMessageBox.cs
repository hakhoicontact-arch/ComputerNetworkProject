using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RCS.Agent.Services.Windows.UI
{
    public class ModernMessageBox : Form
    {
        // --- P/Invoke để bo tròn góc và tạo bóng (DWM API) ---
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public ModernMessageBox(string message, string title = "NOTIFICATION", bool isPanic = false)
        {
            // 1. Cấu hình Form cơ bản
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(450, 220);
            this.TopMost = true; // Luôn nổi trên cùng
            this.ShowInTaskbar = false;
            
            // Màu sắc chủ đạo (Panic Mode: Đỏ, Normal: Xanh đen)
            Color primaryColor = isPanic ? Color.FromArgb(220, 38, 38) : Color.FromArgb(37, 99, 235);
            Color bgColor = Color.FromArgb(30, 41, 59); // Slate-800
            Color textColor = Color.White;

            this.BackColor = bgColor;

            // 2. Header (Thanh tiêu đề)
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = primaryColor
            };
            // Cho phép kéo thả form
            pnlHeader.MouseDown += (s, e) => { 
                if (e.Button == MouseButtons.Left) {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            Label lblTitle = new Label
            {
                Text = title.ToUpper(),
                ForeColor = textColor,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 0, 0, 0)
            };

            // Nút đóng (X)
            Label btnClose = new Label
            {
                Text = "✕",
                ForeColor = textColor,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.FromArgb(200, 0, 0);
            btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.Transparent;

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnClose);

            // 3. Nội dung tin nhắn
            Label lblMessage = new Label
            {
                Text = message,
                ForeColor = Color.FromArgb(203, 213, 225), // Slate-300
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(20)
            };

            // 4. Nút xác nhận
            Button btnOk = new Button
            {
                Text = "OK, I Understand",
                FlatStyle = FlatStyle.Flat,
                ForeColor = textColor,
                BackColor = primaryColor,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(160, 35),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Location = new Point((this.Width - btnOk.Width) / 2, 160);
            btnOk.Click += (s, e) => this.Close();

            // 5. Lắp ráp
            this.Controls.Add(btnOk);
            this.Controls.Add(lblMessage);
            this.Controls.Add(pnlHeader);

            // Bo tròn góc (Windows API)
            this.Load += (s, e) => {
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
            };
        }

        // Vẽ viền mỏng (Optional)
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 1)) // Slate-600
            {
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}