using System;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class CarBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<Car, Guid>
    {
        public int Priority => 0;
        public async Task<State<Car, Guid>> BeforeInsertAsync(Entity<Car, Guid> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Wheels == 120)
                return 100;
            return true;
        }
    }
}
