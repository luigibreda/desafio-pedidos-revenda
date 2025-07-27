using System;
using System.Threading.Tasks;

namespace BeverageDistributor.Application.Interfaces
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null);
        void Remove(string cacheKey);
        void Clear();
    }
}
