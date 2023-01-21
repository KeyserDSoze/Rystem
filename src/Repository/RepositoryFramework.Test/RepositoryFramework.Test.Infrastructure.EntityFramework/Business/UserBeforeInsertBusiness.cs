using RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal;

namespace RepositoryFramework.Test.Domain
{
    public class UserBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<User, int>
    {
        public int Priority => 0;
        public async Task<State<User, int>> BeforeInsertAsync(Entity<User, int> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Key == 120)
                return 100;
            return true;
        }
    }
}
