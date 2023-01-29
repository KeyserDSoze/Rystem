namespace Rystem.Web.Components.Services
{
    public interface IDialogService
    {
        void Show(string title, Func<ValueTask> ok, string? message = null);
        void Cancel();
    }
}
