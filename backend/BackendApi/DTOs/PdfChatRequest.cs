namespace BackendApi.DTOs
{
    public class PdfChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public Guid? DocumentId { get; set; }
    }
}