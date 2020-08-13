using System;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;
using System.Collections.Generic;
using DigiBadges.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace DigiBadges.Utility
{
    public static class BackpackCache
    {
        public static async Task<IList<BackPack>> GetSearchResultAsync(this IDistributedCache cache, string searchId)
        {
            return await cache.GetAsync<IList<BackPack>>(searchId);
        }

        public static async Task AddSearchResultsAsync(this IDistributedCache cache, string searchId, IList<BackPack> backPacks,int sec)
        {
            var options = new DistributedCacheEntryOptions();

            options.SlidingExpiration = TimeSpan.FromSeconds(sec);
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(sec);
            await cache.SetAsync(searchId, backPacks, options);
        }

        public static async Task<List<BackPack>> GetAsync<T>(this IDistributedCache cache, string key) where T : class
        {
            var json = await cache.GetStringAsync(key);
            List<BackPack> ls = new List<BackPack>();
            if (json == null)
                return null;

            List<BackPack> results = JsonConvert.DeserializeAnonymousType<List<BackPack>>(json, ls);

            return results;
        }
        public static async Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options) where T : class
        {
            var json = JsonConvert.SerializeObject(value);
            await cache.SetStringAsync(key, json, options);
        }
    }
}