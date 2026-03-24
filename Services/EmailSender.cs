using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace DoAnWebDemo.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // THAY BẰNG EMAIL VÀ MẬT KHẨU ỨNG DỤNG CỦA BẠN
            var mail = "letanthanh01072005@gmail.com";
            var pw = "deea tnjv gqxo aakw";

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(mail, pw)
            };

            var fromAddress = new MailAddress(mail, "Hệ Thống FastLoan VN");
            var toAddress = new MailAddress(email);

            var mailMessage = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true // Bật chế độ gửi HTML để trang trí email
            };

            return client.SendMailAsync(mailMessage);
        }
    }
}