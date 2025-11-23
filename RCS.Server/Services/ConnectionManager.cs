using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Server.Services
{
    public class ConnectionManager : IConnectionManager
    {
        // Thread-safe dictionary: Key = AgentID, Value = ConnectionID
        private readonly ConcurrentDictionary<string, string> _agentConnections = new();

        public void AddAgent(string agentId, string connectionId)
        {
            _agentConnections.AddOrUpdate(agentId, connectionId, (key, oldValue) => connectionId);
        }

        public void RemoveAgent(string connectionId)
        {
            // Tìm và xóa Agent dựa trên ConnectionId khi ngắt kết nối
            var item = _agentConnections.FirstOrDefault(kvp => kvp.Value == connectionId);
            if (!item.Equals(default(KeyValuePair<string, string>)))
            {
                _agentConnections.TryRemove(item.Key, out _);
            }
        }

        public string GetAgentConnectionId(string agentId)
        {
            _agentConnections.TryGetValue(agentId, out var connectionId);
            return connectionId;
        }

        public IEnumerable<string> GetAllActiveAgents()
        {
            return _agentConnections.Keys;
        }
    }
}