using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Novo

namespace PROJFACILITY.IA.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger; // Novo

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string emailDestino, string assunto, string mensagemHtml)
        {
            try
            {
                var remetente = _configuration["EmailSettings:Remetente"];
                var senha = _configuration["EmailSettings:SenhaApp"]?.Replace(" ", "");
                var servidor = _configuration["EmailSettings:SmtpServer"];
                var portaStr = _configuration["EmailSettings:Porta"];
                int porta = int.TryParse(portaStr, out var p) ? p : 587;

                if (string.IsNullOrEmpty(remetente) || string.IsNullOrEmpty(senha))
                {
                    _logger.LogError("Credenciais de e-mail não configuradas no appsettings.");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Facility.IA", remetente));
                message.To.Add(new MailboxAddress("", emailDestino));
                message.Subject = assunto;

                var bodyBuilder = new BodyBuilder { HtmlBody = mensagemHtml };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(servidor, porta, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(remetente, senha);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("Email enviado com sucesso para {Email}", emailDestino);
                return true; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar e-mail para {Email}", emailDestino);
                return false;
            }
        }
    }
}