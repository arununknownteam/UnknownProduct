using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class Follow
    {
        public virtual Guid Id { get; set; }

        public virtual User Follower { get; set; }

        public virtual User Following { get; set; }

        public virtual DateTime CreatedAt { get; set; }
    }

    public class FollowMap : ClassMap<Follow>
    {
        public FollowMap()
        {
            Table("followers");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.Follower)
                .Column("follower_id");

            References(x => x.Following)
                .Column("following_id");

            Map(x => x.CreatedAt);
        }
    }
}