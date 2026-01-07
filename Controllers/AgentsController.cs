using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Models;

namespace PROJFACILITY.IA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AgentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/agents (Para listar na barra lateral)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agent>>> GetAgents()
        {
            return await _context.Agents.ToListAsync();
        }

        // POST: api/agents (Para cadastrar um novo)
        [HttpPost]
        public async Task<IActionResult> PostAgent([FromBody] Agent agent)
        {
            if (agent == null) return BadRequest();

            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Agente cadastrado com sucesso!", id = agent.Id });
        }
    }
}