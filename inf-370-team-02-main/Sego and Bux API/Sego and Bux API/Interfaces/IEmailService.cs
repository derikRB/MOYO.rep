using System.Threading.Tasks;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailDto e);
    }
}
