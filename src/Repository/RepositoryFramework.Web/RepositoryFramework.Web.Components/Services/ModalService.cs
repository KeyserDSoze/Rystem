using Microsoft.AspNetCore.Components;
using RepositoryFramework.Web.Components.Standard;

namespace RepositoryFramework.Web.Components.Services
{
    public sealed class ModalService
    {
        public List<RenderFragment> Fragments { get; } = new();
        public Action Subscription { get; internal set; }
        public void Show(string title, Func<ValueTask> ok, string? message = null)
        {
            var frag = new RenderFragment(b =>
            {
                b.OpenComponent(1, typeof(Modal));
                b.AddAttribute(2, Constant.Title, title);
                b.AddAttribute(3, Constant.Ok, () => { Cancel(); _ = ok(); });
                b.AddAttribute(4, Constant.Message, message);
                b.AddAttribute(5, Constant.Cancel, Cancel);
                b.CloseComponent();
            });
            Fragments.Add(frag);
            Subscription?.Invoke();
        }
        private void Cancel()
        {
            Fragments.RemoveAt(Fragments.Count - 1);
            Subscription?.Invoke();
        }
    }
}
