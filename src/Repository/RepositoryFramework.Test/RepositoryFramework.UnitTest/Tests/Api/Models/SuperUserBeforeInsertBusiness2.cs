using System;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class SuperUserBeforeInsertBusiness2 : IRepositoryBusinessBeforeInsert<SuperUser, string>
    {
        public int Priority => 1;
        public async Task<State<SuperUser, string>> BeforeInsertAsync(Entity<SuperUser, string> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Port == 120)
                throw new UnauthorizedAccessException("you don't have to stay here.");
            return true;
        }
    }
}
