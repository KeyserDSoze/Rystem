using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Csv;
using System.Text.Json;
using Xunit;
using Xunit.Sdk;

namespace Rystem.Test.UnitTest.Csv
{
    public class CsvTest
    {
        internal sealed class CsvModel
        {
            public string? X { get; set; }
            public int Id { get; set; }
            public string? B { get; set; }
            public Guid E { get; set; }
            public bool Sol { get; set; }
            public List<CsvInnerModel> Inners { get; set; }
            public CsvInnerModel Inner { get; set; }
        }
        internal sealed class CsvInnerModel
        {
            public string X { get; set; }
            public int Y { get; set; }
        }
        public sealed class AppUser
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public List<Group> Groups { get; set; }
            public AppSettings Settings { get; init; }
            public InternalAppSettings InternalAppSettings { get; set; }
            public List<string> Claims { get; set; }
            public string MainGroup { get; set; }
            public string? HashedMainGroup => MainGroup?.ToHash();
        }
        public sealed class Group
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
        public sealed class AppSettings
        {
            public string Color { get; set; }
            public string Options { get; set; }
            public List<string> Maps { get; set; }
        }
        public sealed class InternalAppSettings
        {
            public int Index { get; set; }
            public string Options { get; set; }
            public List<string> Maps { get; set; }
        }
        private static readonly List<CsvModel> _models = new();
        private static readonly List<AppUser> _users = new();
        static CsvTest()
        {
            for (int i = 0; i < 100; i++)
            {
                _models.Add(new CsvModel
                {
                    X = i.ToString(),
                    Id = i,
                    B = i.ToString(),
                    E = Guid.NewGuid(),
                    Sol = i % 2 == 0,
                    Inner = new CsvInnerModel { X = i.ToString(), Y = i },
                    Inners = Get(i + 1)
                });
                _users.Add(new AppUser
                {
                    Email = $"email{i}",
                    Groups = new string[i + 1].Select(x => new Group { Id = Guid.NewGuid().ToString(), Name = Guid.NewGuid().ToString() }).ToList(),
                    Claims = new string[i + 1].Select(x => Guid.NewGuid().ToString()).ToList(),
                    Id = i,
                    MainGroup = Guid.NewGuid().ToString(),
                    InternalAppSettings = new InternalAppSettings
                    {
                        Index = i,
                        Maps = new string[i + 1].Select(x => Guid.NewGuid().ToString()).ToList(),
                        Options = i.ToString()
                    },
                    Name = $"name_{i}",
                    Password = i.ToString(),
                    Settings = new AppSettings
                    {
                        Options = i.ToString(),
                        Maps = new string[i + 1].Select(x => Guid.NewGuid().ToString()).ToList(),
                        Color = $"color_{i}"
                    }
                });
            }
            List<CsvInnerModel> Get(int i)
            {
                List<CsvInnerModel> inners = new();
                for (int x = 0; x < i; x++)
                {
                    inners.Add(new CsvInnerModel
                    {
                        X = $"d,{x}ccc,",
                        Y = x
                    });
                }
                return inners;
            }


        }
        [Fact]
        public void Test()
        {
            var value = _models.ToCsv();
            Assert.NotEmpty(value);
            value = _users.ToCsv();
            Assert.NotEmpty(value);
        }
    }
}