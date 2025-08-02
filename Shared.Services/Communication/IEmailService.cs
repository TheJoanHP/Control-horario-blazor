namespace Shared.Services.Communication
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true);
        Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentPath, bool isHtml = true);
        Task<bool> SendWelcomeEmailAsync(string to, string employeeName, string tempPassword, string companyName);
        Task<bool> SendPasswordResetEmailAsync(string to, string employeeName, string resetLink);
        Task<bool> SendVacationNotificationAsync(string to, string employeeName, string status, DateTime startDate, DateTime endDate);
    }
}