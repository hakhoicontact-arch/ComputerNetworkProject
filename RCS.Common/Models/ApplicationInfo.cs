// -----------------------------------------------------------------------------
// File: ApplicationInfo.cs
// Description:
//      Định nghĩa thông tin về một ứng dụng cài đặt hoặc đang chạy
//
//      Mục đích: Cung cấp cấu trúc dữ liệu để truyền thông tin ứng dụng giữa Client, Server và Agent.
// -----------------------------------------------------------------------------

namespace RCS.Common.Models
{
    // Thông tin về một ứng dụng cài đặt hoặc đang chạy
    public class ApplicationInfo
    {
        public string Name { get; set; }    // Tên ứng dụng
        public string Path { get; set; }    // Đường dẫn đến ứng dụng
        public string Status { get; set; }  // "Running", "Stopped", "Installed"
    }
}