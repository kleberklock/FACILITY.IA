using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Models;
using PROJFACILITY.IA.Services;

namespace PROJFACILITY.IA.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly AppDbContext _context;

        public ChatController(ChatService chatService, AppDbContext context)
        {
            _chatService = chatService;
            _context = context;
        }

        [HttpGet("historico/{agentId}")]
        public async Task<IActionResult> ObterHistorico(string agentId)
        {
            try {
                var userId = ObterUserId();
                var msgs = await _context.Messages
                    .Where(m => m.UserId == userId && m.AgentName == agentId)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(50)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();
                return Ok(msgs);
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("enviar")]
[Consumes("multipart/form-data")] // Força o servidor a aceitar o formato do formulário
public async Task<IActionResult> EnviarMensagem([FromForm] string message, [FromForm] string agentId)
{
    if (string.IsNullOrEmpty(message)) return BadRequest("Mensagem vazia.");

    try
    {
        var userId = ObterUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        // --- LÓGICA DE LIMITES (MONETIZAÇÃO) ---
        int limiteDiario = (user.Plan == "Medium") ? 80 : (user.Plan == "Top" || user.Plan == "Enterprise") ? 99999 : 7;
        var hoje = DateTime.UtcNow.Date;
        var mensagensHoje = await _context.Messages
            .Where(m => m.UserId == userId && m.Sender == "user" && m.Timestamp >= hoje)
            .CountAsync();

        if (mensagensHoje >= limiteDiario)
        {
            return StatusCode(403, new { message = $"Limite atingido para o plano {user.Plan}." });
        }

        // --- SALVAR E CHAMAR IA ---
        var userMsg = new ChatMessage { UserId = userId, Sender = "user", Text = message, AgentName = agentId, Timestamp = DateTime.UtcNow };
        _context.Messages.Add(userMsg);
        await _context.SaveChangesAsync();

        // Busca histórico recente para contexto
        var historico = await _context.Messages
            .Where(m => m.UserId == userId && m.AgentName == agentId)
            .OrderByDescending(m => m.Timestamp).Take(5).OrderBy(m => m.Timestamp).ToListAsync();

        var resultado = await _chatService.GetAIResponse(message, agentId, historico);

        var aiMsg = new ChatMessage { UserId = userId, Sender = "ai", Text = resultado.Response, AgentName = agentId, Timestamp = DateTime.UtcNow, TokensUsed = resultado.Tokens };
        _context.Messages.Add(aiMsg);
        await _context.SaveChangesAsync();

        return Ok(new { reply = resultado.Response });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "Erro interno." });
    }
}

        private int ObterUserId()
        {
            if (User.Identity?.Name != null && int.TryParse(User.Identity.Name, out int id))
                return id;
            return 0;
        }
    }
}