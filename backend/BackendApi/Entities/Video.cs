using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class Video
    {
        public virtual Guid Id { get; set; }

        public virtual User User { get; set; }

        public virtual string Title { get; set; } = string.Empty;

        public virtual string Description { get; set; } = string.Empty;

        public virtual string VideoUrl { get; set; } = string.Empty;

        public virtual string ThumbnailUrl { get; set; } = string.Empty;

        public virtual long ViewsCount { get; set; }

        public virtual DateTime CreatedAt { get; set; }
    }

    public class VideoMap : ClassMap<Video>
    {
        public VideoMap()
        {
            Table("videos");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.User)
                .Column("user_id")
                .Not.Nullable();

            Map(x => x.Title)
                .Not.Nullable();

            Map(x => x.Description);

            Map(x => x.VideoUrl)
                .Not.Nullable();

            Map(x => x.ThumbnailUrl);

            Map(x => x.ViewsCount);

            Map(x => x.CreatedAt);
        }
    }
}