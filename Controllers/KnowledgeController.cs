using Microsoft.AspNetCore.Mvc;
using PROJFACILITY.IA.Services;
using PROJFACILITY.IA.Data; // Necessário para checar o plano
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace PROJFACILITY.IA.Controllers
{
    [Route("api/knowledge")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private readonly KnowledgeService _knowledgeService;
        private readonly AppDbContext _context; // Injeção do Banco

        public KnowledgeController(KnowledgeService knowledgeService, AppDbContext context)
        {
            _knowledgeService = knowledgeService;
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string profession, [FromForm] int userId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nenhum arquivo enviado.");

            if (string.IsNullOrEmpty(profession))
                return BadRequest("Profissão não informada.");

            // 1. VERIFICAÇÃO DE PLANO E TAMANHO
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized("Usuário não identificado.");

            long tamanhoEmBytes = file.Length;
            double tamanhoEmMb = tamanhoEmBytes / (1024.0 * 1024.0);

            // Regras de Negócio (Feature Matrix)
            if (user.Plan == "Free" || user.Plan == "Iniciante")
            {
                if (tamanhoEmMb > 2) // Limite de 2MB para grátis
                {
                    return BadRequest("O plano Iniciante permite arquivos de até 2MB. Faça upgrade para enviar arquivos maiores.");
                }
            }
            else if (user.Plan == "Plus")
            {
                if (tamanhoEmMb > 5) // Limite de 5MB para Plus
                {
                    return BadRequest("O plano Plus permite arquivos de até 5MB. O plano Pro aceita até 50MB.");
                }
            }
            // Pro = 50MB (Configurado no servidor)

            try
            {
                using var stream = file.OpenReadStream();
                await _knowledgeService.ProcessarArquivoEIngerir(stream, file.FileName, profession);
                
                return Ok(new { message = "Arquivo processado e aprendido com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Erro ao processar arquivo: {ex.Message}" });
            }
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> Ingest([FromBody] IngestTextRequest request)
        {
            // Validação simples para texto manual
            if (string.IsNullOrEmpty(request.Text) || string.IsNullOrEmpty(request.Profession))
                return BadRequest("Texto ou profissão inválidos.");

            try
            {
                await _knowledgeService.IngerirConhecimento(request.Text, request.Profession, "Texto Manual");
                return Ok(new { message = "Texto absorvido com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Erro ao ingerir texto: {ex.Message}" });
            }
        }

        public class IngestTextRequest
        {
            public string Text { get; set; } = string.Empty;
            public string Profession { get; set; } = string.Empty;
        }
    }
}