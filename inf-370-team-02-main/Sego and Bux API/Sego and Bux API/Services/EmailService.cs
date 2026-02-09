using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Sego_and__Bux.Config;
using Sego_and__Bux.Models;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Services
{
    public class EmailService : IEmailService
    {
        private readonly HttpClient _http = new();
        private readonly EmailJsSettings _cfg;

        public EmailService(IOptions<EmailJsSettings> opts)
        {
            _cfg = opts.Value;
        }

        public async Task SendEmailAsync(EmailDto e)
        {
            var payload = new
            {
                service_id = _cfg.ServiceId,
                template_id = _cfg.TemplateId,
                user_id = _cfg.UserId,
                template_params = new
                {
                    to_name = e.ToName,
                    to_email = e.ToEmail,
                    reply_to = e.ToEmail,       // ← add this line
                    subject = e.Subject,
                    message = e.Message
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var req = new HttpRequestMessage(HttpMethod.Post,
                "https://api.emailjs.com/api/v1.0/email/send")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

    }
}
