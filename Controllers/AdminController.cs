using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Models;
using PROJFACILITY.IA.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PROJFACILITY.IA.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var topConsumers = await _context.Users
                .Select(u => new {
                    u.Name,
                    TotalTokens = _context.Messages.Where(m => m.UserId == u.Id).Sum(m => m.TokensUsed)
                })
                .OrderByDescending(x => x.TotalTokens)
                .Take(5)
                .ToListAsync();

            var recentLogins = await _context.Users
                .Where(u => u.LastLogin != null)
                .OrderByDescending(u => u.LastLogin)
                .Take(5)
                .Select(u => new { 
                    Action = "Login", 
                    User = u.Name, 
                    Detail = "Acesso ao sistema", 
                    Date = u.LastLogin 
                })
                .ToListAsync();

            var recentUploads = await _context.Documents
                .OrderByDescending(d => d.UploadedAt)
                .Take(5)
                .Select(d => new { 
                    Action = "Upload", 
                    User = "Sistema", 
                    Detail = $"{d.FileName} ({d.AgentName})", 
                    Date = (DateTime?)d.UploadedAt 
                })
                .ToListAsync();

            var activityLog = recentLogins
                .Concat(recentUploads)
                .OrderByDescending(x => x.Date)
                .Take(10);

            return Ok(new { topConsumers, activityLog });
        }

        [HttpGet("usuarios")]
        public async Task<IActionResult> GetUsuarios()
        {
            var users = await _context.Users.ToListAsync();
            var relatorio = new List<object>();

            foreach (var user in users)
            {
                var totalMsgs = await _context.Messages.CountAsync(m => m.UserId == user.Id);
                var totalTokens = await _context.Messages.Where(m => m.UserId == user.Id).SumAsync(m => m.TokensUsed);
                
                var favAgent = await _context.Messages
                    .Where(m => m.UserId == user.Id)
                    .GroupBy(m => m.AgentName)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();

                relatorio.Add(new 
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Plan,
                    user.SubscriptionCycle,
                    user.IsActive, 
                    user.LastLogin,
                    user.CreatedAt,
                    TotalMessages = totalMsgs,
                    TotalTokens = totalTokens,
                    FavoriteAgent = favAgent ?? "Nenhum"
                });
            }

            return Ok(relatorio.OrderByDescending(x => {
                var prop = x.GetType().GetProperty("TotalTokens");
                var val = prop?.GetValue(x, null);
                return val != null ? (int)val : 0;
            }));
        }

        [HttpPost("usuario/plano")]
        public async Task<IActionResult> AlterarPlano([FromBody] UpdatePlanRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return NotFound();

            user.Plan = request.NewPlan;
            if (!string.IsNullOrEmpty(request.NewCycle))
            {
                user.SubscriptionCycle = request.NewCycle;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Assinatura atualizada com sucesso." });
        }

        [HttpPost("usuario/banir/{id}")]
        public async Task<IActionResult> ToggleBan(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // FIX: GetValueOrDefault converte 'bool?' para 'bool' (false se for null)
            user.IsActive = !user.IsActive.GetValueOrDefault(); 
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = (user.IsActive == true) ? "Usuário ativado." : "Usuário banido.", isActive = user.IsActive });
        }

        [HttpDelete("usuario/{id}")]
        public async Task<IActionResult> ExcluirUsuario(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Usuário não encontrado.");

            var mensagens = _context.Messages.Where(m => m.UserId == id);
            _context.Messages.RemoveRange(mensagens);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuário excluído permanentemente." });
        }

        [HttpGet("agente/{agentName}/arquivos")]
        public async Task<IActionResult> GetArquivosAgente(string agentName)
        {
            var docs = await _context.Documents
                .Where(d => d.AgentName == agentName)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
            return Ok(docs);
        }

        [HttpDelete("arquivo/{id}")]
        public async Task<IActionResult> ExcluirArquivo(int id, [FromServices] KnowledgeService knowledgeService)
        {
            bool sucesso = await knowledgeService.ExcluirArquivo(id);
            if (sucesso) return Ok(new { message = "Arquivo removido da memória da IA." });
            return BadRequest(new { message = "Falha ao excluir arquivo." });
        }

        [HttpPost("agente/prompt")]
        public async Task<IActionResult> AtualizarPrompt([FromBody] UpdatePromptRequest request)
        {
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Name == request.AgentName);
            if (agent == null) return NotFound("Agente não encontrado.");

            agent.SystemInstruction = request.NewPrompt;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Personalidade (Prompt) atualizada!" });
        }

        public class UpdatePlanRequest 
        { 
            public int UserId { get; set; } 
            public string NewPlan { get; set; } = string.Empty; 
            public string NewCycle { get; set; } = "Mensal"; 
        }

        public class UpdatePromptRequest 
        { 
            public string AgentName { get; set; } = string.Empty; 
            public string NewPrompt { get; set; } = string.Empty; 
        }
    }
}