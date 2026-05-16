using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class Like
    {
        public virtual Guid Id { get; set; }

        public virtual User User { get; set; }

        public virtual Post Post { get; set; }

        public virtual DateTime CreatedAt { get; set; }
    }

    public class LikeMap : ClassMap<Like>
    {
        public LikeMap()
        {
            Table("likes");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.User)
                .Column("user_id");

            References(x => x.Post)
                .Column("post_id");

            Map(x => x.CreatedAt);
        }
    }
}