namespace RepositoryFramework.Web.Test.BlazorApp.Models
{
    public class Weather
    {
        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }

        public DateOnly Date { get; set; }
        public int Id { get; set; }
    }
}
