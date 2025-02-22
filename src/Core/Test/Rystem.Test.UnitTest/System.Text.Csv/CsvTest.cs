using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Csv;
using System.Text.Json;
using Newtonsoft.Json.Linq;
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
            public string? Id { get; set; }
            public string? Name { get; set; }
        }
        public sealed class AppSettings
        {
            public string? Color { get; set; }
            public string? Options { get; set; }
            public List<string>? Maps { get; set; }
        }
        public sealed class InternalAppSettings
        {
            public int Index { get; set; }
            public string? Options { get; set; }
            public List<string> Maps { get; set; }
        }
        private static readonly List<CsvModel> s_models = [];
        private static readonly List<AppUser> s_users = [];
        static CsvTest()
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
                s_users.Add(new AppUser
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
            var value = s_models.ToCsv();
            Assert.NotEmpty(value);
            var dividedByLines = value.Split('\n');
            Assert.Equal("X,Id,B,E,Sol,Inners[0].X,Inners[1].X,Inners[2].X,Inners[3].X,Inners[4].X,Inners[5].X,Inners[6].X,Inners[7].X,Inners[8].X,Inners[9].X,Inners[10].X,Inners[11].X,Inners[12].X,Inners[13].X,Inners[14].X,Inners[15].X,Inners[16].X,Inners[17].X,Inners[18].X,Inners[19].X,Inners[20].X,Inners[21].X,Inners[22].X,Inners[23].X,Inners[24].X,Inners[25].X,Inners[26].X,Inners[27].X,Inners[28].X,Inners[29].X,Inners[30].X,Inners[31].X,Inners[32].X,Inners[33].X,Inners[34].X,Inners[35].X,Inners[36].X,Inners[37].X,Inners[38].X,Inners[39].X,Inners[40].X,Inners[41].X,Inners[42].X,Inners[43].X,Inners[44].X,Inners[45].X,Inners[46].X,Inners[47].X,Inners[48].X,Inners[49].X,Inners[50].X,Inners[51].X,Inners[52].X,Inners[53].X,Inners[54].X,Inners[55].X,Inners[56].X,Inners[57].X,Inners[58].X,Inners[59].X,Inners[60].X,Inners[61].X,Inners[62].X,Inners[63].X,Inners[64].X,Inners[65].X,Inners[66].X,Inners[67].X,Inners[68].X,Inners[69].X,Inners[70].X,Inners[71].X,Inners[72].X,Inners[73].X,Inners[74].X,Inners[75].X,Inners[76].X,Inners[77].X,Inners[78].X,Inners[79].X,Inners[80].X,Inners[81].X,Inners[82].X,Inners[83].X,Inners[84].X,Inners[85].X,Inners[86].X,Inners[87].X,Inners[88].X,Inners[89].X,Inners[90].X,Inners[91].X,Inners[92].X,Inners[93].X,Inners[94].X,Inners[95].X,Inners[96].X,Inners[97].X,Inners[98].X,Inners[99].X,Inners[0].Y,Inners[1].Y,Inners[2].Y,Inners[3].Y,Inners[4].Y,Inners[5].Y,Inners[6].Y,Inners[7].Y,Inners[8].Y,Inners[9].Y,Inners[10].Y,Inners[11].Y,Inners[12].Y,Inners[13].Y,Inners[14].Y,Inners[15].Y,Inners[16].Y,Inners[17].Y,Inners[18].Y,Inners[19].Y,Inners[20].Y,Inners[21].Y,Inners[22].Y,Inners[23].Y,Inners[24].Y,Inners[25].Y,Inners[26].Y,Inners[27].Y,Inners[28].Y,Inners[29].Y,Inners[30].Y,Inners[31].Y,Inners[32].Y,Inners[33].Y,Inners[34].Y,Inners[35].Y,Inners[36].Y,Inners[37].Y,Inners[38].Y,Inners[39].Y,Inners[40].Y,Inners[41].Y,Inners[42].Y,Inners[43].Y,Inners[44].Y,Inners[45].Y,Inners[46].Y,Inners[47].Y,Inners[48].Y,Inners[49].Y,Inners[50].Y,Inners[51].Y,Inners[52].Y,Inners[53].Y,Inners[54].Y,Inners[55].Y,Inners[56].Y,Inners[57].Y,Inners[58].Y,Inners[59].Y,Inners[60].Y,Inners[61].Y,Inners[62].Y,Inners[63].Y,Inners[64].Y,Inners[65].Y,Inners[66].Y,Inners[67].Y,Inners[68].Y,Inners[69].Y,Inners[70].Y,Inners[71].Y,Inners[72].Y,Inners[73].Y,Inners[74].Y,Inners[75].Y,Inners[76].Y,Inners[77].Y,Inners[78].Y,Inners[79].Y,Inners[80].Y,Inners[81].Y,Inners[82].Y,Inners[83].Y,Inners[84].Y,Inners[85].Y,Inners[86].Y,Inners[87].Y,Inners[88].Y,Inners[89].Y,Inners[90].Y,Inners[91].Y,Inners[92].Y,Inners[93].Y,Inners[94].Y,Inners[95].Y,Inners[96].Y,Inners[97].Y,Inners[98].Y,Inners[99].Y,Inner.X,Inner.Y", dividedByLines.First());
            value = s_users.ToCsv();
            dividedByLines = value.Split('\n');
            Assert.Equal("Id,Name,Email,Password,MainGroup,HashedMainGroup,Groups[0].Id,Groups[1].Id,Groups[2].Id,Groups[3].Id,Groups[4].Id,Groups[5].Id,Groups[6].Id,Groups[7].Id,Groups[8].Id,Groups[9].Id,Groups[10].Id,Groups[11].Id,Groups[12].Id,Groups[13].Id,Groups[14].Id,Groups[15].Id,Groups[16].Id,Groups[17].Id,Groups[18].Id,Groups[19].Id,Groups[20].Id,Groups[21].Id,Groups[22].Id,Groups[23].Id,Groups[24].Id,Groups[25].Id,Groups[26].Id,Groups[27].Id,Groups[28].Id,Groups[29].Id,Groups[30].Id,Groups[31].Id,Groups[32].Id,Groups[33].Id,Groups[34].Id,Groups[35].Id,Groups[36].Id,Groups[37].Id,Groups[38].Id,Groups[39].Id,Groups[40].Id,Groups[41].Id,Groups[42].Id,Groups[43].Id,Groups[44].Id,Groups[45].Id,Groups[46].Id,Groups[47].Id,Groups[48].Id,Groups[49].Id,Groups[50].Id,Groups[51].Id,Groups[52].Id,Groups[53].Id,Groups[54].Id,Groups[55].Id,Groups[56].Id,Groups[57].Id,Groups[58].Id,Groups[59].Id,Groups[60].Id,Groups[61].Id,Groups[62].Id,Groups[63].Id,Groups[64].Id,Groups[65].Id,Groups[66].Id,Groups[67].Id,Groups[68].Id,Groups[69].Id,Groups[70].Id,Groups[71].Id,Groups[72].Id,Groups[73].Id,Groups[74].Id,Groups[75].Id,Groups[76].Id,Groups[77].Id,Groups[78].Id,Groups[79].Id,Groups[80].Id,Groups[81].Id,Groups[82].Id,Groups[83].Id,Groups[84].Id,Groups[85].Id,Groups[86].Id,Groups[87].Id,Groups[88].Id,Groups[89].Id,Groups[90].Id,Groups[91].Id,Groups[92].Id,Groups[93].Id,Groups[94].Id,Groups[95].Id,Groups[96].Id,Groups[97].Id,Groups[98].Id,Groups[99].Id,Groups[0].Name,Groups[1].Name,Groups[2].Name,Groups[3].Name,Groups[4].Name,Groups[5].Name,Groups[6].Name,Groups[7].Name,Groups[8].Name,Groups[9].Name,Groups[10].Name,Groups[11].Name,Groups[12].Name,Groups[13].Name,Groups[14].Name,Groups[15].Name,Groups[16].Name,Groups[17].Name,Groups[18].Name,Groups[19].Name,Groups[20].Name,Groups[21].Name,Groups[22].Name,Groups[23].Name,Groups[24].Name,Groups[25].Name,Groups[26].Name,Groups[27].Name,Groups[28].Name,Groups[29].Name,Groups[30].Name,Groups[31].Name,Groups[32].Name,Groups[33].Name,Groups[34].Name,Groups[35].Name,Groups[36].Name,Groups[37].Name,Groups[38].Name,Groups[39].Name,Groups[40].Name,Groups[41].Name,Groups[42].Name,Groups[43].Name,Groups[44].Name,Groups[45].Name,Groups[46].Name,Groups[47].Name,Groups[48].Name,Groups[49].Name,Groups[50].Name,Groups[51].Name,Groups[52].Name,Groups[53].Name,Groups[54].Name,Groups[55].Name,Groups[56].Name,Groups[57].Name,Groups[58].Name,Groups[59].Name,Groups[60].Name,Groups[61].Name,Groups[62].Name,Groups[63].Name,Groups[64].Name,Groups[65].Name,Groups[66].Name,Groups[67].Name,Groups[68].Name,Groups[69].Name,Groups[70].Name,Groups[71].Name,Groups[72].Name,Groups[73].Name,Groups[74].Name,Groups[75].Name,Groups[76].Name,Groups[77].Name,Groups[78].Name,Groups[79].Name,Groups[80].Name,Groups[81].Name,Groups[82].Name,Groups[83].Name,Groups[84].Name,Groups[85].Name,Groups[86].Name,Groups[87].Name,Groups[88].Name,Groups[89].Name,Groups[90].Name,Groups[91].Name,Groups[92].Name,Groups[93].Name,Groups[94].Name,Groups[95].Name,Groups[96].Name,Groups[97].Name,Groups[98].Name,Groups[99].Name,Settings.Color,Settings.Options,InternalAppSettings.Index,InternalAppSettings.Options", dividedByLines.First());
            Assert.NotEmpty(value);

        }
        [Fact]
        public void TestWithConfiguration()
        {
            var value = s_users.ToCsv(configuration =>
            {
                configuration.ForExcel = true;
                configuration.UseExtendedName = false;
                configuration.ConfigureHeader(x => x.Id, "Identifier");
                configuration.ConfigureHeader(x => x.Groups.First().Name, "GroupName");
                configuration.AvoidProperty(x => x.Password);
                configuration.Delimiter = ";";
            });
            var dividedByLines = value.Split('\n');
            Assert.Equal("\"Identifier\";\"Name\";\"Email\";\"MainGroup\";\"HashedMainGroup\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"Groups\";\"GroupName[0]\";\"GroupName[1]\";\"GroupName[2]\";\"GroupName[3]\";\"GroupName[4]\";\"GroupName[5]\";\"GroupName[6]\";\"GroupName[7]\";\"GroupName[8]\";\"GroupName[9]\";\"GroupName[10]\";\"GroupName[11]\";\"GroupName[12]\";\"GroupName[13]\";\"GroupName[14]\";\"GroupName[15]\";\"GroupName[16]\";\"GroupName[17]\";\"GroupName[18]\";\"GroupName[19]\";\"GroupName[20]\";\"GroupName[21]\";\"GroupName[22]\";\"GroupName[23]\";\"GroupName[24]\";\"GroupName[25]\";\"GroupName[26]\";\"GroupName[27]\";\"GroupName[28]\";\"GroupName[29]\";\"GroupName[30]\";\"GroupName[31]\";\"GroupName[32]\";\"GroupName[33]\";\"GroupName[34]\";\"GroupName[35]\";\"GroupName[36]\";\"GroupName[37]\";\"GroupName[38]\";\"GroupName[39]\";\"GroupName[40]\";\"GroupName[41]\";\"GroupName[42]\";\"GroupName[43]\";\"GroupName[44]\";\"GroupName[45]\";\"GroupName[46]\";\"GroupName[47]\";\"GroupName[48]\";\"GroupName[49]\";\"GroupName[50]\";\"GroupName[51]\";\"GroupName[52]\";\"GroupName[53]\";\"GroupName[54]\";\"GroupName[55]\";\"GroupName[56]\";\"GroupName[57]\";\"GroupName[58]\";\"GroupName[59]\";\"GroupName[60]\";\"GroupName[61]\";\"GroupName[62]\";\"GroupName[63]\";\"GroupName[64]\";\"GroupName[65]\";\"GroupName[66]\";\"GroupName[67]\";\"GroupName[68]\";\"GroupName[69]\";\"GroupName[70]\";\"GroupName[71]\";\"GroupName[72]\";\"GroupName[73]\";\"GroupName[74]\";\"GroupName[75]\";\"GroupName[76]\";\"GroupName[77]\";\"GroupName[78]\";\"GroupName[79]\";\"GroupName[80]\";\"GroupName[81]\";\"GroupName[82]\";\"GroupName[83]\";\"GroupName[84]\";\"GroupName[85]\";\"GroupName[86]\";\"GroupName[87]\";\"GroupName[88]\";\"GroupName[89]\";\"GroupName[90]\";\"GroupName[91]\";\"GroupName[92]\";\"GroupName[93]\";\"GroupName[94]\";\"GroupName[95]\";\"GroupName[96]\";\"GroupName[97]\";\"GroupName[98]\";\"GroupName[99]\";\"Color\";\"Options\";\"Index\";\"Options\"", dividedByLines.First());
            Assert.NotEmpty(value);
        }
    }
}
