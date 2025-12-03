// -----------------------------------------------------------------------------
// File: CommandMessage.cs
// Description:
//      Định nghĩa lệnh từ Client -> Server -> Agent
//
//      Mục đích: Cung cấp cấu trúc dữ liệu để truyền thông tin ứng dụng giữa Client, Server và Agent.
// -----------------------------------------------------------------------------

namespace RCS.Common.Models
{
    // Lệnh từ Client -> Server -> Agent
    public class CommandMessage
    {
        public string Action { get; set; } // Xem ProtocolConstants.cs
        public object Params { get; set; } // Dữ liệu đi kèm (pid, path...)
    }
}