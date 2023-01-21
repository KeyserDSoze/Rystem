using System;
using System.Collections.Generic;

namespace Rystem.Test.UnitTest.System.Population.Random.Models
{
    public class PopulationModelTest
    {
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
        public string P { get; set; } = null!;
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
        public IEnumerable<InnerPopulation>? X { get; set; }
        public IDictionary<string, InnerPopulation>? Y { get; set; }
        public InnerPopulation[]? W { get; set; }
        public ICollection<InnerPopulation>? J { get; set; }
        public IInnerInterface? I { get; set; }
        public IInnerInterfaceForDynamic? II { get; set; }
    }
    public class InnerPopulation
    {
        public string? A { get; set; }
        public int? B { get; set; }
    }
    public interface IInnerInterfaceForDynamic
    {
        string A { get; set; }
        string B { get; set; }
    }
    public interface IInnerInterface
    {
        string A { get; set; }
    }
    public class MyInnerInterfaceImplementation : IInnerInterface
    {
        public string A { get; set; } = null!;
    }
}
