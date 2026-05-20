using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace SalonHair.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otpCode);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var senderEmail = emailSettings["Email"];
            var senderPassword = emailSettings["Password"];
            var host = emailSettings["Host"];
            var port = int.Parse(emailSettings["Port"] ?? "587");

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail!),
                Subject = "Mã xác thực OTP - SalonHair",
                Body = $"<h3>Mã xác thực OTP của bạn là: <strong style='color: blue;'>{otpCode}</strong></h3>" +
                       $"<p>Mã này có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>",
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(message);
        }
    }
}
