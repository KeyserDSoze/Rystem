using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Web.Components
{
    public abstract class RepositoryBase<T, TKey> : ComponentBase
        where TKey : notnull
    {
        [Inject]
        public IServiceProvider? ServiceProvider { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;
        protected IRepository<T, TKey>? Repository { get; private set; }
        protected IQuery<T, TKey>? Query { get; private set; }
        protected ICommand<T, TKey>? Command { get; private set; }
        private protected TypeShowcase TypeShowcase { get; set; } = null!;
        private protected bool CanEdit { get; set; }
        protected override void OnInitialized()
        {
            TypeShowcase = typeof(Entity<T, TKey>)
                .ToShowcase(IFurtherParameter.Create(Constant.FurtherProperty, x => new FurtherProperty(x)));
            base.OnInitialized();
        }
        protected override void OnParametersSet()
        {
            Repository = ServiceProvider?.GetService<IRepository<T, TKey>>();
            if (Repository != null)
            {
                Query = Repository;
                Command = Repository;
            }
            else
            {
                Query = ServiceProvider?.GetService<IQuery<T, TKey>>();
                Command = ServiceProvider?.GetService<ICommand<T, TKey>>();
            }
            CanEdit = Command != null;
            base.OnParametersSet();
        }
    }
}
