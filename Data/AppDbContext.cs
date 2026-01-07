using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Models;

namespace PROJFACILITY.IA.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<ChatMessage> Messages { get; set; }
        public DbSet<Agent> Agents { get; set; }
        
        // NOVO: Tabela para gerenciar arquivos ingeridos
        public DbSet<KnowledgeDocument> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new { m.UserId, m.AgentName });
        }
    }
}