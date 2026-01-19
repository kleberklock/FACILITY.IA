using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Models;
using PROJFACILITY.IA.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PROJFACILITY.IA.Controllers
{
    [Route("api/admin")]
    [ApiController]
    // [Authorize(Roles = "admin")] // Descomente em produção para segurança
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly KnowledgeService _knowledgeService;

        public AdminController(AppDbContext context, KnowledgeService knowledgeService)
        {
            _context = context;
            _knowledgeService = knowledgeService;
        }

        // ==========================================================
        // 1. LISTAGEM COMPLETA DE USUÁRIOS (Compatível com admin.html)
        // ==========================================================
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            var relatorio = new List<object>();

            foreach (var user in users)
            {
                // Calcula qual agente o usuário mais usa
                // Importante: Usa AgentId pois é o nome da coluna no banco
                var favAgentId = await _context.ChatMessages
                    .Where(m => m.UserId == user.Id)
                    .GroupBy(m => m.AgentId) 
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();

                relatorio.Add(new 
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Plan,
                    user.Role,
                    
                    // Tratamento de nulos para evitar erro no JSON
                    SubscriptionCycle = user.SubscriptionCycle ?? "Mensal",
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    
                    // Métricas Calculadas
                    MostUsedAgent = favAgentId ?? "Nenhum", // Nome exato que o HTML espera
                    UsedTokensCurrentMonth = user.UsedTokensCurrentMonth // Nome exato que o HTML espera
                });
            }

            // Ordena quem gasta mais para aparecer primeiro
            return Ok(relatorio.OrderByDescending(x => {
                dynamic d = x;
                return d.UsedTokensCurrentMonth;
            }));
        }

        // ==========================================================
        // 2. ATUALIZAR DADOS DO USUÁRIO (Plano/Ciclo)
        // ==========================================================
        [HttpPost("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return NotFound(new { message = "Usuário não encontrado." });

            // Atualiza Plano
            if (!string.IsNullOrEmpty(request.NewPlan)) 
                user.Plan = request.NewPlan;

            // Atualiza Ciclo (Mensal/Trimestral/Anual)
            if (!string.IsNullOrEmpty(request.NewCycle)) 
                user.SubscriptionCycle = request.NewCycle;
            
            // Lógica de Admin (Promoção/Rebaixamento)
            if (request.NewPlan == "Admin") user.Role = "admin";
            else if (user.Role == "admin" && request.NewPlan != "Admin") user.Role = "user";

            // Resetar Tokens (ex: mudou de mês)
            if (request.ResetTokens) 
                user.UsedTokensCurrentMonth = 0;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuário atualizado com sucesso!" });
        }

        // ==========================================================
        // 3. ESTATÍSTICAS DO DASHBOARD
        // ==========================================================
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
             var totalUsers = await _context.Users.CountAsync();
             var totalPro = await _context.Users.CountAsync(u => u.Plan == "Pro");
             
             // Receita Estimada (Cálculo simples baseado nos planos ativos)
             var monthlyRevenue = await _context.Users
                .SumAsync(u => u.Plan == "Pro" ? 149.90 : u.Plan == "Plus" ? 59.90 : 0);

             return Ok(new { 
                 TotalUsers = totalUsers, 
                 TotalPro = totalPro, 
                 Revenue = monthlyRevenue 
             });
        }

        // ==========================================================
        // 4. GERENCIAMENTO DE ARQUIVOS (Treinamento)
        // ==========================================================
        [HttpDelete("arquivo/{id}")]
        public async Task<IActionResult> ExcluirArquivo(int id)
        {
            bool sucesso = await _knowledgeService.ExcluirArquivo(id);
            if (sucesso) return Ok(new { message = "Arquivo excluído." });
            return BadRequest(new { message = "Erro ao excluir arquivo." });
        }

        [HttpPost("agente/prompt")]
        public async Task<IActionResult> AtualizarPrompt([FromBody] UpdatePromptRequest request)
        {
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Name == request.AgentName);
            if (agent == null) return NotFound("Agente não encontrado.");

            agent.SystemInstruction = request.NewPrompt;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Prompt atualizado!" });
        }

        // DTOs (Data Transfer Objects) - Blindados contra avisos amarelos
        public class UpdateUserRequest 
        { 
            public int UserId { get; set; } 
            public string NewPlan { get; set; } = string.Empty; 
            public string NewCycle { get; set; } = "Mensal"; 
            public bool ResetTokens { get; set; } = false;
        }

        public class UpdatePromptRequest 
        { 
            public string AgentName { get; set; } = string.Empty; 
            public string NewPrompt { get; set; } = string.Empty; 
        }
    }
}