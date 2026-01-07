using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

namespace PROJFACILITY.IA.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string emailDestino, string assunto, string mensagemHtml)
        {
            try
            {
                var remetente = _configuration["EmailSettings:Remetente"];
                // ✅ CORREÇÃO: Remove espaços da senha vinda do appsettings
                var senha = _configuration["EmailSettings:SenhaApp"]?.Replace(" ", "");
                var servidor = _configuration["EmailSettings:SmtpServer"];
                var portaStr = _configuration["EmailSettings:Porta"];
                int porta = int.TryParse(portaStr, out var p) ? p : 587;

                if (string.IsNullOrEmpty(remetente) || string.IsNullOrEmpty(senha))
                {
                    Console.WriteLine("[ERRO EMAIL] Credenciais não configuradas.");
                    return false;
                }

                // Criação da mensagem com MimeKit
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Facility.IA", remetente));
                message.To.Add(new MailboxAddress("", emailDestino));
                message.Subject = assunto;

                var bodyBuilder = new BodyBuilder { HtmlBody = mensagemHtml };
                message.Body = bodyBuilder.ToMessageBody();

                // Envio com MailKit SmtpClient
                using (var client = new SmtpClient())
                {
                    // Conecta e Autentica
                    await client.ConnectAsync(servidor, porta, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(remetente, senha);
                    
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                Console.WriteLine($"[SUCESSO] Email enviado para {emailDestino}");
                return true; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO DE ENVIO]: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"[DETALHE]: {ex.InnerException.Message}");
                return false;
            }
        }
    }
}