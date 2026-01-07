using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore; // Importante para métodos async do EF
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Models;
using System.Security.Claims;

namespace PROJFACILITY.IA.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/user/profile (Busca os dados atuais do banco)
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdStr = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Unauthorized();

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound();

                return Ok(new 
                { 
                    user.Name, 
                    user.Email, 
                    user.Plan, 
                    user.Role 
                });
            }
            catch
            {
                return StatusCode(500, new { message = "Erro ao buscar perfil." });
            }
        }

        // PUT: api/user/profile (Atualiza apenas o Nome)
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
        {
            try
            {
                var userIdStr = User.Identity?.Name;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Unauthorized();

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound();

                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "O nome não pode ser vazio." });

                user.Name = request.Name;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Perfil atualizado com sucesso!", user = new { user.Name, user.Email } });
            }
            catch
            {
                return StatusCode(500, new { message = "Erro ao atualizar perfil." });
            }
        }
    }
}