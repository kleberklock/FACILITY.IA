using System;
using System.ComponentModel.DataAnnotations;

namespace PROJFACILITY.IA.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        
        public string? Role { get; set; } = "user"; 
        public string? Plan { get; set; } = "Free"; // Free, Pro, Enterprise
        public string? SubscriptionCycle { get; set; } = "Mensal"; 

        // --- CONTROLE DE LIMITES (NOVO) ---
        // Se a migration já rodou, certifique-se que esses nomes batem com o banco
        public long UsedTokensCurrentMonth { get; set; } = 0;
        public DateTime LastResetDate { get; set; } = DateTime.UtcNow;
        // ----------------------------------

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool? IsActive { get; set; } = false;
        public DateTime? LastLogin { get; set; }

        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeExpires { get; set; }
    }
}