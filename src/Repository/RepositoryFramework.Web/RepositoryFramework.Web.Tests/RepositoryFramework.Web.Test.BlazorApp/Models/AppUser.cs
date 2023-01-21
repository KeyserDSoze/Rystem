using System.Security.Cryptography;

namespace RepositoryFramework.Web.Test.BlazorApp.Models
{
    internal sealed class AppUserDesignMapper : IRepositoryUiMapper<AppUser, int>
    {
        public void Map(IRepositoryPropertyUiHelper<AppUser, int> mapper)
        {
            mapper
            .MapDefault(x => x.Email, "Default email")
            .MapDefault(x => x.InternalAppSettings, 23)
            .SetTextEditor(x => x.Name, 700)
            .MapDefault(x => x, new AppUser
            {
                Email = "default",
                Groups = new(),
                Id = 1,
                Name = "default",
                Password = "default",
                InternalAppSettings = new InternalAppSettings
                {
                    Index = 44,
                    Maps = new(),
                    Options = "default options"
                },
                Settings = new AppSettings
                {
                    Color = "default",
                    Options = "default",
                    Maps = new()
                }
            })
            .MapDefault(x => x.Settings, new AppSettings { Color = "a", Options = "b", Maps = new() })
            .MapChoices(x => x.Groups, async (serviceProvider, entity) =>
            {
                var repository = serviceProvider.GetService<IRepository<AppGroup, string>>();
                List<LabelValueDropdownItem> values = new();
                await foreach (var item in repository!.QueryAsync())
                    values.Add(new LabelValueDropdownItem
                    {
                        Label = item.Value!.Name,
                        Id = item.Value.Name,
                        Value = new Group
                        {
                            Id = item.Value.Id,
                            Name = item.Value.Name,
                        }
                    });
                return values;
            }, x => x.Name)
            .MapChoices(x => x.Settings.Maps, (serviceProvider, entity) =>
            {
                return Task.FromResult(new List<LabelValueDropdownItem> {
                    "X",
                    "Y",
                    "Z",
                    "A" }.AsEnumerable());
            }, x => x)
            .MapChoice(x => x.MainGroup, async (serviceProvider, entity) =>
            {
                var repository = serviceProvider.GetService<IRepository<AppGroup, string>>();
                List<LabelValueDropdownItem> values = new();
                await foreach (var item in repository!.QueryAsync())
                    values.Add(new LabelValueDropdownItem
                    {
                        Label = item.Value!.Name,
                        Id = item.Value.Id,
                        Value = item.Value.Id
                    });
                return values;
            }, x => x);
        }
    }
    public sealed class AppUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public List<Group> Groups { get; set; } = null!;
        public AppSettings Settings { get; init; } = null!;
        public InternalAppSettings InternalAppSettings { get; set; } = null!;
        public List<string> Claims { get; set; } = null!;
        public string MainGroup { get; set; } = null!;
        public string? HashedMainGroup => MainGroup?.ToHash();
    }
    public sealed class Group
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
    public sealed class AppSettings
    {
        public string Color { get; set; } = null!;
        public string Options { get; set; } = null!;
        public List<string> Maps { get; set; } = null!;
    }
    public sealed class InternalAppSettings
    {
        public int Index { get; set; }
        public string Options { get; set; } = null!;
        public List<string> Maps { get; set; } = null!;
    }
}
