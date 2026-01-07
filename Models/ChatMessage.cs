using System;
using System.ComponentModel.DataAnnotations;

namespace PROJFACILITY.IA.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; } 
        public string Sender { get; set; } = string.Empty; 
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string AgentName { get; set; } = string.Empty;

        // NOVO CAMPO: Armazena o consumo da mensagem
        public int TokensUsed { get; set; } = 0; 
    }
}