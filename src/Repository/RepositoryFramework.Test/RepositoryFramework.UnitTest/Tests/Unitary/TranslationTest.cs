using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RepositoryFramework.UnitTest.Unitary
{
    public record Translatable(int Id, int CcnlId, DateTimeOffset From, DateTimeOffset? To,
            int NumberOfMonths,
            int AdditionalHolidays,
            decimal? HolidaysFraction,
            bool IsEasterPaid,
            bool IsHolidayOnlyOnSecondRestDay,
            int RenewalType,
            int ConsolidateState,
            bool Active,
            DateTimeOffset CreationDate,
            DateTimeOffset LastChangeDate,
            string? CreatedBy,
            string? ChangedBy,
            string? Foolish = null
            )
    {
        public string Currency { get; set; } = "€";
    };
    public sealed class ToTranslateSomething
    {
        public int Idccnl { get; set; }
        public string Folle { get; set; } = null!;
        public int IdccnlValidita { get; set; }
        public DateTime DataInizio { get; set; }
        public DateTime? DataFine { get; set; }
        public int NumeroMensilita { get; set; }
        public int NumeroGiorniFestivita { get; set; }
        public decimal FrazioneFestivita { get; set; }
        public bool DomenicaPasquaRetribuita { get; set; }
        public bool SoloSecondoGiorno { get; set; }
        public byte IdtipoRinnovo { get; set; }
        public byte Stato { get; set; }
        public bool? Attivo { get; set; }
        public DateTime? DataCreazione { get; set; }
        public DateTime? DataModifica { get; set; }
        public string? UtenteCreazione { get; set; }
        public string? UtenteModifica { get; set; }
    }
    public sealed class ToTranslateSomethingElse
    {
        public int Idccnl { get; set; }
        public int IdccnlValidita { get; set; }
        public string Folle { get; set; } = null!;
        public DateTime DataInizio { get; set; }
        public DateTime? DataFine { get; set; }
        public int NumeroMensilita { get; set; }
        public int NumeroGiorniFestivita { get; set; }
        public decimal FrazioneFestivita { get; set; }
        public bool DomenicaPasquaRetribuita { get; set; }
        public bool SoloSecondoGiorno { get; set; }
        public byte IdtipoRinnovo { get; set; }
        public byte Stato { get; set; }
        public bool? Attivo { get; set; }
        public DateTime? DataCreazione { get; set; }
        public DateTime? DataModifica { get; set; }
        public string? UtenteCreazione { get; set; }
        public string? UtenteModifica { get; set; }
    }
    public class TranslatableRepository : IRepository<Translatable, string>
    {
        private readonly List<ToTranslateSomething> _toTranslateSomething = new();
        private readonly List<ToTranslateSomethingElse> _toTranslateSomethingElse = new();
        public Task<BatchResults<Translatable, string>> BatchAsync(BatchOperations<Translatable, string> operations, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<State<Translatable, string>> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<State<Translatable, string>> ExistAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Translatable?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<State<Translatable, string>> InsertAsync(string key, Translatable value, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            _toTranslateSomething.Add(new ToTranslateSomething
            {
                Attivo = true,
                Idccnl = int.Parse(key),
                DataCreazione = DateTime.UtcNow,
                DataFine = DateTime.UtcNow,
                DataInizio = DateTime.UtcNow,
                Folle = "fool"
            });
            _toTranslateSomethingElse.Add(new ToTranslateSomethingElse
            {
                Attivo = true,
                Idccnl = int.Parse(key),
                DataCreazione = DateTime.UtcNow,
                DataFine = DateTime.UtcNow,
                DataInizio = DateTime.UtcNow,
                Folle = "fool"
            });
            return true;
        }

        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<Entity<Translatable, string>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken);

            foreach (var validitum in filter.Apply(_toTranslateSomething))
                yield return Entity.Default(
                    new Translatable(validitum.IdccnlValidita,
                    validitum.Idccnl,
                    validitum.DataInizio,
                    validitum.DataFine,
                    validitum.NumeroMensilita,
                    validitum.NumeroGiorniFestivita,
                    validitum.FrazioneFestivita,
                    validitum.DomenicaPasquaRetribuita, validitum.SoloSecondoGiorno,
                    validitum.IdtipoRinnovo,
                    validitum.Stato,
                    validitum.Attivo ?? false,
                    validitum.DataCreazione ?? DateTime.UtcNow,
                    validitum.DataModifica ?? DateTime.UtcNow,
                    validitum.UtenteCreazione,
                    validitum.UtenteModifica, "fools"),
                    Guid.NewGuid().ToString());

            foreach (var validitum in filter.Apply(_toTranslateSomethingElse))
                yield return Entity.Default(
                    new Translatable(validitum.IdccnlValidita,
                    validitum.Idccnl,
                    validitum.DataInizio,
                    validitum.DataFine,
                    validitum.NumeroMensilita,
                    validitum.NumeroGiorniFestivita,
                    validitum.FrazioneFestivita,
                    validitum.DomenicaPasquaRetribuita, validitum.SoloSecondoGiorno,
                    validitum.IdtipoRinnovo,
                    validitum.Stato,
                    validitum.Attivo ?? false,
                    validitum.DataCreazione ?? DateTime.UtcNow,
                    validitum.DataModifica ?? DateTime.UtcNow,
                    validitum.UtenteCreazione,
                    validitum.UtenteModifica)
                    { Foolish = "fools" },
                    Guid.NewGuid().ToString());
        }

        public Task<State<Translatable, string>> UpdateAsync(string key, Translatable value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
    public class TranslationTest
    {
        private static readonly IServiceProvider? s_serviceProvider;
        static TranslationTest()
        {
            DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration)
                    .AddRepository<Translatable, string, TranslatableRepository>(settings =>
                    {
                        settings
                        .Translate<ToTranslateSomething>()
                           .With(x => x.Foolish, x => x.Folle)
                           .With(x => x.Id, x => x.IdccnlValidita)
                           .With(x => x.CcnlId, x => x.Idccnl)
                           .With(x => x.From, x => x.DataInizio)
                           .With(x => x.To, x => x.DataFine)
                           .With(x => x.NumberOfMonths, x => x.NumeroMensilita)
                           .With(x => x.AdditionalHolidays, x => x.NumeroGiorniFestivita)
                           .With(x => x.HolidaysFraction, x => x.FrazioneFestivita)
                           .With(x => x.IsEasterPaid, x => x.DomenicaPasquaRetribuita)
                           .With(x => x.IsHolidayOnlyOnSecondRestDay, x => x.SoloSecondoGiorno)
                           .With(x => x.RenewalType, x => x.IdtipoRinnovo)
                           .With(x => x.ConsolidateState, x => x.Stato)
                           .With(x => x.Active, x => x.Attivo)
                        .AndTranslate<ToTranslateSomethingElse>()
                            .With(x => x.Foolish, x => x.Folle)
                            .With(x => x.Id, x => x.IdccnlValidita)
                            .With(x => x.CcnlId, x => x.Idccnl)
                            .With(x => x.From, x => x.DataInizio)
                            .With(x => x.To, x => x.DataFine)
                            .With(x => x.NumberOfMonths, x => x.NumeroMensilita)
                            .With(x => x.AdditionalHolidays, x => x.NumeroGiorniFestivita)
                            .With(x => x.HolidaysFraction, x => x.FrazioneFestivita)
                            .With(x => x.IsEasterPaid, x => x.DomenicaPasquaRetribuita)
                            .With(x => x.IsHolidayOnlyOnSecondRestDay, x => x.SoloSecondoGiorno)
                            .With(x => x.RenewalType, x => x.IdtipoRinnovo)
                            .With(x => x.ConsolidateState, x => x.Stato)
                            .With(x => x.Active, x => x.Attivo);
                    })
                .Finalize(out s_serviceProvider)
                .WarmUpAsync()
                .ToResult();
        }
        private readonly IRepository<Translatable, string> _repository;

        public TranslationTest()
        {
            _repository = s_serviceProvider!.GetService<IRepository<Translatable, string>>()!;
        }
        private const string FoolishName = "FOOL";
        [Fact]
        public async Task TestAsync()
        {
            for (var i = 1; i <= 10; i++)
                _ = await _repository.InsertAsync(i.ToString(), default!);
            var items = await _repository.Where(x => x.CcnlId == 4 && x.Active).ToListAsync();
            Assert.Equal(2, items.Count);
            items = await _repository.Where(x => x.CcnlId > 4 && x.Active).ToListAsync();
            Assert.Equal(12, items.Count);
            items = await _repository
                .Where(x => x.Foolish.ToLower().Contains(RepositoryFramework.UnitTest.Unitary.TranslationTest.FoolishName.ToLower()))
                .ToListAsync().NoContext();
            Assert.Equal(20, items.Count);
        }
    }
}
