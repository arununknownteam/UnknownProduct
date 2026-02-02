namespace BackendApi.Entities
{
    public class User
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; } = string.Empty;
        public virtual string Email { get; set; } = string.Empty;
    }
}
