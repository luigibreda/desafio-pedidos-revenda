using System;
using System.Threading;
using System.Threading.Tasks;
using BeverageDistributor.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BeverageDistributor.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(cacheKey));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // Tenta obter o item do cache
            if (_cache.TryGetValue(cacheKey, out T cachedItem))
            {
                _logger.LogDebug("Retrieved item from cache with key: {CacheKey}", cacheKey);
                return cachedItem;
            }

            try
            {
                await _semaphore.WaitAsync();

                // Verifica novamente após adquirir o semáforo para evitar race condition
                if (_cache.TryGetValue(cacheKey, out cachedItem))
                {
                    _logger.LogDebug("Retrieved item from cache with key (after semaphore): {CacheKey}", cacheKey);
                    return cachedItem;
                }

                _logger.LogDebug("Item not found in cache. Executing factory for key: {CacheKey}", cacheKey);
                
                // Executa a factory para obter o item
                var item = await factory();

                if (item != null)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSize(1) // Limite de tamanho para cada entrada
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10)) // Expiração por inatividade
                        .SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Expiração absoluta

                    if (expiration.HasValue)
                    {
                        cacheEntryOptions.SetAbsoluteExpiration(expiration.Value);
                    }

                    // Adiciona o item ao cache
                    _cache.Set(cacheKey, item, cacheEntryOptions);
                    _logger.LogDebug("Item added to cache with key: {CacheKey}", cacheKey);
                }
                else
                {
                    _logger.LogWarning("Factory returned null for key: {CacheKey}", cacheKey);
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating cache item with key: {CacheKey}", cacheKey);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Remove(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(cacheKey));

            _cache.Remove(cacheKey);
            _logger.LogDebug("Item removed from cache with key: {CacheKey}", cacheKey);
        }

        public void Clear()
        {
            // IMemoryCache não tem um método Clear nativo, então usamos um prefixo ou implementamos uma solução personalizada
            // Esta é uma implementação simples que remove todos os itens, mas pode não ser adequada para todos os cenários
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // Remove todos os itens
                _logger.LogInformation("Cache cleared");
            }
            else
            {
                _logger.LogWarning("Clear operation is not supported for the current cache implementation");
            }
        }
    }
}
