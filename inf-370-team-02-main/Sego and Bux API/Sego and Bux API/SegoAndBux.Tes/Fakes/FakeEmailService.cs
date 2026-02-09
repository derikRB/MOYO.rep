using System.Collections.Generic;
using System.Threading.Tasks;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;

namespace SegoAndBux.Tests.Fakes
{
    public class FakeEmailService : IEmailService
    {
        public List<EmailDto> Sent { get; } = new();
        public Task SendEmailAsync(EmailDto e) { Sent.Add(e); return Task.CompletedTask; }
    }
}
