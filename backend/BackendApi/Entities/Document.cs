using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class Document
    {
        public virtual Guid Id { get; set; }
        public virtual User User { get; set; }
        public virtual string FileName { get; set; } = string.Empty;
        public virtual string FilePath { get; set; } = string.Empty;
        public virtual string FileExtension { get; set; } = string.Empty;
        public virtual string FileType { get; set; } = string.Empty;
        public virtual long FileSize { get; set; }
        public virtual int TotalPages { get; set; }
        public virtual int TotalChunks { get; set; }
        public virtual bool IsProcessed { get; set; }
        public virtual DateTime UploadedAt { get; set; }
        public virtual DateTime? ProcessedAt { get; set; }
        public virtual string? ProcessingError { get; set; }
        public virtual IList<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }

    public class DocumentMap : ClassMap<Document>
    {
        public DocumentMap()
        {
            Table("documents");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.User)
                .Column("user_id")
                .Not.Nullable();

            Map(x => x.FileName)
                .Not.Nullable()
                .Length(255);

            Map(x => x.FilePath)
                .Not.Nullable()
                .Length(500);

            Map(x => x.FileExtension)
                .Column("file_extension")
                .Length(10);

            Map(x => x.FileType)
                .Column("file_type")
                .Length(50);

            Map(x => x.FileSize)
                .Not.Nullable();

            Map(x => x.TotalPages)
                .Not.Nullable();

            Map(x => x.TotalChunks)
                .Not.Nullable();

            Map(x => x.IsProcessed)
                .Not.Nullable();

            Map(x => x.UploadedAt)
                .Not.Nullable();

            Map(x => x.ProcessedAt);

            Map(x => x.ProcessingError)
                .Length(1000);

            HasMany(x => x.Chunks)
                .KeyColumn("document_id")
                .Cascade.AllDeleteOrphan()
                .Inverse();
        }
    }
}