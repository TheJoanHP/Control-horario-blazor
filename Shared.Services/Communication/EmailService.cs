using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Shared.Services.Communication
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _username;
        private readonly string _password;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? throw new InvalidOperationException("SMTP Server no configurado");
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _senderEmail = _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("Sender Email no configurado");
            _senderName = _configuration["EmailSettings:SenderName"] ?? "Sphere Time Control";
            _username = _configuration["EmailSettings:Username"] ?? _senderEmail;
            _password = _configuration["EmailSettings:Password"] ?? throw new InvalidOperationException("Email Password no configurado");
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            return await SendEmailAsync(new List<string> { to }, subject, body, isHtml);
        }

        public async Task<bool> SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_senderName, _senderEmail));
                
                foreach (var recipient in to)
                {
                    message.To.Add(new MailboxAddress("", recipient));
                }

                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_username, _password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception (considera usar ILogger aquí)
                Console.WriteLine($"Error enviando email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentPath, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_senderName, _senderEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }

                if (File.Exists(attachmentPath))
                {
                    bodyBuilder.Attachments.Add(attachmentPath);
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_username, _password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando email con adjunto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string to, string employeeName, string tempPassword, string companyName)
        {
            var subject = $"Bienvenido a {companyName} - Acceso al Sistema de Control Horario";
            var body = $@"
                <html>
                <body>
                    <h2>¡Bienvenido/a {employeeName}!</h2>
                    <p>Te damos la bienvenida al sistema de control horario de <strong>{companyName}</strong>.</p>
                    
                    <h3>Datos de acceso:</h3>
                    <ul>
                        <li><strong>Email:</strong> {to}</li>
                        <li><strong>Contraseña temporal:</strong> {tempPassword}</li>
                    </ul>
                    
                    <p><strong>Importante:</strong> Por tu seguridad, te recomendamos cambiar la contraseña después del primer acceso.</p>
                    
                    <p>Si tienes alguna pregunta o problema para acceder, no dudes en contactar con tu supervisor o el departamento de RRHH.</p>
                    
                    <p>¡Esperamos que tengas una excelente experiencia con nuestro sistema!</p>
                    
                    <br>
                    <p>Saludos,<br>
                    Equipo de {companyName}</p>
                </body>
                </html>";

            return await SendEmailAsync(to, subject, body, true);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string to, string employeeName, string resetLink)
        {
            var subject = "Restablecimiento de Contraseña - Sistema de Control Horario";
            var body = $@"
                <html>
                <body>
                    <h2>Restablecimiento de Contraseña</h2>
                    <p>Hola {employeeName},</p>
                    
                    <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en el sistema de control horario.</p>
                    
                    <p>Para restablecer tu contraseña, haz clic en el siguiente enlace:</p>
                    <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Restablecer Contraseña</a></p>
                    
                    <p>Este enlace expirará en 1 hora por motivos de seguridad.</p>
                    
                    <p>Si no solicitaste este cambio, puedes ignorar este mensaje de forma segura.</p>
                    
                    <br>
                    <p>Saludos,<br>
                    Equipo de Soporte</p>
                </body>
                </html>";

            return await SendEmailAsync(to, subject, body, true);
        }

        public async Task<bool> SendVacationNotificationAsync(string to, string employeeName, string status, DateTime startDate, DateTime endDate)
        {
            var statusText = status switch
            {
                "Approved" => "APROBADA",
                "Rejected" => "RECHAZADA",
                "Pending" => "PENDIENTE",
                _ => status.ToUpper()
            };

            var subject = $"Solicitud de Vacaciones {statusText}";
            var body = $@"
                <html>
                <body>
                    <h2>Estado de Solicitud de Vacaciones</h2>
                    <p>Hola {employeeName},</p>
                    
                    <p>Te informamos que tu solicitud de vacaciones ha sido <strong>{statusText}</strong>.</p>
                    
                    <h3>Detalles de la solicitud:</h3>
                    <ul>
                        <li><strong>Fecha de inicio:</strong> {startDate:dd/MM/yyyy}</li>
                        <li><strong>Fecha de fin:</strong> {endDate:dd/MM/yyyy}</li>
                        <li><strong>Estado:</strong> {statusText}</li>
                    </ul>
                    
                    <p>Si tienes alguna pregunta sobre esta decisión, no dudes en contactar con tu supervisor.</p>
                    
                    <br>
                    <p>Saludos,<br>
                    Departamento de Recursos Humanos</p>
                </body>
                </html>";

            return await SendEmailAsync(to, subject, body, true);
        }
    }
}