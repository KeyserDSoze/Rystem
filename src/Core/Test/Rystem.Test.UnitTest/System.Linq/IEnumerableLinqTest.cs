using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Rystem.Test.UnitTest.Linq
{
    public class IEnumerableLinqTest
    {
        [Fact]
        public void CheckResizingOfArrayOfInteger()
        {
            var array = new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            array = array.RemoveWhere(x => x == 9 || x == 8);
            Assert.Equal(8, array.Length);
        }
        [Fact]
        public void CheckResizingOfArrayOfObjects()
        {
            var array = new Something[10]
            {
                new() { Id = 1, Name = "One" },
                new() { Id = 2, Name = "Two" },
                new() { Id = 3, Name = "Three" },
                new() { Id = 4, Name = "Four" },
                new() { Id = 5, Name = "Five" },
                new() { Id = 6, Name = "Six" },
                new() { Id = 7, Name = "Seven" },
                new() { Id = 8, Name = "Eight" },
                new() { Id = 9, Name = "Nine" },
                new() { Id = 10, Name = "Ten" }
            };
            array = array.RemoveWhere(x => x.Id == 9 || x.Id == 8);
            Assert.Equal(8, array.Length);
        }
        [Fact]
        public void CheckResizingOfListOfObjects()
        {
            List<Something> list =
            [
                new() { Id = 1, Name = "One" },
                new() { Id = 2, Name = "Two" },
                new() { Id = 3, Name = "Three" },
                new() { Id = 4, Name = "Four" },
                new() { Id = 5, Name = "Five" },
                new() { Id = 6, Name = "Six" },
                new() { Id = 7, Name = "Seven" },
                new() { Id = 8, Name = "Eight" },
                new() { Id = 9, Name = "Nine" },
                new() { Id = 10, Name = "Ten" }
            ];
            var count = list.RemoveWhere(x => x.Id == 9 || x.Id == 8);
            Assert.Equal(2, count);
            Assert.Equal(8, list.Count);
        }
        [Fact]
        public void CheckResizingOfIListOfObjects()
        {
            IList<Something> list =
            [
                new() { Id = 1, Name = "One" },
                new() { Id = 2, Name = "Two" },
                new() { Id = 3, Name = "Three" },
                new() { Id = 4, Name = "Four" },
                new() { Id = 5, Name = "Five" },
                new() { Id = 6, Name = "Six" },
                new() { Id = 7, Name = "Seven" },
                new() { Id = 8, Name = "Eight" },
                new() { Id = 9, Name = "Nine" },
                new() { Id = 10, Name = "Ten" }
            ];
            var count = list.RemoveWhere(x => x.Id == 9 || x.Id == 8);
            Assert.Equal(2, count);
            Assert.Equal(8, list.Count);
        }

        [Fact]
        public void CheckResizingOfICollectionOfObjects()
        {
            ICollection<Something> collection =
            [
                new() { Id = 1, Name = "One" },
                new() { Id = 2, Name = "Two" },
                new() { Id = 3, Name = "Three" },
                new() { Id = 4, Name = "Four" },
                new() { Id = 5, Name = "Five" },
                new() { Id = 6, Name = "Six" },
                new() { Id = 7, Name = "Seven" },
                new() { Id = 8, Name = "Eight" },
                new() { Id = 9, Name = "Nine" },
                new() { Id = 10, Name = "Ten" }
            ];
            var count = collection.RemoveWhere(x => x.Id == 9 || x.Id == 8);
            Assert.Equal(2, count);
            Assert.Equal(8, collection.Count);
        }
        private sealed class Something
        {
            public required int Id { get; set; }
            public required string Name { get; set; }
        }
    }
}
