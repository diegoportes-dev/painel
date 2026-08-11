using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;

public class MailKitEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public MailKitEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task EnviarEmailAsync(string paraEmail, string assunto, string mensagemHtml)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        email.To.Add(new MailboxAddress("", paraEmail));
        email.Subject = assunto;

        var bodyBuilder = new BodyBuilder { HtmlBody = mensagemHtml };
        email.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();
        
        // Conecta ao servidor usando STARTTLS (Porta 587)
        await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
        
        // Autentica com as credenciais
        await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
        
        // Envia e desconecta
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

}
