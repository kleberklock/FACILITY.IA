using System.Net;
using System.Text.Json;

namespace PROJFACILITY.IA.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Tenta executar a requisição normal
            }
            catch (Exception ex)
            {
                // Se der erro, cai aqui
                _logger.LogError(ex, "ERRO CRÍTICO NÃO TRATADO: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                status = context.Response.StatusCode,
                message = "Ops! Ocorreu um erro interno no servidor. Nossa equipe já foi notificada.",
                errorId = Guid.NewGuid() // Um ID para você rastrear no log depois
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}