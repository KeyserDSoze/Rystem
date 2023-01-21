using System;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class SuperCarBeforeInsertBusiness : IRepositoryBusinessBeforeInsert<SuperCar, Guid>
    {
        public int Priority => 0;
        public async Task<State<SuperCar, Guid>> BeforeInsertAsync(Entity<SuperCar, Guid> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Wheels == 120)
                return 100;
            return true;
        }
    }
}
