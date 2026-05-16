using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class Comment
    {
        public virtual Guid Id { get; set; }

        public virtual User User { get; set; }

        public virtual Post Post { get; set; }

        public virtual string Content { get; set; } = string.Empty;

        public virtual DateTime CreatedAt { get; set; }
    }

    public class CommentMap : ClassMap<Comment>
    {
        public CommentMap()
        {
            Table("comments");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.User)
                .Column("user_id");

            References(x => x.Post)
                .Column("post_id");

            Map(x => x.Content)
                .Not.Nullable();

            Map(x => x.CreatedAt);
        }
    }
}