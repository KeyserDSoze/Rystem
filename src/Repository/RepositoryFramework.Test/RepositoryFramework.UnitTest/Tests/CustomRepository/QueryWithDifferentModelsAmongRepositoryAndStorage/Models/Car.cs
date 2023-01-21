namespace RepositoryFramework.UnitTest.QueryWithDifferentModelsAmongRepositoryAndStorage.Models
{
    public class Car
    {
        public int Id { get; set; }
        public int Id2 { get; set; }
        public string? Plate { get; set; }
        public int NumberOfWheels { get; set; }
        public Driver? Driver { get; set; }
        public string? O { get; set; }
    }
    public class Driver
    {
        public string? Name { get; set; }
    }
}
