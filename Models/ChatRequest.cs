using System.Collections.Generic;

namespace PROJFACILITY.IA.Models 
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public List<string> Modules { get; set; } = new List<string>();
    }
}