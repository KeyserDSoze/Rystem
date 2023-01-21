using RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal;

namespace RepositoryFramework.Test.Domain
{
    public class UserBeforeInsertBusiness2 : IRepositoryBusinessBeforeInsert<User, int>
    {
        public int Priority => 1;
        public async Task<State<User, int>> BeforeInsertAsync(Entity<User, int> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Key == 120)
                throw new UnauthorizedAccessException("you don't have to stay here.");
            return true;
        }
    }
}
