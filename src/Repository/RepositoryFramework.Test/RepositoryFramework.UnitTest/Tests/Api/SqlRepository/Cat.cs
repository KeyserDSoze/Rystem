using System;
using System.Collections.Generic;

namespace RepositoryFramework.Test.Models
{
    public class Room
    {
        public string Name { get; set; } = null!;
        public bool IsSpecial { get; set; }
    }
    public class Cat
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? Something { get; set; }
        public IEnumerable<Room> Rooms { get; set; } = new List<Room>();
        public int Paws { get; set; }
    }
}
