using RepositoryFramework.Test.Infrastructure.EntityFramework;

namespace RepositoryFramework.Test.Domain
{
    public class MappingUserBeforeInsertBusiness2 : IRepositoryBusinessBeforeInsert<MappingUser, int>
    {
        public int Priority => 1;
        public async Task<State<MappingUser, int>> BeforeInsertAsync(Entity<MappingUser, int> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Key == 120)
                throw new UnauthorizedAccessException("you don't have to stay here.");
            return true;
        }
    }
}
