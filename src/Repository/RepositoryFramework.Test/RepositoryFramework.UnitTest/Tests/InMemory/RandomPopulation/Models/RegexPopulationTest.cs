using System;
using System.Collections.Generic;

namespace RepositoryFramework.UnitTest.InMemory.RandomPopulation.Models
{
    public class RegexPopulationTest
    {
        public int Id { get; set; }
        public int A { get; set; }
        public int? AA { get; set; }
        public uint B { get; set; }
        public uint? BB { get; set; }
        public byte C { get; set; }
        public byte? CC { get; set; }
        public sbyte D { get; set; }
        public sbyte? DD { get; set; }
        public short E { get; set; }
        public short? EE { get; set; }
        public ushort F { get; set; }
        public ushort? FF { get; set; }
        public long G { get; set; }
        public long? GG { get; set; }
        public nint H { get; set; }
        public nint? HH { get; set; }
        public nuint L { get; set; }
        public nuint? LL { get; set; }
        public float M { get; set; }
        public float? MM { get; set; }
        public double N { get; set; }
        public double? NN { get; set; }
        public decimal O { get; set; }
        public decimal? OO { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string P { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string? PP { get; set; }
        public bool Q { get; set; }
        public bool? QQ { get; set; }
        public char R { get; set; }
        public char? RR { get; set; }
        public Guid S { get; set; }
        public Guid? SS { get; set; }
        public DateTime T { get; set; }
        public DateTime? TT { get; set; }
        public TimeSpan U { get; set; }
        public TimeSpan? UU { get; set; }
        public DateTimeOffset V { get; set; }
        public DateTimeOffset? VV { get; set; }
        public Range Z { get; set; }
        public Range? ZZ { get; set; }
        public IEnumerable<RegexInnerPopulationTest>? X { get; set; }
        public IDictionary<string, RegexInnerPopulationTest>? Y { get; set; }
        public RegexInnerPopulationTest[]? W { get; set; }
        public ICollection<RegexInnerPopulationTest>? J { get; set; }
    }
    public class RegexInnerPopulationTest
    {
        public string? A { get; set; }
        public int? B { get; set; }
    }
}
