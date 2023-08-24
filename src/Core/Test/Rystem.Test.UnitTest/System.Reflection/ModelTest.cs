using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class ModelTest
    {
        private sealed class InModel
        {
            public string A { get; set; }
        }
        public class SomethingNew
        {
            public string B { get; set; }
        }
        [Theory]
        [InlineData(true, 6, "name1")]
        [InlineData(false, 5, "name2")]
        public void Test1(bool withParent, int numberOfParameters, string name)
        {
            var modelName = $"MyBestModel{name}";
            var modelBuilder = Model
                .Create(modelName)
                .AddProperty("Primary", typeof(int))
                .AddProperty("Secondary", typeof(bool))
                .AddProperty("Name", typeof(string))
                .AddProperty("Id", typeof(Guid))
                .AddProperty("InModel", typeof(InModel));
            if (withParent)
                modelBuilder
                    .AddParent<SomethingNew>();
            var modelType = modelBuilder.Build();
            Assert.NotNull(modelType);
            var instance = Model.Construct(modelName);
            var properties = modelType.GetProperties();
            Assert.Equal(numberOfParameters, properties.Length);
            instance.Primary = 45;
            instance.InModel = new InModel { A = "Salve" };
            instance.Id = Guid.NewGuid();
            if (withParent)
                instance.B = "Aloa";
            Assert.Equal(45, instance.Primary);
            Assert.Equal("Salve", instance.InModel.A);
            if (withParent)
                Assert.Equal("Aloa", instance.B);
        }
    }
}
