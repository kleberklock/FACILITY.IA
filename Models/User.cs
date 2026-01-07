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
        public string? Plan { get; set; } = "Free"; 
        public string? SubscriptionCycle { get; set; } = "Mensal"; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool? IsActive { get; set; } = false; // Começa inativo
        public DateTime? LastLogin { get; set; }

        // Campos para o código de verificação
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeExpires { get; set; }
    }
}