using FluentNHibernate.Mapping;
using BackendApi.Entities;

namespace BackendApi.Entities
{
    public class User
    {
        public virtual Guid Id { get; set; }

        public virtual string UserName { get; set; } = string.Empty;

        public virtual string Email { get; set; } = string.Empty;

        public virtual string PasswordHash { get; set; } = string.Empty;

        public virtual string Bio { get; set; } = string.Empty;

        public virtual string ProfileImageUrl { get; set; } = string.Empty;

        public virtual bool IsPrivate { get; set; }
        public virtual bool IsPublic { get; set; }

        public virtual DateTime CreatedAt { get; set; }

        public virtual IList<Post> Posts { get; set; } = new List<Post>();

        public virtual IList<Video> Videos { get; set; } = new List<Video>();
    }

    public class UserMap : ClassMap<User>
    {
        public UserMap()
        {
            Table("users");

            Id(x => x.Id)
                .GeneratedBy.GuidComb();

            Map(x => x.UserName)
                .Not.Nullable()
                .Length(50);

            Map(x => x.Email)
                .Not.Nullable()
                .Unique();

            Map(x => x.PasswordHash)
                .Not.Nullable();

            Map(x => x.Bio);

            Map(x => x.ProfileImageUrl);

            Map(x => x.IsPrivate)
                .Not.Nullable();
            Map(x => x.IsPublic)
                .Not.Nullable();
            Map(x => x.CreatedAt)
                .Not.Nullable();

            HasMany(x => x.Posts)
                .KeyColumn("user_id")
                .Cascade.All()
                .Inverse();

            HasMany(x => x.Videos)
                .KeyColumn("user_id")
                .Cascade.All()
                .Inverse();
        }
    }
}