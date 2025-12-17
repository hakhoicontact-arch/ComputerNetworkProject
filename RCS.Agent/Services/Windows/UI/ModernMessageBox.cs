using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RCS.Agent.Services.Windows.UI
{
    public class ModernMessageBox : Form
    {
        // P/Invoke để làm đẹp form
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        
        // Đổi tên để tránh xung đột với các phương thức có sẵn
        [DllImport("user32.dll", EntryPoint = "SendMessage")] 
        public static extern int SendMessageNative(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        [DllImport("user32.dll")] 
        public static extern bool ReleaseCapture();

        // Biến lưu nội dung trả lời để truyền ra ngoài
        public string ReplyText { get; private set; } = "";

        public ModernMessageBox(string message, string title = "NOTIFICATION", bool isPanic = false, bool allowReply = false)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            // Tăng chiều cao nếu có ô reply
            this.Size = new Size(500, allowReply ? 320 : 220);
            this.TopMost = true; 
            
            Color primaryColor = isPanic ? Color.FromArgb(220, 38, 38) : Color.FromArgb(37, 99, 235);
            Color bgColor = Color.FromArgb(30, 41, 59);
            Color inputBg = Color.FromArgb(51, 65, 85);
            Color textColor = Color.White;

            this.BackColor = bgColor;

            // --- HEADER ---
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = primaryColor };
            
            // Xử lý kéo thả form
            pnlHeader.MouseDown += (s, e) => { 
                if (e.Button == MouseButtons.Left) { 
                    ReleaseCapture(); 
                    SendMessageNative(Handle, 0xA1, 0x2, 0); 
                } 
            };
            
            Label lblTitle = new Label { Text = title, ForeColor = textColor, Font = new Font("Segoe UI", 11, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15,0,0,0) };
            Label btnClose = new Label { Text = "✕", ForeColor = textColor, Font = new Font("Arial", 12), Dock = DockStyle.Right, Width = 45, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnClose);
            this.Controls.Add(pnlHeader);

            // --- MESSAGE BODY ---
            Label lblMessage = new Label { Text = message, ForeColor = Color.FromArgb(203, 213, 225), Font = new Font("Segoe UI", 11), AutoSize = false, TextAlign = ContentAlignment.TopLeft, Location = new Point(20, 60), Size = new Size(460, 80) };
            this.Controls.Add(lblMessage);

            // --- REPLY SECTION (CHỈ HIỆN KHI CẦN) ---
            TextBox txtReply = null;
            if (allowReply)
            {
                Label lblReply = new Label { Text = "Trả lời Admin:", ForeColor = Color.Gray, Location = new Point(20, 150), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                this.Controls.Add(lblReply);

                txtReply = new TextBox 
                { 
                    Location = new Point(20, 175), 
                    Size = new Size(460, 80), // Multiline
                    Multiline = true,
                    BackColor = inputBg,
                    ForeColor = textColor,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Segoe UI", 10)
                };
                // Padding giả cho textbox
                // Lưu ý: CreateRoundRectRgn trả về IntPtr, Region.FromHrgn nhận IntPtr. Không có lỗi void ở đây.
                txtReply.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, txtReply.Width, txtReply.Height, 10, 10));
                this.Controls.Add(txtReply);
            }

            // --- BUTTONS ---
            Button btnAction = new Button 
            { 
                Text = allowReply ? "GỬI TRẢ LỜI" : "ĐÃ HIỂU", 
                FlatStyle = FlatStyle.Flat, 
                ForeColor = textColor, 
                BackColor = primaryColor, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold), 
                Size = new Size(140, 40), 
                Cursor = Cursors.Hand 
            };
            btnAction.FlatAppearance.BorderSize = 0;
            // Căn vị trí nút tùy theo chế độ
            btnAction.Location = new Point(this.Width - 160, this.Height - 60);
            
            btnAction.Click += (s, e) => 
            {
                if (allowReply && txtReply != null)
                {
                    this.ReplyText = txtReply.Text;
                    this.DialogResult = DialogResult.OK; // Đánh dấu là đã gửi
                }
                else
                {
                    this.DialogResult = DialogResult.OK;
                }
                this.Close();
            };

            this.Controls.Add(btnAction);

            // Bo tròn form
            this.Load += (s, e) => this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(71, 85, 105), 2)) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
    }
}