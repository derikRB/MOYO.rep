//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Sego_and__Bux.Interfaces;

//namespace Sego_and__Bux.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class ChatController : ControllerBase
//    {
//        private readonly IChatService _chatService;

//        public ChatController(IChatService chatService)
//        {
//            _chatService = chatService;
//        }

//        // POST: api/chat/ask
//        [HttpPost("ask")]
//        [AllowAnonymous] // No login required
//        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
//        {
//            if (string.IsNullOrWhiteSpace(request.Message))
//                return BadRequest(new { error = "Message cannot be empty" });

//            var reply = await _chatService.AskQuestionAsync(request.Message);

//            return Ok(new { reply });
//        }
//    }

//    public class ChatRequest
//    {
//        public string Message { get; set; } = string.Empty;
//    }
//}
