namespace BackendApi.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalChunks { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string ProcessingError { get; set; }
    }
}