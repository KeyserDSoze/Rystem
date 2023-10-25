using System.Drawing;
using Rystem.Api.Test.Domain;

namespace Rystem.Api.TestServer.Clients
{
    public sealed class TeamCalculator : ITeamCalculator
    {
        public bool IsLive => true;

        public bool IsEditable => false;

        public int SerieADay => 21;

        public VotedRealPlayerWrapper? Live => new VotedRealPlayerWrapper { Id = 4 };

        public VotedRealPlayerWrapper? Official => new VotedRealPlayerWrapper { Id = 5 };

        public ChancedRealPlayerWrapper? Chance => new() { Id = 6 };

        public LeagueSetting? LeagueSetting => new() { LeagueId = 4 };

        //public ValueTask BuildRequestAsync(int year, int day, LeagueSetting leagueSettings, bool withRealGame, bool withVote, bool withChance)
        //{
        //    return ValueTask.CompletedTask;
        //}

        public Task BuildRequestAsync(int year, int day, LeagueSetting leagueSettings, bool withRealGame, bool withVote, bool withChance)
        {
            return Task.CompletedTask;
        }
        public Task<bool> BuildRequest2Async(int year, int day, LeagueSetting leagueSettings, bool withRealGame, bool withVote, bool withChance, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
        public ValueTask BuildRequestValueAsync(int year, int day, LeagueSetting leagueSettings, bool withRealGame, CancellationToken cancellationToken, bool withVote, bool withChance)
        {
            return ValueTask.CompletedTask;
        }
        public ValueTask<bool> BuildRequestValue2Async(int year, int day, LeagueSetting leagueSettings, bool withRealGame, bool withVote, bool withChance)
        {
            return ValueTask.FromResult(true);
        }
        public Point CalculatePoint(IEnumerable<Player> players)
        {
            return new Point(4);
        }

        public Point CalculatePoint(IEnumerable<EnrichedPlayer> players)
        {
            return new Point(4);
        }

        public EnrichedPlayer Enrich(Player player)
        {
            return new() { Id = 9 };
        }

        public IEnumerable<EnrichedPlayer> Enrich(IEnumerable<Player> players)
        {
            yield return new() { Id = 10 };
            yield return new() { Id = 11 };
        }

        public VoteValue FinalValue(Player player)
        {
            return new() { Id = 56 };
        }

        public VoteValue FinalValue(EnrichedPlayer player)
        {
            return new() { Id = 57 };
        }

        public EnrichedPlayer ForceEnrich(Player player, Vote? vote, Chance? chance, RealGame? realGame)
        {
            return new() { Id = 58 };
        }

        public IEnumerable<EnrichedPlayer> GetRightFormation(IEnumerable<Player> players)
        {
            yield return new() { Id = 12 };
            yield return new() { Id = 13 };
        }

        public async IAsyncEnumerable<EnrichedPlayer> GetRightFormationAsync(IEnumerable<EnrichedPlayer> players)
        {
            await Task.CompletedTask;
            yield return new() { Id = 14 };
            await Task.CompletedTask;
            yield return new() { Id = 15 };
        }

        public void SetLeagueSettings(LeagueSetting leagueSettings)
        {
            var x = leagueSettings;
            return;
        }
    }
}
