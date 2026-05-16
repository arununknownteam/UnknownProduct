using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class Notification
    {
        public virtual Guid Id { get; set; }

        public virtual User User { get; set; }

        public virtual string Type { get; set; } = string.Empty;

        public virtual bool IsRead { get; set; }

        public virtual DateTime CreatedAt { get; set; }
    }

    public class NotificationMap : ClassMap<Notification>
    {
        public NotificationMap()
        {
            Table("notifications");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            References(x => x.User)
                .Column("user_id");

            Map(x => x.Type);

            Map(x => x.IsRead);

            Map(x => x.CreatedAt);
        }
    }
}