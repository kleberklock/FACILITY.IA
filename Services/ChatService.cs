using OpenAI.Chat;
using OpenAI.Embeddings;
using Pinecone;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Models;
using Microsoft.Extensions.Logging;

namespace PROJFACILITY.IA.Services
{
    public class ChatService
    {
        private readonly ChatClient? _chatClient;
        private readonly EmbeddingClient? _embeddingClient;
        private readonly PineconeClient _pinecone;
        private readonly AppDbContext _context;
        private readonly ILogger<ChatService> _logger;
        private readonly string _indexName = "facility-ia";

        // Limites de Plano
        private const int LIMIT_FREE = 5000; 
        private const int LIMIT_PRO = 100000;

        public ChatService(IConfiguration configuration, AppDbContext context, ILogger<ChatService> logger)
        {
            _context = context;
            _logger = logger;

            // Busca a chave nas variáveis de ambiente (Azure) ou appsettings.json
            var apiKey = configuration["OpenAI:ApiKey"];
            var pineconeKey = configuration["Pinecone:ApiKey"] ?? "";

            if (string.IsNullOrEmpty(apiKey)) 
                _logger.LogWarning("OpenAI:ApiKey não encontrada nas configurações.");
            
            if (string.IsNullOrEmpty(pineconeKey)) 
                _logger.LogWarning("Pinecone:ApiKey não encontrada nas configurações.");

            // Inicializa Pinecone (se houver chave)
            if (!string.IsNullOrEmpty(pineconeKey))
            {
                try { _pinecone = new PineconeClient(pineconeKey); }
                catch (Exception ex) { _logger.LogError(ex, "Erro ao iniciar Pinecone"); }
            }

            // --- CORREÇÃO: Inicializa OpenAI sem bloquear chaves específicas ---
            if (!string.IsNullOrEmpty(apiKey))
            {
                try
                {
                    // Usa gpt-4o-mini (mais rápido e barato) ou gpt-3.5-turbo
                    _chatClient = new ChatClient("gpt-4o-mini", apiKey);
                    _embeddingClient = new EmbeddingClient("text-embedding-3-small", apiKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao iniciar OpenAI");
                }
            }
        }

        public async Task<(string Response, int Tokens)> GetAIResponse(string userMessage, string agentId, List<PROJFACILITY.IA.Models.ChatMessage> historicoDb, int userId)
        {
            // 1. VERIFICA PLANO E LIMITES
            var user = await _context.Users.FindAsync(userId);
            if (user == null) 
            {
                _logger.LogWarning($"Usuário {userId} não encontrado ao tentar chat.");
                return ("Erro: Usuário não encontrado.", 0);
            }

            // Reseta tokens se mudou o mês
            if (user.LastResetDate < DateTime.UtcNow.AddMonths(-1))
            {
                user.UsedTokensCurrentMonth = 0;
                user.LastResetDate = DateTime.UtcNow;
            }

            int limiteAtual = user.Plan == "Pro" ? LIMIT_PRO : 
                              user.Plan == "Enterprise" ? int.MaxValue : LIMIT_FREE;

            if (user.UsedTokensCurrentMonth >= limiteAtual)
            {
                return ($"Limite do plano {user.Plan} atingido. Faça upgrade para continuar.", 0);
            }

            // 2. BUSCA CONTEXTO (RAG)
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Name == agentId);
            string systemInstruction = agent?.SystemInstruction ?? "Você é um assistente virtual útil e profissional.";

            string contextoExtraido = await BuscarConhecimentoNoPinecone(userMessage, agentId);

            if (!string.IsNullOrEmpty(contextoExtraido))
            {
                systemInstruction += $"\n\nBASE DE CONHECIMENTO (Use isso para responder):\n{contextoExtraido}\n";
            }

            // Se o cliente OpenAI não foi iniciado (sem chave), retorna erro simulado
            if (_chatClient == null) return (GerarRespostaSimulada(agentId, userMessage), 0);

            try
            {
                // Monta o histórico de mensagens para a IA
                List<OpenAI.Chat.ChatMessage> messages = new() { new SystemChatMessage(systemInstruction) };
                
                // Adiciona as últimas mensagens do banco para manter o contexto
                foreach (var msg in historicoDb)
                {
                    if (msg.Sender == "user") messages.Add(new UserChatMessage(msg.Text));
                    else messages.Add(new AssistantChatMessage(msg.Text));
                }

                // Adiciona a pergunta atual
                messages.Add(new UserChatMessage(userMessage));

                // Chama a OpenAI
                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
                
                if (completion.Content != null && completion.Content.Count > 0)
                {
                    int totalTokens = completion.Usage != null ? completion.Usage.TotalTokens : 0;
                    string resposta = completion.Content[0].Text;

                    // 3. ATUALIZA CONSUMO NO BANCO
                    user.UsedTokensCurrentMonth += totalTokens;
                    user.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return (resposta, totalTokens);
                }
                return ("A IA não retornou resposta.", 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao comunicar com OpenAI para o usuário {UserId}", userId);
                return ($"Erro de comunicação com a IA: {ex.Message}", 0);
            }
        }

        private async Task<string> BuscarConhecimentoNoPinecone(string query, string profissao)
        {
            if (_embeddingClient == null || _pinecone == null) return "";

            try
            {
                var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(query);
                float[] vetorPergunta = embeddingResult.Value.Vector.ToArray();

                var index = _pinecone.Index(_indexName);
                var searchRequest = new QueryRequest
                {
                    Vector = vetorPergunta,
                    TopK = 3,
                    IncludeMetadata = true,
                    Filter = new Metadata { { "profissao", profissao } }
                };

                var searchResponse = await index.QueryAsync(searchRequest);

                if (searchResponse.Matches != null && searchResponse.Matches.Any())
                {
                    var trechos = searchResponse.Matches
                        .Where(m => m.Metadata != null && m.Metadata.ContainsKey("texto"))
                        .Select(m => m.Metadata?["texto"]?.ToString() ?? "")
                        .Where(t => !string.IsNullOrEmpty(t));

                    return string.Join("\n---\n", trechos);
                }
            }
            catch (Exception ex)
            {
                // Loga erro mas não para o chat (apenas fica sem contexto extra)
                _logger.LogError(ex, "Erro ao buscar no Pinecone index {IndexName}", _indexName);
            }
            return "";
        }

        private string GerarRespostaSimulada(string agentId, string userMessage)
        {
             _logger.LogInformation("Gerando resposta simulada (Offline/Sem Chave) para agente {AgentId}", agentId);
             return $"[MODO OFFLINE] A IA não está respondendo. Verifique se a Chave da API (OpenAI:ApiKey) foi configurada corretamente na Azure.";
        }
    }
}