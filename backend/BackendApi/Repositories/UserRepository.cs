using BackendApi.Entities;
using BackendApi.NHibernate;

namespace BackendApi.Repositories
{
    public class UserRepository
    {
        public IList<User> GetAll()
        {
            using var session = NHibernateHelper.SessionFactory.OpenSession();
            return session.Query<User>().ToList();
        }

        public void Add(User user)
        {
            using var session = NHibernateHelper.SessionFactory.OpenSession();
            using var tx = session.BeginTransaction();
            session.Save(user);
            tx.Commit();
        }
    }
}
