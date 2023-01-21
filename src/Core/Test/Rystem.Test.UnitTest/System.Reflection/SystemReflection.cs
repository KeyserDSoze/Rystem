using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class SystemReflection
    {
        public interface IMogalo
        {
            int X { get; }
            int Y { get; }
            int Z { get; set; }
            int U { get; init; }
            int W { get; init; }
        }
        public class MySuperClass : IMogalo
        {
            public int X { get; }
            public int Y { get; }
            public int Z { get; set; }
            public int U { get; init; }
            public int W { get; init; }
            public MySuperClass(int x)
            {
                X = x;
            }
            public MySuperClass(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
        [Fact]
        public async Task StaticTest()
        {
            var result = await Generics
                .WithStatic<SystemReflection>(nameof(StaticCreateAsync), typeof(int))
                .InvokeAsync(3);
            Assert.Equal(3, result);
            var result2 = Generics
                .WithStatic<SystemReflection>(nameof(StaticCreate), typeof(int))
                .Invoke(3);
            Assert.Equal(3, result2);
            var result3 = Generics
                .WithStatic<SystemReflection>(nameof(StaticCreate), typeof(int))
                .Invoke<int>(3);
            Assert.Equal(3, result3);
        }
        [Fact]
        public async Task InstanceTest()
        {
            var result = await Generics
                .With<SystemReflection>(nameof(CreateAsync), typeof(int))
                .InvokeAsync(this, 3);
            Assert.Equal(3, result);
            var result2 = Generics
                .With<SystemReflection>(nameof(Create), typeof(int))
                .Invoke(this, 3);
            Assert.Equal(3, result2);
            var result3 = Generics
                .With<SystemReflection>(nameof(Create), typeof(int))
                .Invoke<int>(this, 3);
            Assert.Equal(3, result3);
        }
        public static async Task<T> StaticCreateAsync<T>(T x)
        {
            await Task.Delay(0);
            return x;
        }
        public static T StaticCreate<T>(T x)
        {
            return x;
        }
        public async Task<T> CreateAsync<T>(T x)
        {
            await Task.Delay(0);
            return x;
        }
        public T Create<T>(T x)
        {
            return x;
        }
        [Fact]
        public void CreateWithDynamicConstructor()
        {
            var superClass = (MySuperClass)typeof(MySuperClass).ConstructWithBestDynamicFit(3, 4, 5, 6);
            Assert.Equal(3, superClass!.X);
            Assert.Equal(4, superClass.Y);
            Assert.Equal(5, superClass.Z);
            Assert.Equal(6, superClass.U);
            Assert.Equal(0, superClass.W);
            var superClass2 = Constructor.InvokeWithBestDynamicFit<MySuperClass>(5, 6, 7, 8);
            Assert.Equal(5, superClass2!.X);
            Assert.Equal(6, superClass2.Y);
            Assert.Equal(7, superClass2.Z);
            Assert.Equal(8, superClass2.U);
            Assert.Equal(0, superClass2.W);
            var mogalo = Constructor.InvokeWithBestDynamicFit<IMogalo>(9, 10, 11, 21);
            Assert.NotNull(mogalo);
            Assert.Equal(9, mogalo!.X);
            Assert.Equal(10, mogalo.Y);
            Assert.Equal(11, mogalo.Z);
            Assert.Equal(21, mogalo.U);
            Assert.Equal(0, mogalo.W);
            var mogalo2 = (IMogalo)typeof(IMogalo).ConstructWithBestDynamicFit(9, 10, 11, 21)!;
            Assert.NotNull(mogalo2);
            Assert.Equal(9, mogalo2!.X);
            Assert.Equal(10, mogalo2.Y);
            Assert.Equal(11, mogalo2.Z);
            Assert.Equal(21, mogalo2.U);
            Assert.Equal(0, mogalo2.W);
        }
    }
}