using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class AnimalBusinessBeforeInsert2 : IRepositoryBusinessBeforeInsert<Animal, AnimalKey>
    {
        public int Priority => 1;
        public async Task<State<Animal, AnimalKey>> BeforeInsertAsync(Entity<Animal, AnimalKey> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            if (entity.Value!.Paws == 120)
                return new State<Animal, AnimalKey>(false, default, default, 100);
            return true;
        }
    }
}
