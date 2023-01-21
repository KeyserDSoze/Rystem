namespace RepositoryFramework.UnitTest.QueryWithDifferentModelsAmongRepositoryAndStorage.Models
{
    public class Auto
    {
        public int Identificativo { get; set; }
        public int Identificativo2 { get; set; }
        public string? Targa { get; set; }
        public Guidatore? Guidatore { get; set; }
        public int NumeroRuote { get; set; }
        public string? O { get; set; }
    }
    public class Guidatore
    {
        public string? Nome { get; set; }
    }
}
