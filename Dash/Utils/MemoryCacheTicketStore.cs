﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Dash
{
    /// <summary>
    /// Store large numbers of claims for authorization.
    /// </summary>
    public class MemoryCacheTicketStore : ITicketStore
    {
        const string _KeyPrefix = "AuthSessionStore-";
        IMemoryCache _Cache;

        public MemoryCacheTicketStore() => _Cache = new MemoryCache(new MemoryCacheOptions());

        public Task RemoveAsync(string key)
        {
            _Cache.Remove(key);
            return Task.FromResult(0);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            var options = new MemoryCacheEntryOptions();
            var expiresUtc = ticket.Properties.ExpiresUtc;
            if (expiresUtc.HasValue)
                options.SetAbsoluteExpiration(expiresUtc.Value);
            options.SetSlidingExpiration(TimeSpan.FromMinutes(15));

            _Cache.Set(key, ticket, options);

            return Task.FromResult(0);
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            _Cache.TryGetValue(key, out AuthenticationTicket ticket);
            return Task.FromResult(ticket);
        }

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var key = _KeyPrefix + Guid.NewGuid().ToString();
            await RenewAsync(key, ticket);
            return key;
        }
    }
}
