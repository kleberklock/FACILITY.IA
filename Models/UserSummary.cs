using System;

namespace PROJFACILITY.IA.Models
{
    public class UserSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Plan { get; set; } = "Free";
        public DateTime CreatedAt { get; set; }

        // --- DADOS DE ATIVIDADE ---
        public int TotalMessages { get; set; }
        public string FavoriteAgent { get; set; } = "Nenhum";
        public int TotalTokens { get; set; } // Necessário para o gráfico de consumo

        // --- CAMPOS FALTANTES PARA O PAINEL DE CONTROLE ---
        public bool IsActive { get; set; } = true;    // Para mostrar Status (Ativo/Banido)
        public DateTime? LastLogin { get; set; }      // Para mostrar "Último Acesso"
    }
}