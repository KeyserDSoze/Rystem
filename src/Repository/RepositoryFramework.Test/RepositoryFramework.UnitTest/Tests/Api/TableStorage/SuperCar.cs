using System;

namespace RepositoryFramework.Test.Models
{
    public class SuperCar
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DateTime Time { get; set; }
        public string Other { get; set; } = null!;
        public int Wheels { get; set; }
    }
}
