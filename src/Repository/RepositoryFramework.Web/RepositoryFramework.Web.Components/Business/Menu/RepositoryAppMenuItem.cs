namespace RepositoryFramework.Web.Components
{
    internal sealed class RepositoryAppMenuItem : IRepositoryModelAppMenuItem
    {
        public int Index { get; set; }
        /// <summary>
        /// Select icon from https://fonts.google.com/icons?selected=Material+Icons&icon.style=Outlined
        /// </summary>
        public string Icon { get; set; } = "hexagon";
        public string Name { get; set; }
        public List<string> Policies { get; } = new();
        public required Type KeyType { get; init; }
        public required Type ModelType { get; init; }
        public required List<IRepositoryAppMenuSingleItem> SubMenu { get; init; }
        public static RepositoryAppMenuItem CreateDefault(Type modelType, Type keyType)
        {
            return new RepositoryAppMenuItem
            {
                KeyType = keyType,
                ModelType = modelType,
                Name = modelType.Name,
                SubMenu = new List<IRepositoryAppMenuSingleItem>
                {
                    new AppMenuSingleItem
                    {
                        Index = 0,
                        Name= "Query",
                        Uri = $"../../../../Repository/{modelType.Name}/Query",
                        Icon = "list"
                    },
                    new AppMenuSingleItem
                    {
                        Index = 1,
                        Name= "Create",
                        Uri = $"../../../../Repository/{modelType.Name}/Create",
                        Icon = "note_add"
                    }
                },
                Index = int.MaxValue
            };
        }
    }
}
