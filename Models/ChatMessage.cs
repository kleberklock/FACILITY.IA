using System;
using System.ComponentModel.DataAnnotations;

namespace PROJFACILITY.IA.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        // CORREÇÃO: Adicionado AgentId para permitir estatísticas no Admin
        public string AgentId { get; set; } = string.Empty; 
        
        public string Sender { get; set; } = string.Empty; 
        public string Text { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public int TokensUsed { get; set; } = 0; 
    }
}