using BackendApi.Entities;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
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
            try
            {
                AppLogger.Info(
                    "NHibernate initialization started."
                );

                var config = Fluently.Configure()

                    .Database(
                        PostgreSQLConfiguration.Standard
                        .Dialect<global::NHibernate.Dialect.PostgreSQL83Dialect>()
                        .ConnectionString(
                            "Host=localhost;Port=5432;Database=unknowndb;Username=postgres;Password=Arun@123./"
                        )
                        .ShowSql()
                    )

                    .Mappings(m =>
                    {
                        m.FluentMappings
                        .AddFromAssembly(
                            System.Reflection.Assembly.GetExecutingAssembly()
                        );
                    })

                    .ExposeConfiguration(cfg =>
                    {
                        cfg.SetProperty(
                            global::NHibernate.Cfg.Environment.ShowSql,
                            "true"
                        );

                        cfg.SetProperty(
                            global::NHibernate.Cfg.Environment.FormatSql,
                            "true"
                        );

                        cfg.SetProperty(
                            global::NHibernate.Cfg.Environment.UseSqlComments,
                            "true"
                        );
                    })

                    .BuildConfiguration();
                    
                    foreach (var mapping in config.ClassMappings)
                    {
                        AppLogger.Info(
                            $"Loaded Mapping: {mapping.EntityName}"
                        );
                    }
                AppLogger.Info(
                    "Schema export started."
                );

               var schemaUpdate =
                new SchemaUpdate(config);

            schemaUpdate.Execute(
                sql =>
                {
                    AppLogger.Sql(sql);
                },
                true
            );
                AppLogger.Info(
                    "Schema export completed."
                );

                var sessionFactory =
                    config.BuildSessionFactory();

                AppLogger.Info(
                    "SessionFactory created successfully."
                );

                return sessionFactory;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex);

                throw;
            }
        }
    }
}