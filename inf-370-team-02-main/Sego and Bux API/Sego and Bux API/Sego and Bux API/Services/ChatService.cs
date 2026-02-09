//using System;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Configuration;
//using Sego_and__Bux.Data;
//using Sego_and__Bux.Interfaces;
//using Sego_and__Bux.Models;

//namespace Sego_and__Bux.Services
//{
//    public class ChatService : IChatService
//    {
//        private readonly IConfiguration _config;
//        private readonly ApplicationDbContext _db;
//        private readonly GeminiService _gemini;
//        private readonly ILogger<ChatService> _log;
//        private readonly IChatbotConfigService _chatbotConfigService;

//        public ChatService(
//            IConfiguration config,
//            ApplicationDbContext db,
//            GeminiService gemini,
//            ILogger<ChatService> log,
//            IChatbotConfigService chatbotConfigService)
//        {
//            _config = config;
//            _db = db;
//            _gemini = gemini;
//            _log = log;
//            _chatbotConfigService = chatbotConfigService;
//        }

//        public async Task<string> AskQuestionAsync(string question)
//        {
//            // Defensive: avoid throwing for transient DB issues
//            try
//            {
//                var sb = new StringBuilder();

//                // --- Top 5 products ---
//                var products = await _db.Products
//                    .Include(p => p.ProductType)
//                    .OrderBy(p => p.Name)
//                    .Take(5)
//                    .Select(p => new { p.Name, ProductTypeName = p.ProductType.ProductTypeName, p.Price })
//                    .ToListAsync();

//                // --- Top 3 recent orders (from Orders->Items) ---
//                var orders = await _db.Orders
//                    .Include(o => o.Items)
//                    .ThenInclude(oi => oi.Product)
//                    .OrderByDescending(o => o.OrderDate)
//                    .Take(3)
//                    .Select(o => new
//                    {
//                        o.OrderID,
//                        o.OrderDate,
//                        Total = o.Items.Sum(oi => oi.Quantity * oi.UnitPrice),
//                        Products = o.Items.Select(oi => $"{oi.Quantity}x {oi.Product.Name}")
//                    })
//                    .ToListAsync();

//                // --- Top 5 FAQs ---
//                var faqs = await _db.FaqItems
//                    .OrderBy(f => f.SortOrder)
//                    .Take(5)
//                    .ToListAsync();

//                // --- ChatbotConfig policy (shipping thresholds, contact) ---
//                var cfg = await _chatbotConfigService.GetAsync();

//                sb.AppendLine("### Context Data from Database");
//                sb.AppendLine();

//                sb.AppendLine("Products:");
//                foreach (var p in products)
//                    sb.AppendLine($"- {p.Name} ({p.ProductTypeName}) — {p.Price:C}");

//                sb.AppendLine();
//                sb.AppendLine("Recent Orders:");
//                foreach (var o in orders)
//                    sb.AppendLine($"- #{o.OrderID} on {o.OrderDate:yyyy-MM-dd} — {o.Total:C} — Items: {string.Join(", ", o.Products)}");

//                sb.AppendLine();
//                sb.AppendLine("Top FAQs:");
//                foreach (var f in faqs)
//                    sb.AppendLine($"- {f.Question?.Trim()}");

//                sb.AppendLine();
//                if (cfg != null)
//                {
//                    sb.AppendLine("Company contact & shipping policy:");
//                    sb.AppendLine($"- WhatsApp: {cfg.WhatsAppNumber}");
//                    sb.AppendLine($"- SupportEmail: {cfg.SupportEmail}");
//                    sb.AppendLine($"- OriginAddress: {cfg.CompanyAddress}");
//                    sb.AppendLine($"- ThresholdKm: {cfg.ThresholdKm}");
//                    sb.AppendLine($"- FlatShippingFee: {cfg.FlatShippingFee:C}");
//                    sb.AppendLine($"- HandToHandFee: {cfg.HandToHandFee:C}");
//                }

//                // Add the user question last (important)
//                sb.AppendLine();
//                sb.AppendLine("User question:");
//                sb.AppendLine(question);

//                // Send to Gemini (the wrapper handles API key & HTTP)
//                var prompt = sb.ToString();

//                var response = await _gemini.SendPromptAsync(prompt);

//                if (string.IsNullOrWhiteSpace(response))
//                {
//                    _log.LogWarning("Gemini returned empty response for question: {Question}", question);
//                    return "Sorry — I couldn't generate an answer right now.";
//                }

//                return response.Trim();
//            }
//            catch (Exception ex)
//            {
//                _log.LogError(ex, "ChatService.AskQuestionAsync failed");
//                return "An error occurred while fetching the answer. Try again later.";
//            }
//        }
//    }
//}
