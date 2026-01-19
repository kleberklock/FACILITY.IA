using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PROJFACILITY.IA.Services;
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace PROJFACILITY.IA.Controllers
{
    [Authorize]
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly AppDbContext _context;

        public ChatController(ChatService chatService, AppDbContext context)
        {
            _chatService = chatService;
            _context = context;
        }

        [HttpPost("enviar")]
        public async Task<IActionResult> EnviarMensagem([FromForm] string message, [FromForm] string agentId)
        {
            try 
            {
                // 1. Identificar Usuário Logado
                var userIdStr = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) 
                    return Unauthorized(new { message = "Usuário não identificado." });

                // 2. Buscar Histórico (Últimas 10 mensagens deste agente para este usuário)
                // Ajuste 'AgentId' e 'UserId' conforme o nome exato na sua Model ChatMessage
                var history = await _context.ChatMessages
                    .Where(m => m.UserId == userId && m.AgentId == agentId) 
                    .OrderByDescending(m => m.Timestamp) // Pega as mais recentes
                    .Take(10)
                    .OrderBy(m => m.Timestamp) // Reordena para cronologia correta
                    .ToListAsync();

                // 3. Salvar Mensagem do Usuário no Banco
                var userMsg = new ChatMessage 
                { 
                    UserId = userId, 
                    AgentId = agentId, 
                    Text = message, 
                    Sender = "user", 
                    Timestamp = DateTime.UtcNow 
                };
                _context.ChatMessages.Add(userMsg);
                await _context.SaveChangesAsync();
                
                // 4. Chamar Serviço de IA (Passando o userId novo!)
                var (responseAI, tokens) = await _chatService.GetAIResponse(message, agentId, history, userId);

                // 5. Salvar Resposta da IA no Banco
                var aiMsg = new ChatMessage 
                { 
                    UserId = userId, 
                    AgentId = agentId, 
                    Text = responseAI, 
                    Sender = "assistant", 
                    Timestamp = DateTime.UtcNow 
                };
                _context.ChatMessages.Add(aiMsg);
                
                // Opcional: Atualizar tokens gastos no banco (se o ChatService já não fizer isso)
                // O ChatService da etapa anterior já fazia, então só salvamos a mensagem.
                
                await _context.SaveChangesAsync();

                return Ok(new { reply = responseAI, tokens = tokens });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro no processamento: " + ex.Message });
            }
        }
    }
}