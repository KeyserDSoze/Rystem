namespace RepositoryFramework.Test.Domain
{
    public record AppUserKey(int Id) : IKey
    {
        public static implicit operator int(AppUserKey app)
           => app.Id;
        public static implicit operator AppUserKey(int app)
            => new(app);
        public override string ToString()
            => Id.ToString();
        public string AsString()
            => Id.ToString();
    }
}
