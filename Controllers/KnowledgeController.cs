using Microsoft.AspNetCore.Mvc;
using PROJFACILITY.IA.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace PROJFACILITY.IA.Controllers
{
    [Route("api/knowledge")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private readonly KnowledgeService _knowledgeService;

        // Injeção de dependência do serviço
        public KnowledgeController(KnowledgeService knowledgeService)
        {
            _knowledgeService = knowledgeService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string profession)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nenhum arquivo enviado.");

            if (string.IsNullOrEmpty(profession))
                return BadRequest("Profissão não informada.");

            try
            {
                using var stream = file.OpenReadStream();
                // Chama o serviço para processar o PDF/Imagem/Txt
                await _knowledgeService.ProcessarArquivoEIngerir(stream, file.FileName, profession);
                
                return Ok(new { message = "Arquivo processado e aprendido com sucesso!" });
            }
            catch (Exception ex)
            {
                // Retorna erro amigável se falhar
                return BadRequest(new { message = $"Erro ao processar arquivo: {ex.Message}" });
            }
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> Ingest([FromBody] IngestTextRequest request)
        {
            if (string.IsNullOrEmpty(request.Text) || string.IsNullOrEmpty(request.Profession))
                return BadRequest("Texto ou profissão inválidos.");

            try
            {
                // Chama o serviço para salvar o texto manual
                await _knowledgeService.IngerirConhecimento(request.Text, request.Profession, "Texto Manual");
                
                return Ok(new { message = "Texto absorvido com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Erro ao ingerir texto: {ex.Message}" });
            }
        }

        // Classe auxiliar para receber o JSON do texto manual
        public class IngestTextRequest
        {
            public string Text { get; set; } = string.Empty;
            public string Profession { get; set; } = string.Empty;
        }
    }
}