using System;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;
using System.Collections.Generic;
using DigiBadges.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Microsoft.Extensions.Caching.Distributed
{
    public static class IDistributedCacheExtensions
    {
        public static async Task<IList<SolrUsersModel>> GetSearchResultsAsync(this IDistributedCache cache, string searchId)
        {
            return await cache.GetAsync<IList<SolrUsersModel>>(searchId);
        }

         public static async Task AddSearchResultsAsync(this IDistributedCache cache, string searchId, IList<SolrUsersModel> flights)
        {
            var options = new DistributedCacheEntryOptions();
           
             options.SlidingExpiration = TimeSpan.FromSeconds(120);
             options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(150);
            await cache.SetAsync(searchId, flights, options);
        }

        public static async Task<List<SolrUsersModel>> GetAsync<T>(this IDistributedCache cache, string key) where T : class
        {
            var json = await cache.GetStringAsync(key);
            List<SolrUsersModel> ls = new List<SolrUsersModel>();
            if (json == null)
                return null;
             
            List<SolrUsersModel> results= JsonConvert.DeserializeAnonymousType<List<SolrUsersModel>>(json, ls);
           
            return results;
        }
        public static async Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options) where T : class
        {
            var json = JsonConvert.SerializeObject(value);
            await cache.SetStringAsync(key, json, options);
        }
    }
}