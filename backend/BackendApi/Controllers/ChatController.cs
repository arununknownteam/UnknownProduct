using BackendApi.DTOs;
using BackendApi.Services;
using BackendApi.NHibernate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;

namespace BackendApi.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly LLMService _llmService;
        private readonly ChatFallbackService _fallbackService;

        public ChatController(LLMService llmService, ChatFallbackService fallbackService)
        {
            _llmService = llmService;
            _fallbackService = fallbackService;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            string rawBody;
            using (var reader = new StreamReader(Request.Body))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            AppLogger.Info($"Chat POST raw body: {rawBody}");

            ChatRequest request = null;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                request = JsonSerializer.Deserialize<ChatRequest>(rawBody, opts);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                return BadRequest(new { error = "Invalid JSON payload", details = ex.Message, raw = rawBody });
            }

            if (request == null || (string.IsNullOrWhiteSpace(request.Message) && (request.Messages == null || request.Messages.Count == 0)))
            {
                return BadRequest(new { error = "Message is required.", raw = rawBody });
            }

            try
            {
                string reply;

                if (request.Messages != null && request.Messages.Count > 0)
                {
                    reply = await _llmService.AskAsync(request.Messages);
                }
                else
                {
                    reply = await _llmService.AskAsync(request.Message);
                }

                AppLogger.Info($"Chat reply: {reply}");

                // If LLM returned an error-like or instructional message, fall back to rule-based bot.
                if (!string.IsNullOrWhiteSpace(reply) &&
                    (reply.Contains("AI request failed", StringComparison.OrdinalIgnoreCase)
                     || reply.Contains("no OpenAI API key", StringComparison.OrdinalIgnoreCase)
                     || reply.Contains("AI chat is ready", StringComparison.OrdinalIgnoreCase)
                     || reply.Contains("Type a message", StringComparison.OrdinalIgnoreCase)
                     || reply.Contains("OpenAI Python SDK is not installed", StringComparison.OrdinalIgnoreCase)
                     || reply.Contains("quota", StringComparison.OrdinalIgnoreCase)
                     || reply.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase)
                     || reply.Contains("no API key", StringComparison.OrdinalIgnoreCase)))
                {
                    var promptForFallback = request.Message ?? (request.Messages != null && request.Messages.Count > 0 ? request.Messages[^1].Content : string.Empty);
                    var fallback = _fallbackService.Respond(promptForFallback);
                    AppLogger.Info($"Using fallback reply: {fallback}");
                    return Ok(new { reply = fallback, fallback = true });
                }

                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                // On exception, use rule-based fallback
                var promptForFallback = request?.Message ?? (request?.Messages != null && request.Messages.Count > 0 ? request.Messages[^1].Content : string.Empty);
                var fallback = _fallbackService.Respond(promptForFallback);
                return Ok(new { reply = fallback, fallback = true, error = ex.Message });
            }
        }
    }
}
