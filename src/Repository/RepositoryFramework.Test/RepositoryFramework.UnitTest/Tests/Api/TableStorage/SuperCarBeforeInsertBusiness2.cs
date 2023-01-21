using System;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class SuperCarBeforeInsertBusiness2 : IRepositoryBusinessBeforeInsert<SuperCar, Guid>
    {
        public int Priority => 1;
        public async Task<State<SuperCar, Guid>> BeforeInsertAsync(Entity<SuperCar, Guid> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Wheels == 120)
                throw new UnauthorizedAccessException("you don't have to stay here.");
            return true;
        }
    }
}
