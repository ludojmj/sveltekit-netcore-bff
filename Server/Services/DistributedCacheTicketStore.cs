using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Server.Services;

public class DistributedCacheTicketStore(IMemoryCache memoryCache) : ITicketStore
{
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString();
        var bytes = SerializeToBytes(ticket);
        memoryCache.Set(key, bytes, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20)
        });
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var bytes = SerializeToBytes(ticket);
        memoryCache.Set(key, bytes, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20)
        });
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        return memoryCache.TryGetValue(key, out byte[] bytes)
            ? Task.FromResult(DeserializeFromBytes(bytes))
            : Task.FromResult<AuthenticationTicket>(null);
    }

    public Task RemoveAsync(string key)
    {
        memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    private static byte[] SerializeToBytes(AuthenticationTicket ticket)
    {
        return TicketSerializer.Default.Serialize(ticket);
    }

    private static AuthenticationTicket DeserializeFromBytes(byte[] bytes)
    {
        return TicketSerializer.Default.Deserialize(bytes);
    }
}
