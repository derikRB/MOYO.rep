using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace Sego_and__Bux.Services
{
    public class EmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string to, string otp)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("By Sego and Bux", _config["EmailSettings:FromEmail"]));
            msg.To.Add(new MailboxAddress("", to));
            msg.Subject = "🔒 Your By Sego & Bux OTP Code";
            msg.Body = new TextPart("plain")
            {
                Text = $@"Hello,

Your One-Time Password is: {otp}
This code expires in 10 minutes.

If you did not initiate this, you can ignore this email.

— The By Sego & Bux Security Team"
            };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _config["EmailSettings:SmtpServer"],
                    int.Parse(_config["EmailSettings:SmtpPort"]),
                    SecureSocketOptions.StartTls
                );
                await client.AuthenticateAsync(
                    _config["EmailSettings:SmtpUser"],
                    _config["EmailSettings:SmtpPass"]
                );
                await client.SendAsync(msg);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log detailed error for diagnosis
                Console.WriteLine("EMAIL SENDING ERROR (OTP): " + ex.ToString());
                throw; // Optionally rethrow for higher-level handling
            }
        }

        public async Task SendPasswordResetAsync(string to, string resetLink)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("By Sego and Bux", _config["EmailSettings:FromEmail"]));
            msg.To.Add(new MailboxAddress("", to));
            msg.Subject = "🔒 Reset Your By Sego & Bux Password";
            msg.Body = new TextPart("plain")
            {
                Text = $@"Hello,

A password reset was requested for your account.
Click the link below to reset your password (valid for 15 minutes):

{resetLink}

If you did not request this, ignore this email.

— The By Sego & Bux Security Team"
            };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _config["EmailSettings:SmtpServer"],
                    int.Parse(_config["EmailSettings:SmtpPort"]),
                    SecureSocketOptions.StartTls
                );
                await client.AuthenticateAsync(
                    _config["EmailSettings:SmtpUser"],
                    _config["EmailSettings:SmtpPass"]
                );
                await client.SendAsync(msg);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log detailed error for diagnosis
                Console.WriteLine("EMAIL SENDING ERROR (Password Reset): " + ex.ToString());
                throw; // Optionally rethrow for higher-level handling
            }
        }
    }
}
