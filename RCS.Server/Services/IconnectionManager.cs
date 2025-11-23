using System.Collections.Generic;

namespace RCS.Server.Services
{
    public interface IConnectionManager
    {
        void AddAgent(string agentId, string connectionId);
        void RemoveAgent(string connectionId);
        string GetAgentConnectionId(string agentId);
        IEnumerable<string> GetAllActiveAgents();
    }
}