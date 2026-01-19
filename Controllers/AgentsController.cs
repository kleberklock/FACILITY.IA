using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Models;

namespace PROJFACILITY.IA.Controllers
{
    [ApiController]
    [Route("api/agents")]
    public class AgentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AgentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agent>>> GetAgents([FromQuery] int userId)
        {
            return await _context.Agents
                .Where(a => a.CreatorId == null || a.CreatorId == userId)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> PostAgent([FromBody] CreateAgentRequest request)
        {
            var user = await _context.Users.FindAsync(request.CreatorId);
            if (user == null) return Unauthorized();

            if (user.Plan == "Free" || user.Plan == "Iniciante")
                return BadRequest("Usuários Iniciantes usam apenas agentes oficiais.");

            if (user.Plan == "Plus")
            {
                int meusAgentes = await _context.Agents.CountAsync(a => a.CreatorId == user.Id);
                if (meusAgentes >= 5) return BadRequest("Limite de 5 Agentes atingido.");
            }

            var agent = new Agent
            {
                Name = request.Name,
                Specialty = "Personalizado",
                SystemInstruction = request.Prompt,
                CreatorId = user.Id
            };

            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Agente criado!", id = agent.Id });
        }

        // CORREÇÃO: Inicializando propriedades para evitar CS8618
        public class CreateAgentRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Prompt { get; set; } = string.Empty;
            public int CreatorId { get; set; }
        }
    }
}