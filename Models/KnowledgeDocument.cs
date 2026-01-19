using System;
using System.ComponentModel.DataAnnotations;

namespace PROJFACILITY.IA.Models
{
    public class KnowledgeDocument
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty; // Ex: lei_123.pdf
        public string AgentName { get; set; } = string.Empty; // Ex: Advogado
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}