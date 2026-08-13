using BackendApi.DTOs;
using BackendApi.Services;
using BackendApi.NHibernate;
using BackendApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.Json;
using System.Security.Claims;
using NHibernate.Linq;

namespace BackendApi.Controllers
{
    [ApiController]
    [Route("api/documents")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly PDFProcessingService _pdfProcessingService;

        public DocumentController(PDFProcessingService pdfProcessingService)
        {
            _pdfProcessingService = pdfProcessingService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file)
        {
            try
            {
                // Get current user from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }


                // Get the actual user entity
                var userEntity = await NHibernateHelper.SessionFactory.OpenSession()
                    .Query<BackendApi.Entities.User>()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (userEntity == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Process and save document
                var document = await _pdfProcessingService.ProcessAndSaveDocumentAsync(userEntity, file);

                var documentDto = new DocumentDto
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    FileExtension = document.FileExtension,
                    FileType = document.FileType,
                    FileSize = document.FileSize,
                    TotalPages = document.TotalPages,
                    TotalChunks = document.TotalChunks,
                    IsProcessed = document.IsProcessed,
                    UploadedAt = document.UploadedAt,
                    ProcessingError = null
                };

                return Ok(documentDto);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                return BadRequest(new { error = $"Upload failed: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDocuments()
        {
            try
            {
                // Get current user from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Get user entity
                var user = await NHibernateHelper.SessionFactory.OpenSession()
                    .Query<BackendApi.Entities.User>()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Get user's documents
                var documents = await _pdfProcessingService.GetUserDocumentsAsync(user);

                var result = documents.Select(d => new DocumentDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FileExtension = d.FileExtension,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    TotalPages = d.TotalPages,
                    TotalChunks = d.TotalChunks,
                    IsProcessed = d.IsProcessed,
                    UploadedAt = d.UploadedAt,
                    ProcessedAt = d.ProcessedAt,
                    ProcessingError = d.ProcessingError
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                return BadRequest(new { error = $"Operation failed: {ex.Message}" });
            }
        }

        [HttpGet("{documentId}")]
        public async Task<IActionResult> GetDocument(Guid documentId)
        {
            try
            {
                // Get current user from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
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
                var document = await _pdfProcessingService.GetDocumentAsync(documentId, user);
                if (document == null)
                {
                    return NotFound(new { error = "Document not found" });
                }

                var documentDto = new DocumentDto
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    FileExtension = document.FileExtension,
                    FileType = document.FileType,
                    FileSize = document.FileSize,
                    TotalPages = document.TotalPages,
                    TotalChunks = document.TotalChunks,
                    IsProcessed = document.IsProcessed,
                    UploadedAt = document.UploadedAt,
                    ProcessedAt = document.ProcessedAt,
                    ProcessingError = document.ProcessingError
                };

                return Ok(documentDto);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{documentId}")]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            try
            {
                // Get current user from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
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
                var document = await _pdfProcessingService.GetDocumentAsync(documentId, user);
                if (document == null)
                {
                    return NotFound(new { error = "Document not found" });
                }

                // Delete document
                await _pdfProcessingService.DeleteDocumentAsync(document);

                return Ok(new { message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{documentId}/file")]
        public async Task<IActionResult> GetDocumentFile(Guid documentId)
        {
            try
            {
                // Get current user from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
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
                var document = await _pdfProcessingService.GetDocumentAsync(documentId, user);
                if (document == null)
                {
                    return NotFound(new { error = "Document not found" });
                }

                // Get file
                var fileResult = await _pdfProcessingService.GetDocumentFileAsync(document);
                byte[] fileBytes = fileResult.FileBytes;
                string fileName = fileResult.FileName;

                // Determine content type based on file extension
                string contentType = document.FileExtension.ToLowerInvariant() switch
                {
                    ".pdf" => "application/pdf",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".doc" => "application/msword",
                    ".txt" => "text/plain",
                    ".csv" => "text/csv",
                    ".md" or ".markdown" => "text/markdown",
                    ".html" or ".htm" => "text/html",
                    _ => "application/octet-stream"
                };

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}