using Microsoft.AspNetCore.SignalR;
using RCS.Common.Models;
using RCS.Server.Services;
using System;
using System.Threading.Tasks;

namespace RCS.Server.Hubs
{
    public class ClientHub : Hub
    {
        private readonly AgentCommandService _commandService;
        
        // MẬT KHẨU QUẢN TRỊ (Bạn có thể đổi ở đây)
        private const string ADMIN_PASSWORD = "1901"; 

        public ClientHub(AgentCommandService commandService)
        {
            _commandService = commandService;
        }

        // --- CHẶN KẾT NỐI NẾU KHÔNG CÓ MẬT KHẨU ---
        public override async Task OnConnectedAsync()
        {
            // Lấy token từ Query String (?access_token=...)
            var httpContext = Context.GetHttpContext();
            
            // SỬA LỖI: Chuyển đổi tường minh sang string để tránh lỗi CS0034
            string token = httpContext?.Request.Query["access_token"].ToString();

            if (string.IsNullOrEmpty(token) || token != ADMIN_PASSWORD)
            {
                // Nếu sai mật khẩu, ngắt kết nối ngay lập tức
                Context.Abort(); 
                return;
            }

            await base.OnConnectedAsync();
        }

        public async Task SendToAgent(string agentId, CommandMessage command)
        {
            try
            {
                await _commandService.SendCommandToAgentAsync(agentId, command);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveResponse", new { error = ex.Message });
            }
        }
    }
}