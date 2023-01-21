using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.UnitTest.AllMethods.Models
{
    internal sealed class AnimalBusiness : IRepositoryBusinessBeforeInsert<Animal, long>, IRepositoryBusinessAfterInsert<Animal, long>
    {
        public int Priority => 0;
        public static int After;
        public Task<State<Animal, long>> AfterInsertAsync(State<Animal, long> state, Entity<Animal, long> entity, CancellationToken cancellationToken = default)
        {
            After++;
            return Task.FromResult(state);
        }

        public static int Before;
        public Task<State<Animal, long>> BeforeInsertAsync(Entity<Animal, long> entity, CancellationToken cancellationToken = default)
        {
            Before++;
            return Task.FromResult(State.Ok(entity));
        }
    }
}
