namespace RepositoryFramework.Test.Domain
{
    public class AppUserBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<AppUser, AppUserKey>
    {
        public int Priority => 0;
        public async Task<State<AppUser, AppUserKey>> BeforeInsertAsync(Entity<AppUser, AppUserKey> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Key!.Id == 120)
                return 100;
            return true;
        }
    }
}
