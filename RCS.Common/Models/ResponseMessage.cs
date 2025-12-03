// -----------------------------------------------------------------------------
// File: ResponseMessage.cs
// Description:
//      Định nghĩa phản hồi từ Agent -> Server -> Client
//
//      Mục đích: Cung cấp cấu trúc dữ liệu để truyền thông tin ứng dụng giữa Client, Server và Agent.
// -----------------------------------------------------------------------------

namespace RCS.Common.Models
{
    // Phản hồi từ Agent -> Server -> Client
    public class ResponseMessage
    {
        public string Action { get; set; }  // Tên hành động đã thực hiện
        public object Response { get; set; } // Dữ liệu trả về hoặc thông báo lỗi
    }

    // Dùng riêng cho Keylogger realtime
    public class RealtimeUpdate
    {
        public string Event { get; set; }   // Sự kiện, ví dụ: "KeyPressed", "MouseClicked"
        public string Data { get; set; }    // Dữ liệu liên quan đến sự kiện
    }
}