﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Minimization;
using Xunit;

namespace Rystem.Test.UnitTest.Minimization
{
    public class MinimizationTest
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
        private static readonly List<CsvModel> s_models = new();
        static MinimizationTest()
        {
            for (var i = 0; i < 100; i++)
            {
                s_models.Add(new CsvModel
                {
                    X = i.ToString(),
                    Id = i,
                    B = i.ToString(),
                    E = Guid.NewGuid(),
                    Sol = i % 2 == 0,
                    Inner = new CsvInnerModel { X = i.ToString(), Y = i },
                    Inners = Get(i + 1)
                });
            }
            List<CsvInnerModel> Get(int i)
            {
                List<CsvInnerModel> inners = new();
                for (var x = 0; x < i; x++)
                {
                    inners.Add(new CsvInnerModel
                    {
                        X = x.ToString(),
                        Y = x
                    });
                }
                return inners;
            }
        }
        [Fact]
        public void Test1()
        {
            var value = s_models.ToMinimize('&');
            Assert.True(value.Length < s_models.ToJson().Length);
            var models2 = value.FromMinimization<List<CsvModel>>('&');
            Assert.Equal(s_models.Count, models2.Count);
        }
        [Fact]
        public void Test2()
        {
            var value = s_models.ToMinimize();
            Assert.True(value.Length < s_models.ToJson().Length);
            var models2 = value.FromMinimization<List<CsvModel>>();
            Assert.Equal(s_models.Count, models2.Count);
        }
    }
}
