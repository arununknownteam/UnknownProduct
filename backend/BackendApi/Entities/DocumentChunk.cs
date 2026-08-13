using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class DocumentChunk
    {
        public virtual Guid Id { get; set; }
        public virtual Document Document { get; set; }
        public virtual string ChunkText { get; set; } = string.Empty;
        public virtual int ChunkIndex { get; set; }
        public virtual int PageNumber { get; set; }
        public virtual int StartCharIndex { get; set; }
        public virtual int EndCharIndex { get; set; }
        public virtual float[] Embedding { get; set; } = Array.Empty<float>();
        public virtual DateTime CreatedAt { get; set; }
    }

    public class DocumentChunkMap : ClassMap<DocumentChunk>
    {
        public DocumentChunkMap()
        {
            Table("document_chunks");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.Document)
                .Column("document_id")
                .Not.Nullable();

            Map(x => x.ChunkText)
                .Column("chunk_text")
                .Not.Nullable()
                .Length(2000);

            Map(x => x.ChunkIndex)
                .Column("chunk_index")
                .Not.Nullable();

            Map(x => x.PageNumber)
                .Column("page_number")
                .Not.Nullable();

            Map(x => x.StartCharIndex)
                .Column("start_char_index")
                .Not.Nullable();

            Map(x => x.EndCharIndex)
                .Column("end_char_index")
                .Not.Nullable();

            // Store embedding as a text-serialized float array using custom VectorType.
            // This avoids dependency on the pgvector extension and Npgsql vector type handler.
            Map(x => x.Embedding)
                .Column("embedding")
                .CustomType<BackendApi.NHibernate.VectorType>()
                .CustomSqlType("text")
                .Length(2000);

            Map(x => x.CreatedAt)
                .Column("created_at")
                .Not.Nullable();
        }
    }
}