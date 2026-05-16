using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class Story
    {
        public virtual Guid Id { get; set; }

        public virtual User User { get; set; }

        public virtual string MediaUrl { get; set; } = string.Empty;

        public virtual DateTime ExpiresAt { get; set; }

        public virtual DateTime CreatedAt { get; set; }
    }

    public class StoryMap : ClassMap<Story>
    {
        public StoryMap()
        {
            Table("stories");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.User)
                .Column("user_id");

            Map(x => x.MediaUrl);

            Map(x => x.ExpiresAt);

            Map(x => x.CreatedAt);
        }
    }
}