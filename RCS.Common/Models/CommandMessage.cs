namespace RCS.Common.Models
{
    // Lệnh từ Client -> Server -> Agent
    public class CommandMessage
    {
        public string Action { get; set; } // Xem ProtocolConstants.cs
        public object Params { get; set; } // Dữ liệu đi kèm (pid, path...)
    }
}