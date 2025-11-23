namespace RCS.Common.Models
{
    // Phản hồi từ Agent -> Server -> Client
    public class ResponseMessage
    {
        public string Action { get; set; }
        public object Response { get; set; } // Dữ liệu trả về hoặc thông báo lỗi
    }

    // Dùng riêng cho Keylogger realtime
    public class RealtimeUpdate
    {
        public string Event { get; set; } // "key_pressed"
        public string Data { get; set; }
    }
}