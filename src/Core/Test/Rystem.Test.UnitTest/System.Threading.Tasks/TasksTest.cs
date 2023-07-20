using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Rystem.Test.UnitTest.Tasks
{
    public class TasksTest
    {
        [Theory]
        [InlineData(45, 12, true)]
        [InlineData(45, 12, false)]
        [InlineData(70, 40, true)]
        [InlineData(82, 40, false)]
        public async Task TestWhenAllWithTimes(int times, int concurrentTasks, bool runEverytimeASlotIsFree)
        {
            var bag = new ConcurrentBag<int>();
            await TaskManager.WhenAll(ExecuteAsync, times, concurrentTasks, runEverytimeASlotIsFree).NoContext();

            Assert.Equal(times, bag.Count);

            async Task ExecuteAsync(int i, CancellationToken cancellationToken)
            {
                await Task.Delay(i * 20, cancellationToken).NoContext();
                bag.Add(i);
            }
        }
        [Theory]
        [InlineData(45, 12, true)]
        [InlineData(45, 12, false)]
        [InlineData(70, 40, true)]
        [InlineData(82, 40, false)]
        public async Task TestWhenAllWithObject(int times, int concurrentTasks, bool runEverytimeASlotIsFree)
        {
            var lists = new List<MyFirstClass>();
            for (var i = 0; i < times; i++)
                lists.Add(new MyFirstClass() { Id = i, Name = i.ToString() });
            var bag = new ConcurrentBag<MyFirstClass>();
            await TaskManager.WhenAll(ExecuteAsync, lists, concurrentTasks, runEverytimeASlotIsFree).NoContext();

            Assert.Equal(times, bag.Count);
            Assert.Contains(bag, x => x.Name == "30");

            async Task ExecuteAsync(MyFirstClass myFirstClass, CancellationToken cancellationToken)
            {
                await Task.Delay(new Random().Next(40), cancellationToken).NoContext();
                bag.Add(myFirstClass);
            }
        }
        [Theory]
        [InlineData(45, 16, 12)]
        [InlineData(45, 17, 12)]
        [InlineData(70, 22, 40)]
        [InlineData(82, 43, 40)]
        public async Task TestWhenAtLeastWithTimes(int times, int atLeast, int concurrentTasks)
        {
            var bag = new ConcurrentBag<int>();
            await TaskManager.WhenAtLeast(ExecuteAsync, times, atLeast, concurrentTasks).NoContext();

            Assert.True(bag.Count < times);
            Assert.True(bag.Count >= atLeast);

            async Task ExecuteAsync(int i, CancellationToken cancellationToken)
            {
                await Task.Delay(i * 20, cancellationToken).NoContext();
                bag.Add(i);
            }
        }
        [Theory]
        [InlineData(45, 16, 12)]
        [InlineData(45, 17, 12)]
        [InlineData(70, 22, 40)]
        [InlineData(82, 43, 40)]
        public async Task TestWhenAtLeastWithObject(int times, int atLeast, int concurrentTasks)
        {
            var lists = new List<MyFirstClass>();
            for (var i = 0; i < times; i++)
                lists.Add(new MyFirstClass() { Id = i, Name = i.ToString() });
            var bag = new ConcurrentBag<MyFirstClass>();
            await TaskManager.WhenAtLeast(ExecuteAsync, lists, atLeast, concurrentTasks).NoContext();

            Assert.True(bag.Count < times);
            Assert.True(bag.Count >= atLeast);

            async Task ExecuteAsync(MyFirstClass myFirstClass, CancellationToken cancellationToken)
            {
                await Task.Delay(new Random().Next(40), cancellationToken).NoContext();
                bag.Add(myFirstClass);
            }
        }
        private sealed class MyFirstClass
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
        }
    }
}
