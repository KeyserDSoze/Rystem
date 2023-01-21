using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Rystem.Test.UnitTest.Reflection
{
    public class PropertyHandlerTest
    {
        public sealed class BootstrapProperty
        {
            public string Id { get; }
            public string NavigationId { get; }
            public string NavigationSelector { get; }
            public string NavigationTabId { get; }
            public string NavigationTabContentId { get; }
            public BootstrapProperty(BaseProperty baseProperty)
            {
                string navigationPath = baseProperty.NavigationPath;
                var selectorName = navigationPath.ToLower().Replace('.', '_');
                Id = $"id_{selectorName}";
                NavigationId = $"nav_{selectorName}";
                NavigationSelector = $"#{NavigationId}";
                NavigationTabId = $"id_{selectorName}_nav";
                NavigationTabContentId = $"id_{selectorName}_nav_content";
            }
        }
        public sealed class Something
        {
            public string Title { get; set; }
            public List<Something2> Somethings { get; set; }
            public Something2 Something2 { get; set; }
        }
        public sealed class Something2
        {
            public string Title { get; set; }
        }
        [Fact]
        public void Execute()
        {
            var showCase = typeof(Something).ToShowcase(
                IFurtherParameter.Create("Bootstrap", x => new BootstrapProperty(x)),
                IFurtherParameter.Create("Title", x => x.NavigationPath));
            var title = showCase.FlatProperties.First().GetProperty<string>("Title");
            Assert.Equal("Title", title);
            var bootstrap = showCase.FlatProperties.First().GetProperty<BootstrapProperty>("Bootstrap");
            Assert.Equal("id_title_nav", bootstrap.NavigationTabId);
        }
    }
}