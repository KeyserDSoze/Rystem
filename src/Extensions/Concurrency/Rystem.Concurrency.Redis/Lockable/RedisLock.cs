using StackExchange.Redis;

namespace System.Threading.Concurrent
{
    public sealed class RedisLock : ILockable
    {
        public RedisLock(IConnectionMultiplexer connectionMultiplexer)
            => _database = connectionMultiplexer.GetDatabase();
        private readonly IDatabase _database;

        public Task<bool> AcquireAsync(string key, TimeSpan? maxWindow = null)
            => _database.StringSetAsync(key, key, maxWindow, When.NotExists);
        public async Task<bool> IsAcquiredAsync(string key)
        {
            var result = await _database.StringGetAsync(key);
            return result.HasValue;
        }
        private static readonly string s_script = @"
                            if redis.call('get', KEYS[1]) == ARGV[1] then
                                return redis.call('del', KEYS[1])
                            else
                                return 0
                            end";
        public async Task<bool> ReleaseAsync(string key)
        {
            _ = await _database.ScriptEvaluateAsync(s_script, [key], [key]);
            return true;
        }
    }
}
