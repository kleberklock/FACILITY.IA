using OpenAI.Chat;
using OpenAI.Embeddings;
using Pinecone;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using PROJFACILITY.IA.Data;
using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Models;

namespace PROJFACILITY.IA.Services
{
    public class ChatService
    {
        private readonly ChatClient? _chatClient;
        private readonly EmbeddingClient? _embeddingClient;
        private readonly PineconeClient _pinecone;
        private readonly AppDbContext _context;
        private readonly string _indexName = "facility-ia";

        public ChatService(IConfiguration configuration, AppDbContext context)
        {
            _context = context;

            var apiKey = configuration["OpenAI:ApiKey"];
            var pineconeKey = configuration["Pinecone:ApiKey"] ?? "";

            _pinecone = new PineconeClient(pineconeKey);

            if (!string.IsNullOrEmpty(apiKey) && apiKey != "SUA_CHAVE_AQUI")
            {
                _chatClient = new ChatClient("gpt-4o-mini", apiKey);
                _embeddingClient = new EmbeddingClient("text-embedding-3-small", apiKey);
            }
        }

        public async Task<(string Response, int Tokens)> GetAIResponse(string userMessage, string agentId, List<PROJFACILITY.IA.Models.ChatMessage> historicoDb)
        {
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Name == agentId);
            string systemInstruction = agent?.SystemInstruction ?? "Você é um assistente virtual da plataforma Facility.IA.";

            string contextoExtraido = await BuscarConhecimentoNoPinecone(userMessage, agentId);

            if (!string.IsNullOrEmpty(contextoExtraido))
            {
                systemInstruction += $"\n\nCONHECIMENTO TÉCNICO ENCONTRADO:\n{contextoExtraido}\n\nUse as informações acima para embasar sua resposta.";
            }

            if (_chatClient == null) return (GerarRespostaSimulada(agentId, userMessage), 0);

            try
            {
                List<OpenAI.Chat.ChatMessage> messages = new() { new SystemChatMessage(systemInstruction) };

                foreach (var msg in historicoDb)
                {
                    if (msg.Sender == "user") messages.Add(new UserChatMessage(msg.Text));
                    else messages.Add(new AssistantChatMessage(msg.Text));
                }

                messages.Add(new UserChatMessage(userMessage));

                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
                
                if (completion.Content != null && completion.Content.Count > 0)
                {
                    int totalTokens = completion.Usage != null ? completion.Usage.TotalTokens : 0;
                    return (completion.Content[0].Text, totalTokens);
                }
                return ("A IA não retornou resposta.", 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO OPENAI]: {ex.Message}");
                return ($"[ERRO] Falha na comunicação: {ex.Message}", 0);
            }
        }

        private async Task<string> BuscarConhecimentoNoPinecone(string query, string profissao)
        {
            if (_embeddingClient == null) return "";

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
                    // CORREÇÃO: Uso de ? para acesso seguro e verificação de nulo
                    var trechos = searchResponse.Matches
                        .Where(m => m.Metadata != null && m.Metadata.ContainsKey("texto"))
                        .Select(m => m.Metadata?["texto"]?.ToString() ?? "")
                        .Where(t => !string.IsNullOrEmpty(t));

                    return string.Join("\n---\n", trechos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO RAG]: {ex.Message}");
            }
            return "";
        }

        private string GerarRespostaSimulada(string agentId, string userMessage)
        {
             return $"[MODO OFFLINE] Olá! Sou o especialista **{agentId}**. Configure sua chave OpenAI para ativar o chat real.";
        }
    }
}