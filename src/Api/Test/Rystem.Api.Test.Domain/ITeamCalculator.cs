using System.Drawing;

namespace Rystem.Api.Test.Domain
{
    public interface ITeamCalculator
    {
        bool IsLive { get; }
        bool IsEditable { get; }
        int SerieADay { get; }
        VotedRealPlayerWrapper? Live { get; }
        VotedRealPlayerWrapper? Official { get; }
        ChancedRealPlayerWrapper? Chance { get; }
        LeagueSetting? LeagueSetting { get; }
        Task BuildRequestAsync(int year, int day, LeagueSetting leagueSettings, bool withRealGame, bool withVote, bool withChance);
        Task<bool> BuildRequest2Async(int year, int day, LeagueSetting leagueSettings, bool withRealGame, bool withVote, bool withChance, CancellationToken cancellationToken);
        ValueTask BuildRequestValueAsync(int year, int day, LeagueSetting leagueSettings, bool withRealGame, CancellationToken cancellationToken, bool withVote, bool withChance);
        ValueTask<bool> BuildRequestValue2Async(int year, int day, LeagueSetting leagueSettings, bool withRealGame, bool withVote, bool withChance);
        void SetLeagueSettings(LeagueSetting leagueSettings);
        Point CalculatePoint(IEnumerable<Player> players);
        Point CalculatePoint(IEnumerable<EnrichedPlayer> players);
        EnrichedPlayer Enrich(Player player);
        EnrichedPlayer ForceEnrich(Player player, Vote? vote, Chance? chance, RealGame? realGame);
        IEnumerable<EnrichedPlayer> Enrich(IEnumerable<Player> players);
        IEnumerable<EnrichedPlayer> GetRightFormation(IEnumerable<Player> players);
        IAsyncEnumerable<EnrichedPlayer> GetRightFormationAsync(IEnumerable<EnrichedPlayer> players);
        VoteValue FinalValue(Player player);
        VoteValue FinalValue(EnrichedPlayer player);
    }
    public sealed class VotedRealPlayerWrapper
    {
        public int Id { get; set; }
    }
    public sealed class ChancedRealPlayerWrapper
    {
        public int Id { get; set; }
    }
    public sealed class LeagueSetting
    {
        public int? LeagueId { get; set; }
    }
    public sealed class Player
    {
        public int Id { get; set; }
    }
    public sealed class EnrichedPlayer
    {
        public int Id { get; set; }
    }
    public sealed class VoteValue
    {
        public int Id { get; set; }
    }
    public sealed class Vote
    {
        public int Id { get; set; }
    }
    public sealed class Chance
    {
        public int Id { get; set; }
    }
    public sealed class RealGame
    {
        public int Id { get; set; }
    }
}
