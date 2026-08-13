using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using BackendApi.Entities;
using BackendApi.NHibernate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NHibernate.Linq;
// 👉 FIX: Avoid ISession conflict
using NHSession = NHibernate.ISession;

namespace BackendApi.Services
{
    public class PDFProcessingService
    {
        private readonly string _pythonExecutable;
        private readonly string _fileProcessorScript;
        private readonly string _uploadFolder;
        private readonly ILogger<PDFProcessingService> _logger;
        private readonly NHSession _session;

        public PDFProcessingService(IWebHostEnvironment env, ILogger<PDFProcessingService> logger, NHSession session)
        {
            _logger = logger;
            _session = session;
            _fileProcessorScript = Path.Combine(env.ContentRootPath, "Python", "file_processor.py");
            _pythonExecutable = GetPythonExecutable();
            _uploadFolder = Path.Combine(env.ContentRootPath, "Uploads", "Documents");
            
            // Create upload directory if it doesn't exist
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
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

        private string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant();
        }

        private string GetFileType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "PDF",
                ".docx" or ".doc" => "Word Document",
                ".txt" => "Text File",
                ".csv" => "CSV",
                ".md" or ".markdown" => "Markdown",
                ".html" or ".htm" => "HTML",
                _ => "Unknown"
            };
        }

        private bool IsSupportedFileType(string extension)
        {
            var supportedTypes = new[] { ".pdf", ".docx", ".doc", ".txt", ".csv", ".md", ".markdown", ".html", ".htm" };
            return supportedTypes.Contains(extension.ToLowerInvariant());
        }

        public async Task<Document> ProcessAndSaveDocumentAsync(User user, IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                    throw new Exception("No file uploaded");

                var fileExtension = GetFileExtension(file.FileName);
                
                if (!IsSupportedFileType(fileExtension))
                    throw new Exception($"Unsupported file type. Supported formats: PDF, DOCX, TXT, CSV, Markdown, HTML");

                // Create document record
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    User = user,
                    FileName = Path.GetFileNameWithoutExtension(file.FileName),
                    FilePath = Path.Combine(_uploadFolder, $"{Guid.NewGuid()}{fileExtension}"),
                    FileExtension = fileExtension,
                    FileType = GetFileType(fileExtension),
                    FileSize = file.Length,
                    TotalPages = 0,
                    TotalChunks = 0,
                    IsProcessed = false,
                    UploadedAt = DateTime.UtcNow,
                    ProcessingError = null
                };

                // Save file to disk
                using (var stream = new FileStream(document.FilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save document to database
                await _session.SaveAsync(document);
                await _session.FlushAsync();

                // Process document asynchronously (don't block the response)
                _ = Task.Run(async () => await ProcessDocumentAsync(document));

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process and save document");
                throw;
            }
        }

        private async Task ProcessDocumentAsync(Document document)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 2000;
            
            try
            {
                _logger.LogInformation($"Starting to process document: {document.FileName} ({document.FileType})");

                // Read file
                byte[] fileBytes = await File.ReadAllBytesAsync(document.FilePath);

                // Prepare input for Python script
                string fileBase64 = Convert.ToBase64String(fileBytes);
                var input = new
                {
                    file_base64 = fileBase64,
                    file_extension = document.FileExtension,
                    chunk_size = 500,
                    overlap = 50
                };

                string inputJson = JsonSerializer.Serialize(input);

                // Execute Python script
                var startInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutable,
                    Arguments = $"\"{_fileProcessorScript}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    throw new Exception($"Could not start Python process for {document.FileType} processing");

                _logger.LogInformation($"Python process started with PID: {process.Id}");

                // Write input to stdin
                try
                {
                    _logger.LogInformation("Writing input to Python process...");
                    await process.StandardInput.WriteAsync(inputJson);
                    process.StandardInput.Close();
                    _logger.LogInformation("Input written successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write to Python process stdin - Broken pipe");
                    throw new Exception($"Failed to send data to Python script: {ex.Message}");
                }

                // Read output and error streams
                string output = string.Empty;
                string error = string.Empty;

                try
                {
                    _logger.LogInformation("Reading Python process output...");
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    
                    // Wait for both with timeout
                    var completedTask = await Task.WhenAny(Task.WhenAll(outputTask, errorTask), Task.Delay(120000));
                    
                    if (completedTask == Task.Delay(120000))
                    {
                        _logger.LogError("Python process timed out after 120 seconds");
                        try { process.Kill(); } catch { }
                        throw new Exception("PDF processing timed out");
                    }
                    
                    output = outputTask.Result;
                    error = errorTask.Result;
                    
                    _logger.LogInformation($"Python output received: {output.Substring(0, Math.Min(500, output.Length))}...");
                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogWarning($"Python stderr: {error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read from Python process");
                    throw new Exception($"Failed to read Python script output: {ex.Message}");
                }

                // Wait for process to exit with timeout
                var exitTimeoutTask = Task.Delay(TimeSpan.FromSeconds(120));
                var exitTask = process.WaitForExitAsync();
                
                var exitCompletedTask = await Task.WhenAny(exitTask, exitTimeoutTask);
                
                if (exitCompletedTask == exitTimeoutTask)
                {
                    _logger.LogError("Python script timed out after 120 seconds");
                    try
                    {
                        process.Kill();
                    }
                    catch { }
                    throw new Exception("PDF processing timed out. The document may be too large or complex.");
                }
                
                await exitTask; // Propagate any exceptions from WaitForExitAsync

                _logger.LogInformation($"Python process exited with code: {process.ExitCode}");
                _logger.LogInformation($"Python script output: {output}");
                _logger.LogInformation($"Python script error: {error}");

                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Python file processor failed with exit code {process.ExitCode}: {error}");
                    throw new Exception($"{document.FileType} processing failed: {error}");
                }

                // Parse result
                var result = JsonSerializer.Deserialize<JsonElement>(output);
                
                if (result.TryGetProperty("error", out var errorElement))
                {
                    throw new Exception($"PDF processing error: {errorElement.GetString()}");
                }

                if (!result.TryGetProperty("success", out var successElement) || !successElement.GetBoolean())
                {
                    throw new Exception("PDF processing returned unsuccessful result");
                }

                int totalPages = result.GetProperty("total_pages").GetInt32();
                int totalChunks = result.GetProperty("total_chunks").GetInt32();
                var chunks = result.GetProperty("chunks");

                _logger.LogInformation($"{document.FileType} processed: {totalPages} sections, {totalChunks} chunks");

                // Database operations with retry logic for transient errors
                int retryCount = 0;
                bool success = false;
                
                while (retryCount < maxRetries && !success)
                {
                    try
                    {
                        // Create a new session for background processing
                        using var newSession = NHibernateHelper.SessionFactory.OpenSession();
                        using var transaction = newSession.BeginTransaction();

                        // Reattach the document to the new session
                        var attachedDocument = await newSession.GetAsync<Document>(document.Id);
                        if (attachedDocument == null)
                        {
                            throw new Exception($"Document {document.Id} not found in database");
                        }

                        // Update document
                        attachedDocument.TotalPages = totalPages;
                        attachedDocument.TotalChunks = totalChunks;
                        attachedDocument.IsProcessed = true;
                        attachedDocument.ProcessedAt = DateTime.UtcNow;

                        // Save chunks to database
                        foreach (var chunkElement in chunks.EnumerateArray())
                        {
                            var chunk = new DocumentChunk
                            {
                                Document = attachedDocument,
                                ChunkText = chunkElement.GetProperty("text").GetString() ?? string.Empty,
                                ChunkIndex = chunkElement.GetProperty("chunk_id").GetInt32(),
                                PageNumber = chunkElement.GetProperty("page_number").GetInt32(),
                                StartCharIndex = 0,
                                EndCharIndex = 0,
                                Embedding = chunkElement.GetProperty("embedding").EnumerateArray()
                                    .Select(e => e.GetSingle())
                                    .ToArray(),
                                CreatedAt = DateTime.UtcNow
                            };

                            await newSession.SaveAsync(chunk);
                        }

                        if (!transaction.IsActive)
                        {
                            throw new InvalidOperationException(
                                "Transaction is no longer active before CommitAsync().");
                        }

                        await transaction.CommitAsync();
                        success = true;
                        
                        _logger.LogInformation($"Successfully processed document: {document.FileName} ({document.FileType}), Sections: {totalPages}, Chunks: {totalChunks}");
                    }
                    catch (Exception ex) when (IsTransientError(ex) && retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        _logger.LogWarning(ex, $"Transient error occurred (attempt {retryCount}/{maxRetries}). Retrying in {retryDelayMs}ms...");
                        await Task.Delay(retryDelayMs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to save processed document to database (attempt {retryCount + 1})");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process document: {document.FileName} ({document.FileType})");
                
                // Retry error status update as well
                int retryCount = 0;
                bool success = false;
                
                while (retryCount < maxRetries && !success)
                {
                    try
                    {
                        // Create a new session for error handling
                        using var errorSession = NHibernateHelper.SessionFactory.OpenSession();
                        
                        document.IsProcessed = false;
                        document.ProcessingError = ex.Message;
                        document.ProcessedAt = DateTime.UtcNow;
                        
                        await errorSession.UpdateAsync(document);
                        await errorSession.FlushAsync();
                        success = true;
                    }
                    catch (Exception updateEx) when (IsTransientError(updateEx) && retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        _logger.LogWarning(updateEx, $"Transient error updating error status (attempt {retryCount}/{maxRetries}). Retrying in {retryDelayMs}ms...");
                        await Task.Delay(retryDelayMs);
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx, "Failed to update document with error status");
                        // Don't throw here - we're already in error handling
                    }
                }
            }
        }
        
        /// <summary>
        /// Determines if an exception is a transient error that can be retried
        /// </summary>
        private bool IsTransientError(Exception ex)
        {
            // Check for Npgsql transient errors
            if (ex is System.Data.Common.DbException dbEx)
            {
                // Npgsql sets IsTransient for connection-related errors
                var isTransientProperty = dbEx.GetType().GetProperty("IsTransient");
                if (isTransientProperty != null)
                {
                    var isTransient = isTransientProperty.GetValue(dbEx) as bool?;
                    if (isTransient == true)
                    {
                        return true;
                    }
                }
            }
            
            // Check inner exceptions
            if (ex.InnerException != null)
            {
                return IsTransientError(ex.InnerException);
            }
            
            // Check for common transient error messages
            var message = ex.Message.ToLower();
            if (message.Contains("exception while reading from stream") ||
                message.Contains("connection") && message.Contains("broken") ||
                message.Contains("timeout") ||
                message.Contains("network") ||
                message.Contains("ioexception"))
            {
                return true;
            }
            
            return false;
        }

        public async Task<List<Document>> GetUserDocumentsAsync(User user)
        {
            return await _session.Query<Document>()
                .Where(d => d.User.Id == user.Id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<Document?> GetDocumentAsync(Guid documentId, User user)
        {
            return await _session.Query<Document>()
                .FirstOrDefaultAsync(d => d.Id == documentId && d.User.Id == user.Id);
        }

        public async Task DeleteDocumentAsync(Document document)
        {
            try
            {
                // Delete physical file
                if (File.Exists(document.FilePath))
                {
                    File.Delete(document.FilePath);
                }

                // Create a new session for deletion to ensure proper cascade
                using var newSession = NHibernateHelper.SessionFactory.OpenSession();
                using var transaction = newSession.BeginTransaction();

                // Load the document with its chunks in the new session
                var attachedDocument = await newSession.GetAsync<Document>(document.Id);
                if (attachedDocument == null)
                {
                    throw new Exception($"Document {document.Id} not found");
                }

                // Delete all chunks first (explicitly handle the relationship)
                var chunks = await newSession.Query<DocumentChunk>()
                    .Where(c => c.Document.Id == document.Id)
                    .ToListAsync();

                foreach (var chunk in chunks)
                {
                    await newSession.DeleteAsync(chunk);
                }

                // Now delete the document
                await newSession.DeleteAsync(attachedDocument);
                
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete document: {document.FileName}");
                throw;
            }
        }

        public async Task<(byte[] FileBytes, string FileName)> GetDocumentFileAsync(Document document)
        {
            if (!File.Exists(document.FilePath))
                throw new Exception("Document file not found");

            byte[] fileBytes = await File.ReadAllBytesAsync(document.FilePath);
            return (fileBytes, document.FileName + document.FileExtension);
        }
    }
}