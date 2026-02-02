using FluentNHibernate.Mapping;
using BackendApi.Entities;

namespace BackendApi.Mappings
{
    public class UserMap : ClassMap<User>
    {
        public UserMap()
        {
            Table("Users");

            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Name).Not.Nullable();
            Map(x => x.Email).Not.Nullable();
        }
    }
}
