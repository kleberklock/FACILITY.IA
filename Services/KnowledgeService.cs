using OpenAI.Embeddings;
using Pinecone;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using Tesseract;
using System.Text;
using PROJFACILITY.IA.Data; 
using PROJFACILITY.IA.Models;
using Microsoft.EntityFrameworkCore;

namespace PROJFACILITY.IA.Services
{
    public class KnowledgeService
    {
        private readonly PineconeClient _pinecone;
        private readonly EmbeddingClient _embeddingClient;
        private readonly AppDbContext _context;
        
        private readonly string _indexName = "facility-ia"; 

        public KnowledgeService(IConfiguration config, AppDbContext context)
        {
            _pinecone = new PineconeClient(config["Pinecone:ApiKey"] ?? "");
            _embeddingClient = new EmbeddingClient("text-embedding-3-small", config["OpenAI:ApiKey"] ?? "");
            _context = context;
        }

        public async Task ProcessarArquivoEIngerir(Stream arquivoStream, string nomeArquivo, string profissao)
        {
            try 
            {
                string extensao = Path.GetExtension(nomeArquivo).ToLower();
                string textoExtraido = "";

                if (extensao == ".pdf") textoExtraido = ExtrairTextoPDF(arquivoStream);
                else if (extensao == ".jpg" || extensao == ".png") textoExtraido = ExtrairTextoImagem(arquivoStream);
                else {
                    using var reader = new StreamReader(arquivoStream);
                    textoExtraido = await reader.ReadToEndAsync();
                }

                if (!string.IsNullOrEmpty(textoExtraido))
                {
                    await IngerirConhecimento(textoExtraido, profissao, nomeArquivo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO PROCESSAMENTO]: {ex.Message}");
                throw;
            }
        }

        public async Task IngerirConhecimento(string textoBruto, string profissao, string nomeArquivo)
        {
            var doc = new KnowledgeDocument 
            { 
                FileName = nomeArquivo, 
                AgentName = profissao, 
                UploadedAt = DateTime.UtcNow 
            };
            
            // CORREÇÃO: KnowledgeDocuments
            _context.KnowledgeDocuments.Add(doc);
            await _context.SaveChangesAsync();

            try 
            {
                var chunks = QuebrarTexto(textoBruto, 1000);
                var index = _pinecone.Index(_indexName);
                var vectors = new List<Vector>();

                foreach (var chunk in chunks)
                {
                    var embedding = await _embeddingClient.GenerateEmbeddingAsync(chunk);
                    vectors.Add(new Vector {
                        Id = Guid.NewGuid().ToString(),
                        Values = embedding.Value.Vector.ToArray(),
                        Metadata = new Metadata {
                            { "texto", chunk },
                            { "profissao", profissao },
                            { "origem", nomeArquivo }
                        }
                    });
                }
                await index.UpsertAsync(new UpsertRequest { Vectors = vectors });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO PINECONE]: {ex.Message}");
                // CORREÇÃO: KnowledgeDocuments
                _context.KnowledgeDocuments.Remove(doc);
                await _context.SaveChangesAsync();
                throw; 
            }
        }

        public async Task<bool> ExcluirArquivo(int docId)
        {
            // CORREÇÃO: KnowledgeDocuments
            var doc = await _context.KnowledgeDocuments.FindAsync(docId);
            if (doc == null) return false;

            try
            {
                var index = _pinecone.Index(_indexName);
                var filter = new Metadata { { "origem", doc.FileName } };
                await index.DeleteAsync(new DeleteRequest { Filter = filter });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO PINECONE]: Falha ao apagar vetores: {ex.Message}");
            }

            // CORREÇÃO: KnowledgeDocuments
            _context.KnowledgeDocuments.Remove(doc);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Métodos Auxiliares ---

        private string ExtrairTextoPDF(Stream stream)
        {
            var sb = new StringBuilder();
            try {
                using (var pdf = PdfDocument.Open(stream))
                    foreach (var page in pdf.GetPages()) sb.AppendLine(page.Text);
            } catch { }
            return sb.ToString();
        }

        private string ExtrairTextoImagem(Stream stream)
        {
            try {
                using var engine = new TesseractEngine(@"./tessdata", "por", EngineMode.Default);
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                using var pix = Pix.LoadFromMemory(ms.ToArray());
                using var page = engine.Process(pix);
                return page.GetText();
            } catch { return ""; }
        }

        private List<string> QuebrarTexto(string texto, int tamanhoMax) {
            var lista = new List<string>();
            if (string.IsNullOrEmpty(texto)) return lista;
            texto = texto.Replace("\r", " ").Replace("\n", " ");
            for (int i = 0; i < texto.Length; i += tamanhoMax) {
                int tamanho = Math.Min(tamanhoMax, texto.Length - i);
                lista.Add(texto.Substring(i, tamanho));
            }
            return lista;
        }
    }
}