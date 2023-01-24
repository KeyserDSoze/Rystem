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
        [Fact]
        public void Test1()
        {
            var modelName = "MyBestModel";
            var modelType = Model
                .Create(modelName)
                .AddProperty("Primary", typeof(int))
                .AddProperty("Secondary", typeof(bool))
                .AddProperty("Name", typeof(string))
                .AddProperty("Id", typeof(Guid))
                .AddProperty("InModel", typeof(InModel))
                .Build();
            Assert.NotNull(modelType);
            var instance = Model.Construct(modelName);
            var properties = modelType.GetProperties();
            Assert.Equal(5, properties.Length);
            instance.Primary = 45;
            instance.InModel = new InModel { A = "Salve" };
            instance.Id = Guid.NewGuid();
            Assert.Equal(45, instance.Primary);
            Assert.Equal("Salve", instance.InModel.A);
        }
    }
}
