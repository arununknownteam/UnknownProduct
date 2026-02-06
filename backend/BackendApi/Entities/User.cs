using FluentNHibernate.Mapping;

namespace BackendApi.Entities
{
    public class User
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; } = string.Empty;
        public virtual string Email { get; set; } = string.Empty;
    }
    public class UserMap : ClassMap<User>
    {
        public UserMap()
        {
            Table("Users");

            Id(x => x.Id).GeneratedBy.Assigned();;
            Map(x => x.Name).Not.Nullable();
            Map(x => x.Email).Not.Nullable();
        }
    }
}
