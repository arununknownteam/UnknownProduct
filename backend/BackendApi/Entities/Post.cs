using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class Post
    {
        public virtual Guid Id { get; set; }

        public virtual User User { get; set; }

        public virtual string Caption { get; set; } = string.Empty;

        public virtual string MediaUrl { get; set; } = string.Empty;

        public virtual string MediaType { get; set; } = string.Empty;

        public virtual int LikesCount { get; set; }

        public virtual DateTime CreatedAt { get; set; }
    }

    public class PostMap : ClassMap<Post>
    {
        public PostMap()
        {
            Table("posts");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.User)
                .Column("user_id")
                .Not.Nullable();

            Map(x => x.Caption);

            Map(x => x.MediaUrl)
                .Not.Nullable();

            Map(x => x.MediaType)
                .Not.Nullable();

            Map(x => x.LikesCount);

            Map(x => x.CreatedAt);
        }
    }
}