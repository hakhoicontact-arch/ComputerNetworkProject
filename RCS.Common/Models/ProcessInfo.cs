// -----------------------------------------------------------------------------
// File: ProcessInfo.cs
// Description:
//      Định nghĩa thông tin về một process đang chạy
//
//      Mục đích: Cung cấp cấu trúc dữ liệu để truyền thông tin ứng dụng giữa Client, Server và Agent.
// -----------------------------------------------------------------------------

namespace RCS.Common.Models
{
    // Thông tin về một process đang chạy
    public class ProcessInfo
    {
        public int Pid { get; set; }    // Process ID
        public string Name { get; set; } // Tên process
        public string Cpu { get; set; } // % CPU sử dụng, ví dụ: "12.5%"
        public string Mem { get; set; } // % RAM sử dụng, ví dụ: "150 MB"
    }
}