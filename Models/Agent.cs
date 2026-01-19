using System.ComponentModel.DataAnnotations;

namespace PROJFACILITY.IA.Models
{
    public class Agent
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Specialty { get; set; } = string.Empty; 

        public string SystemInstruction { get; set; } = "Você é um assistente virtual da plataforma Facility.IA.";

        // NOVO: Quem criou este agente? (Null = Sistema/Público)
        public int? CreatorId { get; set; } 
    }
}