using RepositoryFramework.Test.Infrastructure.EntityFramework;

namespace RepositoryFramework.Test.Domain
{
    public class MappingUserBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<MappingUser, int>
    {
        public int Priority => 0;
        public async Task<State<MappingUser, int>> BeforeInsertAsync(Entity<MappingUser, int> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Key == 120)
                return 100;
            return true;
        }
    }
}
