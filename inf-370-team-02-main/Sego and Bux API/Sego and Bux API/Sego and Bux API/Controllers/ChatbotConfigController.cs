using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using System.Threading.Tasks;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/admin/chatbot-config")]
    public class ChatbotConfigController : ControllerBase
    {
        private readonly IChatbotConfigService _svc;
        public ChatbotConfigController(IChatbotConfigService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<ChatbotConfigDto?>> Get()
            => await _svc.GetAsync();

        [HttpPut]
        public async Task<ActionResult<ChatbotConfigDto?>> Update([FromBody] ChatbotConfigDto dto)
            => await _svc.UpdateAsync(dto);
    }
}
