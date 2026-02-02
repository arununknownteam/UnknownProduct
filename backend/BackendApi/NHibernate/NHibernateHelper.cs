using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;

namespace BackendApi.NHibernate
{
    public static class NHibernateHelper
    {
        private static ISessionFactory? _sessionFactory;

        public static ISessionFactory SessionFactory =>
            _sessionFactory ??= Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2012
                    .ConnectionString("Server=localhost;Database=TestDb;Trusted_Connection=True;TrustServerCertificate=True;"))
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<NHibernateHelper>())
                .BuildSessionFactory();
    }
}
