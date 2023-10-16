using System.Collections;
using System.Reflection;
using global::Microsoft.AspNetCore.Components;
using Radzen;
using RepositoryFramework.Web.Components.Business.Language;
using RepositoryFramework.Web.Components.Services;

namespace RepositoryFramework.Web.Components.Standard
{
    public partial class Visualizer : ComponentBase
    {
        [Parameter]
        public IEnumerable? Entity { get; set; }

        [Inject]
        public ICopyService Copy { get; set; }

        [Inject]
        public DialogService DialogService { get; set; }

        [Inject]
        public ILocalizationHandler LocalizationHandler { get; set; }
        private TypeShowcase TypeShowcase { get; set; } = null !;

        private bool _isPrimitive;
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            var enumerator = Entity?.GetEnumerator();
            if (enumerator?.MoveNext() == true)
            {
                var type = enumerator.Current.GetType();
                _isPrimitive = type.IsPrimitive();
                if (!_isPrimitive)
                    TypeShowcase = type.ToShowcase();
            }
        }

        private async Task ShowMoreValuesAsync(object? entity, BaseProperty property, int nextIndex)
        {
            var retrieve = Try.WithDefaultOnCatch(() => property.Value(entity, null));
            if (retrieve.Exception == null && retrieve.Entity is IEnumerable enumerable && enumerable.GetEnumerator().MoveNext())
            {
                _ = await DialogService.OpenAsync<Visualizer>(property.Self.Name, new Dictionary<string, object> { { Constant.Entity, retrieve.Entity }, }, new DialogOptions { Width = Constant.DialogWidth });
            }
        }
    }
}
