using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Services
{
    /// <summary>
    /// A simple EmailJS wrapper. Call SendEmailAsync(...) with an EmailDto.
    /// </summary>
    public class EmailNotificationService
    {
        private const string EmailJsApi = "https://api.emailjs.com/api/v1.0/email/send";
        private const string ServiceId = "service_tafmopo";
        private const string TemplateId = "template_82rcrfq";
        private const string UserId = "szdFF1Kfu0BD53aWB";

        /// <summary>
        /// Sends an email via EmailJS. Returns true on 2xx, false otherwise.
        /// </summary>
        public async Task<bool> SendEmailAsync(EmailDto dto)
        {
            var payload = new
            {
                service_id = ServiceId,
                template_id = TemplateId,
                user_id = UserId,
                template_params = new
                {
                    to_name = dto.ToName,
                    to_email = dto.ToEmail,
                    subject = dto.Subject,
                    message = dto.Message
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            var response = await client.PostAsync(EmailJsApi, content);

            return response.IsSuccessStatusCode;
        }
    }
}
