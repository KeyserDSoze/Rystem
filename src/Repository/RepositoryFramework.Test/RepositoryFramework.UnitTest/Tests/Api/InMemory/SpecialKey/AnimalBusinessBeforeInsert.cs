using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.Test.Models
{
    public class AnimalBusinessBeforeInsert : IRepositoryBusinessBeforeInsert<Animal, AnimalKey>
    {
        public int Priority => 0;
        public async Task<State<Animal, AnimalKey>> BeforeInsertAsync(Entity<Animal, AnimalKey> entity, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return true;
        }
    }
}
