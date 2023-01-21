using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework.UnitTest.CustomRepository.SpecialKeys.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RepositoryFramework.UnitTest.CustomRepository.SpecialKeys
{
    public class ClassKeyTest
    {
        private static readonly IServiceProvider? s_serviceProvider;
        static ClassKeyTest()
        {
            DiUtility.CreateDependencyInjectionWithConfiguration(out _)
                .AddRepository<ClassAnimal, ClassAnimalKey, ClassAnimalRepository>()
                .Finalize(out s_serviceProvider);
        }
        private readonly IRepository<ClassAnimal, ClassAnimalKey> _repo;
        public ClassKeyTest()
        {
            _repo = s_serviceProvider!.GetService<IRepository<ClassAnimal, ClassAnimalKey>>()!;
        }
        [Fact]
        public async Task TestAsync()
        {
            var id = Guid.NewGuid();
            var animal = await _repo.InsertAsync(new ClassAnimalKey("a", 1, id), new ClassAnimal { Id = 1, Name = "" });
            Assert.True(animal.IsOk);
            animal = await _repo.UpdateAsync(new ClassAnimalKey("a", 1, id), new ClassAnimal { Id = 1, Name = "" });
            Assert.True(animal.IsOk);
            var animal2 = await _repo.GetAsync(new ClassAnimalKey("a", 1, id));
            Assert.True(animal2 != null);
            animal = await _repo.ExistAsync(new ClassAnimalKey("a", 1, id));
            Assert.True(animal.IsOk);
            animal = await _repo.DeleteAsync(new ClassAnimalKey("a", 1, id));
            Assert.True(animal.IsOk);
            animal = await _repo.ExistAsync(new ClassAnimalKey("a", 1, id));
            Assert.False(animal.IsOk);
        }
    }
}
