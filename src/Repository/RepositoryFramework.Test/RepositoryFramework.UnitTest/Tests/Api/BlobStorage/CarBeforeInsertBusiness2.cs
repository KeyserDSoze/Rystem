using System;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class CarBeforeInsertBusiness2 : IRepositoryBusinessBeforeInsert<Car, Guid>
    {
        public int Priority => 1;
        public async Task<State<Car, Guid>> BeforeInsertAsync(Entity<Car, Guid> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Wheels == 120)
                throw new UnauthorizedAccessException("you don't have to stay here.");
            return true;
        }
    }
}
