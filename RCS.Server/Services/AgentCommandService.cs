using Microsoft.AspNetCore.SignalR;
using RCS.Common.Models;
using RCS.Server.Hubs;
using System.Threading.Tasks;

namespace RCS.Server.Services
{
    // Service đóng vai trò trung gian để ClientHub gọi sang AgentHub
    public class AgentCommandService
    {
        private readonly IHubContext<AgentHub> _agentHubContext;
        private readonly IConnectionManager _connectionManager;

        public AgentCommandService(IHubContext<AgentHub> agentHubContext, IConnectionManager connectionManager)
        {
            _agentHubContext = agentHubContext;
            _connectionManager = connectionManager;
        }

        public async Task SendCommandToAgentAsync(string agentId, CommandMessage command)
        {
            var connectionId = _connectionManager.GetAgentConnectionId(agentId);
            
            if (!string.IsNullOrEmpty(connectionId))
            {
                // Gửi lệnh tới hàm "ReceiveCommand" trên Agent
                await _agentHubContext.Clients.Client(connectionId).SendAsync("ReceiveCommand", command);
            }
            else
            {
                throw new System.Exception("Agent is offline or not found.");
            }
        }
    }
}