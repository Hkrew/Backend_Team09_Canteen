using Microsoft.AspNetCore.Identity.UI.Services;

namespace Canteen.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Trả về Task hoàn thành giả (không thực sự gửi email nào đi cả)
            return Task.CompletedTask;
        }
    }
}