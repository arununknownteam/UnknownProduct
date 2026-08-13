using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace BackendApi.Services
{
    public class LLMService
    {
        private readonly string _pythonExecutable;
        private readonly string _scriptPath;
        private readonly ILogger<LLMService> _logger;

        public LLMService(IWebHostEnvironment env, ILogger<LLMService> logger)
        {
            _logger = logger;
            _scriptPath = Path.Combine(env.ContentRootPath, "Python", "ai_chat.py");
            _pythonExecutable = GetPythonExecutable();
        }

        private string GetPythonExecutable()
        {
            var localVenv = Path.Combine(Environment.CurrentDirectory, "Python", ".venv", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Scripts" : "bin", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python.exe" : "python");

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

        public async Task<string> AskAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "Please enter a message for the AI chat.";

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutable,
                Arguments = $"\"{_scriptPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? Environment.GetEnvironmentVariable("GROQ_API_KEY");

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                startInfo.Environment["OPENAI_API_KEY"] = apiKey;
                startInfo.Environment["GROQ_API_KEY"] = apiKey;
            }

            using var process = Process.Start(startInfo);

            if (process == null)
                throw new InvalidOperationException("Could not start Python process.");

            var requestJson = JsonSerializer.Serialize(new { prompt });
            await process.StandardInput.WriteAsync(requestJson);
            process.StandardInput.Close();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            _logger.LogInformation("Python script output: {Output}", output);
            _logger.LogInformation("Python script error: {Error}", error);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Python AI script error: {Error}", error);
                return $"AI service error: {error}";
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogError("Python script returned empty output");
                return "AI service returned empty response. Please try again.";
            }

            try
            {
                var document = JsonDocument.Parse(output);
                if (document.RootElement.TryGetProperty("reply", out var replyElement))
                {
                    return replyElement.GetString() ?? "No answer returned.";
                }

                _logger.LogError("Invalid response format from Python: {Output}", output);
                return "Invalid response format from AI service.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to parse AI response: {Output}", output);
                return $"Error parsing AI response: {ex.Message}";
            }
        }

        public async Task<string> AskAsync(System.Collections.Generic.List<BackendApi.DTOs.ChatMessage> messages)
        {
            if (messages == null || messages.Count == 0)
                return await AskAsync(string.Empty);

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutable,
                Arguments = $"\"{_scriptPath}\"",
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
                throw new InvalidOperationException("Could not start Python process.");

            // convert messages to role/content lower-case keys
            var payloadMessages = messages.ConvertAll(m => new { role = m.Role, content = m.Content });
            var requestJson = JsonSerializer.Serialize(new { messages = payloadMessages });
            await process.StandardInput.WriteAsync(requestJson);
            process.StandardInput.Close();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Python AI script error: {Error}", error);
                throw new Exception(error);
            }

            try
            {
                var document = JsonDocument.Parse(output);
                if (document.RootElement.TryGetProperty("reply", out var replyElement))
                {
                    return replyElement.GetString() ?? "No answer returned.";
                }

                throw new Exception("Invalid chat response format.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to parse AI response: {Output}", output);
                throw new Exception("Invalid AI response.");
            }
        }
    }
}
