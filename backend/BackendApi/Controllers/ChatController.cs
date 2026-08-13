using BackendApi.DTOs;
using BackendApi.Services;
using BackendApi.NHibernate;
using BackendApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using NHibernate.Linq;

namespace BackendApi.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly LLMService _llmService;
        private readonly ChatFallbackService _fallbackService;
        private readonly PDFProcessingService _pdfProcessingService;

        public ChatController(LLMService llmService, ChatFallbackService fallbackService, PDFProcessingService pdfProcessingService)
        {
            _llmService = llmService;
            _fallbackService = fallbackService;
            _pdfProcessingService = pdfProcessingService;
        }

        [HttpPost("pdf")]
        public async Task<IActionResult> PostPdfChat([FromBody] PdfChatRequest request)
        {
            try
            {
                // Get current user from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message is required" });
                }

                if (request.DocumentId == null)
                {
                    return BadRequest(new { error = "DocumentId is required" });
                }

                // Get user entity
                var user = await NHibernateHelper.SessionFactory.OpenSession()
                    .Query<BackendApi.Entities.User>()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Get document
                var document = await _pdfProcessingService.GetDocumentAsync(request.DocumentId.Value, user);
                if (document == null)
                {
                    return NotFound(new { error = "Document not found" });
                }

                if (!document.IsProcessed)
                {
                    return BadRequest(new { error = "Document is still processing. Please wait." });
                }

                // Get all chunks for this document
                var documentId = document.Id;
                var chunks = await NHibernateHelper.SessionFactory.OpenSession()
                    .Query<BackendApi.Entities.DocumentChunk>()
                    .Where(c => c.Document.Id == documentId)
                    .ToListAsync();

                if (chunks == null || chunks.Count == 0)
                {
                    return BadRequest(new { error = "No content found in document" });
                }

                // Prepare chunks for Python RAG search
                var chunksData = chunks.Select(c => new
                {
                    id = c.Id,
                    text = c.ChunkText,
                    embedding = c.Embedding,
                    page_number = c.PageNumber,
                    document_id = c.Document.Id,
                    file_name = document.FileName
                }).ToList();

                // Call Python RAG search script
                string ragScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Python", "rag_search.py");
                var startInfo = new ProcessStartInfo
                {
                    FileName = GetPythonExecutable(),
                    Arguments = $"\"{ragScriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    startInfo.Environment["GROQ_API_KEY"] = apiKey;
                }

                using var process = Process.Start(startInfo);
                if (process == null)
                    throw new Exception("Could not start Python process for RAG search");

                var ragInput = new
                {
                    query = request.Message,
                    chunks = chunksData,
                    top_k = 5
                };

                string inputJson = JsonSerializer.Serialize(ragInput);
                await process.StandardInput.WriteAsync(inputJson);
                process.StandardInput.Close();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var ex = new Exception($"Python RAG search error: {error}");
                    AppLogger.Error(ex);
                    throw new Exception($"RAG search failed: {error}");
                }

                // Parse RAG result
                var ragResult = JsonSerializer.Deserialize<JsonElement>(output);

                // Check for error in RAG result
                if (ragResult.TryGetProperty("error", out var ragError))
                {
                    var errorMsg = ragError.GetString();
                    AppLogger.Error(new Exception($"RAG search error: {errorMsg}"));
                    // Fall back to direct LLM call without RAG
                    var fallbackReply = await _llmService.AskAsync(request.Message);
                    return Ok(new
                    {
                        reply = fallbackReply,
                        document_id = document.Id,
                        document_name = document.FileName,
                        citations = new List<object>(),
                        fallback = true,
                        message = "RAG search failed, using direct AI response"
                    });
                }

                // Check if RAG prompt exists, otherwise use original message
                string ragPrompt = request.Message;
                if (ragResult.TryGetProperty("rag_prompt", out var ragPromptElement))
                {
                    ragPrompt = ragPromptElement.GetString() ?? request.Message;
                }

                // Parse citations if present
                var citationList = new List<object>();
                if (ragResult.TryGetProperty("citations", out var citationsElement) && citationsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var citation in citationsElement.EnumerateArray())
                    {
                        citationList.Add(new
                        {
                            citation_id = citation.GetProperty("citation_id").GetInt32(),
                            document_id = citation.GetProperty("document_id").GetGuid(),
                            file_name = citation.GetProperty("file_name").GetString(),
                            page_number = citation.GetProperty("page_number").GetInt32(),
                            text_snippet = citation.GetProperty("text_snippet").GetString(),
                            relevance_score = citation.GetProperty("relevance_score").GetDouble()
                        });
                    }
                }

                // Call LLM with RAG prompt
                string reply = await _llmService.AskAsync(ragPrompt);


                return Ok(new
                {
                    reply = reply,
                    document_id = document.Id,
                    document_name = document.FileName,
                    citations = citationList
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
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
                    return BadRequest(new { error = "Invalid JSON payload", details = ex.Message });
                }

                if (request == null || (string.IsNullOrWhiteSpace(request.Message) && (request.Messages == null || request.Messages.Count == 0)))
                {
                    return BadRequest(new { error = "Message is required." });
                }

                string reply;

                try
                {
                    if (request.Messages != null && request.Messages.Count > 0)
                    {
                        reply = await _llmService.AskAsync(request.Messages);
                    }
                    else
                    {
                        reply = await _llmService.AskAsync(request.Message);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex);
                    reply = _fallbackService.Respond(request.Message ?? string.Empty);
                    return Ok(new { reply = reply, fallback = true, error = "AI service temporarily unavailable" });
                }

                AppLogger.Info($"Chat reply: {reply}");

                // Check for error messages in reply
                if (reply.StartsWith("AI service error:") || 
                    reply.StartsWith("Error parsing AI response:") ||
                    reply.StartsWith("AI service returned empty response"))
                {
                    var fallback = _fallbackService.Respond(request.Message ?? string.Empty);
                    AppLogger.Info($"Using fallback due to AI error: {reply}");
                    return Ok(new { reply = fallback, fallback = true });
                }

                // Check for rate limit errors
                if (!string.IsNullOrWhiteSpace(reply) && (reply.Contains("Rate limit reached #43212", StringComparison.OrdinalIgnoreCase)
                     || reply.Contains("429", StringComparison.OrdinalIgnoreCase)))
                {
                    AppLogger.Info($"Rate limit detected, returning error message: {reply}");
                    return Ok(new { reply = reply, fallback = false, rateLimited = true });
                }

                return Ok(new { reply = reply });
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                return Ok(new { reply = "Sorry, I encountered an unexpected error. Please try again.", fallback = true });
            }
        }

        private string GetPythonExecutable()
        {
            var localVenv = Path.Combine(Environment.CurrentDirectory, "Python", ".venv", 
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Scripts" : "bin", 
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python.exe" : "python");

            var candidates = new[]
            {
                Environment.GetEnvironmentVariable("PYTHON_PATH"),
                localVenv,
                "python",
                "python3"
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct();

            foreach (var candidate in candidates)
            {
                try
                {
                    var testInfo = new ProcessStartInfo
                    {
                        FileName = candidate,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(testInfo);
                    if (process == null) continue;

                    process.WaitForExit(2000);
                    if (process.ExitCode == 0)
                        return candidate;
                }
                catch
                {
                    // ignore and try next candidate
                }
            }

            throw new InvalidOperationException(
                "Python executable not found. Install Python or set PYTHON_PATH environment variable."
            );
        }
    }
}