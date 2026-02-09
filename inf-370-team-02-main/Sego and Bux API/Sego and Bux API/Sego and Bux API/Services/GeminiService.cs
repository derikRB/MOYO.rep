//using System;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;

//namespace Sego_and__Bux.Services
//{
//    /// <summary>
//    /// Minimal Gemini wrapper. Replace SendPromptAsync TODO with the correct Gemini REST call
//    /// for your chosen Gemini product & payload.
//    /// </summary>
//    public class GeminiService
//    {
//        private readonly IHttpClientFactory _httpFactory;
//        private readonly string _apiKey;
//        private readonly ILogger<GeminiService> _log;

//        public GeminiService(IHttpClientFactory httpFactory, IConfiguration cfg, ILogger<GeminiService> log)
//        {
//            _httpFactory = httpFactory;
//            _log = log;
//            _apiKey = cfg["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey not configured");
//        }

//        /// <summary>
//        /// Sends a prompt and returns the text response.
//        /// TODO: Replace the internal HTTP call with the exact Gemini endpoint/payload you want.
//        /// </summary>
//        public async Task<string> SendPromptAsync(string prompt)
//        {
//            try
//            {
//                // Create a client
//                var client = _httpFactory.CreateClient("gemini");
//                // Example: set Authorization header with API key (adjust as Gemini docs require)
//                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

//                // TODO: Replace URL & request body with actual Gemini API endpoint and body.
//                var endpoint = "https://api.generative.googleapis.com/v1/models/text-bison-001:generate"; // example (update as required)
//                var payload = new
//                {
//                    prompt = new { text = prompt },
//                    // maxOutputTokens = 512,
//                    // temperature = 0.2
//                };

//                var json = JsonSerializer.Serialize(payload);
//                var resp = await client.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));

//                if (!resp.IsSuccessStatusCode)
//                {
//                    var err = await resp.Content.ReadAsStringAsync();
//                    _log.LogError("GeminiService.SendPromptAsync error: {Status} {Body}", resp.StatusCode, err);
//                    return null!;
//                }

//                var respJson = await resp.Content.ReadAsStringAsync();
//                // Attempt to parse a common shape; adapt to exact Gemini response structure.
//                using var doc = JsonDocument.Parse(respJson);
//                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
//                {
//                    var txt = candidates[0].GetProperty("content").GetString();
//                    return txt ?? "";
//                }
//                // fallback attempt
//                if (doc.RootElement.TryGetProperty("output", out var outputEl))
//                {
//                    return outputEl.GetRawText();
//                }

//                return respJson;
//            }
//            catch (Exception ex)
//            {
//                _log.LogError(ex, "GeminiService.SendPromptAsync failed");
//                return null!;
//            }
//        }
//    }
//}
