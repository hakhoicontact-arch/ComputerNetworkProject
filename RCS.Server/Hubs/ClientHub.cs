using Microsoft.AspNetCore.SignalR;
using RCS.Common.Models;
using RCS.Server.Services;
using System.Threading.Tasks;

namespace RCS.Server.Hubs
{
    // Hub dành cho Client (Web) kết nối
    public class ClientHub : Hub
    {
        private readonly AgentCommandService _commandService;

        public ClientHub(AgentCommandService commandService)
        {
            _commandService = commandService;
        }

        // Client gọi hàm này để gửi lệnh
        public async Task SendToAgent(string agentId, CommandMessage command)
        {
            try
            {
                await _commandService.SendCommandToAgentAsync(agentId, command);
            }
            catch (System.Exception ex)
            {
                // Báo lỗi về lại cho chính Client vừa gọi
                await Clients.Caller.SendAsync("ReceiveResponse", new { error = ex.Message });
            }
        }
    }
}