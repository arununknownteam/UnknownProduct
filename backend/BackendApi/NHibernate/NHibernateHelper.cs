using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using System.Reflection;
using NHibernate.Tool.hbm2ddl;

namespace BackendApi.NHibernate
{
    public static class NHibernateHelper
    {
        private static ISessionFactory? _sessionFactory;

        public static ISessionFactory SessionFactory =>
            _sessionFactory ??= CreateSessionFactory();

        private static ISessionFactory CreateSessionFactory()
        {
            var config = Fluently.Configure()
                .Database(
                    PostgreSQLConfiguration.Standard
                    .ConnectionString(
                        "Host=localhost;Port=5432;Database=unknowndb;Username=postgres;Password=Arun@123./"
                    )
                    .ShowSql()
                )
                // 🔥 scan all mapping classes in project
                .Mappings(m =>
                    m.FluentMappings.AddFromAssembly(Assembly.GetExecutingAssembly())
                )
                .BuildConfiguration();

            // 🔥 Auto create/update tables
            new SchemaUpdate(config).Execute(false, true);

            return config.BuildSessionFactory();
        }
    }
}
