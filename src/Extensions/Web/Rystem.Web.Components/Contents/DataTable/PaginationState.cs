namespace Rystem.Web.Components.Contents.DataTable
{
    public sealed class PaginationState
    {
        public int ItemsPerPage { get; set; } = 10;
        public int CurrentPageIndex { get; set; }
        public int? TotalItemCount { get; set; }
        public int? LastPageIndex => (TotalItemCount - 1) / ItemsPerPage;
    }
}
